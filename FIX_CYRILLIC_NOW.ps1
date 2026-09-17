$ErrorActionPreference = "Stop"

try {
    $ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $Assets = Join-Path $ProjectRoot "Assets"

    if (-not (Test-Path -LiteralPath $Assets)) {
        throw "Assets folder not found next to script. Extract archive into D:\Telder_sandbox."
    }

    # Compatible with Windows PowerShell 5.1.
    $Utf8Bom = New-Object System.Text.UTF8Encoding -ArgumentList $true
    $Utf8Strict = New-Object System.Text.UTF8Encoding -ArgumentList $false, $true
    $Cp1251 = [System.Text.Encoding]::GetEncoding(1251)

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupRoot = Join-Path $ProjectRoot ("EditorEncoding_Backups\" + $timestamp)

    $targetRoots = @(
        (Join-Path $Assets "GameData\Editor"),
        (Join-Path $Assets "GameData\World\Structures")
    )

    $extensions = @(
        ".cs",
        ".json",
        ".txt",
        ".uxml",
        ".uss"
    )

    function Get-MojibakeScore([string]$text) {
        if ([string]::IsNullOrEmpty($text)) {
            return 0
        }

        $score = 0

        for ($i = 0; $i -lt $text.Length; $i++) {
            $c = $text[$i]

            if ($c -eq 'Р' -or $c -eq 'С') {
                $score++
            }
        }

        return $score
    }

    function Repair-MojibakeLine(
        [string]$line,
        [System.Text.Encoding]$cp1251
    ) {
        if ([string]::IsNullOrEmpty($line)) {
            return $line
        }

        $current = $line

        for ($pass = 0; $pass -lt 2; $pass++) {
            $before = Get-MojibakeScore $current

            if ($before -lt 2) {
                break
            }

            try {
                $bytes = $cp1251.GetBytes($current)
                $candidate = [System.Text.Encoding]::UTF8.GetString($bytes)
            }
            catch {
                break
            }

            if ($candidate.IndexOf([char]0xFFFD) -ge 0) {
                break
            }

            $after = Get-MojibakeScore $candidate

            if ($after -ge $before) {
                break
            }

            $current = $candidate
        }

        return $current
    }

    function Repair-Text([string]$text) {
        if ([string]::IsNullOrEmpty($text)) {
            return $text
        }

        $normalized = $text.Replace("`r`n", "`n")
        $lines = $normalized.Split([char]"`n")
        $changed = $false

        for ($i = 0; $i -lt $lines.Length; $i++) {
            $old = $lines[$i]
            $new = Repair-MojibakeLine $old $Cp1251

            if ($new -ne $old) {
                $lines[$i] = $new
                $changed = $true
            }
        }

        if (-not $changed) {
            return $text
        }

        return [string]::Join("`r`n", $lines)
    }

    function Read-SourceText([string]$path) {
        $bytes = [System.IO.File]::ReadAllBytes($path)

        if (
            $bytes.Length -ge 3 -and
            $bytes[0] -eq 0xEF -and
            $bytes[1] -eq 0xBB -and
            $bytes[2] -eq 0xBF
        ) {
            return [System.Text.Encoding]::UTF8.GetString(
                $bytes,
                3,
                $bytes.Length - 3
            )
        }

        if (
            $bytes.Length -ge 2 -and
            $bytes[0] -eq 0xFF -and
            $bytes[1] -eq 0xFE
        ) {
            return [System.Text.Encoding]::Unicode.GetString(
                $bytes,
                2,
                $bytes.Length - 2
            )
        }

        try {
            return $Utf8Strict.GetString($bytes)
        }
        catch {
            return $Cp1251.GetString($bytes)
        }
    }

    function Backup-File([string]$path) {
        $relative = $path.Substring($ProjectRoot.Length)

        while (
            $relative.StartsWith("\") -or
            $relative.StartsWith("/")
        ) {
            $relative = $relative.Substring(1)
        }

        $backup = Join-Path $backupRoot $relative
        $backupDir = Split-Path -Parent $backup

        if (-not (Test-Path -LiteralPath $backupDir)) {
            New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
        }

        Copy-Item -LiteralPath $path -Destination $backup -Force
    }

    $filesSeen = 0
    $rewritten = 0
    $mojibakeFixed = 0

    Write-Host ""
    Write-Host "TELDER EDITOR CYRILLIC REPAIR v2" -ForegroundColor Cyan
    Write-Host ("Project: " + $ProjectRoot)
    Write-Host ""

    foreach ($targetRoot in $targetRoots) {
        if (-not (Test-Path -LiteralPath $targetRoot)) {
            Write-Host ("SKIP: " + $targetRoot) -ForegroundColor Yellow
            continue
        }

        Write-Host ("SCAN: " + $targetRoot) -ForegroundColor Cyan

        $files = Get-ChildItem -LiteralPath $targetRoot -Recurse -File

        foreach ($file in $files) {
            $ext = $file.Extension.ToLowerInvariant()

            if ($extensions -notcontains $ext) {
                continue
            }

            $filesSeen++

            try {
                $originalText = Read-SourceText $file.FullName
            }
            catch {
                Write-Host ("READ FAILED: " + $file.FullName) -ForegroundColor Red
                continue
            }

            $repairedText = Repair-Text $originalText
            $wasMojibake = $repairedText -ne $originalText

            Backup-File $file.FullName

            [System.IO.File]::WriteAllText(
                $file.FullName,
                $repairedText,
                $Utf8Bom
            )

            $rewritten++

            if ($wasMojibake) {
                $mojibakeFixed++
                Write-Host ("REPAIRED: " + $file.FullName) -ForegroundColor Green
            }
            else {
                Write-Host ("UTF8-BOM: " + $file.FullName) -ForegroundColor DarkGray
            }
        }
    }

    Write-Host ""
    Write-Host "SUCCESS" -ForegroundColor Green
    Write-Host ("Files checked: " + $filesSeen)
    Write-Host ("Saved UTF-8 BOM: " + $rewritten)
    Write-Host ("Mojibake repaired: " + $mojibakeFixed)
    Write-Host ("Backup: " + $backupRoot)
    Write-Host ""
    Write-Host "Return to Unity and wait for recompilation."
}
catch {
    Write-Host ""
    Write-Host "ERROR" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    exit 1
}
