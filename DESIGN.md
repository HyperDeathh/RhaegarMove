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

## Current source layout

The source is modular:

- `RhaegarMove.cs`: app entry, single-instance guard, watchdog lifecycle, runtime command routing, reload/exit control processing.
- `NativeMethods.cs`: Win32 constants, structs, delegates, and P/Invoke declarations.
- `MouseHook.cs`: low-level mouse hook coordinator.
- `OperationWorker.cs`: coalesces mouse-move operations outside the hook callback and supports preview-only commit mode.
- `WindowController.cs`: target validation, fullscreen/maximized checks, sizing notifications.
- `WindowRules.cs`: clean-room process/class/title and composite window rule matching, with reload support.
- `Geometry.cs`: DWM bounds, monitor work area, input state helpers.
- `DpiHelper.cs`: DPI lookup and restore-size scaling helpers.
- `ResizeEngine.cs`: resize-region selection and rectangle calculation.
- `SizingConstraints.cs`: config-based min/max size clamping.
- `ConfigValidation.cs`: startup/reload config validation, unknown-key, and normalization report.
- `RuleDiagnostics.cs`: rule decision snapshots for debugging.
- `RuntimeCommands.cs`: command-line diagnostics and runtime control helpers.
- `RuntimeControl.cs`: file-based reload and exit request markers.
- `RuntimeWatcher.cs`: file watcher for runtime request markers.
- `SnapDiagnostics.cs`: snap target accept/reject report writer.
- `SnapScoreDiagnostics.cs`: per-edge candidate scoring report writer.
- `SnapPreview.cs`: preview-state snapshots for future outline UI.
- `PreviewOverlay.cs`: optional transparent outline overlay, disabled by default.
- `SnapEngine.cs`: monitor snapping, Aero-style snapping, window-edge snapping, sticky resize basics, snap target diagnostics, and per-edge scoring diagnostics.
- `WindowRestoreStore.cs`: SetProp/GetProp restore metadata with an in-process fallback dictionary.
- `AppSettings.cs`: typed INI option loader with in-place reload and config issue tracking.

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

### Phase 13: min/max sizing

Status: in progress.

- `SizingConstraints` applies config-based min/max clamping after move and resize calculations.
- `MaxWidth` and `MaxHeight` settings exist. A value of `0` means unlimited.
- Oversized rectangles are kept inside monitor work area when they exceed the work area.

Still missing:

- Native app-specific `WM_GETMINMAXINFO` querying.

### Phase 14: rule diagnostics

Status: in progress.

- `RuleDiagnostics` can write a decision snapshot to `%LOCALAPPDATA%\RhaegarMove\rules.txt`.
- `EnableRuleDiagnostics=false` by default.
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

- Custom colors/thickness.

### Phase 17: runtime control

Status: in progress.

- `RuntimeCommands` routes diagnostic commands before starting the main hook app.
- Supported commands: `--status`, `--config-path`, `--diagnose-cursor`, `--preview-status`, `--reload`, and `--request-exit`.
- Runtime command output is persisted to `%LOCALAPPDATA%\RhaegarMove\runtime.txt` so it works with a `winexe` build.
- Runtime helper scripts read back runtime output.
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

Status: in progress.

- `AppSettings` supports in-place reload.
- `WindowRules.Reload()` resets and reloads cached rule lists.
- `RuntimeControl` provides a `reload.request` marker file.
- App loop consumes reload requests, reloads config and rules, and updates watchdog interval.

Still missing:

- Atomic reload outcome summary by section.

### Phase 20: safe exit request

Status: in progress.

- `RuntimeControl` provides an `exit.request` marker file.
- App loop consumes exit requests and exits through `ApplicationContext.ExitThread()`.
- `request_exit.bat` sends the request without using `taskkill`.
- `stop.bat` still remains as the emergency fallback.

Still missing:

- A stronger authenticated local control channel if needed later.

### Phase 21: snap target diagnostics

Status: in progress.

- `EnableSnapDiagnostics=false` by default.
- `SnapDiagnostics` writes accepted and rejected snap targets to `%LOCALAPPDATA%\RhaegarMove\snap-targets.txt`.
- Rejection reasons include active window, hidden, minimized, ignored by rule, not in SnapList, noactivate, no caption/thickframe, and empty rect.
- `snap_targets.bat` displays the last snap target report.

### Phase 22: config validation report

Status: in progress.

- `ConfigValidation` writes `%LOCALAPPDATA%\RhaegarMove\config-report.txt` at startup and reload.
- The report includes normalized values and warnings for advanced/risky options.
- `config_report.bat` displays the last config report.

### Phase 23: stronger runtime control watcher

Status: in progress.

- `RuntimeWatcher` uses `FileSystemWatcher` to notice `reload.request` and `exit.request` files.
- The app loop still consumes marker files as fallback, so missed watcher events do not break control.
- This keeps the simple request-file model while reducing latency.

Still missing:

- Named pipe or window-message control channel.
- Request authentication/nonce if needed later.

### Phase 24: per-edge snap scoring diagnostics

Status: in progress.

- `SnapScoreDiagnostics` writes `%LOCALAPPDATA%\RhaegarMove\snap-score.txt` when `EnableSnapDiagnostics=true`.
- Move snap scoring records candidate edge labels, delta, absolute distance, threshold membership, and best candidate.
- Resize snap scoring records candidate edges for the active resize side.
- `snap_score.bat` displays the last snap score report.

### Phase 25: unknown-key config validation

Status: started.

- `AppSettings` tracks unknown config keys and malformed key/value lines.
- Invalid integer/boolean values are recorded as normalization notes with their fallback value.
- Clamped numeric values are recorded as raw-to-normalized notes such as `SnapGap: 999 -> 127`.
- `ConfigValidation` includes `[unknown keys]` and `[normalization notes]` sections.

Still missing:

- Section-specific unknown-key reporting.
- Duplicate-key warnings.

### Phase 26: monitor-edge scoring

Status: started.

- `SnapScoreDiagnostics.BeginSession` clears the report once per gesture and appends multiple sections.
- Move monitor/workarea edges are scored as `monitor-left`, `monitor-top`, `monitor-right`, and `monitor-bottom`.
- Resize monitor/workarea edges are scored for the active resize side.
- Window-edge scoring still runs after monitor-edge scoring and appends to the same report.

Still missing:

- Separate X/Y best summaries.
- Final post-transform decision line.

### Phase 27: preview-only snap mode

Status: started.

- `EnablePreviewOnlySnap=false` by default.
- When enabled, worker records the calculated rectangle as pending instead of moving/resizing immediately.
- The pending rectangle is committed on mouse release or watchdog finish.
- Cancel discards the pending rectangle.
- Overlay/preview state can show the pending rectangle before commit.

Still missing:

- Differentiating snap-only preview from normal move preview.
- Sticky resize commit support in preview-only mode.

## Safety checklist before testing

1. Open Task Manager before running the app.
2. Keep `stop.bat` visible.
3. Test on Notepad first.
4. Do not install as startup until manual run is stable.
5. If mouse or Alt feels wrong, run `stop.bat` immediately.

## Known current status

RhaegarMove is still a clean-room AltSnap-inspired implementation, not an AltSnap source copy. The code is now modular and has the correct direction: worker boundary, restore metadata, snap-to-window basics, advanced rules, DPI-aware restore, sticky resize infrastructure, config-based min/max sizing, runtime diagnostics, optional UI-thread-safe preview overlay, config reload, safe exit request, snap target diagnostics, config validation reports, runtime watcher support, per-edge snap scoring diagnostics, unknown-key reporting, monitor-edge scoring, and preview-only commit mode. The next high-value area is duplicate-key config reporting, final snap decision summaries, and tray/config UX.
