# Phase 08 — F7 Texto V1 — Closure Plan

**Status:** AUTOMATED CLOSURE FINAL CHECKS  
**Branch:** `feat/f7-text-v1`  
**Base:** `feat/f6-images` @ `c6d762efca01d50bfe3932d1f05617190a464fc6`  
**Manual Windows QA:** NOT RUN

## Goal

Deliver conservative local editing of real top-level PDF text page objects inside the single EDITAR mode, preserving PDFium as the only editor engine and using an offline redistributable TTF fallback only when the original font route is not safe.

## Frozen boundaries

- Top-level `FPDF_PAGEOBJ_TEXT` only; no OCR, reflow or text inside Form XObjects.
- One EDITAR shell for images + text; no second mode, writer, guard chain or Save As flow.
- PDFium remains the only PDF editor engine; calls remain behind `PdfiumRuntime.NativeGate`.
- Managed persistent state; no durable native handles.
- Original font only under conservative Rune-based policy.
- Fallback uses pinned DejaVu Sans 2.37 through `FPDFText_LoadCidType2Font` with explicit `ToUnicode` + `CIDToGIDMap`.
- `FPDFText_LoadFont` is explicitly rejected as the F7 product fallback route.
- Source PDF is never overwritten by EDITAR.
- Save path is temp → reopen/validate/render → atomic publish.
- Cryptographic signatures and password-opened sources are hard Blocks.
- No cloud/account/API key/network runtime, no second PDF engine and no framework expansion.
- Manual Windows QA remains independent from automated PASS.

## Executed tasks

| Task | Slice | Status |
|---:|---|---|
| 1 | Exact PDFium text capability + Unicode fallback spike | AUTO PASS |
| 2 | Redistributable fallback font gate — DejaVu Sans 2.37 | AUTO PASS |
| 3 | Promote shared EDITAR infrastructure | AUTO PASS |
| 4 | Promote single EDITAR shell | AUTO PASS |
| 5 | Active-page top-level text discovery | AUTO PASS |
| 6 | Text + mixed image/text hit-testing | AUTO PASS |
| 7 | Conservative policy + TextEditWorkspace | AUTO PASS |
| 8 | Combined writer — OriginalFont route | AUTO PASS |
| 9 | Combined writer — CID Type2 fallback route | AUTO PASS |
| 10 | Combined PdfEditOutputValidator | AUTO PASS |
| 11 | Independent F7 preservation matrix + preflight | AUTO PASS |
| 12 | Texto V1 UI inside single EDITAR | AUTO PASS |
| 13 | Hardening, lifecycle, ownership and regressions | AUTO PASS |
| 14 | Docs, full audit and stacked draft PR closure | FINAL CHECKS |

## Functional closure evidence

Task-13 checkpoint: `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`.

Fresh exact-head Task-13 verification:

- workflow `38089770428`: PASS;
- locked restore PASS;
- Release build: 0 warnings / 0 errors;
- tests: 766 passed / 0 failed / 0 skipped.

F6→F7 pre-docs audit:

- 100 commits ahead / 0 behind;
- merge base exactly F6 closure;
- no new NuGet and no package lockfile change;
- the app `.csproj` only adds build/publish copying for the pinned TTF asset;
- PDFium remains the only PDF editor engine;
- no runtime network/cloud/service/account/API-key layer;
- only DejaVu Sans 2.37 is added as a runtime asset, with upstream/version/archive hash/TTF hash/license/purpose recorded in `third_party/manifest.json`.

## Capability and requirement truth

- TEXT-01 — AUTO PASS: active-page detection/selection of top-level text objects with deterministic mixed hit-testing.
- TEXT-02 — AUTO PASS: conservative in-place editing with read-only handling and managed candidate-first workspace.
- TEXT-03 — AUTO PASS: pinned redistributable DejaVu Sans fallback via explicit CID Type2 Unicode maps.
- TEXT-04 — AUTO PASS: basic properties plus combined save/reopen/validation before atomic publication.

## Preservation policy

See `docs/history/2026-10-10-F7-preservation-matrix.md`.

The real combined image+text writer independently proves preservation of representative:

- AcroForm + field value;
- bookmarks;
- named destinations + internal links;
- tagged structure;
- page labels;
- embedded attachments;
- metadata;
- page rotation.

These are `ProvenPreserved / Info` for the tested route. Cryptographic signatures and password-opened sources remain blocked.

## Task 14 closure sequence

1. Audit full F6→F7 diff and dependency/runtime boundaries.
2. Reconcile STATE/ROADMAP/REQUIREMENTS, phase plan and durable F7 history/checkpoint.
3. Keep manual QA explicitly NOT RUN.
4. Commit closure docs/checkpoint and require exact-head Windows CI PASS.
5. Open draft PR `F7 — Texto V1`, base `feat/f6-images`, head `feat/f7-text-v1`.
6. Require PR CI PASS on the expected F7 head/merge ref.
7. Verify PR remains draft/open/unmerged and `main` unchanged.
8. STOP F7; do not start F8 in this closure.

## Manual QA still required

- selection accuracy across zoom levels and page rotations;
- overlapping image/text and text/text selection through the real UI;
- unsupported render modes visibly read-only;
- TextBox typing, Ctrl+Z ownership and Apply validation UX;
- size/color controls and invalid input behavior;
- OriginalFont edits on representative PDFs;
- DejaVu fallback edits with Spanish/new code points;
- mixed image + text Save As on the same page;
- warning/block/cancel/error dialogs;
- large/heavy PDF responsiveness and memory behavior;
- network-disabled smoke;
- LEER/FIRMAR/ORGANIZAR/ZPL/F6-images end-to-end regression on a real Windows workstation.

## Next phase

After F7 automated closure, next permitted work is **F8 Comentarios design/spec only**. F8 implementation does not begin in the F7 closure gate.
