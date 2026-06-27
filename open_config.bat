@echo off
setlocal
cd /d "%~dp0"

set "LOCAL_CONFIG=%~dp0RhaegarMove.ini"
set "INSTALLED_CONFIG=%ProgramFiles%\RhaegarMove\RhaegarMove.ini"

if exist "%LOCAL_CONFIG%" (
  start "" notepad "%LOCAL_CONFIG%"
  exit /b 0
)

if exist "%INSTALLED_CONFIG%" (
  start "" notepad "%INSTALLED_CONFIG%"
  exit /b 0
)

echo [RhaegarMove] Config file not found.
endlocal
