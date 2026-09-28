@echo off
setlocal

cd /d "%~dp0"

set "PROJECT_ROOT=%CD%"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Apply_StructureCursor_Fix.ps1" -ProjectRoot "%PROJECT_ROOT%"

echo.
pause
