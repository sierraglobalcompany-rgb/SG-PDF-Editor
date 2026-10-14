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

Authoritative continuation is `feat/f12-7-pdf-to-png-mvp-green3`.

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

## Next authoritative work

Functional F12 is frozen. Do **not** add new features opportunistically.

Next sequence:
1. execute representative QA packs without converting automated PASS into physical/manual PASS;
2. consolidate stale operational docs only where they can misroute work;
3. audit branch/PR ancestry and build one conservative integration candidate;
4. keep `main` read-only until explicit approval;
5. release-candidate gate: clean clone, locked restore, Release build/tests, packaged runtime/installer smoke, offline smoke, no P0/P1 blockers.

Recommended first next bounded task:
`Pack A — Reader / Organize / Editing representative QA preflight + executable manual checklist`, unless a newer exact-head release checkpoint already supersedes it.

No merge to `main` without explicit user approval.
