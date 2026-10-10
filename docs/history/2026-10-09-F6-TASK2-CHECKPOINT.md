# F6 Images — Task 2 Checkpoint

**Local date:** 2026-10-09 — America/Bogota  
**Branch:** `feat/f6-images`  
**Task:** F6 Task 2 — logical edit model + source fingerprint + command history  
**Status:** CLOSED — automated gate PASS  
**Task 1 base:** `48b148af33205cb38021f7d93ab692d2df4c1825`  
**Functional GREEN head:** `779ba6b27cc8973d651c9e678f87686975392cce`  
**Main:** `31c0594758a83ec555d73ecdd7c597cdf8791fd7` — unchanged / unmerged

## 1. RED evidence

RED commit:

```text
ba57784cdebf3dbcc0c47af1d421d4225a3096f0
```

Windows CI:

```text
run 38014280706
Build: PASS — 0 warnings / 0 errors
Tests: expected FAIL
Previous suite: 560 PASS
New Task 2 tests: 10 FAIL
Reason: TypeLoadException — ImageEditWorkspace did not exist yet
```

This was a valid behavioral RED: the solution compiled cleanly and the only failing tests were the ten newly added Task 2 contracts.

## 2. GREEN evidence

Functional GREEN commit:

```text
779ba6b27cc8973d651c9e678f87686975392cce
```

Windows exact-head CI for the functional code:

```text
run 38014390826 — SUCCESS
Restore locked: PASS
Release build: PASS
Warnings: 0
Errors: 0
Tests: 570 PASS / 0 FAIL / 0 SKIPPED
```

The full suite contains the ten focused Task 2 tests, so their GREEN result is included in the 570/570 pass.

## 3. Delivered contract

Added pure logical model under `Features/Edit/Images`:

- `ImageEditSourceFingerprint`
  - captures `FileLength + LastWriteTimeUtcTicks`;
  - compares current source state without mutating the file;
  - workspace stores normalized full source path.
- `ImageObjectKey`
  - durable logical key = `PageIndex + PageObjectIndex`.
- `ImageObjectRef`
  - key + original managed `PdfImageObjectInfo` snapshot;
  - no native handles are retained.
- `ImageEditState`
  - original object ref;
  - current matrix;
  - replacement slot;
  - opacity slot;
  - z-order target slot;
  - logical delete flag.
- `ImageEditMutation`
  - operation kind + key + before + after state.
- `ImageEditWorkspace`
  - `SourcePath`;
  - `SourceFingerprint`;
  - `SourceOpenedWithPassword`;
  - one state per image key;
  - idempotent `EnsureObject`;
  - before/after history;
  - undo/redo;
  - redo cleared by a new edit after undo;
  - no-op commit creates no history;
  - failed unknown-object commit creates no history;
  - dirty state relative to the last saved logical baseline;
  - `MarkSavedBaseline()` preserves states, clears history and marks clean.

## 4. Saved-baseline invariant

Owner test:

```text
SaveBaseline_SecondSaveFromOriginalReappliesPreviouslySavedLogicalEdits
```

The workspace deliberately retains the complete logical state that differs from the original source even after `MarkSavedBaseline()`.

Therefore a later Save As can reopen the original source and rematerialize:

```text
all previously saved logical edits
+
all edits made after the saved baseline
```

This prevents the second-save regression where edits from the first save would disappear merely because history was cleared.

Undo after a post-save edit correctly returns to the saved logical baseline and `IsDirty == false`.

## 5. Forward-only contract note

`ImageReplacementAsset` exists only as an empty forward record so the approved `ImageEditState` signature remains stable.

Task 2 does **not** implement:

- PNG/JPEG loading;
- replacement bytes;
- transparency handling;
- replacement writer behavior.

Those remain owned by F6 Task 5.

## 6. Scope audit

Diff from Task 1 checkpoint to functional GREEN changes only:

```text
src/SGPdf.App/Features/Edit/Images/ImageEditSourceFingerprint.cs
src/SGPdf.App/Features/Edit/Images/ImageObjectRef.cs
src/SGPdf.App/Features/Edit/Images/ImageEditState.cs
src/SGPdf.App/Features/Edit/Images/ImageEditWorkspace.cs
tests/SGPdf.App.Tests/ImageEditWorkspaceTests.cs
```

No existing production file was modified.

Not implemented / not touched:

- EDITAR UI;
- hit-test;
- overlays/handles;
- WPF integration;
- PDF mutation;
- writer;
- image extraction/replacement behavior;
- product opacity;
- product z-order;
- F6 Task 3+;
- F6 PR;
- `main`;
- merge.

## 7. Checkpoint CI rule

This document is the durable Task 2 checkpoint. Its own commit SHA is the commit containing this file. The branch push CI for that exact checkpoint head must be verified after the checkpoint is published; do not amend the checkpoint merely to cite its own run ID, because that would move the head again.

## 8. Next exact gate — DO NOT START WITHOUT NEXT `continua`

F6 Task 3 / F6.2:

```text
EDITAR mode
+ real image selection
+ deterministic hit-test
+ read-only overlay/handles
+ rotated quad support
+ signed/password block before mutable workspace
```

Task 2 is CLOSED. STOP before Task 3.
