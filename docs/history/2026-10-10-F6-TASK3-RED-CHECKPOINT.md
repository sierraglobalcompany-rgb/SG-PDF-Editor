# F6 — Task 3 RED checkpoint — EDITAR selection + hit-test

**Date:** 2026-10-10  
**Branch:** `feat/f6-images`  
**Checkpoint type:** INTERMEDIATE / RED ONLY  
**Functional head before this documentation commit:** `5c588e5d7f3fcca48e7951cbc9bf3fc72956ea9f`  
**Scope:** F6 Task 3 only — tests for EDITAR surface, eligibility, image selection, PDF-space hit-test and overlay contract. No product implementation has started.

## 1. Status

Task 3 is intentionally paused after the RED witness.

- Task 1: CLOSED / GREEN.
- Task 2: CLOSED / GREEN.
- Task 3: RED established, implementation NOT started.
- Task 4+: NOT started.
- No merge to `main`.
- No F6 PR created.

## 2. RED commit

Commit:

`5c588e5d7f3fcca48e7951cbc9bf3fc72956ea9f`

Message:

`test(images): add F6 Task 3 selection RED`

Files added:

1. `tests/SGPdf.App.Tests/ImageHitTesterTests.cs`
2. `tests/SGPdf.App.Tests/MainWindowEditImagesTests.cs`

No production file was changed by the RED commit.

## 3. Windows CI RED witness

Workflow run:

`38015215408`

Exact head:

`5c588e5d7f3fcca48e7951cbc9bf3fc72956ea9f`

Evidence:

- Repository hygiene: PASS.
- Locked restore: PASS.
- Release build: PASS.
- Build warnings: 0.
- Build errors: 0.
- Test suite: RED as intended.
- Passed: 570.
- Failed: 10.
- Skipped: 0.
- Total: 580.

The failures are attributable to the Task-3 production contracts being intentionally absent, not to compilation or unrelated regressions.

## 4. RED contracts now frozen

### Pure geometry / hit-test

`ImageHitTesterTests` requires:

- PDF-space hit-testing independent of zoom;
- plain/non-square image selection;
- near-edge selection;
- rotated-image selection using the real affine quad rather than AABB-only logic;
- rejection of AABB false positives;
- deterministic overlap resolution;
- use of the Task-1-proven enumeration/paint-order evidence so the last painted matching image wins in the representative supported route;
- device→PDF mapping that returns the same PDF identity across multiple zooms.

The RED currently fails because `SGPdf.App.Features.Edit.Images.ImageHitTester` does not yet exist.

### WPF EDITAR surface / selection

`MainWindowEditImagesTests` requires:

- `EditModeButton` exists between `FIRMAR` and `ORGANIZAR`;
- button disabled without an open PDF;
- eligible PDF can enter EDITAR;
- EDITAR switches continuous reader to active single-page `PdfImage`;
- transparent `ImageEditOverlayCanvas` is visible;
- active page image objects are enumerated;
- mutable `ImageEditWorkspace` is created only after eligibility passes;
- signed source blocks before workspace creation;
- password-opened source blocks before workspace creation;
- dirty FIRMAR Cancel blocks transition into EDITAR;
- selecting an image sets one active image selection;
- clicking a non-image point clears selection;
- Escape clears selection;
- selection draws overlay visuals without mutating the PDF.

The RED currently fails because the Task-3 UI fields/methods/surface do not yet exist.

## 5. Important design evidence already available

Task 1 proved representative paint order for the overlap fixture: later enumerated image paints above earlier page objects. Therefore Task 3 may select the last matching image candidate for the proven representative route rather than inventing an unproven topmost heuristic.

Task 1 also proved rotated-bounds support in the exact pinned PDFium runtime, but Task 3 can remain KISS by deriving the image quad directly from the affine matrix + unit square for pure hit-testing unless a later RED requires the native rotated-bounds API in the UI path.

Existing `PdfPageDeviceTransform` and the signature coordinate mapper already provide the project pattern for device/PDF coordinate conversion. Task 3 should reuse that geometry concept rather than create a parallel zoom-dependent selection model.

## 6. Resume point

On the next approved `continua`, resume exactly here:

1. Implement `src/SGPdf.App/Features/Edit/Images/ImageHitTester.cs` minimally to satisfy the four geometry RED tests.
2. Add `src/SGPdf.App/MainWindow.EditImages.cs` with an isolated Loaded hook and EDITAR UI integration.
3. Modify `MainWindow.Reader.cs` only as narrowly needed to coexist with the single-page edit surface and reader transitions.
4. Honor existing FIRMAR/ORGANIZAR transition guards.
5. Keep Task 3 read-only: no native PDF mutation, move/resize/rotate/delete, undo/redo commands or save writer yet.
6. Run focused tests, then full suite, then exact-head Windows CI.
7. Only after GREEN create the formal Task-3 closure checkpoint.

Do not start Task 4 until Task 3 is GREEN and closed.
