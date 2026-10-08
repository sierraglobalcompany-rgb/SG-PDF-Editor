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

**Current gate:** formal design spec written + self-reviewed; **awaiting user approval**. Product code **NOT STARTED**.

Spec: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.

Planned slices after spec + implementation-plan approval:

- **F4.1 Continuous Reader Core** — virtualized continuous pages, current-page tracking, zoom/fit, bounded visible render window, LEER↔FIRMAR surface boundary.
- **F4.2 Thumbnails** — lazy virtualized page thumbnails + synchronized navigation.
- **F4.3 Password PDFs** — controlled password error/prompt/retry/cancel, no persistence.
- **F4.4 Search + Copy** — PDFium text search, active-match highlight, single-page text selection/copy.
- **F4.5 Bookmarks + Links** — read-only cycle-safe outline, internal destinations, confirmed HTTP/HTTPS links only.
- **F4.6 Shortcuts + Recents + Hardening** — shortcuts, max-10 LocalAppData recent paths, no startup path probing, regression/performance/offline closure.

**Explicit design ruling:** no multi-document tabs in F4; reconsider only after a stable reader proves the value.

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
F4                            design spec written/self-reviewed / awaiting approval / code NOT STARTED
F5–F12                        pending
```

Next gate: **user review/approval of the written F4 spec**. Only after approval may the detailed TDD implementation plan be written. No merge to `main` without explicit user approval.
