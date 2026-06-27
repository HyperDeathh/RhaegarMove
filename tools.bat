@echo off
setlocal EnableExtensions

set "EXE=dist\RhaegarMove.exe"
if not exist "%EXE%" set "EXE=%ProgramFiles%\RhaegarMove\RhaegarMove.exe"
set "DIR=%LOCALAPPDATA%\RhaegarMove"

:menu
cls
echo RhaegarMove Tools
echo.
echo  1. Status
echo  2. Open settings
echo  3. Reload config
echo  4. Request graceful exit
echo  5. Open config file
echo  6. Diagnose window under cursor
echo  7. Show config report
echo  8. Show snap targets report
echo  9. Show snap score report
echo 10. Show native min/max report
echo 11. Show DPI snap report
echo 12. Open diagnostics folder
echo  0. Exit
echo.
set /p choice=Choose: 

if "%choice%"=="1" goto status
if "%choice%"=="2" goto settings
if "%choice%"=="3" goto reload
if "%choice%"=="4" goto exit_request
if "%choice%"=="5" goto config
if "%choice%"=="6" goto cursor
if "%choice%"=="7" goto config_report
if "%choice%"=="8" goto snap_targets
if "%choice%"=="9" goto snap_score
if "%choice%"=="10" goto minmax
if "%choice%"=="11" goto dpi_snap
if "%choice%"=="12" goto folder
if "%choice%"=="0" exit /b 0
goto menu

:need_exe
if not exist "%EXE%" (
  echo RhaegarMove.exe not found. Build first or install the app.
  pause
  goto menu
)
exit /b 0

:status
call :need_exe
"%EXE%" --status
call :type_file "%DIR%\runtime.txt"
pause
goto menu

:settings
call :need_exe
"%EXE%" --settings
goto menu

:reload
call :need_exe
"%EXE%" --reload
call :type_file "%DIR%\runtime.txt"
pause
goto menu

:exit_request
call :need_exe
"%EXE%" --request-exit
call :type_file "%DIR%\runtime.txt"
pause
goto menu

:config
if not exist "RhaegarMove.ini" type nul > "RhaegarMove.ini"
notepad "RhaegarMove.ini"
goto menu

:cursor
call :need_exe
"%EXE%" --diagnose-cursor
call :type_file "%DIR%\runtime.txt"
echo.
echo Rule snapshot:
call :type_file "%DIR%\rules.txt"
pause
goto menu

:config_report
call :type_file "%DIR%\config-report.txt"
pause
goto menu

:snap_targets
call :type_file "%DIR%\snap-targets.txt"
pause
goto menu

:snap_score
call :type_file "%DIR%\snap-score.txt"
pause
goto menu

:minmax
call :type_file "%DIR%\minmax.txt"
pause
goto menu

:dpi_snap
call :type_file "%DIR%\dpi-snap.txt"
pause
goto menu

:folder
if not exist "%DIR%" mkdir "%DIR%"
explorer "%DIR%"
goto menu

:type_file
if exist %1 (
  type %1
) else (
  echo Not generated yet: %~1
)
exit /b 0
