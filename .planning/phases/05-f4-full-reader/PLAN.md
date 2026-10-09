# Phase 5 — F4 Full Reader

**Status:** **AUTOMATED CLOSURE PASS**; final docs-only exact-head push + PR checks must remain green. Manual Windows QA remains NOT RUN.  
**Branch:** `feat/f4-full-reader`  
**Base:** F3.4 closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`  
**Draft PR:** #23 `F4 — Full Reader`, open/unmerged

## Sources

- Design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.
- TDD plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md`.
- Runtime/status truth: `.planning/STATE.md` + GitHub exact-head CI.
- Closure history: `docs/history/2026-10-08-F4.md`.

## Frozen Architecture

- PDFium only; one current `PdfDocumentSession`.
- `LEER` continuous virtualized; `FIRMAR` existing single-page `PdfImage` + `SignatureEditState`.
- Existing `PdfScrollViewer` retained as legacy FIRMAR/ZPL host.
- Full-resolution retention = visible + one neighbor each side; thumbnails lazy.
- Search page-by-page via PDFium; no OCR/permanent index.
- One-page text drag selection.
- Bookmarks bounded/cycle-safe; only internal GOTO + confirmed HTTP/HTTPS URI links execute.
- Passwords never persisted.
- Recents max 10, atomic LocalAppData JSON, no startup/menu target probing.
- No tabs, database, WebView2, second PDF engine or app-wide MVVM/DI.

## Task Order

1. **DONE / AUTO PASS** — capability/layout — CI `37843337925`.
2. **DONE / AUTO PASS** — continuous reader/FIRMAR boundary — CI `37852149659`.
3. **DONE / AUTO PASS** — lazy thumbnails — CI `37855511813`.
4. **DONE / AUTO PASS** — password PDFs — CI `37857787178`.
5. **DONE / AUTO PASS** — Unicode search — CI `37867206589`.
6. **DONE / AUTO PASS** — one-page selection/copy — CI `37870778333`.
7. **DONE / AUTO PASS** — bookmarks/safe links — CI `37872707082`.
8. **DONE / AUTO PASS** — shortcuts/atomic recents/offline hardening — CI `37875513602`; docs checkpoint CI `37875773158`.
9. **DONE / AUTO PASS — closure**
   - [x] verify F4 and `main` refs;
   - [x] audit exact F3.4 closure → F4 full diff;
   - [x] confirm no package/second engine/WebView-network service/tabs/eager full-doc cache/password persistence/unsafe actions;
   - [x] confirm no `Features/Sign` or `Features/Labels` changes;
   - [x] reconcile STATE/ROADMAP/REQUIREMENTS/phase plan;
   - [x] create `docs/history/2026-10-08-F4.md`;
   - [x] reconcile READER-01..07 as AUTO PASS;
   - [x] keep manual Windows QA NOT RUN;
   - [x] closure-docs push CI `37890536672` PASS on `48a79b412f5b4b2903c954f79ab37e420f87e6ae`;
   - [x] open draft PR #23 with exact base/head;
   - [x] PR CI `37890669463` PASS on the same `48a79b...` SHA;
   - [x] verify `main` remains `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
   - [x] no merge performed.

The final docs-only state commit after these steps intentionally does not self-reference its own SHA. It is accepted only after GitHub shows both push and PR Windows checks green for that exact current head.

## Task 9 Audit Result

Audit `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b..c36a8630e685cb143b312a0f260301b87a62bbd5`:

- 118 ahead / 0 behind;
- no product/test `.csproj` or lockfile changes;
- no `Features/Sign` changes;
- no `Features/Labels` changes;
- no new runtime package, second PDF engine, network runtime, database, OCR, tabs or F5+ scope;
- FIRMAR/ZPL legacy surface boundaries preserved;
- unsupported PDF actions remain no-op; external URIs restricted to confirmed absolute HTTP/HTTPS;
- no critical/high defect found by source/diff audit.

## Manual QA

**NOT RUN**: real Windows scroll/zoom/performance, large-document thumbnails, protected PDFs, Unicode search, mouse selection/copy/zoom, bookmarks/links, shortcuts/recents/stale UNC/offline behavior.

## Stop / Next Gate

```text
final docs-only exact-head push CI PASS + PR CI PASS
→ confirm PR #23 draft/open/unmerged
→ confirm main unchanged
→ STOP F4

next continuation
→ F5 Organize design/spec
→ implementation only after approved plan
```
