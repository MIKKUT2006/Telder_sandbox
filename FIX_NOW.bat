@echo off
setlocal
cd /d "%~dp0"

echo ================================================
echo TELDER CYRILLIC FIX v2
echo ================================================
echo.
echo Folder:
echo %CD%
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_CYRILLIC_NOW.ps1" > "%~dp0FIX_LOG.txt" 2>&1
set "ERR=%ERRORLEVEL%"

type "%~dp0FIX_LOG.txt"

echo.
echo ================================================
if "%ERR%"=="0" (
    echo FIX FINISHED SUCCESSFULLY
) else (
    echo FIX FAILED. ERROR CODE: %ERR%
)
echo.
echo Log saved to:
echo %~dp0FIX_LOG.txt
echo ================================================
echo.
pause

exit /b %ERR%
