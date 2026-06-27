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

A global keyboard hook is easy to get wrong. If Alt key-up or injected-key state is mishandled, Windows may feel like Alt is stuck. v0.1 therefore uses a conservative model:

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
- Keep window rules data-driven so fragile shell/app surfaces can be excluded without code changes.
- Marshal UI preview work back to the WinForms UI thread.
- Runtime control should go through explicit, simple signals rather than hidden behavior.

## Current source layout

The source is modular:

- `RhaegarMove.cs`: app entry, single-instance guard, watchdog lifecycle, runtime command routing, reload/exit control processing.
- `NativeMethods.cs`: Win32 constants, structs, delegates, and P/Invoke declarations.
- `MouseHook.cs`: low-level mouse hook coordinator.
- `OperationWorker.cs`: coalesces mouse-move operations outside the hook callback.
- `WindowController.cs`: target validation, fullscreen/maximized checks, sizing notifications.
- `WindowRules.cs`: clean-room process/class/title and composite window rule matching, with reload support.
- `Geometry.cs`: DWM bounds, monitor work area, input state helpers.
- `DpiHelper.cs`: DPI lookup and restore-size scaling helpers.
- `ResizeEngine.cs`: resize-region selection and rectangle calculation.
- `SizingConstraints.cs`: config-based min/max size clamping.
- `RuleDiagnostics.cs`: rule decision snapshots for debugging.
- `RuntimeCommands.cs`: command-line diagnostics and runtime control helpers.
- `RuntimeControl.cs`: file-based reload and exit request markers.
- `SnapDiagnostics.cs`: snap target accept/reject report writer.
- `SnapPreview.cs`: preview-state snapshots for future outline UI.
- `PreviewOverlay.cs`: optional transparent outline overlay, disabled by default.
- `SnapEngine.cs`: monitor snapping, Aero-style snapping, window-edge snapping, sticky resize basics, and snap target diagnostics.
- `WindowRestoreStore.cs`: SetProp/GetProp restore metadata with an in-process fallback dictionary.
- `AppSettings.cs`: typed INI option loader with in-place reload.

`build.bat` compiles `src\*.cs` directly. It does not depend on generated source preparation.

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

### Phase 5: optional advanced input

Status: planned and constrained.

- Keyboard hook is intentionally not added yet.
- Phase 5 input safety document exists.
- Future keyboard work requires a documented external stop path and Alt-up reset plan.

### Phase 6: UX layer

Status: in progress.

- `status.bat` exists.
- `open_config.bat` exists.
- Runtime and diagnostics helper scripts exist.
- GitHub Actions artifact includes helper scripts.

Still missing:

- Tray UI.
- Config UI.

### Phase 7: source cleanup

Status: done for the active build.

- `src/RhaegarMove.cs` is now a small app entry file.
- Main logic has been split into focused source files.
- `build.bat` compiles `src\*.cs` directly.
- `tools/Prepare-Source.ps1` is no longer called by the build.

Note: the retired script file may still exist in the repository if GitHub content deletion is blocked, but it is not part of the build path.

### Phase 8: operation worker and state machine

Status: in progress.

- Mouse hook delegates movement to `OperationWorker`.
- Mouse move events are coalesced through a worker thread.
- The hook callback no longer performs the heavy move/resize work directly.
- Watchdog cleanup still exists.
- The worker tracks the last rectangle so resize side effects can be calculated safely.

Still missing:

- Explicit cancel hotkey, intentionally deferred because there is no keyboard hook yet.

### Phase 9: restore metadata and window snap

Status: in progress.

- `WindowRestoreStore` stores snapped-window restore size and flags with `SetProp/GetProp` plus fallback storage.
- Aero snap writes restore metadata before snapping.
- Dragging a snapped/maximized window uses restore metadata when available.
- `SnapEngine` collects other top-level windows and supports basic edge-to-window snapping.

Still missing:

- FancyZones compatibility.

### Phase 10: advanced window rules

Status: in progress.

- `WindowRules` supports composite `process:title|class` rules.
- `Rules` can block fragile windows.
- `SnapList` can act as a snap target allow-list.
- `NoSizingNotify` can suppress move/size start/end notifications for matching windows.
- `NoResize` can block right-button resize on matching windows.
- Window rules can be reloaded at runtime.

Still missing:

- Dedicated per-action allow/deny lists for every future action.

### Phase 11: DPI-aware restore

Status: in progress.

- `DpiHelper` reads per-window DPI when available.
- Restore metadata stores the DPI used when the restore size was captured.
- Restore size is scaled when dragging a snapped/maximized window on a monitor with different DPI.

Still missing:

- Monitor-DPI fallback for older Windows versions.
- DPI-aware placement policy for mixed-DPI multi-monitor setups.

### Phase 12: smart snap and sticky resize

Status: in progress.

- `StickyResize` setting exists and is disabled by default.
- `SnapEngine.ApplyStickyResize` can adjust adjacent snap targets when the active window is resized.
- Worker passes previous/current rectangles to the sticky resize path.

Still missing:

- Smarter neighbor selection when multiple windows touch the same edge.
- Protection against tiny adjacent windows being over-shrunk beyond their app-specific constraints.

### Phase 13: min/max sizing

Status: in progress.

- `SizingConstraints` applies config-based min/max clamping after move and resize calculations.
- `MaxWidth` and `MaxHeight` settings exist. A value of `0` means unlimited.
- Oversized rectangles are kept inside monitor work area when they exceed the work area.

Still missing:

- Native app-specific `WM_GETMINMAXINFO` querying. The first interop attempt was blocked by tooling, so this remains a later isolated task.

### Phase 14: rule diagnostics

Status: in progress.

- `RuleDiagnostics` can write a decision snapshot to `%LOCALAPPDATA%\RhaegarMove\rules.txt`.
- `EnableRuleDiagnostics=false` by default.
- Gesture start can write class/title and ignore/snap/resize decisions for the selected window.
- Diagnostics include matched rule explanations such as `Classes:...`, `Rules:...`, `SnapList:...`, `NoResize:...`.
- `diagnose_cursor.bat` can dump the rule decision for the window under the cursor.

Still missing:

- A richer interactive inspector UI.

### Phase 15: preview and outline foundation

Status: in progress.

- `SnapPreview` records the latest move/resize target rectangle to `%LOCALAPPDATA%\RhaegarMove\preview.txt`.
- `EnablePreviewState=false` by default.
- Worker records preview state before applying the move/resize.
- `preview_status.bat` can show the last preview state.

### Phase 16: preview overlay

Status: in progress.

- `PreviewOverlay` implements an optional transparent outline window.
- `EnablePreviewOverlay=false` by default.
- Overlay creation happens on the WinForms UI thread.
- Worker updates are marshaled to the UI thread before changing overlay bounds.

Still missing:

- Preview-only mode that commits snap only on mouse release.
- Custom colors/thickness.

### Phase 17: runtime control

Status: in progress.

- `RuntimeCommands` routes diagnostic commands before starting the main hook app.
- Supported commands: `--status`, `--config-path`, `--diagnose-cursor`, `--preview-status`, `--reload`, and `--request-exit`.
- Runtime command output is persisted to `%LOCALAPPDATA%\RhaegarMove\runtime.txt` so it works with a `winexe` build.
- `status.bat`, `diagnose_cursor.bat`, `preview_status.bat`, `reload.bat`, and `request_exit.bat` read back runtime output.
- Reload and exit requests use simple files in `%LOCALAPPDATA%\RhaegarMove`.

Still missing:

- Named pipe or window-message control channel.

### Phase 18: diagnostics refinement

Status: in progress.

- Rule diagnostics report the rule category and matched pattern when possible.
- Cursor diagnostics helper exists.
- Runtime status points to diagnostic output files.

Still missing:

- Per-action diagnostics for future non-move/resize actions.

### Phase 19: config reload

Status: started.

- `AppSettings` supports in-place reload.
- `WindowRules.Reload()` resets and reloads cached rule lists.
- `RuntimeControl` provides a `reload.request` marker file.
- App loop consumes reload requests, reloads config and rules, and updates watchdog interval.

Still missing:

- Validation report for bad config values.
- Atomic reload outcome summary by section.

### Phase 20: safe exit request

Status: started.

- `RuntimeControl` provides an `exit.request` marker file.
- App loop consumes exit requests and exits through `ApplicationContext.ExitThread()`.
- `request_exit.bat` sends the request without using `taskkill`.
- `stop.bat` still remains as the emergency fallback.

Still missing:

- A stronger authenticated local control channel if needed later.

### Phase 21: snap target diagnostics

Status: started.

- `EnableSnapDiagnostics=false` by default.
- `SnapDiagnostics` writes accepted and rejected snap targets to `%LOCALAPPDATA%\RhaegarMove\snap-targets.txt`.
- Rejection reasons include active window, hidden, minimized, ignored by rule, not in SnapList, noactivate, no caption/thickframe, and empty rect.
- `snap_targets.bat` displays the last snap target report.

Still missing:

- Per-edge snap candidate scoring report.
- Best-candidate explanation for the final snap decision.

## Safety checklist before testing

1. Open Task Manager before running the app.
2. Keep `stop.bat` visible.
3. Test on Notepad first.
4. Do not install as startup until manual run is stable.
5. If mouse or Alt feels wrong, run `stop.bat` immediately.

## Known current status

RhaegarMove is still a clean-room AltSnap-inspired implementation, not an AltSnap source copy. The code is now modular and has the correct direction: worker boundary, restore metadata, snap-to-window basics, advanced rules, DPI-aware restore, sticky resize infrastructure, config-based min/max sizing, runtime diagnostics, optional UI-thread-safe preview overlay, config reload, safe exit request, and snap target diagnostics. The next high-value area is a stronger control channel, config validation reporting, and per-edge snap scoring diagnostics.
