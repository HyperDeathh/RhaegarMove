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
- DWM extended-frame / invisible-border correction.
- DPI-aware restore sizing.
- Full Aero snap and quarter snap.
- Snap-to-other-windows.
- Smart restore metadata for snapped windows.
- Zone layout support.
- MDI window support.
- Transparent outline or hollow-drag mode.
- Tray/config UI.
- Worker thread for coalescing mouse move messages.

## Roadmap toward AltSnap-level quality

### Phase 1: stable minimal core

- Build must succeed from a clean clone.
- Manual run must be safe before installing as startup.
- `stop.bat` and `uninstall.bat` must always work.
- Test with Notepad, Explorer, Terminal, browser windows, and admin windows.

### Phase 2: window targeting and geometry correctness

- Add better target filtering by class and process name.
- Add a blacklist file with process/class/title matching.
- Add DWM extended-frame bounds handling.
- Add maximized and snapped restore metadata.
- Add per-monitor work-area handling.

### Phase 3: snapping

- Add monitor-edge snap.
- Add left/right/top/corner Aero-style snap.
- Add snap-to-other-windows.
- Add speed threshold to avoid accidental snap while moving quickly.

### Phase 4: resize quality

- Add side/center resize regions.
- Add optional center resize mode.
- Respect min/max sizing more carefully.
- Keep sending sizing notifications during resize.

### Phase 5: optional advanced input

- Only consider a keyboard hook after v0.1 is stable.
- If a keyboard hook is added, it must include explicit Alt-up handling, emergency cancel, sent-input ignore counters, and a documented failure plan.
- Avoid synthetic keys unless there is no safer approach.

### Phase 6: UX layer

- Optional tray icon.
- Optional config UI.
- Portable config fallback.
- Clear logs for debugging.

## Safety checklist before testing

1. Open Task Manager before running the app.
2. Keep `stop.bat` visible.
3. Test on Notepad first.
4. Do not install as startup until manual run is stable.
5. If mouse or Alt feels wrong, run `stop.bat` immediately.

## Known current status

The first source version is intentionally minimal and should be tested manually on Windows. Further iterations should be committed only after each build/test cycle.
