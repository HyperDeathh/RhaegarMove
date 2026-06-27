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
if not exist build mkdir build

set "TMP_SRC=build\RhaegarMove.generated.cs"
copy /Y "src\RhaegarMove.cs" "%TMP_SRC%" >nul

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$p='%TMP_SRC%'; $s=[IO.File]::ReadAllText($p); $s=$s.Replace('private readonly Timer watchdog;', 'private readonly System.Windows.Forms.Timer watchdog;'); $s=$s.Replace('watchdog = new Timer();', 'watchdog = new System.Windows.Forms.Timer();'); $s=$s.Replace('private enum Operation { None, Move, Resize }', 'private enum OperationKind { None, Move, Resize }'); $s=$s.Replace('Operation.Move', 'OperationKind.Move'); $s=$s.Replace('Operation.Resize', 'OperationKind.Resize'); $s=$s.Replace('Operation.None', 'OperationKind.None'); $s=$s.Replace('public Operation Operation;', 'public OperationKind Kind;'); $s=$s.Replace('state.Operation', 'state.Kind'); [IO.File]::WriteAllText($p, $s, [Text.UTF8Encoding]::new($false))"
if errorlevel 1 (
  echo [RhaegarMove] Gecici kaynak dosyasi hazirlanamadi.
  exit /b 1
)

echo [RhaegarMove] Derleniyor...
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /out:"dist\RhaegarMove.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "%TMP_SRC%"
if errorlevel 1 (
  echo [RhaegarMove] Derleme basarisiz.
  exit /b 1
)

echo [RhaegarMove] OK: dist\RhaegarMove.exe
endlocal
