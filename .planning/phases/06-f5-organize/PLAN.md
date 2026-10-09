# Phase 06 — F5 Organizar — Closure Plan

**Status:** AUTOMATED CLOSURE FINAL CHECKS  
**Branch:** `feat/f5-organize`  
**Base:** `feat/f4-full-reader` @ `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`  
**Manual Windows QA:** NOT RUN

## Goal

Deliver a local-first ORGANIZAR workspace for page-level structural operations while keeping PDFium as the only PDF engine, protecting source files with Save As, and reporting preservation risk honestly.

## Frozen boundaries

- Plan-first; UI mutations do not rewrite the PDF.
- One page-reference model powers reorder/delete/duplicate/insert/merge/extract/split.
- Structural output is Save As only.
- PDFium native calls remain behind `PdfiumRuntime.NativeGate`.
- Cryptographically signed and password-opened sources are blocked.
- Unknown/changed document-level preservation is never presented as preserved.
- No second engine, cloud, account, API key, network runtime, generic object graph or dependency expansion.
- LEER, FIRMAR and ZPL remain separate surfaces.

## Executed tasks

| Task | Slice | Status |
|---:|---|---|
| 0 | Fix Navigation serialization to use global `SemaphoreSlim` gate | AUTO PASS |
| 1 | Verify pinned PDFium organize APIs + protected-session marker | AUTO PASS |
| 2 | `OrganizePlan`, sources/pages, move/rotate/delete/duplicate/insert operations | AUTO PASS |
| 3 | Structural preflight contracts/detectors | AUTO PASS |
| 4 | Transactional writer + reopen/render validator | AUTO PASS |
| 5 | Dedicated organize surface + lazy/bounded thumbnails | AUTO PASS |
| 6 | Windows selection, drag reorder, rotate/delete/duplicate, Save As | AUTO PASS |
| 7 | Candidate-first insert ranges + merge/append | AUTO PASS |
| 8 | Extract + split + deterministic naming + batch publisher | AUTO PASS |
| 9 | Representative preservation fixtures + matrix | AUTO PASS |
| 10 | Dirty guard, full regression/audit/docs/draft PR closure | FUNCTIONAL GREEN; final docs/PR checks pending |

## Functional closure evidence

Before closure docs:

- functional head `d92cd0cc60fb7de9a3d9d595bdfd0468ee38ff8a`;
- CI `37990505245` attempt 2 PASS;
- Release build: 0 warnings / 0 errors;
- tests: 550 passed / 0 failed / 0 skipped;
- F4→F5 compare: 82 ahead / 0 behind;
- no `.csproj` or lockfile changes;
- no second PDF engine or runtime network layer.

The first CI attempt on the same SHA had one intermittent existing secondary-thumbnail `Assert.NotNull` failure; exact-SHA rerun passed 550/550 with no code changes. It is tracked as test flakiness, not product failure.

## Preservation policy

See `docs/history/2026-10-09-F5-preservation-matrix.md`.

- Cryptographic signature → Block / no override.
- Password-opened source → Block / no override.
- Form, bookmark, named destination, internal link, tagged structure, page label, attachment and original metadata → representative output evidence shows changed/lost with the current page-import writer; Warning + explicit confirmation.

## Task 10 closure sequence

1. Dirty guard TDD GREEN.
2. Fresh full Windows CI GREEN.
3. Audit complete F4→F5 diff.
4. Reconcile STATE/ROADMAP/REQUIREMENTS/spec/history.
5. Push exact docs head and require CI PASS.
6. Open draft PR `F5 — Organizar PDF`, base `feat/f4-full-reader`, head `feat/f5-organize`.
7. Require PR CI PASS on exact head.
8. Create final Task-10 checkpoint; require final exact-head push + PR CI PASS.
9. Verify PR remains draft/open/unmerged and `main` unchanged.
10. STOP F5.

## Manual QA still required

Automated PASS does not close real Windows QA. Still NOT RUN:

- large-document organize scrolling/thumbnail memory/perceived performance;
- Ctrl/Shift selection and drag/drop UX;
- rotate/delete/duplicate behavior through real UI;
- Save As replacement/cancel/error flows;
- insert/merge from representative real PDFs;
- extract/split multi-output flow;
- signed/password/warning dialogs;
- offline run with network disabled;
- regression smoke through LEER, FIRMAR and ZPL on a real Windows workstation.

## Next phase

After F5 closure, next permitted work is **F6 Images design/spec only**. No F6 implementation begins without its approved design and TDD plan.
