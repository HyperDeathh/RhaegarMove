@echo off
setlocal
cd /d "%~dp0"
if not exist "dist\RhaegarMove.exe" (
  call build.bat
  if errorlevel 1 exit /b 1
)
start "" "dist\RhaegarMove.exe"
endlocal
