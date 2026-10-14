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
| F12.7 Audited Conversions | **IN PROGRESS / GATE PASS — PDF→PNG selected** | **current next: PDF→PNG core MVP** |
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
F12.7       IN PROGRESS — capability gate PASS; PDF→PNG selected
Release     Integration + manual/private/physical QA — pending
```

Current closed F12.6 baseline:
- exact final HEAD `6e82e18d60c6c3e8eee74b4e96769a2cb8892e08` on `feat/f12-6-qr-generator`;
- exact-head Release build **PASS / 0 warnings / 0 errors**;
- exact-head tests **1197/1197 PASS**, 0 failed, 0 skipped;
- exact-head CI run `38534984770`, job `115755569827`;
- no merge to `main`.

An earlier implementation run surfaced `NU1902`/`NU1903` for the test-only `System.Security.Cryptography.Pkcs 8.0.1`; the final exact-head locked restore/build did not reproduce them. Advisory/package review remains stabilization work, not part of the conversion MVP.

## Remaining development sequence

### 1. F12.7 Audited Conversions — PDF→PNG next

Capability/scope gate result: **GO for PDF→PNG as the first and only currently approved conversion MVP**.

Why this direction wins:
- it extends an existing owner instead of adding a new engine: `PdfDocumentSession` already owns PDFium rendering;
- it is local/offline;
- fidelity claim is narrow and auditable: render each PDF page to PNG;
- page count, output validity, dimensions, deterministic naming and overwrite behavior are straightforward to test;
- no Office runtime, cloud service or generalized conversion architecture is required.

Frozen next flow:
`current PDF open → PDF→PNG export action → choose output folder → existing PdfDocumentSession/PDFium → render all pages → one PNG/page`.

Frozen contract:
- current/open PDF only;
- all pages only;
- deterministic file names, e.g. `<basename>-pagina-001.png`;
- never silently overwrite an existing output;
- source PDF remains untouched;
- cancel is a no-op;
- controlled errors;
- cancellation propagated;
- reuse the existing PDFium owner rather than duplicating P/Invoke/render abstractions.

Explicitly outside the first MVP:
- JPEG/WebP/TIFF or generic image format selector;
- page ranges;
- custom DPI/quality studio;
- OCR/content interpretation;
- image→PDF in the same slice;
- DOCX/XLSX/PPTX↔PDF;
- PDF→Office;
- LibreOffice runtime;
- Microsoft Office automation;
- cloud conversion;
- second PDF/document engine;
- generic `Converter`, registry, plugin system, workflow designer or queue.

Gate classification:
- **GO — PDF→PNG:** implement next with TDD from the clean F12.6 lineage.
- **DEFER — image→PDF:** potentially valuable but belongs to a separate capability gate because it constructs/assembles a PDF from raster input rather than exporting an existing PDF render. Do not couple the two directions.
- **NO-GO baseline — Office/document conversions:** any path that requires LibreOffice, Office automation, cloud, another document engine or broad fidelity promises needs explicit product value and approval before architecture expands.

Branch hygiene:
- authoritative continuation: `feat/f12-7-conversions-gate`, created directly from exact F12.6 closure `6e82e18d60c6c3e8eee74b4e96769a2cb8892e08`;
- existing `feat/f12-7-pdf-to-png` is a **divergent/non-authoritative prototype**; it carries unrelated ancestry/work and is evidence only, not a merge or implementation base;
- no cleanup/rebase/merge of that prototype is authorized here.

Next bounded implementation task:
1. add RED contract for PDF→PNG core;
2. verify intended RED;
3. minimal GREEN reusing `PdfDocumentSession`;
4. test multiple pages, PNG signature/dimensions, deterministic naming, overwrite refusal, original preservation and cancellation/error behavior;
5. full exact-head CI;
6. only then expose minimal UI/export if the core is clean.

### 2. Real QA packs

After functional F12, group the accumulated physical/manual/private work into bounded packs instead of one giant final QA block:
- Reader/Organize/Editing;
- ZPL + physical print/scan;
- visual + cryptographic signatures;
- OCR/Forms/Compare/Batch/QR/Conversions.

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
- review dependency advisories and close installer/license provenance items.

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
- AI/cloud/backend features;
- Office/document conversion engines.

## Operating cadence

> **1 `continua` = 1 meaningful bounded product task closed**, with RED → GREEN → focused verification → full CI → KISS/noise audit executed internally for implementation tasks.

Capability gates intentionally stop after evidence + frozen scope, before production implementation.

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
- F12.7 capability gate: operational decision recorded in `.planning/STATE.md` + this roadmap; no extra history Markdown added.

No merge to `main` without explicit user approval.