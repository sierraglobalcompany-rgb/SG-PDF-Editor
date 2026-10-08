# Phase 5 — F4 Full Reader

**Status:** design + implementation plan approved; Task 1 automated PASS; Task 2 next.  
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
- Existing ZPL surface remains separate.
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
2. **NEXT** — Continuous WPF surface + bounded rendering + FIRMAR boundary.
3. Lazy thumbnails + left navigation.
4. Password-protected PDFs.
5. PDFium text core + find navigation.
6. One-page text selection + clipboard copy.
7. Bookmarks + safe explicit PDF links.
8. Shortcuts + atomic recent files + offline hardening.
9. Closure audit/docs + exact-head CI + draft stacked PR.

Each task is RED → confirm expected failure → minimal GREEN → focused regression → full exact-head CI. Project cadence is one task per user `continúa` unless the user explicitly changes it.

## Review Focus

- stale render publication after rapid scroll/zoom;
- isolated per-page render failure;
- wrong/cancelled password preserving prior workspace;
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

Manual Windows performance/UX/offline QA is a separate gate and remains **NOT RUN** until actually executed. Automated PASS never implies physical/manual PASS.

## Current Gate

```text
Task 1 AUTO PASS
→ next user `continúa`
→ Task 2 RED
→ confirm expected failure
→ minimal GREEN
→ exact-head full CI
→ report evidence and stop before Task 3
```
