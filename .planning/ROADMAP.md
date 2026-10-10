# Roadmap — SG PDF Editor

> Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo vigente: `.planning/STATE.md`. GitHub exact heads/CI are the execution authority.

## Phase 1 — F0 PDF Base
**Estado:** F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.

## Phase 2 — F1 Gate ZPL-A
**Estado:** synthetic Gate PASS → Labelize 1.7.0 selected; private Mercado Libre corpus **NOT RUN**.

## Phase 3 — F2 Etiquetas ZPL
**Estado:** F2.1–F2.6 automated PASS; private corpus + thermal/ruler/scanner QA **NOT RUN**.

## Phase 4 — F3 Firma Visual
**Estado:** F3.1–F3.4 automated PASS; corresponding real QA **NOT RUN**. Draft PR #22 open/unmerged.

## Phase 5 — F4 Lector Completo
**Estado:** automated closure PASS; manual Windows QA **NOT RUN**. Draft PR #23 open/unmerged.

## Phase 6 — F5 Organizar
**Estado:** automated closure PASS; real Windows/manual QA **NOT RUN**. Closure `327d7064c14131e603e3bce6947b593a10f46363`; draft PR #24 open/unmerged.

## Phase 7 — F6 Imágenes
**Goal:** edit real PDF image page objects locally/offline without raster-overlay fallbacks or a second PDF engine.

**Estado:** **AUTOMATED FUNCTIONAL CLOSURE PASS; Task 10 docs/PR finalization in progress.** Real Windows/manual QA **NOT RUN**.

Fresh Task-10 functional evidence:

- functional head `7f70a68b2148504888040483a4d9a9e9dd6b4a03`;
- Windows workflow `38057258528`, attempt 2: PASS;
- Release build: 0 warnings / 0 errors;
- tests: 680/680 PASS;
- F5→F6 pre-docs compare: 64 commits ahead / 0 behind;
- no project/lock dependency changes, second PDF engine, runtime network layer or generic object graph.

Delivered:

- real-image detection/selection and contextual actions;
- PNG extraction and PNG/JPEG replacement with geometry preservation;
- move, resize, rotate, delete, opacity and z-order;
- undo/redo and dirty guards;
- transactional Save As with source fingerprint, temp/reopen/render validation and atomic publication;
- independent preservation matrix: representative forms, bookmarks, named destinations, internal links, tagged structure, page labels, attachments and metadata are `ProvenPreserved` for the tested F6 writer route;
- cryptographic signatures and password-opened sources remain hard Blocks;
- bitmap allocation bounds and active-page-only discovery.

`IMG-01..04`: **AUTO PASS**. Opacity and z-order exact-runtime gates both passed.

Real Windows image-edit UX/performance/dialog/offline QA: **NOT RUN**.

## Phase 8 — F7 Texto V1
**Estado:** pending. Next permitted work after F6 closure is design/spec only.

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

## Current Position

```text
A0/A1                         PASS
F0                            automated PASS / physical QA pending
F1                            synthetic PASS / private corpus pending
F2                            automated PASS / private + physical QA pending
F3                            automated PASS / manual QA pending
F4                            automated closure PASS / manual QA pending
F5                            automated closure PASS / manual QA pending
F6                            automated functional closure PASS / Task 10 finalization / manual QA pending
F7–F12                        pending
```

No merge to `main` without explicit user approval.
