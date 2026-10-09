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

**Status:** Tasks 1–8 **AUTO PASS**. Task 9 full-branch audit and documentation reconciliation are complete; final exact-head CI + draft PR/PR-CI are the remaining closure evidence.

- Design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.
- Plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md`.
- Exact F3.4 base: `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.
- Pre-closure audited F4 head: `c36a8630e685cb143b312a0f260301b87a62bbd5`.
- Pre-closure CI `37875773158`: PASS; **417 tests PASS; build 0 warnings / 0 errors**.
- Full compare F3.4→F4: **118 commits ahead / 0 behind**.
- No product/test `.csproj` or lockfile changes; no new runtime package, second PDF engine, WebView/network service, database, OCR or tabs.
- No changes under `src/SGPdf.App/Features/Sign/` or `src/SGPdf.App/Features/Labels/`.
- FIRMAR stays on the existing single-page editing path; ZPL keeps the established legacy host.
- Unsafe PDF actions remain unsupported/no-op; only current-document GOTO and confirmed absolute HTTP/HTTPS URI links activate.
- Passwords remain transient; render retention stays bounded; thumbnails remain lazy.

Implementation tasks:

1. **F4.1a Native capability + pure layout — AUTO PASS** — `ce167be7...`; CI `37843337925`.
2. **F4.1b Continuous reader — AUTO PASS** — `bfbc7740...`; CI `37852149659`.
3. **F4.2 Thumbnails — AUTO PASS** — `c968aa8e...`; CI `37855511813`.
4. **F4.3 Password PDFs — AUTO PASS** — `c27dfa73...`; CI `37857787178`.
5. **F4.4a Search — AUTO PASS** — `984545a8...`; CI `37867206589`.
6. **F4.4b Copy — AUTO PASS** — `0a5a8cff...`; CI `37870778333`.
7. **F4.5 Bookmarks + Links — AUTO PASS** — `e85f8c78...`; CI `37872707082`.
8. **F4.6 Shortcuts + Recents + Hardening — AUTO PASS** — `02c979f2...`; CI `37875513602`; Task-8 docs checkpoint `c36a8630...`, CI `37875773158`.
9. **Closure — IN PROGRESS** — audit/docs complete; final exact-head CI + draft stacked PR + PR CI remain.

READER-01..07 are reconciled as **AUTO PASS** in `.planning/REQUIREMENTS.md`. Manual Windows QA remains **NOT RUN** and is not inferred from automation.

Frozen rulings: PDFium only; continuous LEER + single-page FIRMAR; existing `PdfScrollViewer` remains the legacy FIRMAR/ZPL host; visible+one-neighbor full-resolution retention; lazy thumbnails; search page-by-page without permanent index; one-page selection; bookmarks max 10,000/depth 128; only current-document GOTO and confirmed HTTP/HTTPS explicit annotations activate; recents max 10 in atomic LocalAppData JSON with no menu/startup target probing; no tabs; no OCR; no second PDF engine; no required network; passwords transient only.

## Phase 6 — F5 Organizar
**Goal:** reorder/rotate/delete/duplicate/insert/extract/merge/split with preservation preflight.  
**Estado:** pending. **Next design/spec gate only after formal F4 closure.**

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
F4 Tasks 1–8                  automated PASS
F4 Task 9                     audit/docs complete; exact-head CI + draft PR/PR-CI pending
F5–F12                        pending
```

No merge to `main` without explicit user approval.
