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

## Safety checklist before testing

1. Open Task Manager before running the app.
2. Keep `stop.bat` visible.
3. Test on Notepad first.
4. Do not install as startup until manual run is stable.
5. If mouse or Alt feels wrong, run `stop.bat` immediately.

## Known current status

The first source version is intentionally minimal and should be tested manually on Windows. Further iterations should be committed only after each build/test cycle.
