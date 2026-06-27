# Security and input-safety policy

RhaegarMove touches low-level Windows input. That makes conservative design mandatory.

Rules for this repository:

1. Do not copy source code from AltSnap or other GPL projects.
2. Do not add keylogging, telemetry, network calls, persistence tricks, or hidden self-defense behavior.
3. Keep hooks minimal and easy to stop.
4. The app must always pass through unrelated user input.
5. The app must never try to hide from Task Manager.
6. `stop.bat` and `uninstall.bat` must keep working.
7. Any future keyboard hook must have a documented failure plan before it is merged.

Current design preference:

- Use a mouse hook for Alt+mouse gestures.
- Avoid a global keyboard hook unless there is a strong reason.
- Read Alt state only to decide whether an Alt+mouse drag belongs to RhaegarMove.
- Do not synthesize fake Alt/Ctrl keystrokes.
