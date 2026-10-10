# Phase 07 — F6 Imágenes — Closure Plan

**Status:** AUTOMATED CLOSURE FINAL CHECKS  
**Branch:** `feat/f6-images`  
**Base:** `feat/f5-organize` @ `327d7064c14131e603e3bce6947b593a10f46363`  
**Manual Windows QA:** NOT RUN

## Goal

Deliver a local-first image editor for real PDF image page objects: detect/select, extract/replace, transform, opacity/z-order, undo/redo and transactional Save As, without raster overlays, a second PDF engine or durable native handles.

## Frozen boundaries

- Real `FPDF_PAGEOBJ_IMAGE` objects only.
- One active image selection; no initial crop or cross-PDF image copy/paste.
- Logical workspace first; native mutation only during Save As.
- PDFium is the F6 PDF engine; calls remain behind `PdfiumRuntime.NativeGate`.
- Source PDF is never overwritten by EDITAR.
- Save path is temp → reopen/render/validate → atomic publish.
- Cryptographic signatures and password-opened sources are hard Blocks.
- No generic PDF object graph, cloud/account/API key/network runtime or dependency expansion.
- Manual Windows QA is independent from automated PASS.

## Executed tasks

| Task | Slice | Status |
|---:|---|---|
| 1 | Exact PDFium capability gate + image discovery | AUTO PASS |
| 2 | Logical edit model, fingerprint and history | AUTO PASS |
| 3 | EDITAR surface, selection, hit-test and overlay | AUTO PASS |
| 4 | Move/resize/rotate/delete + undo/redo + dirty guard | AUTO PASS |
| 5 | PNG extraction + PNG/JPEG replacement assets | AUTO PASS |
| 6 | Transactional writer + validator | AUTO PASS |
| 7 | Opacity + z-order exact-runtime capability paths | AUTO PASS |
| 8 | Independent preservation matrix + Save As warning policy | AUTO PASS |
| 9 | Hardening, regressions, offline/temp/memory safety | AUTO PASS |
| 10 | Docs, full audit and stacked draft PR closure | FINAL CHECKS |

## Functional closure evidence

Functional head: `7f70a68b2148504888040483a4d9a9e9dd6b4a03`.

Fresh Task-10 rerun:

- workflow `38057258528`, attempt 2: PASS;
- locked restore PASS;
- Release build: 0 warnings / 0 errors;
- tests: 680 passed / 0 failed / 0 skipped.

Task-9 durable checkpoint head before Task-10 docs: `e1dfc5e41b17fa8444a02ebab7cbfabce741955f`.

F5→F6 pre-docs audit:

- 64 commits ahead / 0 behind;
- merge base exactly F5 closure;
- no `.csproj` or lockfile changes;
- no second PDF engine;
- no runtime network/cloud layer;
- no generic object graph or unrelated architecture expansion.

## Capability and requirement truth

- IMG-01 — AUTO PASS.
- IMG-02 — AUTO PASS.
- IMG-03 — AUTO PASS: move/resize/rotate/delete plus opacity and z-order all passed exact-runtime and save/reopen evidence.
- IMG-04 — AUTO PASS.

Optional capability verdicts on pinned PDFium:

- opacity: PROVEN AVAILABLE;
- exact-index z-order: PROVEN AVAILABLE;
- rotated bounds route: available where used;
- rendered image-object bitmap route: available with fallback characterized.

## Preservation policy

See `docs/history/2026-10-09-F6-preservation-matrix.md`.

Representative F6 writer fixtures independently prove preservation of:

- forms;
- bookmarks;
- named destinations;
- internal links;
- tagged structure;
- page labels;
- attachments;
- representative metadata values.

These are `ProvenPreserved / Info` for the tested route. Cryptographic signatures and password-opened sources are `Unknown / Block` and never materialized.

## Task 10 closure sequence

1. Fresh exact functional-head Windows verification.
2. Audit full F5→F6 diff.
3. Reconcile STATE/ROADMAP/REQUIREMENTS and durable F6 history.
4. Keep manual QA explicitly NOT RUN.
5. Commit closure docs/checkpoint and require exact-head push CI PASS.
6. Open draft PR `F6 — Imágenes`, base `feat/f5-organize`, head `feat/f6-images`.
7. Require exact-head PR CI PASS.
8. Verify PR remains draft/open/unmerged and `main` unchanged.
9. STOP F6; do not start F7 in the same closure.

## Manual QA still required

- selection accuracy across zoom levels;
- rotated/overlapping real images;
- mouse move/resize/rotate feel;
- keyboard shortcuts and mode transitions;
- Save As warning/block/cancel/error dialogs;
- extracted PNG opened externally;
- PNG/JPEG replacements with representative real assets;
- transparent PNG rendering;
- opacity and z-order UI behavior;
- large/heavy PDF responsiveness and memory behavior;
- network-disabled smoke;
- LEER/FIRMAR/ORGANIZAR/ZPL end-to-end regression on a real Windows workstation.

## Next phase

After F6 closure, next permitted work is **F7 Text V1 design/spec only**. No F7 implementation begins until a dedicated design and implementation plan are approved.
