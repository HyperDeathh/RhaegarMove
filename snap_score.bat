@echo off
setlocal

set "SCORE=%LOCALAPPDATA%\RhaegarMove\snap-score.txt"
if exist "%SCORE%" (
  type "%SCORE%"
) else (
  echo Snap score diagnostics file not found.
  echo Set EnableSnapDiagnostics=true in RhaegarMove.ini, run reload.bat, then perform a move/resize gesture.
)

endlocal
