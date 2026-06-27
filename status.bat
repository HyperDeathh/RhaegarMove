@echo off
setlocal

echo [RhaegarMove] Process status:
tasklist /FI "IMAGENAME eq RhaegarMove.exe"

echo.
echo [RhaegarMove] Scheduled task status:
schtasks /Query /TN "RhaegarMove" /FO LIST 2>nul
if errorlevel 1 (
  echo Scheduled task not found.
)

endlocal
