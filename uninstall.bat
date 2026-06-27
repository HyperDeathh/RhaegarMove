@echo off
setlocal

net session >nul 2>&1
if errorlevel 1 (
  echo [RhaegarMove] Yonetici izni gerekiyor. UAC ile yeniden baslatiliyor...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  pause
  exit /b
)

taskkill /IM RhaegarMove.exe /F >nul 2>&1
schtasks /Delete /TN "RhaegarMove" /F >nul 2>&1

set "APPDIR=%ProgramFiles%\RhaegarMove"
if exist "%APPDIR%" rmdir /S /Q "%APPDIR%"

echo [RhaegarMove] Kaldirildi.
pause
endlocal
