@echo off
setlocal
set "REPORT=%LOCALAPPDATA%\RhaegarMove\minmax.txt"
if not exist "%REPORT%" (
  echo Min/max diagnostics report not found yet.
  echo EnableSnapDiagnostics=true and perform a resize to generate it.
  exit /b 0
)
type "%REPORT%"
endlocal
