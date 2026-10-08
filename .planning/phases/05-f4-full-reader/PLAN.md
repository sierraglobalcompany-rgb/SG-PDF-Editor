# Phase 5 — F4 Full Reader

**Status:** design + implementation plan approved; Tasks 1–4 automated PASS; Task 5 next.  
**Branch:** `feat/f4-full-reader`  
**Base:** F3.4 closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`

## Sources

- Design spec: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md` — approved.
- TDD implementation plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md` — approved.
- Runtime/status truth: `.planning/STATE.md` + GitHub exact-head CI.

## Frozen Architecture

- PDFium only; required F4 exports verified against the pinned runtime in Task 1.
- One `PdfDocumentSession` per current document.
- `LEER` = continuous virtualized reader.
- `FIRMAR` = existing single `PdfImage` + `SignatureEditState` editor.
- Existing `PdfScrollViewer` remains the legacy single-page host for FIRMAR and the established ZPL path.
- Full-resolution reader bitmap retention = visible pages + one neighbor each side.
- Lazy thumbnails; no eager whole-document render.
- Search/copy via PDFium; no OCR in F4.
- One-page text drag selection.
- Bookmarks read-only, cycle-safe, max 10,000 nodes/depth 128.
- Only internal GOTO + confirmed HTTP/HTTPS URI links execute.
- Passwords never persisted.
- Recents max 10, LocalAppData atomic JSON, no startup/menu path probing.
- No tabs, database, WebView2, second PDF engine or app-wide MVVM/DI.

## Task Order

1. **DONE / AUTO PASS** — Native capability gate + pure page/layout primitives.
   - Capability commit `adb4b0254fdc140ce21f503bd11297ae3cd6b5d3`.
   - RED head `ad676a933065eafd2e4ece816d026c8e651e26bb`: 10 expected failures; build clean; 302 existing tests PASS.
   - GREEN functional head `ce167be7ae0b7769c4fc9253c400bc5a9b59216a`.
   - CI `37843337925`: PASS; 312 tests; 0 failures; build 0 warnings / 0 errors.
2. **DONE / AUTO PASS** — Continuous WPF surface + bounded rendering + FIRMAR boundary.
   - RED head `91b8d9b97a1d331207d4ecd478b4bce8baddad9d`.
   - RED CI `37845233691`: expected failure; build clean; 16 new tests failed while 312 prior tests passed.
   - Functional/verified head `bfbc774066f91acafc960c8e7f77ba0df5f344f6`.
   - CI `37852149659`: PASS; 328 tests; 0 failures; build 0 warnings / 0 errors.
   - Delivered continuous virtualized LEER, visible+neighbor sequential rendering, stale-result suppression, per-page error isolation, current-page navigation/zoom and bridge into existing FIRMAR; existing ZPL behavior preserved.
3. **DONE / AUTO PASS** — Lazy thumbnails + left navigation.
   - Final RED head `c4315a10c05f0b21badd6307e47767f157a8cc87`.
   - RED CI `37854879113`: expected build failure only for missing `ReaderThumbnailItem`; 0 warnings.
   - Functional/verified head `c968aa8e94f8b22820e3aa32ba1097d0e58516a2`.
   - CI `37855511813`: PASS; 336 tests; 0 failures; build 0 warnings / 0 errors.
   - Delivered ~132 px lazy thumbnails, Páginas/Marcadores left navigation, recycling/pixel virtualization, realized+neighbor bounded bitmap retention, separate sequential thumbnail scheduler, stale-result suppression, isolated thumbnail failure, and bidirectional navigation synchronization.
4. **DONE / AUTO PASS** — Password-protected PDFs.
   - RED head `a99287dc81623b742032ab234c6d1e37aa4d88b4`.
   - RED CI `37856357399`: expected build failure with 9 missing-contract errors for typed open error/dialog; 0 warnings.
   - Functional/verified head `cca16e38daff1509a75d1324ac89b53fab8ee9d8`.
   - CI `37856882768`: PASS; 346 tests; 0 failures; build 0 warnings / 0 errors.
   - Delivered authoritative `FPDF_ERR_PASSWORD (4)` classification, typed open errors, masked local WPF password entry, retry/cancel flow and candidate-first preservation of the prior workspace.
   - Privacy: password is transient attempt/dialog data only; no password field in `PdfDocumentSession`, recents/settings/JSON/status/title persistence or new diagnostics.
   - Scope vs Task 3 closure: password/open path and tests only; no package/lock, F3 writer/model, ZPL, print, recents, database or network changes.
5. **NEXT** — PDFium text core + find navigation.
6. One-page text selection + clipboard copy.
7. Bookmarks + safe explicit PDF links.
8. Shortcuts + atomic recent files + offline hardening.
9. Closure audit/docs + exact-head CI + draft stacked PR.

Each task is RED → confirm expected failure → minimal GREEN → focused regression → full exact-head CI. Project cadence is one task per user `continúa` unless the user explicitly changes it.

## Review Focus

- stale render publication after rapid scroll/zoom — covered by Task 2 automated PASS;
- isolated per-page render failure — covered by Task 2 automated PASS;
- lazy thumbnail range/stale publication — covered by Task 3 automated PASS;
- wrong/cancelled password preserving prior workspace and password non-persistence — covered by Task 4 automated PASS;
- cyclic/deep/oversized bookmark outline;
- UNC/stale recent paths never probed merely by opening the menu.

## Stop Conditions

Return to design before continuing if implementation appears to require:

- replacing PDFium or adding a second PDF SDK;
- a missing required PDFium export with no KISS equivalent in the pinned runtime;
- moving F3 signing into a new multi-page placement model;
- password persistence;
- unsafe PDF action execution;
- unbounded full-document bitmap/thumbnail caching;
- custom scrolling engine before standard WPF virtualization is proven insufficient;
- app-wide framework/refactor work;
- network service/runtime dependency;
- F5+ organization/editing scope.

## Manual QA

Manual Windows performance/UX/offline QA is a separate gate and remains **NOT RUN** until actually executed. Automated PASS never implies physical/manual PASS. Protected-PDF prompt/retry/cancel/printing QA is also still **NOT RUN** physically.

## Current Gate

```text
Tasks 1–4 AUTO PASS
→ next user `continúa`
→ Task 5 RED for PDFium text extraction/search + pure navigator + find-bar behavior
→ confirm expected RED
→ minimal GREEN PDFium text core + on-demand find navigation
→ exact-head full CI
→ report evidence and stop before Task 6
```
