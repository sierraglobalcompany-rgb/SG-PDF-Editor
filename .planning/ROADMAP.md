# Roadmap — SG PDF Editor

> Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo vigente: `.planning/STATE.md`. GitHub exact heads/CI remain the execution authority.

## Phase 1 — F0 PDF Base
**Goal:** lector PDF usable y estable.  
**Estado:** **F0.1–F0.6 automated PASS**; physical Windows UI/print/offline smoke **NOT RUN**.

## Phase 2 — F1 Gate ZPL-A
**Goal:** elegir renderer ZPL con evidencia.  
**Estado:** synthetic Gate PASS → **Labelize 1.7.0 selected**. Private Mercado Libre real corpus **NOT RUN**.

## Phase 3 — F2 Etiquetas ZPL
**Goal:** reemplazar el flujo manual Labelary local/offline.  
**Estado:** **F2.1–F2.6 automated PASS**. Private real-label corpus + thermal/ruler/scanner QA **NOT RUN**.

Delivered: parse/open, Labelize preview, quantities/dimensions, layout/PDF export, Windows thermal print preflight, validation/hardening.

## Phase 4 — F3 Firma Visual
**Goal:** firmar visualmente PDFs localmente sin crear un segundo motor de firma.

- F3.1 Core placement/save — automated PASS; manual real-signature QA NOT RUN.
- F3.2 Photo/scan — automated PASS; real-photo/scanner QA NOT RUN.
- F3.3 Draw/InkCanvas — automated PASS; hardware QA NOT RUN.
- F3.4 Local signature library — **automated closure PASS**.
  - closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`;
  - closure CI `37837613618` PASS;
  - PR CI `37837833517` PASS;
  - 301 tests, 0 failures;
  - PR #22 draft/open/unmerged;
  - real Windows save→restart→reuse/rename/delete/offline QA **NOT RUN**.

## Phase 5 — F4 Lector Completo
**Goal:** continuous scroll, thumbnails, bookmarks/links, search/copy text, password PDFs, shortcuts and recent files while preserving F3 page-local editing.

**Current gate:** design + TDD plan **APPROVED**. **Tasks 1–6 AUTO PASS**; Task 7 waits for the next user `continúa`.

- Design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.
- Plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md`.
- Task 1 functional head: `ce167be7ae0b7769c4fc9253c400bc5a9b59216a`; CI `37843337925` PASS; 312 tests PASS.
- Task 2 functional/verified head: `bfbc774066f91acafc960c8e7f77ba0df5f344f6`; CI `37852149659` PASS; 328 tests PASS.
- Task 3 functional/verified head: `c968aa8e94f8b22820e3aa32ba1097d0e58516a2`; CI `37855511813` PASS; 336 tests PASS.
- Task 4 final head: `c27dfa738d946147bec5cb48c4e7ab2afde776bc`; CI `37857787178` PASS; 346 tests PASS; build 0 warnings / 0 errors.
- Task 5 functional/verified head: `984545a8e8b6b098a55dad55b5059aa4c6020b3b`; CI `37867206589` PASS; 368 tests PASS; build 0 warnings / 0 errors.
- Task 6 clean RED head: `3e0ef221dcc40a98ec9337bdea725f2d5ce8b0e0`; CI `37870209624` expected build failure with 8 missing selection-contract errors and 0 warnings.
- Task 6 strengthened zoom RED head: `670a9d8416f5ec29b0456f8dead7b171e07149d5`; CI `37870595171` build clean with 378 PASS / 1 expected failure for missing automatic geometry reprojection.
- Task 6 functional/verified head: `0a5a8cff5677956b3498ec333c5b4d76f59e0180`.
- Task 6 CI: `37870778333` PASS.
- Task 6 verification: Release build 0 warnings / 0 errors; 379 tests PASS.
- Task 6 scope: one-page selection model + feature-local reader selection integration + tests only; no PDFium binding, package/lock, F3, ZPL, print, persistence/database/network or bookmark/link changes.

Implementation tasks:

1. **F4.1a Native capability + pure layout — AUTO PASS** — pinned PDFium exports verified; page metrics, current-page and visible+neighbor render-window logic delivered.
2. **F4.1b Continuous reader — AUTO PASS** — virtualized continuous `LEER`, bounded sequential render window, stale-publication protection, isolated page errors and LEER↔FIRMAR bridge delivered while preserving ZPL.
3. **F4.2 Thumbnails — AUTO PASS** — lazy virtualized page thumbnails, realized+neighbor bounded retention, separate scheduler, stale-result suppression and synchronized navigation delivered.
4. **F4.3 Password PDFs — AUTO PASS** — authoritative PDFium error-4 classification, masked prompt, retry/cancel, prior-workspace preservation and no password persistence.
5. **F4.4a Search — AUTO PASS** — PDFium Unicode text primitives, page-by-page on-demand find navigation, Ctrl+F/Enter/Shift+Enter/Escape, active-match highlight and stale-search suppression; no OCR/index.
6. **F4.4b Copy — AUTO PASS** — one-page PDF-coordinate drag selection, cross-page rejection, automatic zoom reprojection and exact Unicode clipboard copy; independent from F3 signature geometry.
7. **F4.5 Bookmarks + Links — NEXT** — read-only cycle-safe outline + safe explicit links.
8. **F4.6 Shortcuts + Recents + Hardening** — local atomic max-10 recents, no startup path probing.
9. **Closure** — audit/docs/exact-head CI/draft stacked PR.

Frozen rulings: PDFium only; continuous LEER + single-page FIRMAR; existing `PdfScrollViewer` remains the legacy FIRMAR/ZPL host; visible+one-neighbor full-resolution retention; lazy thumbnails; search page-by-page without permanent index; one-page selection uses `PdfPageDeviceTransform` and stored PDF text rects; no tabs; no OCR; no second PDF engine; no required network; passwords are transient only and never persisted.

## Phase 6 — F5 Organizar
**Goal:** reorder/rotate/delete/duplicate/insert/extract/merge/split with preservation preflight.  
**Estado:** pending.

## Phase 7 — F6 Imágenes
**Estado:** pending.

## Phase 8 — F7 Texto V1
**Estado:** pending.

## Phase 9 — F8 Comentarios
**Estado:** pending.

## Phase 10 — F9 Utilidades
**Estado:** pending.

## Phase 11 — F10 OCR
**Estado:** pending.

## Phase 12 — F11 Texto V2
**Estado:** pending.

## Phase 13 — F12 Profesional
**Estado:** pending.

---

## Current Position

```text
A0/A1                         PASS
F0                            automated PASS / physical QA pending
F1                            synthetic Gate PASS / private corpus pending
F2.1–F2.6                     automated PASS / private + physical QA pending
F3.1–F3.4                     automated PASS / corresponding manual QA pending
F4 Task 1–6                   automated PASS
F4 Task 7–9                   pending
F5–F12                        pending
```

Next gate: **Task 7 only after the next user `continúa`**. No merge to `main` without explicit user approval.
