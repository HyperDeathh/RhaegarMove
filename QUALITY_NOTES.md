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

Status: started.

Implemented:

- `MINMAXINFO` and `WM_GETMINMAXINFO` interop declarations.
- `WindowMinMax` helper that queries native min/max tracking size with `SendMessageTimeout`.
- `SizingConstraints.ApplyAll(...)` pipeline: config constraints first, native app constraints second.
- Worker integration for both move and resize paths.
- `RespectWindowMinMaxInfo=true` default setting.

Why this matters:

- Some windows have app-specific minimum or maximum track sizes.
- Respecting those limits reduces resize weirdness and brings behavior closer to native Windows resizing.

Still missing:

- Detailed diagnostics for the native min/max values returned by each app.
- Per-window opt-out rule such as `NoMinMaxInfo`.

## Phase 44: preview-only sticky resize commit

Status: started.

Implemented:

- Preview-only mode now commits the pending rectangle on release.
- If the operation is a resize and `StickyResize=true`, adjacent-window sticky resize can run at commit time instead of being lost.

Why this matters:

- Preview-only mode is closer to a safe snap preview UX.
- Sticky side effects should match the final committed rectangle, not only live updates.

Still missing:

- Preview-only sticky resize preview for affected neighboring windows.
- Constraints for neighboring windows beyond the global minimum size.

## Phase 45: richer snap winner diagnostics

Status: started.

Implemented:

- Final snap diagnostics now include `xSource` and `ySource` in addition to the combined `source`.
- Possible axis sources: `aero`, `monitor`, `window`, `monitor+window`, `none`.

Why this matters:

- A final snap can be mixed: horizontal movement may come from monitor snapping while vertical movement may come from window-edge snapping.
- Axis-level reporting makes snap tuning and bug reports much easier.

Still missing:

- Axis-level best candidate labels, not just source class.
- Separate monitor-vs-window scoring summary for X and Y axes.

## Phase 46 candidates

Next high-value quality work:

1. **DWM cloaked-window filtering**
   - Avoid hidden UWP/virtual desktop surfaces becoming targets.
2. **Per-window native min/max diagnostics**
   - Show returned min/max values in debug reports.
3. **NoMinMaxInfo rule list**
   - Allow disabling native min/max for problematic apps.
4. **Neighbor constraints for sticky resize**
   - Do not over-shrink adjacent windows when sticky resize is enabled.
5. **Build verification**
   - Confirm Windows build with GitHub Actions or local `build.bat` before calling the app stable.
