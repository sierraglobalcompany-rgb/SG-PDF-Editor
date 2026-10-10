# F6 — Task 7 checkpoint — conditional opacity + z-order

**Date:** 2026-10-10  
**Branch:** `feat/f6-images`  
**Task 6 checkpoint base:** `ab16919f723e9bc6b7c3a77c79743da523188653`  
**Task 7 RED:** `da93276d1c7ad12925079c550fcb844309038f69`  
**Functional GREEN before this checkpoint:** `221a9262ab2030151e82f2f6dd83669569925608`  
**Scope:** Task 7 only — activate the Task-1-proven opacity and exact-index z-order routes, logical commands/history, contextual controls and native materialization.

## 1. Gate authority

Task 1 already proved against pinned `bblanchon.PDFium.Win32 156.0.8076` that both optional routes are available on the representative synthetic corpus:

- opacity via `FPDFPageObj_SetFillColor` + `FPDFPage_GenerateContent` + save/reopen/render;
- exact-index z-order via remove + `FPDFPage_InsertObjectAtIndex` + regenerate + save/reopen/render.

Task 7 therefore implements both capabilities. No fallback engine, page rasterization or overlay materialization was added.

## 2. RED witness

RED commit: `da93276d1c7ad12925079c550fcb844309038f69`  
Workflow run: `38021745588`

The first attempt had 10 failures: the 9 intended Task-7 failures plus the known intermittent ORGANIZAR secondary-thumbnail test. The same exact SHA was rerun before production changes.

Same-SHA rerun result:

- Release build: PASS;
- warnings: 0;
- errors: 0;
- tests: 632 passed / 9 failed / 0 skipped / 641 total;
- all 9 failures were Task-7 contracts only;
- the inherited thumbnail test passed on rerun.

The RED proved:

- opacity 50%/0% was still ignored by the Task-6 writer;
- opacity command/undo/redo did not exist;
- z-order user commands did not exist;
- contextual `Opacidad` / `Orden` controls did not exist.

## 3. GREEN implementation

Functional commit: `221a9262ab2030151e82f2f6dd83669569925608`  
Workflow run: `38022143956`

Exact-head Windows evidence:

- checkout: `221a9262ab2030151e82f2f6dd83669569925608`;
- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- build warnings: 0;
- build errors: 0;
- tests: **641 passed / 0 failed / 0 skipped**.

### Opacity

- `SetSelectedImageOpacityPercent(int)` accepts 0–100.
- 50% maps to native alpha 128 using deterministic rounding.
- 0% maps to alpha 0.
- 100% normalizes to logical `Opacity = null`, restoring the original baseline instead of forcing a new graphics-state mutation.
- changes use `ImageEditOperationKind.SetOpacity`, so undo/redo remains workspace-native.
- contextual `Opacidad` exposes a 0–100 slider.
- the writer applies only the Task-1-proven `FPDFPageObj_SetFillColor(..., alpha)` route.
- representative 100/50/0 outputs pass save → reopen → render pixel validation.

### Z-order

User commands now support:

- `Enviar al fondo`;
- `Retroceder`;
- `Adelantar`;
- `Traer al frente`.

The logical state uses `TargetObjectIndex` and `ImageEditOperationKind.ChangeZOrder`; returning to the source ordinal normalizes back to `null`.

`PdfDocumentSession.ImageEditOptional.cs` adds a bounded managed `GetPageObjectCount(...)` snapshot under `PdfiumRuntime.NativeGate`. This is intentionally based on **all page objects**, not only discovered images, so one-step/front/back operations can cross text/vector neighbors correctly.

The Task-6 writer's proven remove/reinsert implementation remains the materializer. Representative tests prove:

- image ↔ vector crossing;
- front/back/forward/backward visible order;
- no duplicate/lost page object in the fixture;
- save/reopen/render expected paint order;
- undo/redo of logical z-order.

## 4. IMG-03 status after Task 7

For the pinned runtime and representative automated corpus:

- move: implemented/proven in prior Task 4 + Task 6;
- resize: implemented/proven in prior Task 4 + Task 6;
- rotate: implemented/proven in prior Task 4 + Task 6;
- delete: implemented/proven in prior Task 4 + Task 6;
- opacity: **PROVEN AVAILABLE + Task-7 product path GREEN**;
- z-order: **PROVEN AVAILABLE + Task-7 product path GREEN**.

This closes the optional opacity/z-order implementation slice. It does not pre-empt Task 8 preservation/preflight policy or final F6 closure.

## 5. Exact scope audit

Relative to Task-6 checkpoint `ab16919f723e9bc6b7c3a77c79743da523188653`, Task 7 changes exactly:

1. `tests/SGPdf.App.Tests/ImageEditOptionalCapabilitiesTests.cs` — RED tests;
2. `src/SGPdf.App/MainWindow.EditImages.Commands.cs` — logical commands, contextual controls and undo/redo integration;
3. `src/SGPdf.App/Pdf/PdfDocumentSession.ImageEditOptional.cs` — managed full page-object count for ordering;
4. `src/SGPdf.App/Pdf/PdfImageEditWriter.cs` — native opacity materialization.

No Task-7 change to `PdfiumNative.cs` was required because the proven bindings already existed from Task 1.

No Task-7 change touches FIRMAR, ORGANIZAR, LEER, ZPL, dependencies, lockfiles, source overwrite policy or Task-8 preservation logic.

## 6. Repository-history hygiene

During RED publication, an accidental connector write briefly advanced the branch to commit `53e1fb12648eb3a88ad758faef8718baa6c549a1`. It was immediately removed from active history with force-with-lease after verifying the branch head. The active Task-7 RED ancestry is clean:

`ab16919f...` → `da93276d...` → `221a9262...` → this checkpoint.

The accidental commit/file is not part of `feat/f6-images` active history.

## 7. Boundaries preserved

- PDFium remains the only F6 PDF editing engine.
- Source PDF is never overwritten.
- UI operations remain logical until save/materialization.
- No persistent native handles are introduced.
- No opacity/z-order raster fallback exists.
- `main` remains `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No PR or merge has been created.
- Manual Windows UX QA remains **NOT RUN**.
- Task 8 is **NOT STARTED**.

## 8. Next gate

Run fresh exact-head Windows CI for this checkpoint commit. If green, Task 7 is CLOSED/GREEN and work must stop. The next user-approved `continua` may start **Task 8 — F6.7 independent preservation matrix + Save As warning policy** using RED → GREEN.
