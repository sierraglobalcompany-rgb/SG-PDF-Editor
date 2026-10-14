# Roadmap — SG PDF Editor

> Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo: `.planning/STATE.md`. GitHub exact heads/CI mandan sobre snapshots históricos.

## Estado por fase

| Fase | Estado automatizado | QA manual/privado/físico |
|---|---|---|
| F0 PDF Base | CLOSED / AUTO PASS | NOT RUN completo |
| F1 Gate ZPL-A | synthetic PASS | private corpus NOT RUN |
| F2 ZPL Workspace | CLOSED / AUTO PASS | private + thermal/ruler/scanner NOT RUN |
| F3 Firma Visual | CLOSED / AUTO PASS | Windows/photo/stylus/library NOT RUN |
| F4 Lector Completo | CLOSED / AUTO PASS | Windows real NOT RUN |
| F5 Organizar | CLOSED / AUTO PASS | Windows real NOT RUN |
| F6 Imágenes | CLOSED / AUTO PASS | Windows real NOT RUN |
| F7 Texto V1 | CLOSED / AUTO PASS | Windows real NOT RUN |
| F8 Comentarios | CLOSED / AUTO PASS | stylus/touch/external readers NOT RUN |
| F9 Utilidades | CLOSED / AUTO PASS | Windows/external/private repair corpus NOT RUN |
| F10 OCR Local | CLOSED / AUTO PASS | Windows OCR/external readers/private corpus NOT RUN |
| F11 Texto V2 | CLOSED / AUTO PASS | Windows reflow/private layouts/external readers NOT RUN |
| **F12 Profesional** | **IN PROGRESS** | **real/private/manual gates remain open** |

## F12 Professional — authoritative order

| Slice | State | Next action |
|---|---|---|
| F12.1 Safe Redaction | **CLOSED / AUTO PASS** | keep limits honest |
| F12.2 Cryptographic Signature PFX/P12 MVP | **CLOSED / AUTO PASS** | advanced signing only by separate approval |
| F12.3 Existing AcroForm text fields | **CLOSED / AUTO PASS** | advanced form types only by separate approval |
| F12.4 Compare | **CLOSED / AUTO PASS** — core + minimal UI | representative real-PDF/manual QA later |
| F12.5 Batch | **PLANNED / NOT STARTED** | **current next: narrow scope/capability gate** |
| F12.6 Code Generator | **PLANNED / NOT STARTED** | scope/capability gate after Batch |
| F12.7 Audited Conversions | **PLANNED / NOT STARTED** | each conversion approved and audited separately |
| Release integration/stabilization | **OPEN** | deliberate branch/main consolidation + manual/private QA |

## Current position

```text
F0–F11      CLOSED / automated
F12.1       CLOSED — safe real redaction
F12.2       CLOSED — PFX/P12 cryptographic signature MVP
F12.3       CLOSED — existing classic AcroForm /Tx
F12.4       CLOSED — Compare core + minimal existing-panel UI
F12.5       Batch — current next, capability/scope gate first
F12.6       Code Generator — pending
F12.7       Audited Conversions — pending
Release     Integration + manual/private/physical QA — pending
```

Current automated functional baseline before this documentation-closure commit:
- exact implementation HEAD `488d59f7a9e47ae0ccaafe474e7215cdc4cdc1d2` on `feat/f12-4-task3-compare-ui`;
- **1186/1186** tests PASS, 0 failed, 0 skipped;
- Release build **0 warnings / 0 errors**;
- no merge to `main`.

## Remaining development sequence

### 1. F12.5 Batch — current next

Open with a narrow capability/scope gate, not implementation by assumption.

Baseline requirements:
- local/offline;
- explicit inputs and outputs;
- protect originals / no silent overwrite;
- per-item outcome/failure reporting;
- reuse existing operations rather than duplicate pipelines.

KISS guardrails:
- no generic `BatchEngine` unless a demonstrated need exists;
- no workflow designer;
- no job database;
- no worker/scheduler/task-queue subsystem;
- sequential execution is the default unless measurement proves bounded concurrency is needed;
- choose only operations with an existing safe owner and clear batch value.

Candidate operations may be evaluated from already-implemented capabilities such as watermark, Bates/page numbering, OCR, protection or simple organize actions. Candidate status does **not** mean approved scope.

### 2. F12.6 Code Generator

Open only after an explicit scope/capability gate determines which code formats are actually required and supported. Do not assume barcode/QR symbologies prematurely. Prefer a tiny generator slice that reuses existing export paths over a generic barcode studio.

### 3. F12.7 Audited Conversions

Each conversion is its own narrow capability/quality gate. No universal-fidelity claim. No second engine, LibreOffice runtime, Office automation or cloud dependency without explicit approval.

### 4. Real QA packs

After functional F12, group the accumulated physical/manual/private work into bounded packs instead of one giant final QA block:
- Reader/Organize/Editing;
- ZPL + physical print/scan;
- visual + cryptographic signatures;
- OCR/Forms/Compare.

### 5. Documentation consolidation

One bounded cleanup after functional F12:
- reconcile stale `AGENTS.md` / historical master-plan wording where useful;
- preserve one operational source of truth;
- mark superseded docs rather than multiplying handoffs;
- do not create Markdown for every commit.

### 6. Release integration / stabilization

Before release:
- map active branch/PR ancestry and unique commits;
- choose one conservative integration strategy;
- use a dedicated integration branch, never experimental work on `main`;
- reconcile stacked branches/PRs without destructive cleanup;
- bring `main` forward only with explicit user approval;
- run complete exact-head CI;
- execute prioritized real/private/manual/physical QA;
- close installer/license provenance items.

### 7. Release candidate / final

Release candidate requires clean-clone reproducibility, locked restore, Release build/tests, packaged runtime/installer smoke, offline smoke and no P0/P1 blockers. Final release adds no feature creep.

## What is not automatically in baseline F12

These require separate approval and do not block the current baseline roadmap:
- advanced form types (`/Btn`, `/Ch`, XFA, designer);
- advanced signing (Windows store, smartcard/HSM, TSA, legal qualification);
- universal PDF preservation/compatibility promises;
- AI/cloud/backend features.

## Operating cadence

> **1 `continua` = 1 meaningful bounded product task closed**, with RED → GREEN → focused verification → full CI → KISS/noise audit executed internally.

Separate user stops remain only for decisions, architecture changes, new dependencies, capability NO-GO or irreversible Git integration.

## Closure evidence

- F12.1: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`
- F12.2: `docs/history/2026-10-12-F12-2-cryptographic-signature-closure.md`
- F12.3: `docs/history/2026-10-13-F12-3-exact-head-closure.md`
- F12.4 core: `docs/history/2026-10-14-F12-4-compare-core-closure.md`
- F12.4 UI design: `docs/superpowers/specs/2026-10-14-f12-4-compare-ui-design.md`
- F12.4 UI plan: `docs/superpowers/plans/2026-10-14-f12-4-compare-ui.md`
- F12.4 UI closure: `docs/history/2026-10-14-F12-4-compare-ui-closure.md`

No merge to `main` without explicit user approval.