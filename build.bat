@echo off
setlocal
cd /d "%~dp0"

set "CSC64=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "CSC32=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if exist "%CSC64%" (
  set "CSC=%CSC64%"
) else if exist "%CSC32%" (
  set "CSC=%CSC32%"
) else (
  echo [RhaegarMove] csc.exe bulunamadi.
  echo [RhaegarMove] .NET Framework 4.x compiler Windows'ta normalde hazir gelir.
  exit /b 1
)

if not exist dist mkdir dist

echo [RhaegarMove] Derleniyor...
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /out:"dist\RhaegarMove.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "src\RhaegarMove.cs"
if errorlevel 1 (
  echo [RhaegarMove] Derleme basarisiz.
  exit /b 1
)

echo [RhaegarMove] OK: dist\RhaegarMove.exe
endlocal
