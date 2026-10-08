# Roadmap — SG PDF Editor

> GSD-managed product roadmap. Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo vigente: `.planning/STATE.md`. GitHub exact heads/CI remain the execution authority.

## Phase 1 — F0 PDF Base
**Goal:** lector PDF usable y estable.  
**Estado:** **F0.1–F0.6 automated PASS**; physical Windows UI/print/offline smoke **NOT RUN**.

## Phase 2 — F1 Gate ZPL-A
**Goal:** elegir renderer ZPL con evidencia.  
**Estado:** synthetic Gate PASS → **Labelize 1.7.0 selected**. Private Mercado Libre real corpus **NOT RUN**, so formal private-corpus acceptance remains open.

## Phase 3 — F2 Etiquetas ZPL
**Goal:** reemplazar el flujo manual Labelary de forma local/offline.  
**Estado:** **F2.1–F2.6 automated PASS**. Private real-label corpus and physical thermal/ruler/scanner QA remain **NOT RUN**.

Delivered automated slices:
- F2.1 Parse + Open — PASS.
- F2.2 Labelize + Preview — PASS.
- F2.3 Quantity + Dimensions — PASS.
- F2.4 Layout + PDF export — PASS.
- F2.5 Windows Thermal Print — PASS.
- F2.6 Validation + Hardening — PASS.

Phase continuity index: `.planning/phases/03-f2-zpl-workspace/PLAN.md`.

## Phase 4 — F3 Firma Visual
**Goal:** firmar visualmente PDFs de forma local y reutilizable sin crear un segundo motor de firma.

- **F3.1 Core placement/save — automated PASS.** Manual real-signature UX remains NOT RUN.
- **F3.2 Photo/scan preparation — automated PASS.** Real-photo/scanner QA remains NOT RUN.
- **F3.3 Draw signature / InkCanvas — automated PASS.** Final head `8b5bfd35b59fbc75826f8d7616aaaca3e2f31233`; PR CI `37802865627` PASS; 246 tests. Hardware QA remains NOT RUN.
- **F3.4 Local signature library — CURRENT.** Written spec approved by user. TDD implementation plan exists at `docs/superpowers/plans/2026-10-08-f3-4-local-signature-library.md`, has been self-audited against the spec, and is awaiting user approval. Product code has not started and no F3.4 PR is open yet.

## Phase 5 — F4 Lector Completo
**Goal:** continuous scroll, thumbnails, bookmarks/links, search/copy text, password PDFs, shortcuts/recent files.  
**Estado:** pending.

## Phase 6 — F5 Organizar
**Goal:** page reordering/rotation/delete/duplicate/insert/extract/merge/split with preservation preflight.  
**Estado:** pending.

## Phase 7 — F6 Imágenes
**Goal:** inspect/extract/replace/edit PDF images conservatively.  
**Estado:** pending.

## Phase 8 — F7 Texto V1
**Goal:** conservative object-level text editing, not Word-like reflow.  
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
F3.1                          automated PASS / manual QA pending
F3.2                          automated PASS / real-photo QA pending
F3.3                          automated PASS / hardware QA pending
F3.4                          spec approved / TDD plan awaiting approval / no code
F4–F12                        pending
```

Next permitted step after plan approval: **execute F3.4 RED → GREEN task-by-task**. No product-code implementation before that approval.
