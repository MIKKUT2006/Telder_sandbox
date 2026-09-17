$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Assets = Join-Path $ProjectRoot "Assets"

if (-not (Test-Path $Assets)) {
    Write-Host ""
    Write-Host "ERROR: Assets folder not found." -ForegroundColor Red
    Write-Host "Extract this archive into the ROOT of the Unity project."
    Read-Host "Press Enter to close"
    exit 1
}

$files = Get-ChildItem -Path $Assets -Recurse -Filter "BlockTransformAutoInstaller.cs" -File

if ($files.Count -eq 0) {
    Write-Host "ERROR: BlockTransformAutoInstaller.cs was not found under Assets." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Join-Path $ProjectRoot ("BlockTransform_Backups\cs0103_" + $timestamp)
$fixed = 0

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw

    # Only repair files that actually call IsOurFile but don't declare it.
    if ($text -notmatch '\bIsOurFile\s*\(') {
        continue
    }

    if ($text -match '\bstatic\s+bool\s+IsOurFile\s*\(') {
        Write-Host ("ALREADY OK: " + $file.FullName) -ForegroundColor Yellow
        continue
    }

    $anchor = "        private struct PatchResult"
    $index = $text.IndexOf($anchor)

    if ($index -lt 0) {
        Write-Host ("SKIPPED (anchor not found): " + $file.FullName) -ForegroundColor Red
        continue
    }

    $helper = @"
        private static bool IsOurFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized =
                path.Replace('\\', '/');

            return
                normalized.Contains("/Assets/GameData/BlockTransforms/")
                ||
                normalized.Contains("/Assets/Editor/BlockTransforms/")
                ||
                normalized.Contains("/Assets/GameData/Editor/BlockTransforms/");
        }

"@

    $relative = $file.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
    $backup = Join-Path $backupRoot $relative
    $backupDir = Split-Path -Parent $backup

    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $backup -Force

    $newText = $text.Insert($index, $helper)

    [System.IO.File]::WriteAllText(
        $file.FullName,
        $newText,
        (New-Object System.Text.UTF8Encoding($false))
    )

    Write-Host ("FIXED: " + $relative) -ForegroundColor Green
    $fixed++
}

Write-Host ""
if ($fixed -gt 0) {
    Write-Host ("CS0103 repair complete. Fixed " + $fixed + " installer file(s).") -ForegroundColor Green
    Write-Host ("Backup: " + $backupRoot)
    Write-Host ""
    Write-Host "Return to Unity and wait for compilation."
    Write-Host "Then run: Tools -> Game -> Apply Block Rotation System"
} else {
    Write-Host "No repair was required or the expected installer pattern was not found." -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Press Enter to close"
