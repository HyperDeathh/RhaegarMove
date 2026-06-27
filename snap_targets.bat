@echo off
setlocal

set "SNAP=%LOCALAPPDATA%\RhaegarMove\snap-targets.txt"
if exist "%SNAP%" (
  type "%SNAP%"
) else (
  echo Snap target diagnostics file not found.
  echo Set EnableSnapDiagnostics=true in RhaegarMove.ini, run reload.bat, then perform a move/resize gesture.
)

endlocal
