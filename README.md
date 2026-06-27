# RhaegarMove

RhaegarMove is a small Windows utility for moving and resizing desktop windows with a modifier key.

Default controls:

- `Alt + Left Mouse Drag` moves the window under the cursor.
- `Alt + Right Mouse Drag` resizes the window under the cursor.

## Current status

This is an early clean-room implementation. It is not AltSnap-level yet.

Current v0.1 intentionally keeps the input model conservative:

- Uses a low-level mouse hook.
- Does not use a low-level keyboard hook yet.
- Checks whether Alt is physically down when mouse events arrive.
- Swallows only mouse events that belong to an active RhaegarMove operation.
- Does not synthesize Alt/Ctrl keystrokes.
- Does not try to hide from Task Manager.

The goal is to approach AltSnap-level quality step by step while keeping the implementation original and legally clean.

## Clean-room note

This project is written as a clean-room implementation. AltSnap is useful as a public reference for understanding the kinds of Windows edge cases a tool like this must consider, but this repository must not copy AltSnap source code.

Design lessons applied here:

- Keep global hooks minimal and fail-safe.
- Always unhook on exit, cancel, or watchdog reset.
- Never do heavy work inside low-level hook callbacks.
- Use an explicit state machine instead of treating Alt as a simple boolean.
- Swallow mouse events only while RhaegarMove owns an active operation.
- Prefer safe cancellation over trying to be clever.

## Build

Open a normal Command Prompt in the repository folder and run:

```bat
build.bat
```

The output will be:

```text
dist\RhaegarMove.exe
```

The build script uses the .NET Framework C# compiler that is normally available on Windows:

```text
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

No MSYS2, MinGW, or Visual Studio install is required for the normal build path.

Note: the current build script compiles from a temporary generated source file under `build\RhaegarMove.generated.cs`. This is temporary and exists only to keep the first public source buildable while the main source is being cleaned up.

## Test first

Before installing as startup, test locally:

```bat
build.bat
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

Then it creates a Windows Scheduled Task named `RhaegarMove` that starts on user logon with highest privileges.

## Uninstall

Run as administrator:

```bat
uninstall.bat
```

## Config

The default config file is `RhaegarMove.ini`.

Important options:

```ini
SnapThreshold=16
MinWidth=120
MinHeight=80
EnableEdgeSnap=true
WatchdogMs=250
```

## Safety notes

RhaegarMove touches low-level Windows input. Bugs in this kind of software can make input feel broken, so the code is intentionally conservative:

- It does not synthesize Alt/Ctrl keystrokes.
- It does not suppress Alt key-up by default.
- It ends operations on Alt release, mouse release, or watchdog timeout.
- It has no persistence tricks beyond the explicit Scheduled Task created by `install.bat`.

If anything feels wrong, run:

```bat
stop.bat
```

or from any administrator Command Prompt:

```bat
taskkill /IM RhaegarMove.exe /F
schtasks /Delete /TN "RhaegarMove" /F
```
