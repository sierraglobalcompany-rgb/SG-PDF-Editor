# Phase 5 — F4 Full Reader

**Status:** design + implementation plan approved; Tasks 1–8 automated PASS; Task 9 closure audit/docs complete, final exact-head CI + draft PR/PR-CI pending.  
**Branch:** `feat/f4-full-reader`  
**Base:** F3.4 closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`

## Sources

- Design spec: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md` — approved.
- TDD implementation plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md` — approved.
- Runtime/status truth: `.planning/STATE.md` + GitHub exact-head CI.
- Closure history: `docs/history/2026-10-08-F4.md`.

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
- One-page text drag selection; PDF text rectangles remain durable interaction geometry.
- Bookmarks read-only, cycle-safe, max 10,000 nodes/depth 128.
- Only internal GOTO + confirmed HTTP/HTTPS URI links execute.
- Passwords never persisted.
- Recents max 10, LocalAppData atomic JSON, no startup/menu path probing.
- No tabs, database, WebView2, second PDF engine or app-wide MVVM/DI.

## Task Order

1. **DONE / AUTO PASS** — Native capability gate + pure page/layout primitives. `ce167be7...`; CI `37843337925`; 312 tests.
2. **DONE / AUTO PASS** — Continuous WPF surface + bounded rendering + FIRMAR boundary. `bfbc7740...`; CI `37852149659`; 328 tests.
3. **DONE / AUTO PASS** — Lazy thumbnails + left navigation. `c968aa8e...`; CI `37855511813`; 336 tests.
4. **DONE / AUTO PASS** — Password-protected PDFs. `c27dfa73...`; CI `37857787178`; 346 tests.
5. **DONE / AUTO PASS** — PDFium text core + find navigation. `984545a8...`; CI `37867206589`; 368 tests.
6. **DONE / AUTO PASS** — One-page text selection + clipboard copy. `0a5a8cff...`; CI `37870778333`; 379 tests.
7. **DONE / AUTO PASS** — Bookmarks + safe explicit PDF links. `e85f8c78...`; CI `37872707082`; 399 tests.
8. **DONE / AUTO PASS** — Shortcuts + atomic recent files + offline hardening. `02c979f2...`; CI `37875513602`; 417 tests. Task-8 docs checkpoint `c36a8630...`; CI `37875773158` PASS.
9. **IN PROGRESS — closure**
   - [x] verify F4 and `main` refs before modifying closure docs;
   - [x] verify pre-closure exact-head Windows CI (`37875773158`) including locked restore, Release build and tests;
   - [x] audit full diff exact F3.4 closure → F4 pre-closure head;
   - [x] confirm no new runtime package/second PDF engine/WebView-network service/tabs/eager full-document cache/password persistence/unsafe PDF action execution;
   - [x] confirm no `Features/Sign` or `Features/Labels` file changes and preserve narrow FIRMAR/ZPL integration boundaries;
   - [x] reconcile `.planning/STATE.md`, `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md`, this phase plan;
   - [x] create `docs/history/2026-10-08-F4.md`;
   - [x] reconcile READER-01..07 as AUTO PASS only from implemented evidence;
   - [x] keep real Windows/manual QA explicitly **NOT RUN**;
   - [ ] exact-head CI PASS for closure docs SHA;
   - [ ] open draft PR base `feat/f3-4-local-signature-library` → head `feat/f4-full-reader`, title `F4 — Full Reader`;
   - [ ] PR CI PASS on the same final SHA;
   - [ ] verify `main` still `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
   - [ ] STOP without merge.

## Task 9 Full-Branch Audit Result

Audit range: `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b..c36a8630e685cb143b312a0f260301b87a62bbd5`.

- Compare status: **118 ahead / 0 behind**.
- No product/test `.csproj` or lockfile changes.
- No changes inside `src/SGPdf.App/Features/Sign/`.
- No changes inside `src/SGPdf.App/Features/Labels/`.
- Main product changes are Reader feature files, PDFium reader primitives, narrow `MainWindow` integration, tests and docs/planning.
- MainWindow integration explicitly gates the continuous reader out of signature mode and ZPL mode; existing legacy surface remains available.
- `PdfDocumentSession.Navigation` maps only GOTO/URI to actionable models; unsupported action types remain `Unsupported`.
- `MainWindow.ReaderLinks` activates only valid internal destinations or absolute HTTP/HTTPS URI links after confirmation.
- Offline/WebView2 regression tests are green at the pre-closure head.
- No evidence of F5+ product scope in the diff.

No critical/high bug was found by the Task 9 source/diff audit.

## Manual QA

Manual Windows performance/UX/offline QA remains a separate gate and is **NOT RUN** until actually executed. Automated PASS never implies physical/manual PASS. Protected-PDF, real text-search/Unicode, real mouse selection/copy/zoom, real bookmark/link confirmation, and real shortcuts/recents/offline UX QA remain **NOT RUN**.

## Stop Conditions

Return to design before continuing if work appears to require replacing PDFium, adding a second PDF SDK, moving F3 into a new placement model, persisting passwords, executing unsafe PDF actions, unbounded caches, a custom scrolling engine without evidence, app-wide framework refactor, required network runtime, or F5+ editing scope.

## Current Gate

```text
Task 9 audit/docs complete
→ closure docs exact-head CI PASS
→ draft stacked PR
→ PR CI PASS on same final SHA
→ verify main unchanged
→ F4 automated closure PASS
→ STOP — no merge
```
