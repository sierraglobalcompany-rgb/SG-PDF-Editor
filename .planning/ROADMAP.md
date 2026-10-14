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
| F12.5 Batch | **CLOSED / AUTO PASS — TextWatermark MVP** | additional batch operations only by separate value/capability decision |
| F12.6 Code Generator | **IN PROGRESS / AUTO PASS — QR core MVP** | **current next: minimal QR value-entry + Guardar PNG UI** |
| F12.7 Audited Conversions | **PLANNED / NOT STARTED** | each conversion approved and audited separately |
| Release integration/stabilization | **OPEN** | deliberate branch/main consolidation + manual/private QA |

## Current position

```text
F0–F11      CLOSED / automated
F12.1       CLOSED — safe real redaction
F12.2       CLOSED — PFX/P12 cryptographic signature MVP
F12.3       CLOSED — existing classic AcroForm /Tx
F12.4       CLOSED — Compare core + minimal existing-panel UI
F12.5       CLOSED — sequential TextWatermark Batch MVP
F12.6       IN PROGRESS — QR capability gate + core renderer path GREEN; minimal UI/export next
F12.7       Audited Conversions — pending
Release     Integration + manual/private/physical QA — pending
```

Current F12.6 core implementation checkpoint before documentation-only commits:
- exact implementation HEAD `c532d9d4b8c5b1fab07ad3174e57ad5d7ebf2dc6` on `feat/f12-6-qr-generator`;
- exact-head Release build step PASS;
- exact-head full Test step PASS (baseline 1192 + 4 QR contract cases = 1196 cases);
- exact-head CI run `38511920615`, job `115682120833`;
- no merge to `main`.

## Remaining development sequence

### 1. F12.6 Code Generator — current

Capability gate result: **reuse the existing Labels/ZPL/Labelize owner; QR only for the first MVP**.

Current GREEN core:
- text/URL payload;
- UTF-8 bytes encoded safely as ZPL `^FH` field data;
- minimal `^BQ` source;
- existing `ZplDocumentParser`;
- existing `LabelizeProcessRenderer`;
- local PNG bytes;
- real decode assertion back to the exact source value, including UTF-8 and ZPL-looking text;
- blank input rejected and payload bounded.

Current next action is one small user-facing slice:
`Herramientas/menu existing pattern → QR value entry → QrCodeGenerator → Guardar PNG`.

KISS guardrails:
- no generic barcode/code studio;
- no plugin architecture or factory for symbologies;
- no new runtime dependency;
- no Code128 until real value justifies extending the same owner;
- no cloud/backend or second renderer;
- reuse existing WPF/menu/dialog/save patterns;
- preserve local/offline behavior.

F12.6 closes only after that minimal UI/export path is exact-head GREEN; the current checkpoint is intentionally not mislabeled as a complete user-facing feature.

### 2. F12.7 Audited Conversions

Each conversion is its own narrow capability/quality gate. No universal-fidelity claim. No second engine, LibreOffice runtime, Office automation or cloud dependency without explicit approval.

### 3. Real QA packs

After functional F12, group the accumulated physical/manual/private work into bounded packs instead of one giant final QA block:
- Reader/Organize/Editing;
- ZPL + physical print/scan;
- visual + cryptographic signatures;
- OCR/Forms/Compare/Batch/QR.

### 4. Documentation consolidation

One bounded cleanup after functional F12:
- reconcile stale `AGENTS.md` / historical master-plan wording where useful;
- preserve one operational source of truth;
- mark superseded docs rather than multiplying handoffs;
- do not create Markdown for every commit.

### 5. Release integration / stabilization

Before release:
- map active branch/PR ancestry and unique commits;
- choose one conservative integration strategy;
- use a dedicated integration branch, never experimental work on `main`;
- reconcile stacked branches/PRs without destructive cleanup;
- bring `main` forward only with explicit user approval;
- run complete exact-head CI;
- execute prioritized real/private/manual/physical QA;
- close installer/license provenance items.

### 6. Release candidate / final

Release candidate requires clean-clone reproducibility, locked restore, Release build/tests, packaged runtime/installer smoke, offline smoke and no P0/P1 blockers. Final release adds no feature creep.

## F12.5 Batch — closed MVP

Closed scope is intentionally one operation: **TextWatermark**.

Behavior:
- multi-select local PDFs;
- reuse existing watermark configuration and write owners;
- explicit output folder;
- sequential execution;
- per-item success/error result;
- one bad item does not stop the others;
- protect originals and refuse silent output overwrite;
- all-pages scope resolves independently for each document;
- empty batch/blank destination rejected;
- final UI summary of successes/errors.

Not part of this MVP:
- generic `BatchEngine`;
- workflow designer;
- queue/workers/scheduler/database;
- concurrency framework;
- implicit support for Bates, OCR, protection or organize operations.

Those operations can be evaluated later only if real value justifies extending the same narrow owner.

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
- F12.5: operational closure recorded in `.planning/STATE.md` + this roadmap; no extra history Markdown added to avoid documentation noise.
- F12.6 core checkpoint: operational evidence recorded in `.planning/STATE.md` + this roadmap; no extra history Markdown until the user-facing slice closes.

No merge to `main` without explicit user approval.