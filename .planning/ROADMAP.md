# Roadmap — SG PDF Editor

> Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo vigente: `.planning/STATE.md`. GitHub exact heads/CI are the execution authority.

## Phase 1 — F0 PDF Base
**Estado:** F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.

## Phase 2 — F1 Gate ZPL-A
**Estado:** synthetic Gate PASS → Labelize 1.7.0 selected; private Mercado Libre corpus **NOT RUN**.

## Phase 3 — F2 Etiquetas ZPL
**Estado:** F2.1–F2.6 automated PASS; private real-label corpus + thermal/ruler/scanner QA **NOT RUN**.

## Phase 4 — F3 Firma Visual
**Estado:** F3.1–F3.4 automated PASS; corresponding real QA **NOT RUN**.

F3.4 closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`; PR #22 draft/open/unmerged.

## Phase 5 — F4 Lector Completo
**Goal:** continuous reader, thumbnails, protected PDFs, search/copy, bookmarks/safe links, shortcuts and recents while preserving F3/ZPL boundaries.

**Estado:** **AUTOMATED CLOSURE PASS**. Manual Windows QA remains **NOT RUN**.

Key evidence:

- exact base F3.4: `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`;
- functional/Task-8 checkpoint `c36a8630e685cb143b312a0f260301b87a62bbd5`;
- CI `37875773158` PASS; 417 tests PASS; build 0 warnings / 0 errors;
- full audit: 118 commits ahead / 0 behind; no product/test csproj or lockfile changes; no Sign/Labels feature-file changes;
- Task-9 closure-docs checkpoint `48a79b412f5b4b2903c954f79ab37e420f87e6ae`;
- push CI `37890536672` PASS and PR CI `37890669463` PASS on that same SHA;
- draft PR #23 `F4 — Full Reader`, correct stacked base/head, open/unmerged;
- `main` verified unchanged at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.

The final docs-only state commit after PR creation must keep both push and PR checks green; GitHub exact-head is authoritative.

Tasks:

1. Capability/layout — AUTO PASS.
2. Continuous reader + FIRMAR boundary — AUTO PASS.
3. Lazy thumbnails — AUTO PASS.
4. Password PDFs — AUTO PASS.
5. Unicode search — AUTO PASS.
6. One-page selection/copy — AUTO PASS.
7. Bookmarks + safe explicit links — AUTO PASS.
8. Shortcuts + atomic recents + offline hardening — AUTO PASS.
9. Full closure audit/docs/draft PR — AUTO PASS subject to final docs-only exact-head checks staying green.

`READER-01..07`: AUTO PASS. Manual real Windows performance/UX/protected-PDF/search/selection/bookmark/link/shortcuts/recents/offline QA: **NOT RUN**.

Frozen through F4: PDFium only; continuous LEER + existing single-page FIRMAR; legacy FIRMAR/ZPL host preserved; bounded visible+neighbor page retention; lazy thumbnails; no OCR/index/tabs/database/cloud/network runtime/password persistence/F5 editing.

## Phase 6 — F5 Organizar
**Goal:** reorder/rotate/delete/duplicate/insert/extract/merge/split with preservation preflight.  
**Estado:** pending. **Next gate: design/spec planning only after the final F4 exact-head checks are green.**

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

## Current Position

```text
A0/A1                         PASS
F0                            automated PASS / physical QA pending
F1                            synthetic PASS / private corpus pending
F2                            automated PASS / private + physical QA pending
F3                            automated PASS / manual QA pending
F4                            automated closure PASS / manual QA pending
F5–F12                        pending
```

No merge to `main` without explicit user approval.
