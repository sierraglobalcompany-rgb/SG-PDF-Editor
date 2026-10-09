# F6 — Imágenes — DESIGN CHECKPOINT

**Date:** 2026-10-09  
**Status:** conversational design APPROVED; formal written spec awaiting user review  
**Branch:** `design/f6-images`  
**Base:** exact F5 automated-closure head `327d7064c14131e603e3bce6947b593a10f46363`  
**No F6 product code has been implemented.**

## 1. Frozen prior state

F5 `ORGANIZAR` remains closed at automated level only.

- F5 head: `327d7064c14131e603e3bce6947b593a10f46363`.
- Draft PR #24: open / draft / unmerged.
- F5 push CI `37991847671`: PASS.
- F5 PR CI `37991854689`: PASS.
- Build 0 warnings / 0 errors; 550/550 tests PASS.
- `main` remains untouched at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- Real/manual Windows F5 QA remains NOT RUN.

F6 design work is isolated on `design/f6-images` and must not modify PR #24 or merge anything.

## 2. F6 requirements

- **IMG-01:** detect/select real PDF images and contextual actions.
- **IMG-02:** extract/save and replace while preserving geometry where viable.
- **IMG-03:** move/resize/rotate/opacity/z-order/delete.
- **IMG-04:** undo/redo.

## 3. Process state

F6 is architectural. The user approved all four conversational design sections.

The formal specification now exists at:

`docs/superpowers/specs/2026-10-09-f6-images-design.md`

Current spec commit after self-review corrections:

`558ca8d6c2dff985750a625ab809adbbe742f7ec`

The next gate is **user review/approval of the written spec**. Do not create an implementation plan or product code until that approval occurs.

## 4. Approved architecture

### Real PDF image objects only

`EDITAR` operates on `FPDF_PAGEOBJ_IMAGE` objects. No hidden white rectangles, page screenshots or silent raster overlays are allowed as fallbacks.

### Logical plan first

UI gestures mutate in-memory logical image state. Native PDF mutation occurs only during `Guardar como...`.

Conceptual state:

```text
ImageEditPlan
ImageObjectRef(PageIndex, PageObjectIndex, OriginalBounds, OriginalMatrix, OriginalImageMetadata)
ImageEditState(CurrentMatrix, ReplacementAsset?, Opacity?, ZOrderOperation?, Deleted)
```

No native `IntPtr` is retained as durable UI identity.

### Single selection

Initial F6 edits one active image at a time. Multi-select, crop, copy/paste and advanced properties are out of initial scope.

### Hit-test

Use real PDF geometry. Prefer proven rotated/quad bounds; otherwise derive geometry from the affine image matrix. Axis-aligned fallback is acceptable only if fixtures demonstrate acceptable behavior.

Overlapping-image topmost selection is not assumed; it must be characterized against the pinned runtime or replaced by deterministic cycling.

### Transforms

- move;
- proportional resize by default;
- `Shift` permits free-aspect resize;
- rotate around visual center;
- delete logically until save.

### Undo/redo

Command-based history for every enabled mutation. A continuous drag becomes one history item. New mutation after undo clears redo.

After successful Save As:

- current logical image states remain;
- original source remains the active source;
- undo/redo clear;
- dirty becomes false;
- later saves rematerialize the complete current logical state from the unchanged original source.

### Extract

Initial extraction is visually faithful PNG, not a promise of exact original embedded bytes.

### Replace

Initial replacement input: PNG and JPEG/JPG. Replacement bytes are captured into an immutable in-memory asset so later save does not depend on the selected replacement file remaining unchanged. Geometry stays unchanged initially.

Transparent PNG must pass save/reopen/render evidence or that subcase is blocked; no silent white flattening.

## 5. Capability gates

The exact pinned `pdfium.dll` is the authority. Public/upstream API availability is only a candidate route.

Core F6.1 must prove:

1. page-object count/get;
2. image type detection;
3. bounds/matrix access;
4. extraction bitmap route;
5. bitmap replacement;
6. JPEG route if selected;
7. matrix mutation for move/resize/rotate;
8. object removal;
9. page content regeneration;
10. whole-document save/reopen after mutation.

Conditional gates:

- opacity;
- exact-index insertion/reordering for z-order;
- tighter rotated bounds;
- rendered image-object bitmap extraction.

Opacity and z-order failure do not block the rest of F6, but **IMG-03 may not be marked AUTO PASS** if either remains unsupported.

No second engine or destructive fallback is introduced merely to turn those capabilities green.

## 6. Save/materialization

F6 does not reuse F5's page-import writer as its normal writer.

Pipeline:

```text
validate fingerprint + preflight
→ reopen original source fresh
→ resolve all edited objects per touched page
→ apply native edits
→ FPDFPage_GenerateContent on touched pages
→ save to destination-directory temp
→ close native handles
→ reopen temp
→ validate every output page at 36 DPI sequentially + edited-object expectations
→ atomic publish
→ cleanup
```

The source is never overwritten. Existing destination is preserved on failure.

## 7. Safety/preflight

- cryptographic signatures: Block, no override;
- password-opened source: Block, no password persistence/reuse;
- stale/missing source: Block;
- object mismatch: Block;
- invalid replacement/transparency failure/native failure/save failure/validation failure/cancel/publication failure: no source mutation and no invalid destination publication.

F6 gets its own preservation matrix because its writer differs from F5. Representative fixtures must classify forms, bookmarks, destinations, links, tagged structure, page labels, attachments and metadata as `PROVEN PRESERVED`, `PROVEN CHANGED/LOST` or `UNKNOWN`.

## 8. Approved slices

1. **F6.1** capability gate + model.
2. **F6.2** selection + hit-test + overlay.
3. **F6.3** move/resize/rotate/delete + undo/redo + dirty state.
4. **F6.4** extract + replace.
5. **F6.5** transactional writer.
6. **F6.6** opacity + z-order only when their gates pass.
7. **F6.7** preservation + hardening + regression + closure.

## 9. Current exact next step

Stop at the written-spec review gate.

If the user approves `docs/superpowers/specs/2026-10-09-f6-images-design.md`, then and only then:

1. read the writing-plans skill;
2. create a detailed F6 implementation plan preserving F6.1–F6.7 boundaries;
3. commit the plan on the design/planning branch;
4. stop again for plan review/execution-method approval;
5. do not start TDD implementation in the same step.

No F6 code, tests, native bindings or dependency changes have been created yet.
