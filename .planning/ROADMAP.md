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
**Goal:** editar conservadoramente objetos de texto PDF reales dentro del único modo EDITAR, manteniendo PDFium como único motor y usando fallback TTF offline solo cuando la fuente original no es segura.

**Estado:** **AUTOMATED FUNCTIONAL CLOSURE PASS; Task 14 docs/PR finalization in progress.** Real Windows/manual QA **NOT RUN**.

Evidence before final Task-14 docs gate:

- Task-13 checkpoint `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`;
- Windows workflow `38089770428`: PASS;
- Release build: 0 warnings / 0 errors;
- tests: 766/766 PASS;
- F6→F7 pre-docs compare: 100 commits ahead / 0 behind;
- no new NuGet/package-lock changes;
- `.csproj` change is asset copy only;
- PDFium remains the only editor engine;
- no runtime network/cloud/service/account/API-key layer;
- DejaVu Sans 2.37 is the only new runtime asset and is pinned with provenance/license/hash.

Delivered:

- active-page top-level text discovery with exact Unicode and managed snapshots;
- mixed image/text topmost hit-testing;
- conservative read-only/edit policy and text workspace;
- OriginalFont route plus DejaVu Sans CID Type2 fallback with explicit Unicode maps;
- combined image+text materialization and validation before atomic publication;
- independent F7 preservation matrix including page rotation;
- Texto V1 UI inside the existing EDITAR shell with shared dirty guards and Save As;
- lifecycle/performance hardening without durable native handles.

`TEXT-01..04`: **AUTO PASS**.

Real Windows text-edit UX/offline/manual QA: **NOT RUN**.

## Phase 9 — F8 Comentarios
**Estado:** pending. Do not start inside F7 closure.

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
F7                            automated functional closure PASS / Task 14 finalization / manual QA pending
F8–F12                        pending
```

No merge to `main` without explicit user approval.
