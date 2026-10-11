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
**Estado:** **AUTOMATED CLOSURE PASS**; real Windows/manual QA **NOT RUN**. Closure `c6d762efca01d50bfe3932d1f05617190a464fc6`; stacked draft PR #25 open/unmerged.

`IMG-01..04`: **AUTO PASS**.

## Phase 8 — F7 Texto V1
**Estado:** **AUTOMATED CLOSURE PASS**; real Windows/manual QA **NOT RUN**. Closure `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`; stacked draft PR #26 open/draft/unmerged.

`TEXT-01..04`: **AUTO PASS**.

## Phase 9 — F8 Comentarios
**Goal:** comentarios PDF reales y conservadores en un modo `COMENTAR` independiente, offline y compatible con lectores comerciales, usando PDFium como único motor de edición.

**Estado:** **AUTOMATED CLOSURE PASS; final docs/PR exact-head gate in progress.** Real Windows/stylus/touch/external-reader QA **NOT RUN**.

Evidence before final closure-docs gate:

- Task-11 checkpoint `88fea2d0f0b4de2ccbb323620c2b45bdf6bba12c`;
- Task-12 hardening `2359dc793667f59266062b64eb12e0e0f4efe08c`;
- Windows workflow `38104169562`, job `114366028415`: PASS;
- Release build: 0 warnings / 0 errors;
- tests: 851/851 PASS;
- F7→F8 pre-docs compare: 61 commits ahead / 0 behind;
- no `.csproj` or `packages.lock.json` change;
- PDFium remains the only editor engine;
- no unjustified runtime package;
- no runtime network/cloud/service/account/API-key layer.

Delivered:

- independent `COMENTAR` mode;
- Select + Highlight/Underline/Strikeout/Note/Ink/Rectangle/Ellipse;
- real native annotation persistence for `TEXT`, `SQUARE`, `CIRCLE`, `HIGHLIGHT`, `UNDERLINE`, `STRIKEOUT`, `INK`;
- managed candidate-first workspace with undo/redo and no durable native handles;
- stable structural identity with fail-closed duplicate/source mismatch handling;
- deterministic geometry/hit-testing including rotated-page device transforms;
- fresh-source writer + reopen validator + transactional atomic Save As;
- password/signature blocks, source-race detection, cancellation/failure cleanup;
- fresh F8 preservation matrix including unsupported `STAMP` preservation/read-only behavior;
- create→reopen→edit→reopen→delete→reopen lifecycle proof;
- Task-12 regression/hardening with full 851-test Windows suite green.

`COMMENTS`: **AUTO PASS**.

Real Windows comment UX, stylus/touch and manual Adobe/Foxit/Edge interop: **NOT RUN**.

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
F6                            automated closure PASS / manual QA pending / PR #25 draft
F7                            automated closure PASS / manual QA pending / PR #26 draft
F8                            automated closure PASS / final docs+draft PR / manual QA pending
F9–F12                        pending
```

No merge to `main` without explicit user approval.
