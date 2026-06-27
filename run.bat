@echo off
setlocal
cd /d "%~dp0"

if not exist "dist\RhaegarMove.exe" (
  call verify_build.bat
  if errorlevel 1 (
    echo.
    echo [RhaegarMove] Build failed. Window will stay open so you can read the error.
    pause
    exit /b 1
  )
)

echo [RhaegarMove] Starting dist\RhaegarMove.exe ...
start "" "dist\RhaegarMove.exe"
timeout /t 1 /nobreak >nul

echo.
echo [RhaegarMove] Runtime status:
"dist\RhaegarMove.exe" --status
if exist "%LOCALAPPDATA%\RhaegarMove\runtime.txt" type "%LOCALAPPDATA%\RhaegarMove\runtime.txt"

echo.
echo If running=false or mouse hook install failed, paste the status above.
echo If running=true, try Alt+Left Mouse Drag on Notepad title/body area.
echo.
pause
endlocal
