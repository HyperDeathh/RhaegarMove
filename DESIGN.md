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
- Advanced blacklist format by process, title, and class.
- DPI-aware restore sizing.
- Snap-to-other-windows.
- Smart restore metadata for snapped windows.
- Zone layout support.
- MDI window support.
- Transparent outline or hollow-drag mode.
- Tray/config UI.
- Worker thread for coalescing mouse move messages.

## Roadmap toward AltSnap-level quality

### Phase 1: stable minimal core

Status: in progress.

- Build workflow exists.
- `stop.bat` and `uninstall.bat` exist.
- Manual safety test plan exists.

### Phase 2: window targeting and geometry correctness

Status: in progress.

- Basic process/class/title blacklist exists.
- DWM extended-frame bounds are generated into the build.
- Per-monitor work-area handling is used by snapping helpers.

Still missing:

- Snapped restore metadata.
- More complete `process:title|class` matching.

### Phase 3: snapping

Status: in progress.

- Monitor-edge snap exists.
- Left/right/top/corner Aero-style snap is generated into the build.
- `EnableAeroSnap` and `AeroThreshold` config options exist.

Still missing:

- Snap-to-other-windows.
- Speed threshold to avoid accidental snap.

### Phase 4: resize quality

Status: started.

- Side/center resize region generation exists.
- `ResizeCenterMode`, `CenterFraction`, and `SidesFraction` config options exist.
- Resize still sends sizing messages during resize.

Still missing:

- True symmetric center resize.
- More complete min/max sizing behavior.

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

## Safety checklist before testing

1. Open Task Manager before running the app.
2. Keep `stop.bat` visible.
3. Test on Notepad first.
4. Do not install as startup until manual run is stable.
5. If mouse or Alt feels wrong, run `stop.bat` immediately.

## Known current status

The current source still uses generated-source preparation in `tools/Prepare-Source.ps1`. The goal is to eventually clean `src/RhaegarMove.cs` directly, but generated-source preparation allows smaller, safer iterations while the project is still early.
