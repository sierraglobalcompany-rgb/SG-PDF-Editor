# F6 — Task 3 checkpoint — EDITAR surface, selection, hit-test and overlay

**Date:** 2026-10-10  
**Branch:** `feat/f6-images`  
**Functional head before this checkpoint:** `a01b6f848b2f77a69231874ac9d271eac77af7d5`  
**Scope:** F6 Task 3 only — read-only EDITAR mode, eligibility gate, real image selection, deterministic PDF-space hit-test and WPF selection overlay.

## 1. Status

Task 3 functional code/tests are GREEN on Windows CI before this documentation commit.

- RED commit: `5c588e5d7f3fcca48e7951cbc9bf3fc72956ea9f`
- RED run: `38015215408`
- Functional GREEN commit: `a01b6f848b2f77a69231874ac9d271eac77af7d5`
- Functional GREEN run: `38015760165`
- Restore locked dependencies: PASS
- Release build: PASS
- Build warnings: 0
- Build errors: 0
- Tests: 580 passed / 0 failed / 0 skipped

A fresh exact-head CI is required after this checkpoint commit before Task 3 is considered closed.

## 2. TDD witness

The Task-3 RED added exactly two test files:

1. `tests/SGPdf.App.Tests/ImageHitTesterTests.cs`
2. `tests/SGPdf.App.Tests/MainWindowEditImagesTests.cs`

The RED run built successfully and produced exactly the intended new failures:

- prior tests passed: 570;
- Task-3 failures: 10;
- total: 580;
- geometry failures were caused by the missing `ImageHitTester` contract;
- WPF failures were caused by the missing EDITAR fields/methods/surface.

No unrelated regression was used as RED evidence.

## 3. Pure PDF-space hit-test

`src/SGPdf.App/Features/Edit/Images/ImageHitTester.cs` now provides:

- `PdfPoint` as the small shared PDF-space point contract required by the frozen F6 interfaces;
- device → PDF conversion through the existing `PdfPageDeviceTransform` model;
- PDF → device conversion for overlay projection;
- affine image quad generation from the image matrix + unit square;
- inverse-affine point containment, so rotated AABB false positives are rejected;
- deterministic overlap resolution by greatest original `PageObjectIndex` among matching images.

Task 1 had already characterized the representative page-object enumeration/paint-order route: the later enumerated image paints above earlier objects. Task 3 therefore uses the greatest matching original object ordinal instead of inventing an unproven screen-space heuristic.

### Ruling — rotated geometry

Task 1 proved that the pinned PDFium runtime exports rotated-bounds support, but the frozen managed `PdfImageObjectInfo` snapshot does not retain a rotated quad. The approved F6 spec explicitly allows the fallback of deriving the quadrilateral from the affine matrix + image unit rectangle. Task 3 uses that route because it is exact for the managed image transform, pure/testable and does not require expanding the Task-1 native contract.

## 4. EDITAR WPF surface

`src/SGPdf.App/MainWindow.EditImages.cs` now adds an isolated Loaded hook and keeps the existing MainWindow partial/KISS architecture.

Behavior covered by automated tests:

- `EDITAR` is inserted between `FIRMAR` and `ORGANIZAR`;
- button is disabled until a PDF is open by reusing the existing PDF-enabled binding;
- entering EDITAR reuses the existing `EnsureEditSurfaceForCurrentPageAsync()` single-page reader path;
- continuous reader is collapsed and the active `PdfImage` surface is shown;
- `ImageEditOverlayCanvas` is transparent and visible only in EDITAR;
- only image objects from the active page are enumerated;
- one `ImageEditWorkspace` is created for an eligible source;
- a selected image shows a rotated outline plus four corner handles;
- handles are visual only in Task 3 and have no mutation events;
- click outside an image clears selection;
- Escape clears selection through an event hook, without adding a second `OnPreviewKeyDown` override.

## 5. Safety / transition gates

A mutable `ImageEditWorkspace` is created only after eligibility succeeds.

Task 3 blocks before workspace creation when:

- the source session was opened with a password;
- the PDF contains one or more cryptographic signatures;
- signature-count inspection itself fails.

Entering EDITAR also reuses the existing transition guards:

- dirty FIRMAR edits call `TryResolvePendingSignatureEdits(...)`; Cancel keeps FIRMAR active and creates no image workspace;
- active ORGANIZAR calls `TryLeaveOrganizeModeWithGuard()` before EDITAR can proceed, preserving the existing ORGANIZAR dirty/discard guard.

## 6. Reader integration ruling

The Task-3 plan allowed a narrow modification to `MainWindow.Reader.cs` **if needed**. No modification was necessary:

- `EnsureEditSurfaceForCurrentPageAsync()` already existed from the reader/FIRMAR work;
- it already renders or reuses the active page, switches continuous reader → single-page `PdfImage`, and preserves the existing device transform;
- reusing it avoids duplicating reader logic or expanding Task 3 beyond selection.

This is the smaller KISS implementation and leaves navigation/mutation-specific hardening to the later tasks that introduce mutable image commands.

## 7. Exact scope audit

Relative to the closed Task-2 checkpoint `19dc09ccb3ec8e18aecb64d0edcf64b48803d110`, Task 3 adds only:

1. `docs/history/2026-10-10-F6-TASK3-RED-CHECKPOINT.md`
2. `src/SGPdf.App/Features/Edit/Images/ImageHitTester.cs`
3. `src/SGPdf.App/MainWindow.EditImages.cs`
4. `tests/SGPdf.App.Tests/ImageHitTesterTests.cs`
5. `tests/SGPdf.App.Tests/MainWindowEditImagesTests.cs`

This closure document is the sixth Task-3 file.

No existing production file was modified for Task 3. No writer, native mutation, replacement loader, image transform command, delete command, opacity mutation, z-order mutation, Save As pipeline, PR, merge or `main` change was introduced.

Branch hygiene was re-audited after publishing the functional commit; the functional diff from the RED checkpoint contains only the two intended production files.

## 8. Boundaries preserved

- EDITAR is read-only in Task 3.
- No PDF page object is mutated.
- No move/resize/rotate/delete behavior exists yet.
- No Task-4 command/history UI was started.
- No save writer exists yet.
- PDFium remains the only PDF object engine used by F6.
- No runtime package was added.
- No private/customer fixture was committed.
- `main` remains `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No F6 PR exists.
- Manual Windows UX QA remains NOT RUN.

## 9. Next gate

After fresh exact-checkpoint Windows CI passes, Task 3 is CLOSED.

The next approved `continua` may start **Task 4 — F6.3 move/resize/rotate/delete + undo/redo + dirty guard** using a new RED → GREEN cycle.

Do not start Task 4 in the Task-3 closure step.
