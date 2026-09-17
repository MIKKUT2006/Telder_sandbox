$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Assets = Join-Path $ProjectRoot "Assets"

if (-not (Test-Path $Assets)) {
    Write-Host ""
    Write-Host "ERROR: Assets folder was not found." -ForegroundColor Red
    Write-Host "Extract this archive into the ROOT of D:\Telder_sandbox."
    Write-Host ""
    Read-Host "Press Enter to close"
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Join-Path $ProjectRoot ("HeldItemPoseMenu_Backups\" + $timestamp)
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null

$obsoleteNames = @(
    "HeldItemPoseEditorMenuBridge.cs",
    "HeldItemPosePreviewV30Installer.cs"
)

$removed = 0

foreach ($name in $obsoleteNames) {
    $files = Get-ChildItem -Path $Assets -Recurse -Filter $name -File -ErrorAction SilentlyContinue

    foreach ($file in $files) {
        $relative = $file.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
        $backup = Join-Path $backupRoot $relative
        $backupDir = Split-Path -Parent $backup

        New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $backup -Force

        Remove-Item -LiteralPath $file.FullName -Force

        $meta = $file.FullName + ".meta"
        if (Test-Path $meta) {
            $metaRelative = $meta.Substring($ProjectRoot.Length).TrimStart('\','/')
            $metaBackup = Join-Path $backupRoot $metaRelative
            $metaBackupDir = Split-Path -Parent $metaBackup

            New-Item -ItemType Directory -Force -Path $metaBackupDir | Out-Null
            Copy-Item -LiteralPath $meta -Destination $metaBackup -Force
            Remove-Item -LiteralPath $meta -Force
        }

        Write-Host ("REMOVED: " + $relative) -ForegroundColor Green
        $removed++
    }
}

# Clean up the harmless CS0414 warning in the real editor source.
$editorFiles = Get-ChildItem -Path $Assets -Recurse -Filter "HeldItemPoseEditorWindow.cs" -File -ErrorAction SilentlyContinue

foreach ($file in $editorFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw

    if ($text -notmatch 'class\s+HeldItemPoseEditorWindow') {
        continue
    }

    $original = $text

    # Remove the unused previewAnimator field.
    $text = [regex]::Replace(
        $text,
        '(?ms)\r?\n[ \t]*private Animator previewAnimator;\r?\n',
        "`r`n"
    )

    # Remove simple previewAnimator = null assignments.
    $text = [regex]::Replace(
        $text,
        '(?ms)\r?\n[ \t]*previewAnimator\s*=\s*null;\s*\r?\n',
        "`r`n"
    )

    if ($text -ne $original) {
        $relative = $file.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
        $backup = Join-Path $backupRoot $relative

        if (-not (Test-Path $backup)) {
            $backupDir = Split-Path -Parent $backup
            New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $backup -Force
        }

        [System.IO.File]::WriteAllText(
            $file.FullName,
            $text,
            (New-Object System.Text.UTF8Encoding($false))
        )

        Write-Host ("CLEANED WARNING: " + $relative) -ForegroundColor Green
    }
}

Write-Host ""
Write-Host ("Removed obsolete menu scripts: " + $removed) -ForegroundColor Cyan
Write-Host ("Backup: " + $backupRoot)
Write-Host ""
Write-Host "Return to Unity and wait for compilation."
Write-Host ""
Write-Host "After recompilation the menu should be:"
Write-Host "Tools -> Game -> Held Item Pose Editor"
Write-Host ""
Write-Host "It should be a clickable command, NOT a submenu."
Write-Host ""

Read-Host "Press Enter to close"
