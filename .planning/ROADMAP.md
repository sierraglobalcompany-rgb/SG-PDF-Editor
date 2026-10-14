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
| F12.4 Compare | **IN PROGRESS** — core CLOSED, UI plan READY | implement minimal UI |
| F12.5 Batch | **PLANNED / NOT STARTED** | scope/capability gate after Compare |
| F12.6 Code Generator | **PLANNED / NOT STARTED** | scope/capability gate after Batch |
| F12.7 Audited Conversions | **PLANNED / NOT STARTED** | each conversion approved and audited separately |
| Release integration/stabilization | **OPEN** | deliberate branch/main consolidation + manual/private QA |

## Current position

```text
F0–F11      CLOSED / automated
F12.1       CLOSED — safe real redaction
F12.2       CLOSED — PFX/P12 cryptographic signature MVP
F12.3       CLOSED — existing classic AcroForm /Tx
F12.4 core  CLOSED — identical / text-only / visual-only / page-count structure
F12.4 UI    PLAN READY — current next implementation
F12.5       Batch — pending
F12.6       Code Generator — pending
F12.7       Audited Conversions — pending
Release     Integration + manual/private/physical QA — pending
```

Current automated baseline before this roadmap-sync commit:
- `1178/1178` tests PASS;
- Release build 0 warnings / 0 errors;
- current exact implementation HEAD `149f716b704c43ea43bf94fa517fa5922920237b`.

## Remaining development sequence

### 1. Close F12.4 Compare UI

KISS MVP:
`PDF A open → Archivo > Comparar con otro PDF... → select B → compare locally → existing right panel summary → close comparison`.

No side-by-side viewer, no page matching, no OCR/semantic diff, no report export.

### 2. F12.5 Batch

Open only after Compare UI is closed. The exact batch operations must be scoped and capability-gated before implementation. Baseline requirements:
- local/offline;
- explicit inputs and outputs;
- protect originals / no silent overwrite;
- per-item outcome/failure reporting;
- reuse existing operations rather than duplicate pipelines.

### 3. F12.6 Code Generator

Open only after an explicit scope/capability gate determines which code formats are actually required and supported. Do not assume barcode/QR symbologies prematurely.

### 4. F12.7 Audited Conversions

Each conversion is its own narrow capability/quality gate. No universal-fidelity claim. No second engine or cloud dependency without explicit approval.

### 5. Release integration / stabilization

Before release:
- decide controlled integration strategy for the active implementation lineage;
- reconcile stacked branches/PRs without destructive cleanup;
- bring `main` forward only with explicit user approval;
- run complete exact-head CI;
- execute prioritized real/private/manual/physical QA;
- close installer/license provenance items.

## What is not automatically in baseline F12

These require separate approval and do not block the current baseline roadmap:
- advanced form types (`/Btn`, `/Ch`, XFA, designer);
- advanced signing (Windows store, smartcard/HSM, TSA, legal qualification);
- universal PDF preservation/compatibility promises;
- AI/cloud/backend features.

## Operating cadence

From the 2026-10-14 audit:

> **1 `continua` = 1 meaningful bounded product task closed**, with RED → GREEN → focused verification → full CI → KISS/noise audit executed internally.

Separate user stops remain only for decisions, architecture changes, new dependencies, capability NO-GO or irreversible Git integration.

## Closure evidence

- F12.1: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`
- F12.2: `docs/history/2026-10-12-F12-2-cryptographic-signature-closure.md`
- F12.3: `docs/history/2026-10-13-F12-3-exact-head-closure.md`
- F12.4 core: `docs/history/2026-10-14-F12-4-compare-core-closure.md`
- F12.4 UI design: `docs/superpowers/specs/2026-10-14-f12-4-compare-ui-design.md`
- F12.4 UI plan: `docs/superpowers/plans/2026-10-14-f12-4-compare-ui.md`

No merge to `main` without explicit user approval.
