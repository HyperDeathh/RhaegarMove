@echo off
setlocal
set "INNER_NO_PAUSE=%RHAEGAR_NO_PAUSE%"
set "RHAEGAR_NO_PAUSE=1"
call build.bat
if errorlevel 1 (
  echo Build failed.
  if not "%INNER_NO_PAUSE%"=="1" if /i not "%CI%"=="true" pause
  exit /b 1
)
if not exist "dist\RhaegarMove.exe" (
  echo Build completed but dist\RhaegarMove.exe was not found.
  if not "%INNER_NO_PAUSE%"=="1" if /i not "%CI%"=="true" pause
  exit /b 1
)
echo Build verified: dist\RhaegarMove.exe exists.
if not "%INNER_NO_PAUSE%"=="1" if /i not "%CI%"=="true" pause
endlocal
