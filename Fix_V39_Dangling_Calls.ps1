param(
    [string]$ProjectRoot = "D:\Telder_sandbox"
)

$editorDir = Join-Path $ProjectRoot "Assets\GameData\Editor"
$mainFile = Join-Path $editorDir "HeldItemPoseEditorWindow.cs"

if (!(Test-Path $mainFile)) {
    Write-Host "ERROR: not found: $mainFile" -ForegroundColor Red
    exit 1
}

$backup = "$mainFile.before_v39_cleanup.bak"

if (!(Test-Path $backup)) {
    Copy-Item $mainFile $backup
    Write-Host "Backup: $backup" -ForegroundColor Cyan
}

$text = Get-Content $mainFile -Raw

$calls = @(
    "V39DrawWeaponPanel();",
    "V39WeaponPanelOnItemLoaded();",
    "V39SaveWeaponToJson();",
    "V39ApplyWeaponPreview();",
    "V39CleanupWeaponPreview();"
)

$removed = 0

foreach ($call in $calls) {
    $escaped = [Regex]::Escape($call)

    $before = $text

    # Remove the whole line containing an injected V39 call.
    $text = [Regex]::Replace(
        $text,
        "(?m)^[\t ]*" + $escaped + "[\t ]*\r?\n?",
        ""
    )

    if ($text -ne $before) {
        Write-Host "Removed: $call" -ForegroundColor Green
        $removed++
    }
}

# "partial" is legal even with one file, so it does not need to be changed.
# We intentionally leave the class declaration alone.

Set-Content `
    -Path $mainFile `
    -Value $text `
    -Encoding UTF8

# Remove obsolete generated V39 extension/installer so they cannot be re-imported.
$obsolete = @(
    (Join-Path $editorDir "HeldItemPoseEditorWindow.Weapons.cs"),
    (Join-Path $editorDir "HeldItemPoseEditorWindow.Weapons.template.txt"),
    (Join-Path $editorDir "HeldItemPoseWeaponEditorInstaller.cs")
)

foreach ($file in $obsolete) {
    if (Test-Path $file) {
        Remove-Item $file -Force
        Write-Host "Deleted obsolete file: $file" -ForegroundColor Yellow
    }

    $meta = "$file.meta"

    if (Test-Path $meta) {
        Remove-Item $meta -Force
    }
}

Write-Host ""
Write-Host "DONE. Removed V39 hook groups: $removed" -ForegroundColor Green
Write-Host "Return to Unity and let scripts recompile." -ForegroundColor White
Write-Host ""
Write-Host "If Unity reports another V39... error, search HeldItemPoseEditorWindow.cs for 'V39' and remove the remaining injected line." -ForegroundColor White

Read-Host "Press Enter to close"
