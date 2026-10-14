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
| F12.4 Compare | **CLOSED / AUTO PASS — core + minimal UI** | representative real-PDF/manual QA later |
| F12.5 Batch | **CLOSED / AUTO PASS — TextWatermark MVP** | additional operations only by separate value/capability decision |
| F12.6 Code Generator | **CLOSED / AUTO PASS — QR PNG MVP** | Code128/other formats only by separate value/capability decision |
| F12.7 Audited Conversions | **PLANNED / NOT STARTED** | **current next: narrow capability/scope gate** |
| Release integration/stabilization | **OPEN** | deliberate branch/main consolidation + manual/private QA |

## Current position

```text
F0–F11      CLOSED / automated
F12.1       CLOSED — safe real redaction
F12.2       CLOSED — PFX/P12 cryptographic signature MVP
F12.3       CLOSED — existing classic AcroForm /Tx
F12.4       CLOSED — Compare core + minimal existing-panel UI
F12.5       CLOSED — sequential TextWatermark Batch MVP
F12.6       CLOSED — local QR PNG MVP using existing Labelize owner
F12.7       Audited Conversions — current next, capability/scope gate first
Release     Integration + manual/private/physical QA — pending
```

Current F12.6 implementation checkpoint before documentation-only closure commits:
- exact implementation HEAD `2be0fc5345160e500d78102d99a833c9b9729bae` on `feat/f12-6-qr-generator-ui`;
- repository hygiene PASS;
- runtime staging + locked restore PASS;
- exact-head Release Build step PASS;
- exact-head full Test step PASS;
- exact-head CI run `38513237239`, job `115686247754`;
- no merge to `main`.

## F12.6 Code Generator — closed QR PNG MVP

Capability decision: **reuse the existing Labels/ZPL/Labelize owner and support QR only in baseline F12.6**.

Closed flow:
`Herramientas → Generar código QR... → texto/URL → QrCodeGenerator → ZplDocumentParser → LabelizeProcessRenderer → Guardar PNG`.

Closed automated behavior:
- local/offline generation;
- QR payload encoded as safe UTF-8 `^FH` field data;
- actual rendered PNG decodes back to the exact source value;
- regression includes UTF-8 plus ZPL-looking sequences (`^XZ`, `~JA`);
- blank input rejected; payload bounded to 512 UTF-8 bytes;
- existing `Herramientas` menu owner reused;
- focused value-entry dialog only;
- explicit PNG `SaveFileDialog` with overwrite confirmation;
- generation is independent of an open PDF and does not modify document state;
- controlled cancel/error paths.

KISS guardrails preserved:
- no generic barcode/code studio;
- no plugin/factory architecture for symbologies;
- no new runtime dependency;
- no production ZXing dependency;
- no cloud/backend or second renderer;
- no Code128 in baseline.

Code128, styling/logo customization, templates, batch QR, inserting QR into PDFs and additional symbologies require separate value/capability approval; they do not reopen F12.6 baseline.

## Remaining development sequence

### 1. F12.7 Audited Conversions — current next

Each conversion is its own narrow capability/quality gate. Start from actual existing owners and prove output quality before exposing anything.

KISS guardrails:
- no “convert everything” framework;
- no universal-fidelity claim;
- no second PDF engine merely for convenience;
- no LibreOffice runtime, Office automation or cloud conversion service without explicit approval;
- protect originals and use explicit output paths;
- one conversion slice at a time, with representative validation and honest limitations.

### 2. Real QA packs

After functional F12, group accumulated physical/manual/private work into bounded packs instead of one giant final QA block:
- Reader/Organize/Editing;
- ZPL + QR + physical print/scan;
- visual + cryptographic signatures;
- OCR/Forms/Compare/Batch.

### 3. Documentation consolidation

One bounded cleanup after functional F12:
- reconcile stale `AGENTS.md` / historical master-plan wording where useful;
- preserve one operational source of truth;
- mark superseded docs rather than multiplying handoffs;
- do not create Markdown for every commit.

### 4. Release integration / stabilization

Before release:
- map active branch/PR ancestry and unique commits;
- choose one conservative integration strategy;
- use a dedicated integration branch, never experimental work on `main`;
- reconcile stacked branches/PRs without destructive cleanup;
- bring `main` forward only with explicit user approval;
- run complete exact-head CI;
- execute prioritized real/private/manual/physical QA;
- close installer/license provenance items.

### 5. Release candidate / final

Release candidate requires clean-clone reproducibility, locked restore, Release build/tests, packaged runtime/installer smoke, offline smoke and no P0/P1 blockers. Final release adds no feature creep.

## F12.5 Batch — closed MVP

Closed scope remains one operation: **TextWatermark**. It reuses existing watermark request/planner/writer, runs sequentially, protects originals, refuses silent output overwrite and reports each item independently. No generic BatchEngine, workflow designer, workers/queue/database or concurrency framework exists.

Additional Bates/OCR/protection/organize batch operations require a separate value/capability decision.

## What is not automatically in baseline F12

These require separate approval and do not block the current baseline roadmap:
- advanced form types (`/Btn`, `/Ch`, XFA, designer);
- advanced signing (Windows store, smartcard/HSM, TSA, legal qualification);
- additional code symbologies or barcode studio behavior;
- universal PDF/conversion preservation promises;
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
- F12.6: operational closure recorded in `.planning/STATE.md`, `.planning/REQUIREMENTS.md` + this roadmap; no extra history Markdown added to avoid documentation noise.

No merge to `main` without explicit user approval.