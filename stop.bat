@echo off
taskkill /IM RhaegarMove.exe /F
if errorlevel 1 (
  echo [RhaegarMove] Process not running or could not be stopped.
) else (
  echo [RhaegarMove] Stopped.
)
pause
