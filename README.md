# RhaegarMove

RhaegarMove is a small Windows utility for moving and resizing desktop windows with Alt + mouse drag.

Default controls:

```text
Alt + Left Mouse Drag  = move window
Alt + Right Mouse Drag = resize window
```

## Status

RhaegarMove is a clean-room AltSnap-inspired implementation. It is feature-rich now, but it should still be treated as a cautious build until a real Windows build/test result is confirmed.

The input model is intentionally conservative:

- Uses a low-level mouse hook.
- Does not use a low-level keyboard hook.
- Does not synthesize Alt/Ctrl keystrokes.
- Does not try to hide from Task Manager.
- Tray icon is disabled by default.

## Files you actually need

For normal use, these are the important files:

```text
RhaegarMove.exe      main app, built into dist\
RhaegarMove.ini      config
install.bat          install to C:\Program Files\RhaegarMove and enable startup
uninstall.bat        remove install/startup task
stop.bat             emergency stop
status.bat           quick status check
settings.bat         open settings window without tray
tools.bat            optional debug/tools menu
```

Debug reports are no longer separate `.bat` files. Open `tools.bat` if you need diagnostics.

## Build

Open a normal Command Prompt in the repository folder and run:

```bat
verify_build.bat
```

It runs `build.bat` and verifies this file exists:

```text
dist\RhaegarMove.exe
```

The normal build path uses the .NET Framework C# compiler usually available on Windows:

```text
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

No MSYS2, MinGW, or Visual Studio install is required for the normal build path.

## Test before installing

Before installing as startup, test locally:

```bat
verify_build.bat
run.bat
```

Use Notepad for the first test.

## Install

Run as administrator:

```bat
install.bat
```

It copies the executable and config file to:

```text
C:\Program Files\RhaegarMove
```

Then it creates a Windows Scheduled Task named `RhaegarMove` that starts on user logon.

## Uninstall

Run as administrator:

```bat
uninstall.bat
```

## Settings

Open the settings UI without tray:

```bat
settings.bat
```

Tray icon is disabled by default:

```ini
EnableTrayIcon=false
```

It can be enabled from config/settings if desired, but the default UX is trayless.

## Diagnostics

Use one entry point:

```bat
tools.bat
```

It contains status, reload, graceful exit, cursor diagnostics, config report, snap report, min/max report, and DPI snap report.

## Safety notes

RhaegarMove touches low-level Windows input. Bugs in this kind of software can make input feel broken, so the code is intentionally conservative.

If anything feels wrong, run:

```bat
stop.bat
```

or from any administrator Command Prompt:

```bat
taskkill /IM RhaegarMove.exe /F
schtasks /Delete /TN "RhaegarMove" /F
```

## Clean-room note

This project is written as a clean-room implementation. It must not copy AltSnap source code or remove attribution from third-party code. The project can be inspired by the category of behavior, but implementation remains original.
