# Manual testing plan

RhaegarMove changes low-level mouse behavior, so every feature must be tested in small steps.

## Before every test

1. Open Task Manager.
2. Keep a Command Prompt open in the repository directory.
3. Keep `stop.bat` visible.
4. Do not run `install.bat` until local `run.bat` testing is stable.

Emergency command:

```bat
taskkill /IM RhaegarMove.exe /F
```

## Phase 1 smoke test

Target app: Notepad.

1. Run:

```bat
build.bat
run.bat
```

2. Open Notepad.
3. Hold Alt and left-drag inside the Notepad window.
4. Confirm the window moves.
5. Release the mouse button.
6. Confirm normal left click still works.
7. Hold Alt and right-drag inside the Notepad window.
8. Confirm the window resizes.
9. Release the mouse button.
10. Confirm normal right click still works.
11. Run:

```bat
stop.bat
```

12. Confirm the process exits.

## Phase 2 blacklist and geometry test

Targets that should not move:

- Desktop / Program Manager
- Taskbar
- Start menu
- Alt-Tab / task switcher
- Notification overflow popup

Steps:

1. Run `run.bat`.
2. Try Alt + left drag on the desktop.
3. Try Alt + left drag on the taskbar.
4. Try Alt + left drag around Start menu/search surfaces.
5. Confirm RhaegarMove ignores those targets.
6. Move a normal app near screen edges and confirm the visible frame does not drift because of invisible borders.

## Phase 3 Aero snap test

Target app: Notepad or File Explorer.

1. Run `run.bat`.
2. Hold Alt + left drag the window to the left edge.
3. Confirm it fills the left half of the monitor.
4. Drag to the right edge.
5. Confirm it fills the right half of the monitor.
6. Drag to the top edge.
7. Confirm it fills the monitor work area.
8. Drag to each corner.
9. Confirm quarter-screen snap works.
10. Release Alt/mouse and confirm normal clicks still work.

Config toggles:

```ini
EnableAeroSnap=true
AeroThreshold=8
```

If snap triggers too easily, lower `AeroThreshold`. If it does not trigger, raise it.

## App compatibility pass

Test in this order:

1. Notepad
2. File Explorer
3. Windows Terminal
4. Browser window
5. A normal non-admin app
6. An admin/elevated app only after installing with `install.bat`

## Failure signs

Stop immediately if any of these happen:

- Left click remains swallowed after releasing Alt.
- Right click menus stop opening globally.
- Alt feels stuck.
- You cannot click the taskbar normally.
- The dragged app keeps moving after mouse release.
- Aero snap keeps fighting normal dragging away from edges.

Run:

```bat
stop.bat
```

or:

```bat
taskkill /IM RhaegarMove.exe /F
```

## Notes for future phases

Before adding a keyboard hook, the project must have:

- A reproducible build.
- Manual smoke test pass.
- A documented Alt-up failure plan.
- A documented emergency-exit mechanism that does not rely on the hook itself.
