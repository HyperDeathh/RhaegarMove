@echo off
setlocal
cd /d "%~dp0"

set "EXE=%~dp0dist\RhaegarMove.exe"
if not exist "%EXE%" set "EXE=%ProgramFiles%\RhaegarMove\RhaegarMove.exe"
if not exist "%EXE%" (
  echo RhaegarMove.exe not found.
  exit /b 1
)

"%EXE%" --settings

endlocal
