@echo off
setlocal
call build.bat
if errorlevel 1 (
  echo Build failed.
  if /i not "%CI%"=="true" pause
  exit /b 1
)
if not exist "dist\RhaegarMove.exe" (
  echo Build completed but dist\RhaegarMove.exe was not found.
  if /i not "%CI%"=="true" pause
  exit /b 1
)
echo Build verified: dist\RhaegarMove.exe exists.
if /i not "%CI%"=="true" pause
endlocal
