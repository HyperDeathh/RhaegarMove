# RhaegarMove

RhaegarMove is a small Windows utility for moving and resizing desktop windows with a modifier key.

Default controls:

- `Alt + Left Mouse Drag` moves the window under the cursor.
- `Alt + Right Mouse Drag` resizes the window under the cursor.
- `Esc` cancels the current drag/resize state.
- `Ctrl + Alt + Backspace` exits RhaegarMove immediately as an emergency stop.

## Clean-room note

This project is written as a clean-room implementation. AltSnap is useful as a public reference for understanding the kinds of Windows edge cases a tool like this must consider, but this repository must not copy AltSnap source code.

Design lessons applied here:

- Keep global hooks minimal and fail-safe.
- Install the mouse hook only while the hotkey is active.
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
```

## Safety notes

RhaegarMove uses global keyboard and mouse hooks. Bugs in this kind of software can make input feel broken, so the code is intentionally conservative:

- It does not synthesize Alt/Ctrl keystrokes.
- It does not suppress Alt key-up by default.
- It ends operations on Alt release, mouse release, Escape, or watchdog timeout.
- It keeps the mouse hook active only while needed.

If anything feels wrong, run:

```bat
stop.bat
```

or from any administrator Command Prompt:

```bat
taskkill /IM RhaegarMove.exe /F
schtasks /Delete /TN "RhaegarMove" /F
```
