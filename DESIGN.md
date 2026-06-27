# Clean-room implementation notes

This file records the design decisions for RhaegarMove without copying source code from AltSnap.

## Scope for v0.1

- Move windows with `Alt + left mouse drag`.
- Resize windows with `Alt + right mouse drag`.
- Avoid a global keyboard hook in v0.1.
- Do not synthesize keyboard input.
- Do not block unrelated input.
- Keep all advanced behavior opt-in or diagnosable.
- Tray icon is opt-in only; the default runtime has no tray icon.

## Why v0.1 avoids a keyboard hook

A global keyboard hook is easy to get wrong. If Alt key-up or injected-key state is mishandled, Windows may feel like Alt is stuck. v0.1 therefore uses a conservative model:

- Install only a low-level mouse hook.
- On mouse events, check whether Alt is physically down.
- Swallow only the mouse down/move/up events that belong to an active RhaegarMove gesture.
- If Alt is released during a gesture, finish the operation and swallow the matching mouse-up once.

## Current source layout

The source is modular:

- `RhaegarMove.cs`: app entry, single-instance guard, watchdog lifecycle, runtime command routing, reload/exit processing.
- `NativeMethods.cs`: Win32 constants, structs, delegates, and P/Invoke declarations.
- `MouseHook.cs`: low-level mouse hook coordinator.
- `OperationWorker.cs`: coalesces mouse-move operations outside the hook callback and supports preview-only commit mode.
- `WindowController.cs`: target validation, fullscreen/maximized checks, sizing notifications.
- `WindowRules.cs`: clean-room process/class/title and composite window rule matching, with reload support.
- `Geometry.cs`: DWM bounds, monitor work area, input state helpers.
- `DpiHelper.cs`: DPI lookup and restore-size scaling helpers.
- `ResizeEngine.cs`: resize-region selection and rectangle calculation.
- `SizingConstraints.cs`: config-based min/max size clamping.
- `ConfigDefaults.cs`: resettable General and Blacklist defaults.
- `ConfigFileUpdater.cs`: safe section key updater for UI edits.
- `ConfigValidation.cs`: startup/reload config validation, unknown-key, duplicate-key, rule validation, and normalization report.
- `RuleValidation.cs`: validates blacklist/rule-list syntax and risky broad patterns.
- `RuleDiagnostics.cs`: rule decision snapshots for debugging.
- `RuntimeCommands.cs`: command-line diagnostics, settings UI launcher, status/control-mode report, and runtime control helpers.
- `RuntimeControl.cs`: file-based reload/exit request markers and last successful reload tracking.
- `RuntimeWatcher.cs`: file watcher for runtime request markers.
- `SettingsForm.cs`: basic WinForms settings editor for safe options, with warnings and reset confirmations.
- `RuleListForm.cs`: blacklist/rule-list editor with inline validation and reset default rules action.
- `TrayIcon.cs`: optional notification-area menu; disabled by default and not created unless enabled.
- `SnapDiagnostics.cs`: snap target accept/reject report writer.
- `SnapScoreDiagnostics.cs`: per-edge candidate scoring and final decision report writer.
- `SnapPreview.cs`: preview-state snapshots for future outline UI.
- `PreviewOverlay.cs`: optional transparent outline overlay, disabled by default.
- `SnapEngine.cs`: monitor snapping, Aero-style snapping, window-edge snapping, sticky resize basics, snap target diagnostics, and per-edge scoring diagnostics.
- `WindowRestoreStore.cs`: SetProp/GetProp restore metadata with an in-process fallback dictionary.
- `AppSettings.cs`: typed INI option loader with in-place reload and config issue tracking.

`build.bat` compiles `src\\*.cs` directly. It does not depend on generated source preparation.

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
- Snap-to-other-windows exists for move and resize basics.

### Phase 4: resize quality
Status: in progress.
- Side/center resize regions exist.
- `ResizeCenterMode`, `CenterFraction`, and `SidesFraction` config options exist.
- Symmetric center resize exists.

### Phase 5: optional advanced input
Status: planned and constrained.
- Keyboard hook is intentionally not added yet.
- Future keyboard work requires a documented external stop path and Alt-up reset plan.

### Phase 6: UX layer
Status: in progress.
- Runtime helper scripts and diagnostics helper scripts exist.
- GitHub Actions artifact includes helper scripts.
- Basic settings form exists and can be opened without tray.
- Tray support exists but is opt-in and disabled by default.

### Phase 7: source cleanup
Status: done for the active build.
- Main logic has been split into focused source files.
- `build.bat` compiles `src\\*.cs` directly.

### Phase 8: operation worker and state machine
Status: in progress.
- Mouse hook delegates movement to `OperationWorker`.
- Mouse move events are coalesced through a worker thread.
- Watchdog cleanup still exists.

### Phase 9: restore metadata and window snap
Status: in progress.
- `WindowRestoreStore` stores snapped-window restore size and flags with `SetProp/GetProp` plus fallback storage.
- Aero snap writes restore metadata before snapping.
- Snap-to-other-windows basics exist.

### Phase 10: advanced window rules
Status: in progress.
- `WindowRules` supports composite `process:title|class` rules.
- `Rules`, `SnapList`, `NoSizingNotify`, and `NoResize` exist.
- Window rules can be reloaded at runtime.

### Phase 11: DPI-aware restore
Status: in progress.
- `DpiHelper` reads per-window DPI when available.
- Restore metadata stores DPI and restore size is scaled on drag restore.

### Phase 12: smart snap and sticky resize
Status: in progress.
- `StickyResize` exists and is disabled by default.
- Adjacent snap targets can be adjusted when the active window is resized.

### Phase 13: min/max sizing
Status: in progress.
- `SizingConstraints` applies config-based min/max clamping after move and resize calculations.
- `MaxWidth` and `MaxHeight` exist; `0` means unlimited.

### Phase 14: rule diagnostics
Status: in progress.
- `RuleDiagnostics` writes `%LOCALAPPDATA%\\RhaegarMove\\rules.txt`.
- Diagnostics include matched rule explanations.

### Phase 15: preview and outline foundation
Status: in progress.
- `SnapPreview` records the latest move/resize target rectangle.
- `preview_status.bat` can show the last preview state.

### Phase 16: preview overlay
Status: in progress.
- `PreviewOverlay` implements an optional transparent outline window.
- `EnablePreviewOverlay=false` by default.
- Overlay work is marshaled to the WinForms UI thread.

### Phase 17: runtime control
Status: in progress.
- Supported commands: `--status`, `--settings`, `--config-path`, `--diagnose-cursor`, `--preview-status`, `--reload`, and `--request-exit`.
- Runtime command output is persisted to `%LOCALAPPDATA%\\RhaegarMove\\runtime.txt`.

### Phase 18: diagnostics refinement
Status: in progress.
- Rule diagnostics report rule category and matched pattern.
- Runtime status points to diagnostic output files.

### Phase 19: config reload
Status: in progress.
- `AppSettings` supports in-place reload.
- `WindowRules.Reload()` resets cached rule lists.
- App loop consumes reload requests and updates watchdog interval.

### Phase 20: safe exit request
Status: in progress.
- App loop consumes exit requests and exits through `ApplicationContext.ExitThread()`.
- `request_exit.bat` sends the request without using `taskkill`.
- `stop.bat` remains the emergency fallback.

### Phase 21: snap target diagnostics
Status: in progress.
- `EnableSnapDiagnostics=false` by default.
- `SnapDiagnostics` writes accepted and rejected snap targets to `snap-targets.txt`.

### Phase 22: config validation report
Status: in progress.
- `ConfigValidation` writes `config-report.txt` at startup and reload.
- The report includes normalized values and warnings.

### Phase 23: stronger runtime control watcher
Status: in progress.
- `RuntimeWatcher` uses `FileSystemWatcher` for `reload.request` and `exit.request`.
- Marker-file fallback remains.

### Phase 24: per-edge snap scoring diagnostics
Status: in progress.
- `SnapScoreDiagnostics` writes `snap-score.txt` when `EnableSnapDiagnostics=true`.

### Phase 25: unknown-key config validation
Status: in progress.
- `AppSettings` tracks unknown config keys and malformed key/value lines.
- Invalid integer/boolean values are recorded as normalization notes.

### Phase 26: monitor-edge scoring
Status: in progress.
- Monitor/workarea move and resize edges are scored.
- Monitor and window scoring append to the same gesture report.

### Phase 27: preview-only snap mode
Status: in progress.
- `EnablePreviewOnlySnap=false` by default.
- Pending rectangles are committed on release/watchdog finish when enabled.

### Phase 28: duplicate-key config reporting
Status: in progress.
- `AppSettings` tracks duplicate config keys.
- Last value wins, and the report records which occurrence overrode the previous value.

### Phase 29: final snap decision summary
Status: in progress.
- Final reports include `source`, `dx`, `dy`, `dw`, `dh`, and `changed`.
- Source classification includes `aero`, `monitor`, `window`, `monitor+window`, and `none`.

### Phase 30: tray/config UX start
Status: revised after UX decision.
- Tray support exists only as an optional feature.
- `EnableTrayIcon=false` by default.
- If disabled, `TrayIcon` avoids creating a `NotifyIcon` object.
- Settings can be opened with `settings.bat` or `RhaegarMove.exe --settings` instead of relying on tray.

### Phase 31: optional tray polish
Status: opt-in only.
- If tray is enabled, tooltip reports live vs preview-only mode and snap on/off state.
- Tray report submenu can open diagnostic files.

### Phase 32: final snap winner classification
Status: in progress.
- Final snap decision includes `source=aero`, `source=monitor`, `source=window`, `source=monitor+window`, or `source=none`.

### Phase 33: settings UI start
Status: in progress.
- `SettingsForm` provides a basic WinForms UI for safe general options.
- `settings.bat` opens the settings UI without tray.

### Phase 34: settings validation UI
Status: in progress.
- Settings form shows live warnings for risky options such as high snap threshold, sticky resize, preview overlay, preview-only snap, diagnostics, and optional tray.
- The form explicitly notes that tray is optional and disabled by default.

### Phase 35: rule-list editor
Status: in progress.
- `RuleListForm` edits `Classes`, `Processes`, `Titles`, `Rules`, `SnapList`, `NoSizingNotify`, and `NoResize` under `[Blacklist]`.
- `ConfigFileUpdater` can update named sections while preserving other config content.
- Settings form links to the rule editor.

### Phase 36: no-tray default UX
Status: in progress.
- `EnableTrayIcon=false` in default config and fallback settings.
- `settings.bat` is included in build artifacts.
- Tray support remains configurable, but no tray icon is created unless explicitly enabled.

### Phase 37: rule validation
Status: in progress.
- `RuleValidation` validates rule-list CSV fields and composite `process:title|class` syntax.
- Warnings include missing keys, empty composite segments, too many separators, tabs, repeated spaces, and broad `SnapList=*`.
- Rule validation is included in `config-report.txt`.

### Phase 40: inline rule validation UI
Status: started.
- `RuleListForm` now shows live rule validation warnings while editing.
- Composite rule examples are shown next to rule-list labels.
- Save prompts for confirmation if rule validation has warnings.

Still missing:
- Rich examples/help panel for each rule category.

### Phase 38 / 41: reset defaults and confirmations
Status: started.
- `ConfigDefaults` centralizes resettable General and Blacklist defaults.
- Settings UI has `Reset general defaults` and `Reset window rules` actions.
- Rule-list editor has `Reset default window rules`.
- Reset actions now ask for confirmation before applying.

Still missing:
- Reset-all command-line helper.

### Phase 39 / 42: status and last successful reload tracking
Status: started.
- `--status` reports `controlMode=file-marker+watcher`.
- `--status` reports `settingsCommand=available`, `trayDefault=false`, and `trayIconConfigured=<value>`.
- `--status` reports reload/exit request paths, pending marker status, runtime path, runtime last write time, last reload file path, and last reload summary.
- `RuntimeControl.MarkReloadApplied()` writes `%LOCALAPPDATA%\\RhaegarMove\\last-reload.txt` after a runtime reload succeeds.

Still missing:
- Distinguishing reload source in more detail, such as settings UI vs helper script.

## Safety checklist before testing

1. Open Task Manager before running the app.
2. Keep `stop.bat` visible.
3. Test on Notepad first.
4. Do not install as startup until manual run is stable.
5. If mouse or Alt feels wrong, run `stop.bat` immediately.

## Known current status

RhaegarMove is still a clean-room AltSnap-inspired implementation, not an AltSnap source copy. The project now has modular hook/worker geometry, restore metadata, snapping, advanced rules, diagnostics, config reload, safe exit request, preview overlay, preview-only commit mode, config validation, snap scoring, no-tray default UX, standalone settings UI, rule-list editing with inline validation, reset-to-defaults confirmations, and last successful reload tracking. The next high-value area is reset-all CLI, richer rule examples, and reload-source tagging.
