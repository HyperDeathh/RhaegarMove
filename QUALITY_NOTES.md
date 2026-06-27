# RhaegarMove AltSnap-quality notes

This document tracks implementation work that moves RhaegarMove from a basic Alt+drag tool toward a more AltSnap-like quality bar while staying clean-room.

## Current direction

RhaegarMove still avoids a global keyboard hook by default. The main quality strategy is:

- keep the mouse hook callback small;
- push heavy work into `OperationWorker`;
- respect target-window rules and app constraints;
- make edge cases diagnosable;
- keep risky features opt-in;
- keep tray disabled by default.

## Phase 43: native window min/max constraints

Status: in progress.

Implemented:

- `MINMAXINFO` and `WM_GETMINMAXINFO` interop declarations.
- `WindowMinMax` helper that queries native min/max tracking size with `SendMessageTimeout`.
- `SizingConstraints.ApplyAll(...)` pipeline: config constraints first, native app constraints second.
- Worker integration for both move and resize paths.
- `RespectWindowMinMaxInfo=true` default setting.
- `NoMinMaxInfo` per-window opt-out rule.

Why this matters:

- Some windows have app-specific minimum or maximum track sizes.
- Respecting those limits reduces resize weirdness and brings behavior closer to native Windows resizing.

Still missing:

- Dedicated UI validation examples for app-specific min/max opt-out rules.

## Phase 44: preview-only sticky resize commit

Status: in progress.

Implemented:

- Preview-only mode now commits the pending rectangle on release.
- If the operation is a resize and `StickyResize=true`, adjacent-window sticky resize can run at commit time instead of being lost.
- Sticky resize now passes neighbor rectangles through the same global/native sizing constraint pipeline before applying them.

Why this matters:

- Preview-only mode is closer to a safe snap preview UX.
- Sticky side effects should match the final committed rectangle, not only live updates.
- Neighbor windows should not be over-shrunk past their own limits.

Still missing:

- Preview-only sticky resize preview for affected neighboring windows.

## Phase 45: richer snap winner diagnostics

Status: in progress.

Implemented:

- Final snap diagnostics now include `xSource` and `ySource` in addition to the combined `source`.
- Possible axis sources: `aero`, `monitor`, `window`, `monitor+window`, `none`.

Why this matters:

- A final snap can be mixed: horizontal movement may come from monitor snapping while vertical movement may come from window-edge snapping.
- Axis-level reporting makes snap tuning and bug reports much easier.

Still missing:

- Axis-level best candidate labels, not just source class.
- Separate monitor-vs-window scoring summary for X and Y axes.

## Phase 46: DWM cloaked-window filtering

Status: started.

Implemented:

- `DWMWA_CLOAKED` interop support.
- `Geometry.IsDwmCloaked(...)` helper.
- Main target selection skips cloaked windows by default.
- Snap target collection skips cloaked windows by default.
- `AllowCloakedWindows=false` default setting for opt-in override.

Why this matters:

- UWP, virtual desktop, and shell surfaces can appear in enumeration while not being truly user-visible.
- Filtering cloaked windows prevents hidden surfaces from being moved, resized, or used as snap targets.

Still missing:

- Separate diagnostics command that explains whether the cursor window is cloaked.

## Phase 47: native min/max diagnostics

Status: started.

Implemented:

- `WindowMinMaxDiagnostics` writes `%LOCALAPPDATA%\RhaegarMove\minmax.txt` when snap diagnostics are enabled.
- Diagnostics include class, title, min track size, max track size, max size, max position, before rect, after rect, and changed flag.
- `minmax_report.bat` helper prints the report.

Why this matters:

- Native min/max bugs are app-specific.
- Seeing the exact returned values makes it easier to create focused `NoMinMaxInfo` rules instead of disabling the feature globally.

Still missing:

- A cursor-specific min/max diagnostic command.

## Phase 48: NoMinMaxInfo rule list

Status: started.

Implemented:

- `WindowRules.ShouldRespectMinMaxInfo(...)`.
- `[Blacklist] NoMinMaxInfo=` config field.
- Rule-list editor support.
- Rule validation support.

Why this matters:

- Some apps may return weird min/max values.
- This lets the user disable native min/max behavior for one app without degrading all windows.

Still missing:

- Preset examples for common problematic windows, if any are discovered during testing.

## Next high-value quality work

1. **Cursor-specific cloaked/minmax diagnostics**
   - Make `diagnose_cursor.bat` show cloaked state and native min/max values.
2. **Build verification**
   - Confirm Windows build with GitHub Actions or local `build.bat` before calling the app stable.
3. **Axis-level best candidate labels**
   - Report the exact winning edge label for X and Y.
4. **Per-monitor DPI snap audit**
   - Add diagnostics for monitor DPI, work area, and restore DPI during snap/restore.
