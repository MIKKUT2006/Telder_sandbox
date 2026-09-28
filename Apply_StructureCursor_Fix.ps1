param(
    [string]$ProjectRoot = ""
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "TELDER Structure Editor cursor fix v3" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrWhiteSpace($ProjectRoot))
{
    $ProjectRoot = (Get-Location).Path
}

$ProjectRoot = $ProjectRoot.Trim()
$ProjectRoot = $ProjectRoot.Trim('"')

try
{
    $ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
}
catch
{
    Write-Host "ERROR: invalid project root:" -ForegroundColor Red
    Write-Host $ProjectRoot -ForegroundColor Yellow
    exit 1
}

$controller = [System.IO.Path]::Combine(
    $ProjectRoot,
    "Assets",
    "GameData",
    "World",
    "Structures",
    "EditorRuntime",
    "StructureEditorController.cs"
)

Write-Host "Project root:" -ForegroundColor DarkGray
Write-Host $ProjectRoot -ForegroundColor Gray
Write-Host ""
Write-Host "Controller:" -ForegroundColor DarkGray
Write-Host $controller -ForegroundColor Gray
Write-Host ""

if (-not [System.IO.File]::Exists($controller))
{
    Write-Host "ERROR: StructureEditorController.cs not found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected:" -ForegroundColor Yellow
    Write-Host $controller -ForegroundColor Yellow
    exit 1
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false

$text = [System.IO.File]::ReadAllText(
    $controller,
    [System.Text.Encoding]::UTF8
)

$original = $text

$text = $text.Replace(
    "Cursor.visible",
    "global::UnityEngine.Cursor.visible"
)

$text = $text.Replace(
    "Cursor.lockState",
    "global::UnityEngine.Cursor.lockState"
)

while ($text.Contains(
    "global::UnityEngine.global::UnityEngine.Cursor"
))
{
    $text = $text.Replace(
        "global::UnityEngine.global::UnityEngine.Cursor",
        "global::UnityEngine.Cursor"
    )
}

if ($text -eq $original)
{
    Write-Host "Cursor references are already fixed." -ForegroundColor Green
}
else
{
    $backup = $controller + ".cursor_fix_backup"

    if (-not [System.IO.File]::Exists($backup))
    {
        [System.IO.File]::Copy(
            $controller,
            $backup,
            $false
        )
    }

    [System.IO.File]::WriteAllText(
        $controller,
        $text,
        $utf8NoBom
    )

    Write-Host "Cursor references fixed successfully." -ForegroundColor Green
    Write-Host ""
    Write-Host "Backup:" -ForegroundColor DarkGray
    Write-Host $backup -ForegroundColor Gray
}

Write-Host ""
Write-Host "Return to Unity and let it compile." -ForegroundColor Cyan
Write-Host ""
