---
gsd_state_version: '1.0'
status: implementing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 13
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

> Numeric GSD progress is not product-completion truth while automated and physical/private gates are tracked separately. GitHub exact heads/CI + this status are authoritative for executed work.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 5 — **F4 Lector Completo**, Task 9 closure in progress. Functional Tasks 1–8 are automated PASS; the full-branch closure audit is complete and final exact-head CI + draft PR evidence remain before F4 may be called closed.

## Previous Gate — F3.4 closed automatically

- Branch: `feat/f3-4-local-signature-library`.
- Closure head: `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.
- Closure push CI `37837613618`: PASS.
- PR CI `37837833517`: PASS.
- Tests: 301 PASS / 0 FAIL / 0 SKIPPED.
- PR #22: draft/open/unmerged, stacked on F3.3.
- Manual real Windows QA: **NOT RUN**.

## Current Gate — F4 Task 9 closure

- Branch: `feat/f4-full-reader`.
- Base: exact F3.4 closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.
- Pre-closure audited head: `c36a8630e685cb143b312a0f260301b87a62bbd5`.
- Pre-closure exact-head CI `37875773158`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: **417 PASS / 0 FAIL / 0 SKIPPED**.
- Formal design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md` — approved.
- TDD plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md` — approved.
- Companion: `.planning/phases/05-f4-full-reader/PLAN.md`.

### Full-branch audit — PASS

Audit range: `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b..c36a8630e685cb143b312a0f260301b87a62bbd5`.

- Git compare: **118 commits ahead / 0 behind** the exact F3.4 closure head.
- No product/test `.csproj` or lockfile changes in F4; no new runtime package and no second PDF engine.
- No files under `src/SGPdf.App/Features/Sign/` changed.
- No files under `src/SGPdf.App/Features/Labels/` changed.
- F4 changes are scoped to Reader/PDF primitives, narrow `MainWindow` integration, tests, and planning/docs.
- Existing FIRMAR single-page surface remains authoritative; continuous LEER is disabled while signature mode is active.
- Existing legacy `PdfScrollViewer` remains available for FIRMAR/ZPL integration.
- No WebView2/network client/runtime service was added; regression barriers remain green.
- Passwords remain transient and are not persisted.
- Reader full-resolution retention remains bounded to visible pages + one neighbor before/after; thumbnails remain lazy.
- PDF actions are restricted to current-document GOTO and explicit absolute HTTP/HTTPS URI links after user confirmation; unsupported/unsafe actions remain no-op.
- No multi-document tabs, OCR, database, cloud/account/server, permanent search index, or F5+ editing scope entered F4.

### F4 requirements — automated evidence

- **READER-01 AUTO PASS** — Tasks 2–3: continuous virtualized reading + lazy thumbnails; bounded render retention.
- **READER-02 AUTO PASS** — Tasks 5–6: PDFium Unicode search + one-page selection/copy; no OCR.
- **READER-03 AUTO PASS** — Task 7: bounded bookmarks + safe explicit links.
- **READER-04 AUTO PASS** — Task 4: protected-PDF retry/cancel + no password persistence.
- **READER-05 AUTO PASS** — Task 8: shortcuts + atomic max-10 recents + no startup/menu target probing.
- **READER-06 AUTO PASS** — Tasks 2/6/8: continuous LEER preserved separately from existing FIRMAR state/placement path.
- **READER-07 AUTO PASS** — Tasks 1–3: visible+neighbor full-resolution window, lazy thumbnails, stale render rejection.

### Task evidence summary

1. Task 1 — AUTO PASS — `ce167be7...`, CI `37843337925`, 312 tests.
2. Task 2 — AUTO PASS — `bfbc7740...`, CI `37852149659`, 328 tests.
3. Task 3 — AUTO PASS — `c968aa8e...`, CI `37855511813`, 336 tests.
4. Task 4 — AUTO PASS — `c27dfa73...`, CI `37857787178`, 346 tests.
5. Task 5 — AUTO PASS — `984545a8...`, CI `37867206589`, 368 tests.
6. Task 6 — AUTO PASS — `0a5a8cff...`, CI `37870778333`, 379 tests.
7. Task 7 — AUTO PASS — `e85f8c78...`, CI `37872707082`, 399 tests.
8. Task 8 — AUTO PASS — `02c979f2...`, CI `37875513602`, 417 tests; docs checkpoint `c36a8630...`, CI `37875773158` PASS.
9. Task 9 — **IN PROGRESS** — full diff audit + reconciliation/docs complete in this closure commit; final exact-head CI, draft PR and PR CI still required.

## Parallel Acceptance Gates Still Open

Automated PASS does not imply physical/manual PASS.

- F0 physical Windows UI/print/offline smoke: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private real-label corpus + thermal/ruler/scanner QA: **NOT RUN**.
- F3.1–F3.4 corresponding real Windows/photo/hardware/library QA: **NOT RUN**.
- F4 real Windows reader scroll/zoom/performance/thumbnails QA: **NOT RUN**.
- F4 real protected-PDF prompt/retry/cancel/printing QA: **NOT RUN**.
- F4 real Unicode search + mouse selection/copy/zoom QA: **NOT RUN**.
- F4 real bookmarks/internal/external link confirmation QA: **NOT RUN**.
- F4 real shortcuts/recents/stale-UNC/offline QA: **NOT RUN**.

## Runtime / Architecture Decisions

- Windows x64 + C# + .NET 10 + WPF.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium is the only PDF engine; native calls stay behind `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 is the only approved runtime ZPL renderer.
- PDFsharp remains labels-only in product runtime.
- ZXing remains test/QA-only.
- F3 sources converge to `SignatureAsset` → `AddSignatureAsset(...)`.
- No merge to `main` without explicit user approval.

## Next Gate

```text
Task 9 closure docs commit
→ exact-head Windows CI PASS
→ open draft PR base feat/f3-4-local-signature-library / head feat/f4-full-reader
→ PR CI PASS on the same final SHA
→ verify main still 31c0594758a83ec555d73ecdd7c597cdf8791fd7
→ mark F4 automated closure PASS
→ STOP — no merge
```

After F4 is formally closed, the next product gate is **F5 Organize design/spec planning**, not implementation by inference.

## Continuity

- F3.4 closure: `docs/history/2026-10-08-F3.4.md`.
- F4 closure history: `docs/history/2026-10-08-F4.md`.
- F4 design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.
- F4 plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md`.
