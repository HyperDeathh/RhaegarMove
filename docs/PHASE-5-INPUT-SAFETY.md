# Phase 5: advanced input safety

RhaegarMove will not add a low-level keyboard hook until the mouse-only core is stable.

## Current rule

The current implementation should keep using the conservative model:

- Mouse hook observes mouse events.
- Alt state is read only when mouse events arrive.
- RhaegarMove only swallows mouse events that belong to an active RhaegarMove gesture.
- It should not synthesize fake Alt or Ctrl keystrokes.
- It should not hide from Task Manager.

## Requirements before any future keyboard hook

A future keyboard hook is allowed only if all of these exist first:

1. A passing Windows build workflow.
2. A working external stop path that does not rely on the hook.
3. A documented Alt-up state reset path.
4. A documented sent-input ignore counter.
5. A documented cancel path for Escape or a similarly safe key.
6. No collection, storage, or transmission of typed content.

## Non-goals

RhaegarMove must not become:

- a keylogger,
- a stealth process,
- a self-protecting process,
- a tool that hides from Task Manager,
- a tool that blocks unrelated keyboard or mouse input.

## Safer alternatives

Before adding a keyboard hook, prefer these features:

- Better mouse-only state cleanup.
- A named stop signal.
- A visible status command.
- Safer blacklist coverage.
- More conservative snap thresholds.
