# RhaegarMove

> **Project status: unfinished / abandoned experiment**
>
> This repository is left in a half-finished state. It was an experimental clean-room attempt to build an AltSnap-like Windows Alt+drag utility, but it is **not stable**, **not final**, and **not recommended for daily use**. The current code may build on some machines, but real-world input behavior was not confirmed as reliable.
>
> Development has been stopped unless the project is explicitly resumed later.

RhaegarMove is a small Windows utility experiment for moving and resizing desktop windows with Alt + mouse drag.

Default intended controls:

```text
Alt + Left Mouse Drag  = move window
Alt + Right Mouse Drag = resize window
```

## Status

This project is **unfinished**.

Known situation:

- The project reached a feature-heavy prototype state.
- Build/run behavior was not reliably confirmed on a real Windows setup.
- The app did not successfully work in the first real Notepad drag test.
- Batch/script UX had issues during testing and was only partially cleaned up.
- Low-level input utilities can make mouse/keyboard behavior feel broken if implemented incorrectly.

Do not treat this as a finished AltSnap replacement.

The input model was intentionally conservative:

- Uses a low-level mouse hook.
- Does not use a low-level keyboard hook.
- Does not synthesize Alt/Ctrl keystrokes.
- Does not try to hide from Task Manager.
- Tray icon is disabled by default.

## Files that were intended to matter

For normal use, these were the intended important files:

```text
RhaegarMove.exe      main app, built into dist\
RhaegarMove.ini      config
build.bat            compile locally
verify_build.bat     compile and check dist\RhaegarMove.exe exists
run.bat              run local test build
install.bat          install to C:\Program Files\RhaegarMove and enable startup
uninstall.bat        remove install/startup task
stop.bat             emergency stop
status.bat           quick status check
settings.bat         open settings window without tray
tools.bat            optional debug/tools menu
```

Debug reports were consolidated under `tools.bat`.

## Build

The intended build command was:

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

## Test before installing

If anyone resumes this project, test locally before installing:

```bat
verify_build.bat
run.bat
```

Use Notepad for the first test.

## Install

Installation was intended to be done with:

```bat
install.bat
```

It copies the executable and config file to:

```text
C:\Program Files\RhaegarMove
```

Then it creates a Windows Scheduled Task named `RhaegarMove` that starts on user logon.

Because the project is unfinished, installing it as a startup app is not recommended.

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

## Diagnostics

Use one entry point:

```bat
tools.bat
```

It contains status, reload, graceful exit, cursor diagnostics, config report, snap report, min/max report, and DPI snap report.

## Safety notes

RhaegarMove touches low-level Windows input. Bugs in this kind of software can make input feel broken.

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
