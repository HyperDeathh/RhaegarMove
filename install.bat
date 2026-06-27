@echo off
setlocal
cd /d "%~dp0"

net session >nul 2>&1
if errorlevel 1 (
  echo [RhaegarMove] Yonetici izni gerekiyor. UAC ile yeniden baslatiliyor...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  pause
  exit /b
)

if not exist "dist\RhaegarMove.exe" (
  set "RHAEGAR_NO_PAUSE=1"
  call verify_build.bat
  if errorlevel 1 (
    echo [RhaegarMove] Build failed. Install stopped.
    pause
    exit /b 1
  )
)

set "APPDIR=%ProgramFiles%\RhaegarMove"
if not exist "%APPDIR%" mkdir "%APPDIR%"

taskkill /IM RhaegarMove.exe /F >nul 2>&1
copy /Y "dist\RhaegarMove.exe" "%APPDIR%\RhaegarMove.exe" >nul
copy /Y "RhaegarMove.ini" "%APPDIR%\RhaegarMove.ini" >nul

schtasks /Create /TN "RhaegarMove" /TR "\"%APPDIR%\RhaegarMove.exe\"" /SC ONLOGON /RL HIGHEST /F >nul
start "" "%APPDIR%\RhaegarMove.exe"
timeout /t 1 /nobreak >nul

"%APPDIR%\RhaegarMove.exe" --status
if exist "%LOCALAPPDATA%\RhaegarMove\runtime.txt" type "%LOCALAPPDATA%\RhaegarMove\runtime.txt"

echo.
echo [RhaegarMove] Kuruldu ve baslatildi.
pause
endlocal
