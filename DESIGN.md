# Clean-room implementation notes

This file records the design decisions for RhaegarMove without copying source code from AltSnap.

## Scope for v0.1

- Move windows with `Alt + left mouse drag`.
- Resize windows with `Alt + right mouse drag`.
- Do not use a global keyboard hook in v0.1.
- Do not synthesize keyboard input.
- Do not block unrelated input.
- Do not add tray UI yet.

## Why v0.1 avoids a keyboard hook

A global keyboard hook is easy to get wrong. If Alt key-up or injected-key state is mishandled, Windows may feel like Alt is stuck. v0.1 therefore uses a more conservative model:

- Install only a low-level mouse hook.
- On mouse events, check whether Alt is physically down.
- Swallow only the mouse down/move/up events that belong to an active RhaegarMove gesture.
- If Alt is released during a gesture, finish the operation and swallow the matching mouse-up once.

## Lessons taken from studying AltSnap's architecture

These are high-level lessons only, not copied code:

- Use a clear state machine for input gestures.
- Keep hook callbacks small and exception-safe.
- Notify target windows with move/size start and end messages.
- Send sizing messages during resize so apps can constrain their own size.
- Keep restore/maximized behavior conservative.
- Add watchdog cleanup so a lost input event does not leave the app in a bad state.
- Coalesce mouse-move work outside of the hook callback.
- Store snapped-window restore metadata so dragging a snapped window can restore to a sensible size.

## Current source layout

The source is now modular:

- `RhaegarMove.cs`: app entry, single-instance guard, watchdog lifecycle.
- `NativeMethods.cs`: Win32 constants, structs, delegates, and P/Invoke declarations.
- `MouseHook.cs`: low-level mouse hook coordinator.
- `OperationWorker.cs`: coalesces mouse-move operations outside the hook callback.
- `WindowController.cs`: target validation, fullscreen/maximized checks, sizing notifications.
- `WindowRules.cs`: clean-room process/class/title blacklist matching.
- `Geometry.cs`: DWM bounds, monitor work area, input state helpers.
- `ResizeEngine.cs`: resize-region selection and rectangle calculation.
- `SnapEngine.cs`: monitor snapping, Aero-style snapping, and window-edge snapping.
- `WindowRestoreStore.cs`: SetProp/GetProp restore metadata with an in-process fallback dictionary.
- `AppSettings.cs`: typed INI option loader.

`build.bat` now compiles `src\*.cs` directly. It no longer depends on generated source preparation.

## Additional AltSnap study notes

AltSnap is not just an Alt-drag loop. The repository separates responsibilities across multiple layers:

- `altsnap.c`: app lifetime, config path resolution, single-instance behavior, tray commands, hook DLL loading, update/reload messages.
- `hooks.h`: shared constants, app names, private messages, action IDs, action metadata, and hotkey helpers.
- `hooks.c`: input state machine, low-level keyboard and mouse processing, movement and resize state, snapping, worker-thread dispatch, blacklist checks, and action execution.
- `snap.c`: restore metadata for snapped/maximized windows using window properties, plus fallback storage for special windows.
- `zones.c`: user-defined snap layouts, grid zones, nearest-zone logic, preview window behavior, and layout switching.
- `tray.c`: notification icon lifecycle, explorer/taskbar recovery, context menu, and zone layout commands.
- `config.c`: settings UI, autostart, elevation, blacklist editor, and option persistence.
- `unfuck.h`: dynamic Windows API compatibility wrappers, monitor/DWM/DPI fallbacks, logging, and old OS support helpers.
- `AltSnap.dni`: default behavior matrix. Many quality decisions live in configuration, not only in code.

Important features RhaegarMove does not have yet:

- Full keyboard hook state machine.
- `ignorekey` / `ignoreclick` style sent-input guards.
- `blockaltup` / end-key behavior.
- Advanced `process:title|class` blacklist format.
- DPI-aware restore scaling.
- Zone layout support.
- MDI window support.
- Transparent outline or hollow-drag mode.
- Tray/config UI.
- Runtime config reload command.

## Roadmap toward AltSnap-level quality

### Phase 1: stable minimal core

Status: in progress.

- Build workflow exists.
- `stop.bat` and `uninstall.bat` exist.
- Manual safety test plan exists.

### Phase 2: window targeting and geometry correctness

Status: in progress.

- Basic process/class/title blacklist exists.
- DWM extended-frame bounds are used by the geometry layer.
- Per-monitor work-area handling is used by snapping helpers.

Still missing:

- More complete `process:title|class` matching.

### Phase 3: snapping

Status: in progress.

- Monitor-edge snap exists.
- Left/right/top/corner Aero-style snap exists.
- `EnableAeroSnap`, `AeroThreshold`, `AeroMaxSpeed`, and `AeroSpeedTau` config options exist.
- Snap-to-other-windows exists for move and resize basics.

Still missing:

- Smart snapped-window dimension sharing.
- More exact speed smoothing.

### Phase 4: resize quality

Status: in progress.

- Side/center resize regions exist.
- `ResizeCenterMode`, `CenterFraction`, and `SidesFraction` config options exist.
- Symmetric center resize exists.
- Resize still sends sizing messages during resize.

Still missing:

- More complete min/max sizing behavior.
- Per-app resize allow/deny lists.

### Phase 5: optional advanced input

Status: planned and constrained.

- Keyboard hook is intentionally not added yet.
- Phase 5 input safety document exists.
- Future keyboard work requires a documented external stop path and Alt-up reset plan.

### Phase 6: UX layer

Status: started.

- `status.bat` exists.
- `open_config.bat` exists.
- GitHub Actions artifact includes helper scripts.

Still missing:

- Tray UI.
- Config UI.
- Runtime logging.
- Runtime reload command.

### Phase 7: source cleanup

Status: done for the active build.

- `src/RhaegarMove.cs` is now a small app entry file.
- Main logic has been split into focused source files.
- `build.bat` compiles `src\*.cs` directly.
- `tools/Prepare-Source.ps1` is no longer called by the build.

Note: the retired script file may still exist in the repository if GitHub content deletion is blocked, but it is not part of the build path.

### Phase 8: operation worker and state machine

Status: started.

- Mouse hook now delegates movement to `OperationWorker`.
- Mouse move events are coalesced through a worker thread.
- The hook callback no longer performs the heavy move/resize work directly.
- Watchdog cleanup still exists.

Still missing:

- More detailed state telemetry.
- Explicit cancel hotkey, intentionally deferred because there is no keyboard hook yet.

### Phase 9: restore metadata and window snap

Status: started.

- `WindowRestoreStore` stores snapped-window restore size and flags with `SetProp/GetProp` plus fallback storage.
- Aero snap writes restore metadata before snapping.
- Dragging a snapped/maximized window uses restore metadata when available.
- `SnapEngine` now collects other top-level windows and supports basic edge-to-window snapping.

Still missing:

- DPI-aware restore scaling.
- FancyZones compatibility.
- Sticky resize of adjacent snapped windows.

## Safety checklist before testing

1. Open Task Manager before running the app.
2. Keep `stop.bat` visible.
3. Test on Notepad first.
4. Do not install as startup until manual run is stable.
5. If mouse or Alt feels wrong, run `stop.bat` immediately.

## Known current status

RhaegarMove is still a clean-room AltSnap-inspired implementation, not an AltSnap source copy. The code now has the correct direction for a maintainable implementation: modular source, a worker boundary, restore metadata, and snap-to-window basics. The next high-value area is more complete blacklist matching and more precise restore/DPI behavior.
