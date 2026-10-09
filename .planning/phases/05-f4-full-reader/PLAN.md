# Phase 5 — F4 Full Reader

**Status:** design + implementation plan approved; Tasks 1–7 automated PASS; Task 8 next.  
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
- Search is on demand page-by-page; no permanent whole-document index.
- One-page text drag selection; PDF text rectangles remain the durable interaction geometry.
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
3. **DONE / AUTO PASS** — Lazy thumbnails + left navigation.
   - Final RED head `c4315a10c05f0b21badd6307e47767f157a8cc87`.
   - RED CI `37854879113`: expected build failure only for missing `ReaderThumbnailItem`; 0 warnings.
   - Functional/verified head `c968aa8e94f8b22820e3aa32ba1097d0e58516a2`.
   - CI `37855511813`: PASS; 336 tests; 0 failures; build 0 warnings / 0 errors.
4. **DONE / AUTO PASS** — Password-protected PDFs.
   - RED head `a99287dc81623b742032ab234c6d1e37aa4d88b4`.
   - RED CI `37856357399`: expected build failure with 9 missing-contract errors; 0 warnings.
   - Final verified head `c27dfa738d946147bec5cb48c4e7ab2afde776bc`.
   - CI `37857787178`: PASS; 346 tests; 0 failures; build 0 warnings / 0 errors.
   - Delivered authoritative `FPDF_ERR_PASSWORD (4)` classification, masked local WPF password entry, retry/cancel and candidate-first preservation; passwords never persisted.
5. **DONE / AUTO PASS** — PDFium text core + find navigation.
   - RED head `b1e6f68922d52c92a76a93de23301cf3096872a9`.
   - RED CI `37866817670`: expected build failure with 6 missing `PdfTextMatch` / `ReaderSearchCursor` contract errors; 0 warnings.
   - Functional/verified head `984545a8e8b6b098a55dad55b5059aa4c6020b3b`.
   - CI `37867206589`: PASS; 368 tests; 0 failures; build 0 warnings / 0 errors.
   - Delivered PDFium text search/range/hit-test/rect primitives behind `NativeGate`, Unicode extraction, pure next/previous/wrap navigator, compact Ctrl+F find bar, active-result-only highlight and stale-generation rejection.
   - Search remains on demand page-by-page with no permanent index/global results cache and no OCR.
6. **DONE / AUTO PASS** — One-page text selection + clipboard copy.
   - Clean contract RED head `3e0ef221dcc40a98ec9337bdea725f2d5ce8b0e0`.
   - RED CI `37870209624`: expected build failure with 8 missing selection-model/range errors; 0 warnings.
   - Strengthened zoom RED head `670a9d8416f5ec29b0456f8dead7b171e07149d5`.
   - Zoom RED CI `37870595171`: build clean; 378 tests PASS and one expected failure proving page geometry did not yet automatically reproject selection.
   - Functional/verified head `0a5a8cff5677956b3498ec333c5b4d76f59e0180`.
   - CI `37870778333`: PASS; 379 tests; 0 failures; build 0 warnings / 0 errors.
   - Delivered inclusive forward/back text range normalization, one-page drag selection, cross-page rejection, click-to-clear, PDF-coordinate rectangle overlay, geometry/zoom reprojection and exact Unicode Ctrl+C.
   - Entering FIRMAR clears reader selection only; signature state is unchanged. Selection gestures are marked handled before Task 7 link activation.
   - Coordinate ruling: uses each page's `PdfPageDeviceTransform`; does not reuse Sign `PdfRect`, `SignatureCoordinateMapper` or signature overlay state.
   - KISS: integration stayed in `MainWindow.ReaderSelection.cs`; no `MainWindow.Reader.cs` edit was needed because `ReaderPageItem.PropertyChanged` exposes geometry changes directly.
   - Scope vs Task 5 closure: exactly 4 Task 6 files; no PDFium binding, package/lock, F3, ZPL, print, persistence/database/network or bookmark/link changes.
7. **DONE / AUTO PASS** — Bookmarks + safe explicit PDF links.
   - Clean contract RED head `502ed9ea3722bd8eb45d796e51d9f57a5969e03d`.
   - RED CI `37872082122`: expected build failure with exactly 2 missing navigation-model errors; 0 warnings.
   - Intermediate GREEN CI `37872367149`: compile failed only on three localized integration mistakes; no runtime/safety redesign required.
   - Functional/verified head `e85f8c783eaf327c6e133fef172aeda198cfe1bf`.
   - CI `37872707082`: PASS; 399 tests; 0 failures; build 0 warnings / 0 errors.
   - Delivered `PdfBookmarkNode` / `PdfPageLink`, read-only PDFium bookmark/action/link extraction behind `NativeGate`, UTF-16 bookmark titles, strict bounded UTF-8 URI extraction, cycle/repeated-handle guard, max depth 128, max 10,000 accepted nodes and cancellation.
   - Existing `ReaderBookmarksTree` is populated hierarchically; valid bookmarks and internal GOTO annotations navigate only inside the current PDF.
   - URI actions are accepted only when absolute `http`/`https`; the exact URL is confirmed before the OS-browser seam. Launch, JavaScript, remote GOTO, file/shell and other/non-HTTP actions never execute and are unsupported/no-op.
   - Link metadata is loaded lazily for visible pages, stored as PDF-coordinate rectangles and reprojected on geometry/zoom changes. The link overlay is non-hit-testable; click-like motion is required so text-selection drag wins.
   - Scope vs Task 6 closure `a54a299371b2ca745e493499ffa284850ed1b9c6`: exactly 8 Task 7 code/test files; no package/lock, F3, ZPL, print, recents, database or required-network changes.
8. **NEXT** — Shortcuts + atomic recent files + offline hardening.
9. Closure audit/docs + exact-head CI + draft stacked PR.

Each task is RED → confirm expected failure → minimal GREEN → focused regression → full exact-head CI. Project cadence is one task per user `continúa` unless the user explicitly changes it.

## Review Focus

- stale render publication after rapid scroll/zoom — covered by Task 2 automated PASS;
- isolated per-page render failure — covered by Task 2 automated PASS;
- lazy thumbnail range/stale publication — covered by Task 3 automated PASS;
- wrong/cancelled password preserving prior workspace and password non-persistence — covered by Task 4 automated PASS;
- stale search generation, Unicode extraction and active-match-only highlight — covered by Task 5 automated PASS;
- one-page selection range, cross-page rejection, zoom reprojection, Unicode clipboard and future-link precedence — covered by Task 6 automated PASS;
- cyclic/deep/oversized bookmark outline, unsupported action no-op and confirmed HTTP/HTTPS only — covered by Task 7 automated PASS;
- UNC/stale recent paths never probed merely by opening the menu.

## Stop Conditions

Return to design before continuing if implementation appears to require:

- replacing PDFium or adding a second PDF SDK;
- a missing required PDFium export with no KISS equivalent in the pinned runtime;
- moving F3 signing into a new multi-page placement model;
- password persistence;
- unsafe PDF action execution;
- unbounded full-document bitmap/thumbnail/search caching;
- custom scrolling engine before standard WPF virtualization is proven insufficient;
- app-wide framework/refactor work;
- network service/runtime dependency;
- F5+ organization/editing scope.

## Manual QA

Manual Windows performance/UX/offline QA is a separate gate and remains **NOT RUN** until actually executed. Automated PASS never implies physical/manual PASS. Protected-PDF, real text-search/Unicode, real mouse text-selection/copy/zoom, and real bookmark/internal-link/external-link confirmation QA are still **NOT RUN** physically.

## Current Gate

```text
Tasks 1–7 AUTO PASS
→ next user `continúa`
→ Task 8 RED for keyboard shortcuts + atomic max-10 recents + no menu/startup path probing
→ confirm expected RED
→ minimal GREEN shortcuts/recents/offline hardening
→ exact-head full CI
→ report evidence and stop before Task 9 closure
```
