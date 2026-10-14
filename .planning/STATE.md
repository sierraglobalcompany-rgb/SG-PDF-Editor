---
gsd_state_version: '1.1'
status: f12-functional-baseline-closed-release-open
progress:
  total_phases: 13
  completed_phases: 13
  functional_percent_estimate: 100
  release_readiness_percent_estimate: 90
  overall_percent_estimate: 95
---

# Project State

## Source of truth

Operational authority order:
1. live GitHub exact head + exact-head CI;
2. this file;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. approved slice specs/plans;
6. historical handoffs/checkpoints.

Architecture/product scope remains `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`, except where newer operational decisions explicitly narrow a slice.

**Core value:** resolver PDF + ZPL diario de forma rápida, privada, estable y offline.

## Current position — 2026-10-14

**F0–F12 automated functional baseline: CLOSED.**

Current authoritative functional branch:
`feat/f12-7-pdf-to-png-mvp-green3`

Current exact functional HEAD:
`8e1845d6d8fe72374d7a80994d8500b6d36077cc`

Exact-head Windows CI:
- run `38555829715`;
- job `115824598759`;
- repository hygiene PASS;
- pinned Labelize/qpdf/Tesseract staging PASS;
- locked restore PASS;
- Release build **0 warnings / 0 errors**;
- tests **1202/1202 PASS**, 0 failed, 0 skipped.

`main` remains intentionally untouched at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge/rebase/force-push to `main` is authorized without explicit user approval.

### Release QA checkpoint — Pack A

Canonical Pack A record:
`docs/release/2026-10-14-pack-a-reader-organize-editing.md`

Release-QA branch:
`plan/release-pack-a-qa`

Pack A record commit:
`4e329da2d08f271613c2832f788d780f806ed9c3`

Exact-head CI for that record:
- run `38583576077`;
- job `115919513476`;
- repository hygiene PASS;
- pinned Labelize/qpdf/Tesseract staging PASS;
- locked restore PASS;
- Release build PASS;
- full test step PASS.

Pack A status:
- automated evidence preflight: **PASS**;
- real/manual Windows QA: **NOT RUN — ENVIRONMENT**;
- final Pack A gate: **NOT RUN**;
- no P0/P1 discovered by manual QA because manual QA was not executed;
- no product code, tests, dependencies, PDF engine or runtime architecture changed.

This is an honest environment endpoint, not a manual PASS. Pack A real QA remains an open release gate and must be executed on a suitable Windows/manual environment before release closure.

### Release QA checkpoint — Pack B

Canonical Pack B record:
`docs/release/2026-10-14-pack-b-zpl-qr-physical.md`

Release-QA branch:
`plan/release-pack-b-qa`

Pack B record commit:
`48a5f79bfc804b833bfbb2091995dcd2dc3061a0`

Exact-head CI for that record:
- run `38585443538`;
- job `115926082976`;
- repository hygiene PASS;
- pinned Labelize/qpdf/Tesseract staging PASS;
- locked restore PASS;
- Release build PASS;
- full test step PASS.

Pack B status:
- automated ZPL/QR evidence preflight: **PASS**;
- private Mercado Libre corpus QA: **NOT RUN — ENVIRONMENT**;
- thermal printer / physical-size / ruler QA: **NOT RUN — ENVIRONMENT**;
- real barcode/QR scanner or phone interoperability: **NOT RUN — ENVIRONMENT**;
- physical offline smoke: **NOT RUN — ENVIRONMENT**;
- final Pack B gate: **NOT RUN**;
- no product code, tests, dependencies, renderer or runtime architecture changed.

`ZplGSCustom.ttf` provenance/license remains a separate installer/release gate; Pack B preflight does not close it.

This is an honest environment endpoint, not a physical PASS. Pack B remains an open release gate until representative private/physical evidence is actually executed.

### Release QA checkpoint — Pack C

Canonical Pack C record:
`docs/release/2026-10-14-pack-c-signatures.md`

Release-QA branch:
`plan/release-pack-c-qa`

Pack C record commit:
`c94bff6dad91f3e9c22ecad2a8ab2cf869e991e0`

Exact-head CI for that record:
- run `38586356772`;
- job `115929288257`;
- repository hygiene PASS;
- pinned Labelize/qpdf/Tesseract staging PASS;
- locked restore PASS;
- Release build PASS;
- full test step PASS.

Pack C status:
- automated visual-signature + PFX/P12 evidence preflight: **PASS**;
- real Windows visual-signature UX: **NOT RUN — ENVIRONMENT**;
- real photo/scan cleanup quality: **NOT RUN — ENVIRONMENT**;
- mouse/stylus/touch representative QA: **NOT RUN — ENVIRONMENT**;
- representative local PFX/P12 signing QA: **NOT RUN — ENVIRONMENT**;
- external-reader signature interoperability: **NOT RUN — ENVIRONMENT**;
- password/cancel/failure/destination-preservation manual checks: **NOT RUN — ENVIRONMENT**;
- physical offline signature smoke: **NOT RUN — ENVIRONMENT**;
- final Pack C gate: **NOT RUN**;
- no product code, tests, dependencies, signing engine or runtime architecture changed.

Pack C does not claim Windows Certificate Store, smartcard/HSM, TSA/timestamping, revocation policy, legal/regulatory qualification, universal-reader compatibility or cloud signing. Those remain outside the approved baseline.

This is an honest environment endpoint, not a manual signature PASS. Pack C remains an open release gate until representative real/manual evidence is executed.

## Phase status

| Slice | Automated status |
|---|---|
| F0–F11 | CLOSED / AUTO PASS |
| F12.1 Safe Redaction | CLOSED / AUTO PASS |
| F12.2 Cryptographic Signature PFX/P12 MVP | CLOSED / AUTO PASS |
| F12.3 Existing AcroForm `/Tx` | CLOSED / AUTO PASS |
| F12.4 Compare | CLOSED / AUTO PASS — core + minimal UI |
| F12.5 Batch | CLOSED / AUTO PASS — TextWatermark MVP |
| F12.6 Code Generator | CLOSED / AUTO PASS — QR PNG MVP |
| F12.7 Audited Conversions | **CLOSED / AUTO PASS — PDF→PNG MVP** |
| Release integration/stabilization | OPEN |
| Real/private/manual/physical QA | OPEN |

## F12.7 closed scope — PDF→PNG MVP

KISS flow:
`PDF abierto → Herramientas → Exportar PDF a PNG... → elegir carpeta → PdfDocumentSession/PDFium → PNG por página`.

Closed automated behavior:
- current/open PDF only;
- all pages only;
- fixed export density: **144 DPI**;
- deterministic names: `<basename>-pagina-001.png`, `<basename>-pagina-002.png`, ...;
- existing `PdfDocumentSession`/PDFium remains the render owner;
- WPF `PngBitmapEncoder` is reused; no imaging package added;
- originals remain untouched;
- preflight rejects any target collision before writing;
- `FileMode.CreateNew` provides a second no-overwrite barrier;
- partial outputs created by the operation are best-effort cleaned if a later page fails;
- cancellation is accepted/propagated by the core exporter;
- minimal existing `Herramientas` owner is reused for UI;
- explicit output-folder selection; cancel is a no-op;
- controlled UI error/status handling;
- **0 new runtime dependencies**;
- no second PDF engine, cloud/backend or generic conversion framework.

Explicitly outside this MVP:
- JPEG/WebP/TIFF;
- page ranges;
- DPI/quality studio;
- image→PDF;
- Office↔PDF / PDF→Office;
- LibreOffice/Office automation;
- cloud conversion;
- generic converter registry/plugins/workflow engine.

Conversion classification remains:
- PDF→PNG: CLOSED baseline MVP;
- image→PDF: DEFER / separate capability gate;
- Office/document conversions requiring another engine/cloud/runtime: NO-GO baseline unless separately approved.

## F12.7 TDD / recovery evidence

Core RED:
- SHA `a61ea392484a7c66dc7e15eb17a836a242c3c6d0`;
- expected build failure: `SGPdf.App.Features.Conversions` absent (`CS0234`).

Core GREEN:
- SHA `ede790484b9d6dc638854e560e2717ef18a61708`;
- CI run `38554558763`, job `115820363712`;
- Release build 0 warnings / 0 errors;
- tests 1201/1201 PASS.

UI RED:
- SHA `8e953a7a9d2eca1d2f04dd9a8f261c3bfe4d97e0`;
- test intentionally required `ExportPdfToPng_Click` + `TryExportPdfToPngAsync` before production methods existed.

Recovery checkpoint before GREEN:
- SHA `14bb639734126e1542d616b28638e115dd87e2c7`.

Initial UI GREEN attempt:
- SHA `cc98c5390b58c728c7229e52df5b8bab45d025af`;
- build failed only because `RoutedEventArgs` lacked `using System.Windows;`.

Root-cause correction and final GREEN:
- SHA `8e1845d6d8fe72374d7a80994d8500b6d36077cc`;
- exact-head CI PASS;
- tests 1202/1202 PASS.

## Branch hygiene notes

Authoritative functional continuation remains `feat/f12-7-pdf-to-png-mvp-green3`.

Release QA/preflight progression is recorded through:
- `plan/release-pack-a-qa` — Pack A preflight checkpoint;
- `plan/release-pack-b-qa` — Pack B preflight checkpoint;
- `plan/release-pack-c-qa` — current Pack C preflight checkpoint.

These release-QA planning branches do not replace the functional baseline branch.

Do not use as operational bases:
- divergent historical `feat/f12-7-pdf-to-png`;
- empty aliases accidentally created during branch cutting: `feat/f12-7-pdf-to-png-mvp-green`, `...-green2`;
- older `feat/f12-7-pdf-to-png-mvp` remains the RED/checkpoint lineage, not the final closed head.

The empty aliases contain no unique product commits and are release-hygiene noise only; do not spend a functional slice cleaning them.

## Engineering laws

### KISS
Simple, efficient, optimized, effective. Prefer A→B. No architecture expansion without demonstrated need.

### Anti-noise
Before ADD: DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD. Never stack patches over an unknown root cause.

### Anti-patch flow
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

### Git safety
- no merge to `main` without explicit user approval;
- no destructive branch cleanup during feature work;
- no force-push/rebase as a convenience;
- preserve exact-head evidence.

## Planning estimates

These are planning estimates, not telemetry:
- automated functional implementation: **~100%**;
- release readiness: **~90%**;
- weighted overall project state: **~95%**.

Release readiness remains below 100% because real/private/manual/physical QA, integration/hygiene, installer/runtime smoke and release-candidate validation remain open.

Pack A, Pack B and Pack C preflight documentation/CI do not increase these percentages because their required real/manual/private/physical gates remain NOT RUN.

## Next authoritative work

Functional F12 is frozen. Do **not** add new features opportunistically.

Next sequence:
1. continue bounded QA preparation/execution without converting automated PASS into physical/manual PASS;
2. keep Pack A manual A01–A15 open until a suitable Windows/manual environment executes them;
3. keep Pack B private/physical B01–B11 open until suitable corpus/hardware executes them;
4. keep Pack C real/manual C01–C14 open until suitable Windows/photo/stylus/PFX/external-reader environment executes them;
5. prepare **Pack D — OCR / Forms / Compare / Batch / PDF→PNG** automated evidence map + executable manual checklist, reusing existing evidence and adding no tests unless a real gap is demonstrated;
6. consolidate stale operational docs only where they can misroute work;
7. audit branch/PR ancestry and build one conservative integration candidate;
8. keep `main` read-only until explicit approval;
9. release-candidate gate: clean clone, locked restore, Release build/tests, packaged runtime/installer smoke, offline smoke, no P0/P1 blockers.

Recommended next bounded task:
`Pack D — OCR / Forms / Compare / Batch / PDF→PNG representative QA preflight + executable manual checklist`.

No merge to `main` without explicit user approval.
