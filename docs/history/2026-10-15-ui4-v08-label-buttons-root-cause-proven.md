# UI-4 V08 — Label Export/Print buttons stale-disabled after ZPL load

**Date:** 2026-10-15  
**Base audited:** `abd52d05279e3e656723ad4ff5a21d36de91508e`  
**Status:** ROOT CAUSE PROVEN FROM SOURCE + WINDOWS STATE / FIX NOT YET APPLIED

## Windows evidence

Reported after the signature fix:
- `MediaKind = Thermal`;
- `_labelLayoutPlan != null`;
- `_isBusy = false`;
- `ExportLabelPdfButton.IsEnabled = false`;
- `PrintLabelButton.IsEnabled = false`;
- validation text empty.

This contradicts the intended UI contract.

## Source trace

`OpenZpl_Click` calls `SetBusy(true)` before loading/rendering.

On success, `CommitLoadedZpl(...)`:
1. assigns the ZPL document/rendered labels;
2. calls `ResetLabelLayoutState()`, which builds a valid `LabelLayoutPlan`;
3. calls `UpdateLabelPropertiesUi()` while `_isBusy` is still **true**.

`UpdateLabelLayoutPropertiesUiCore()` sets:
- Export enabled only when plan exists **and `!_isBusy`**;
- Print enabled only when ZPL/rendered labels/plan exist, media is Thermal **and `!_isBusy`**.

Therefore both controls are correctly set to disabled during that intermediate busy state.

The `OpenZpl_Click` `finally` then calls `SetBusy(false)`. `SetBusy(false)` refreshes `UpdateViewerControlsUi()` but does **not** refresh `UpdateLabelPropertiesUi()`.

Result: `_isBusy` becomes false but the two button properties retain their stale disabled values.

This exactly explains the Windows snapshot: valid Thermal plan + not busy + both controls disabled.

## Root cause

**Missing post-busy label-properties refresh at the end of the initial ZPL-open lifecycle.**

Not a planner failure, media failure or validation failure.

## Minimal fix candidate

After `SetBusy(false)` in the `OpenZpl_Click` finalization path, refresh label properties using the existing `UpdateLabelPropertiesUi()` owner method.

Prefer the narrow owner-local fix over broadening generic `SetBusy()` unless a causal test proves a broader invariant is required.

## Required causal test

Reproduce the real lifecycle rather than calling `CommitLoadedZpl` in a non-busy test context:

`busy=true → valid rendered ZPL committed → plan exists while controls disabled → busy=false → label UI refresh → Export enabled; Print enabled for Thermal`.

Also preserve:
- A4/Letter Print remains disabled;
- invalid plan keeps Export/Print disabled;
- busy=true keeps both disabled;
- no forced `IsEnabled=true` bypass.

## Sequencing

Do NOT mix this with the already-proven signature fix commit.

Order:
1. freeze/push/audit signature fix;
2. exact-head CI for signature commit;
3. apply V08 as separate commit on the accepted head;
4. focused causal test + full suite + exact-head CI;
5. Windows V08 reverify.
