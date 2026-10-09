# Roadmap — SG PDF Editor

> Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo vigente: `.planning/STATE.md`. GitHub exact heads/CI are the execution authority.

## Phase 1 — F0 PDF Base
**Estado:** F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.

## Phase 2 — F1 Gate ZPL-A
**Estado:** synthetic Gate PASS → Labelize 1.7.0 selected; private Mercado Libre corpus **NOT RUN**.

## Phase 3 — F2 Etiquetas ZPL
**Estado:** F2.1–F2.6 automated PASS; private corpus + thermal/ruler/scanner QA **NOT RUN**.

## Phase 4 — F3 Firma Visual
**Estado:** F3.1–F3.4 automated PASS; corresponding real QA **NOT RUN**.

F3.4 closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`; draft PR #22 open/unmerged.

## Phase 5 — F4 Lector Completo
**Estado:** **AUTOMATED CLOSURE PASS**; manual Windows QA **NOT RUN**.

Closure `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`; draft PR #23 open/unmerged.

## Phase 6 — F5 Organizar
**Goal:** reorder/rotate/delete/duplicate/insert/extract/merge/split with preservation preflight and transactional Save As.

**Estado:** **AUTOMATED CLOSURE PASS**, provided the exact Task-10 checkpoint head has both push and PR Windows CI green. Real Windows/manual QA **NOT RUN**.

Closure evidence before the final checkpoint commit:

- stacked base F4: `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`;
- functional head `d92cd0cc60fb7de9a3d9d595bdfd0468ee38ff8a` → CI `37990505245` attempt 2 PASS, 550/550, build 0/0;
- reconciled-docs head `e9f96566c1d31a442da6373e1a648cc61a9e5bfc` → push CI `37991289080` PASS, 550/550, build 0/0;
- draft PR #24 base `feat/f4-full-reader`, head `feat/f5-organize`, open/unmerged;
- PR CI `37991479820` PASS, 550/550, build 0/0;
- no project/lock dependency changes, second PDF engine or runtime network layer;
- preservation matrix: `docs/history/2026-10-09-F5-preservation-matrix.md`;
- signed/password-opened PDFs hard-block structural output;
- representative fixtures prove current writer changes/loses forms, bookmarks, internal links, named destinations, tagged structure, page labels, attachments and original metadata, therefore explicit warning confirmation is required;
- Task 10 adds dirty-plan protection for opening another PDF and switching away from ORGANIZAR; successful Save As clears dirty state.

Tasks 0–10: **AUTO PASS** when the exact checkpoint head's push + PR CI are green. `ORG-01..04`: **AUTO PASS**.

Real Windows organize UX/performance/dialog/offline QA: **NOT RUN**.

## Phase 7 — F6 Imágenes
**Estado:** pending. **Next allowed gate after F5 closure: design/spec only.**

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
F5                            automated closure PASS / manual QA pending
F6–F12                        pending
```

Final F5 exact-head CI evidence is recorded in PR #24 conversation to avoid changing the tested git head solely to cite its own CI.

No merge to `main` without explicit user approval.
