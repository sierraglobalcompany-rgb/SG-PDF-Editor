# F6 — Imágenes — PLAN REVIEW CHECKPOINT

**Date:** 2026-10-09  
**Status:** WRITTEN SPEC APPROVED; IMPLEMENTATION PLAN WRITTEN + SELF-REVIEWED; implementation NOT STARTED  
**Branch:** `design/f6-images`  
**Base:** exact F5 automated-closure head `327d7064c14131e603e3bce6947b593a10f46363`  
**No F6 product code or tests have been implemented.**

## 1. Frozen prior state

F5 `ORGANIZAR` remains closed at automated level only.

- F5 head: `327d7064c14131e603e3bce6947b593a10f46363`.
- Draft PR #24: open / draft / unmerged.
- F5 push CI `37991847671`: PASS.
- F5 PR CI `37991854689`: PASS.
- Build: 0 warnings / 0 errors.
- Tests: 550/550 PASS.
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- Real/manual Windows F5 QA: NOT RUN.

F6 planning is isolated on `design/f6-images`; do not modify PR #24 and do not merge anything.

## 2. F6 requirements

- **IMG-01:** detect/select real PDF images and contextual actions.
- **IMG-02:** extract/save and replace while preserving geometry where viable.
- **IMG-03:** move/resize/rotate/opacity/z-order/delete.
- **IMG-04:** undo/redo.

Opacity and z-order are exact-runtime capability gates. If either fails, core F6 may continue but IMG-03 remains `PARTIAL/OPEN`; never falsify AUTO PASS and never add a second engine merely to satisfy the checkbox.

## 3. Process state

F6 is architectural.

Completed gates:

1. read-only project exploration;
2. architecture alternatives;
3. conversational design section 1 approved;
4. conversational design section 2 approved;
5. conversational design section 3 approved;
6. conversational design section 4 approved;
7. formal written spec created + self-reviewed;
8. user explicitly approved the written spec with `continua`;
9. implementation plan created + self-reviewed.

Current gate: **user review/approval of the implementation plan and execution method**.

Do not start F6 code/TDD until that gate is approved.

## 4. Authoritative documents

Formal design spec:

`docs/superpowers/specs/2026-10-09-f6-images-design.md`

Implementation plan:

`docs/superpowers/plans/2026-10-09-f6-images.md`

The spec header may still contain the earlier phrase “written spec awaiting user review”; this checkpoint records the later user approval and is the authoritative process-state correction until normal documentation reconciliation updates that metadata line.

## 5. Approved architecture

### Real image objects only

`EDITAR` operates on actual `FPDF_PAGEOBJ_IMAGE` objects. No hidden white rectangles, page screenshots, flattening or silent raster-overlay fallback.

### Logical plan first

UI gestures mutate `ImageEditWorkspace`; no source PDF mutation occurs during mouse/keyboard interaction.

No native `IntPtr` is retained as durable UI identity.

Logical identity starts from `PageIndex + PageObjectIndex`, source fingerprint and original object descriptors. During materialization all edited original objects for a touched page must be resolved before any delete/z-order action can shift object indices.

### Single selection

Initial F6 edits one active image at a time. Multi-select, crop, copy/paste and advanced properties remain outside initial scope.

### Hit-test

Use real PDF geometry: proven rotated bounds when available or quadrilateral derived from affine matrix. Never rely blindly on AABB for rotated images.

Overlapping-object topmost behavior is used only if the pinned runtime proves enumeration/paint-order mapping; otherwise deterministic cycling.

### Transform/history

- move;
- proportional resize by default;
- Shift = free aspect resize;
- rotate around center;
- logical delete;
- command-based undo/redo;
- one continuous drag = one history entry.

After successful Save As:

- logical image states remain;
- original source remains active source;
- undo/redo clear;
- dirty=false;
- later saves rematerialize full logical state from unchanged original source.

### Extract/replace

- extraction: visually faithful PNG, not claimed original embedded bytes;
- replace: PNG/JPEG;
- replacement bytes captured into immutable in-memory asset;
- geometry unchanged initially;
- transparent PNG requires evidence; no flatten-to-white fallback.

### Writer

F6 does not reuse F5 page-import writer.

```text
validate fingerprint + preflight
→ fresh open original source
→ resolve all edited original objects on touched page
→ apply edits
→ GenerateContent once/touched page
→ save same-directory temp
→ close native handles
→ reopen temp
→ validate count/sizes/rotations + every page at 36 DPI sequentially
→ validate edited-object expectations
→ atomic publish
→ cleanup
```

Signatures/password-opened sources are Block. Source and valid existing destination survive any failed/cancelled output.

## 6. Plan decomposition

The implementation plan has 10 task gates:

1. **F6.1:** exact PDFium capability gate + image discovery.
2. **F6.1:** logical edit model + fingerprint + history.
3. **F6.2:** EDITAR surface + hit-test + selection overlay.
4. **F6.3:** move/resize/rotate/delete + undo/redo + dirty guard.
5. **F6.4:** PNG extraction + PNG/JPEG replacement assets.
6. **F6.5:** transactional writer + validator.
7. **F6.6:** opacity/z-order conditionally, only if exact gates pass.
8. **F6.7:** independent preservation matrix + warning policy + Save As UI.
9. **F6.7:** hardening/regressions/offline/temp safety.
10. **F6 closure:** docs/audit/stacked draft PR/exact-head CI; stop before F7.

Every implementation task is RED → minimum GREEN → full regression → exact-head Windows CI → checkpoint. Availability probes may legitimately pass immediately; they are probes, not fake TDD REDs. Behavior depending on them still receives a witnessed RED before production code.

## 7. Review-focus tests frozen in the plan

1. `Writer_TwoEditedImagesSamePage_DeleteAndReorder_ResolvesAllOriginalHandlesBeforeMutation`
2. `ReplacementAsset_SourceFileDeletedAfterSelection_SaveStillUsesCapturedBytes`
3. `SaveBaseline_SecondSaveFromOriginalReappliesPreviouslySavedLogicalEdits`
4. `HitTest_RotatedOverlappingImages_IsDeterministicAtMultipleZooms`
5. `SaveAs_ValidationFailure_WithExistingDestination_PreservesDestinationAndCleansTemp`

These are cross-task correctness anchors and must not be removed merely to make CI green.

## 8. Plan self-review corrections

The first plan draft was corrected before this checkpoint:

- exact-export availability tests are explicitly capability probes and are not mislabeled as mandatory RED;
- `ImageEditWorkspace` now exposes `SourcePath`, `SourceFingerprint` and `SourceOpenedWithPassword`, which the writer requires;
- optional opacity/z-order paths remain conditional;
- Save As UI is deliberately delayed until preservation policy is characterized;
- Task 6 writer resolves all original handles for a touched page before mutating object order;
- Task 10 truthfully allows F6 closure with IMG-03 `PARTIAL/OPEN` when an optional gate fails.

No placeholders/TODO implementation decisions are intentionally left in the plan. A missing core PDFium capability is a stop condition, not permission to improvise a second engine.

## 9. Stop conditions

Return to design if:

1. pinned PDFium lacks a core capability for detect/select/extract/replace/move/resize/rotate/delete;
2. image identity requires durable native handles;
3. normal PNG/JPEG replacement requires page flattening/raster overlay;
4. transactional output cannot protect source/prior destination;
5. credentials must be retained/reused;
6. core F6 requires a second engine;
7. WPF selection requires a large custom renderer;
8. preservation needs a generic PDF object-tree rewriter;
9. an F0–F5 regression cannot be narrowly isolated.

Opacity/z-order failure alone is not a stop condition for core F6.

## 10. Exact next step

Wait for user approval of `docs/superpowers/plans/2026-10-09-f6-images.md` and execution method.

Recommended execution in this chat/product: **inline/native executing-plans**, one task per user `continua`, because F6 tasks are sequential and depend on exact CI evidence from the previous gate.

On approval:

1. read `executing-plans`, TDD and verification skills;
2. create `feat/f6-images` from the approved planning head;
3. execute **Task 1 only**;
4. capability probe first, then behavior RED → GREEN;
5. exact-head Windows CI + Task-1 checkpoint;
6. STOP before Task 2.

Do not create the F6 PR until closure Task 10. Do not merge anything.
