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
| F12.6 Code Generator | **CLOSED / AUTO PASS — QR PNG MVP** | additional symbologies only by separate value/capability decision |
| F12.7 Audited Conversions | **PLANNED / NOT STARTED** | **current next: capability/scope gate, one conversion at a time** |
| Release integration/stabilization | **OPEN** | deliberate branch/main consolidation + manual/private QA |

## Current position

```text
F0–F11      CLOSED / automated
F12.1       CLOSED — safe real redaction
F12.2       CLOSED — PFX/P12 cryptographic signature MVP
F12.3       CLOSED — existing classic AcroForm /Tx
F12.4       CLOSED — Compare core + minimal existing-panel UI
F12.5       CLOSED — sequential TextWatermark Batch MVP
F12.6       CLOSED — QR PNG MVP, existing Labelize owner reused
F12.7       Audited Conversions — current next, capability/scope gate first
Release     Integration + manual/private/physical QA — pending
```

Current F12.6 implementation checkpoint before documentation-only closure commits:
- exact implementation HEAD `5387e705098b448c7eb0aab87831c48acc3a9060` on `feat/f12-6-qr-generator`;
- exact-head Release build PASS / 0 errors;
- build exposes 2 pre-existing NuGet vulnerability warnings for test dependency `System.Security.Cryptography.Pkcs 8.0.1` (`NU1902`, `NU1903`), deferred to stabilization because F12.6 did not introduce them;
- exact-head tests **1197/1197 PASS**, 0 failed, 0 skipped;
- exact-head CI run `38534493694`, job `115753962287`;
- no merge to `main`.

## Remaining development sequence

### 1. F12.7 Audited Conversions — current next

Each conversion is its own narrow capability/quality gate. Do not assume a universal converter.

Required decision order:
1. identify the smallest conversion with real product value;
2. check whether an existing local owner can do it safely;
3. define accepted fidelity limits before implementation;
4. RED → minimal GREEN → exact-head CI;
5. defer any conversion that requires a second engine or disproportionate runtime unless separately approved.

KISS guardrails:
- no universal conversion framework;
- no plugin architecture for formats;
- no LibreOffice runtime, Office automation, cloud conversion or second PDF engine without explicit value + approval;
- no universal-fidelity claim;
- one direction/format at a time;
- local/offline behavior preferred.

### 2. Real QA packs

After functional F12, group the accumulated physical/manual/private work into bounded packs instead of one giant final QA block:
- Reader/Organize/Editing;
- ZPL + physical print/scan;
- visual + cryptographic signatures;
- OCR/Forms/Compare/Batch/QR.

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
- resolve current NuGet advisories and close installer/license provenance items.

### 5. Release candidate / final

Release candidate requires clean-clone reproducibility, locked restore, Release build/tests, packaged runtime/installer smoke, offline smoke and no P0/P1 blockers. Final release adds no feature creep.

## F12.6 Code Generator — closed MVP

Capability gate result: **reuse the existing Labels/ZPL/Labelize owner; QR only**.

Closed behavior:
- text/URL value entry from `Herramientas`;
- explicit `Guardar PNG` destination;
- UTF-8 bytes encoded safely as ZPL `^FH` field data;
- minimal `^BQ` source;
- existing `ZplDocumentParser` + `LabelizeProcessRenderer`;
- output PNG decodes back to the exact source value in automated tests;
- blank input rejected;
- payload bounded to 512 UTF-8 bytes;
- cancel/error paths are controlled.

Not part of this MVP:
- Code128 or other symbologies;
- generic barcode/code studio;
- preview/scanner/history/catalog;
- batch QR;
- PDF insertion;
- size/color/ECC editor;
- new runtime QR dependency;
- cloud/backend or second renderer.

Manual Windows UX and representative scanner interoperability remain later QA, not an automated universal claim.

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
- F12.6: operational closure recorded in `.planning/STATE.md` + this roadmap; no extra history Markdown added to avoid documentation noise.

No merge to `main` without explicit user approval.