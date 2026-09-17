$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Assets = Join-Path $ProjectRoot "Assets"

if (-not (Test-Path $Assets)) {
    Write-Host ""
    Write-Host "ERROR: Assets folder was not found next to this script." -ForegroundColor Red
    Write-Host "Extract this archive into the ROOT of the Unity project first."
    Write-Host ""
    Read-Host "Press Enter to close"
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Join-Path $ProjectRoot ("BlockTransform_Backups\repair_" + $timestamp)

$files = Get-ChildItem -Path $Assets -Recurse -Filter *.cs -File
$fixed = 0

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw

    if ($text -notmatch '\[BT-AUTO-STRUCTURE-DATA\]') {
        continue
    }

    $isUnsafe =
        ($text -match '\bstatic\s+class\b') -or
        ($file.BaseName -match 'Runtime|Generation|Generator|Controller|Manager|Scanner|Cache|Registry|Loader')

    if (-not $isUnsafe) {
        continue
    }

    $cleaned = [regex]::Replace(
        $text,
        '(?m)^[ \t]*// \[BT-AUTO-STRUCTURE-DATA\][ \t]*\r?\n^[ \t]*public byte Transform;[ \t]*\r?\n?',
        ''
    )

    if ($cleaned -eq $text) {
        continue
    }

    $relative = $file.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
    $backup = Join-Path $backupRoot $relative
    $backupDir = Split-Path -Parent $backup

    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $backup -Force

    [System.IO.File]::WriteAllText(
        $file.FullName,
        $cleaned,
        (New-Object System.Text.UTF8Encoding($false))
    )

    Write-Host ("FIXED: " + $relative) -ForegroundColor Green
    $fixed++
}

# Overwrite the installer with the v1.3 version already extracted into Assets.
Write-Host ""
if ($fixed -gt 0) {
    Write-Host ("Removed invalid Transform field from " + $fixed + " file(s).") -ForegroundColor Green
    Write-Host ("Backup: " + $backupRoot)
} else {
    Write-Host "No invalid runtime/static Transform patch was found." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Now return to Unity and wait for compilation."
Write-Host "Then run: Tools -> Game -> Apply Block Rotation System"
Write-Host ""
Read-Host "Press Enter to close"
