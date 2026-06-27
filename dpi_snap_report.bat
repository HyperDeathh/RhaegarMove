@echo off
setlocal
set "REPORT=%LOCALAPPDATA%\RhaegarMove\dpi-snap.txt"
if not exist "%REPORT%" (
  echo DPI snap diagnostics report not found yet.
  echo EnableSnapDiagnostics=true and perform a move or resize to generate it.
  exit /b 0
)
type "%REPORT%"
endlocal
