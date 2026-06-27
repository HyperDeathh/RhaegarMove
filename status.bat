@echo off
setlocal
cd /d "%~dp0"

set "EXE=%~dp0dist\RhaegarMove.exe"
if not exist "%EXE%" set "EXE=%ProgramFiles%\RhaegarMove\RhaegarMove.exe"
if exist "%EXE%" "%EXE%" --status

echo [RhaegarMove] Process status:
tasklist /FI "IMAGENAME eq RhaegarMove.exe"

echo.
echo [RhaegarMove] Scheduled task status:
schtasks /Query /TN "RhaegarMove" /FO LIST 2>nul
if errorlevel 1 echo Scheduled task not found.

echo.
echo [RhaegarMove] Runtime file:
set "RUNTIME=%LOCALAPPDATA%\RhaegarMove\runtime.txt"
if exist "%RUNTIME%" type "%RUNTIME%" else echo Runtime file not found.

echo.
pause
endlocal
