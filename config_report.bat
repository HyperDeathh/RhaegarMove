@echo off
setlocal

set "REPORT=%LOCALAPPDATA%\RhaegarMove\config-report.txt"
if exist "%REPORT%" (
  type "%REPORT%"
) else (
  echo Config report not found.
  echo Run RhaegarMove once, or run reload.bat while it is running.
)

endlocal
