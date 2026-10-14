---
gsd_state_version: '1.1'
status: f12-6-code-generator-capability-gated
progress:
  total_phases: 13
  completed_phases: 12
  functional_percent_estimate: 98
  release_readiness_percent_estimate: 88
  overall_percent_estimate: 93
---

# Project State

## Source of truth

- Architecture/product scope: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.
- Operational state: this file + `.planning/ROADMAP.md`.
- Detailed acceptance: approved slice specs/plans.
- Reality check: GitHub exact heads and CI override stale snapshots.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.

## Current position — 2026-10-14

F12 Professional is **IN PROGRESS**.

Current functional branch: `feat/f12-5-batch-contract`.

| Slice | Status |
|---|---|
| F0–F11 | CLOSED / AUTO PASS, with manual/private/physical gates still open where documented |
| F12.1 Safe Redaction | CLOSED / AUTO PASS |
| F12.2 Cryptographic Signature PFX/P12 MVP | CLOSED / AUTO PASS |
| F12.3 Existing AcroForm `/Tx` | CLOSED / AUTO PASS |
| F12.4 Compare | CLOSED / AUTO PASS — core + minimal UI |
| F12.5 Batch | **CLOSED / AUTO PASS — TextWatermark MVP** |
| F12.6 Code Generator | **CAPABILITY GATE COMPLETE / IMPLEMENTATION BLOCKED ON DEPENDENCY APPROVAL** |
| F12.7 Audited Conversions | PLANNED / NOT STARTED |
| Release integration/stabilization | OPEN |

Current exact implementation baseline before the F12.6 capability-gate documentation commit:
- branch: `feat/f12-5-batch-contract`;
- exact HEAD: `ea8486ec2aaf6ebe26dd8947c925f32eef092e2d`;
- Windows Release build: **0 warnings / 0 errors**;
- tests: **1192/1192 PASS**, 0 failed, 0 skipped;
- exact-head CI run: `38509929841`, job `115675852458`;
- no merge to `main`.

## Engineering progress estimates

These are planning estimates, not telemetry.

- **Automated functional implementation: ~98%.**
- **Release readiness including manual/private/physical QA and integration: ~88%.**
- **Headline weighted project state: ~93%.**

Why release readiness is lower than functional completion:
1. representative real/private/manual/physical QA remains open across several slices;
2. interoperability/real-device checks remain open where documented;
3. the active development lineage remains far ahead of `main`;
4. stacked branches/PRs need deliberate release integration/hygiene.

## F12.3 Forms closed scope

- existing classic AcroForm text fields (`/Tx`) only;
- flat, hierarchical and inherited `/FT`;
- read/edit/save transactional copy;
- original protected;
- value + appearance + representative structure validation;
- **representative preservation gate** only, not universal preservation;
- no `/Btn`, `/Ch`, XFA, JS/calculation/actions, form designer, flattening or universal-reader guarantee.

Closure: `docs/history/2026-10-13-F12-3-exact-head-closure.md`.

## F12.4 Compare closed scope

### Core

`PdfCompareService` reuses existing PDFium signals:
- page count;
- text objects;
- rendered pixels.

Closed automated behavior:
1. identical documents;
2. text-only change;
3. visual-only change;
4. structural page-count difference.

`VisualChanged` intentionally means **visual change independent of text**. The core does not claim decomposition of an additional graphic change when text also changed.

Core closure: `docs/history/2026-10-14-F12-4-compare-core-closure.md`.

### Minimal UI

Closed KISS flow:
`PDF A already open → Archivo > Comparar con otro PDF... → choose PDF B → PdfCompareService.Compare(A,B) → existing right panel summary → close/reset`.

Automated UI closure includes:
- local B selection with cancel as no-op;
- reuse of existing `PdfCompareService`;
- A/B names and page counts;
- text, visual-independent, structure and overall status;
- close/reset without replacing A;
- existing Edit/Organize/Sign/Form guards;
- controlled failure for invalid/unreadable/protected B;
- shared workspace reset owner instead of duplicated mode-specific resets.

No second viewer, new sidebar, ModeManager, cloud/backend, new PDF engine, global XAML change or new dependency was introduced.

UI design: `docs/superpowers/specs/2026-10-14-f12-4-compare-ui-design.md`.  
UI implementation plan: `docs/superpowers/plans/2026-10-14-f12-4-compare-ui.md`.  
Exact-head closure: `docs/history/2026-10-14-F12-4-compare-ui-closure.md`.

## F12.5 Batch closed scope

Closed MVP is deliberately narrow: **text watermark batch only**.

KISS flow:
`Herramientas → Marca de agua por lotes... → select local PDFs → reuse existing watermark dialog → select output folder → sequential processing → one result per input + final summary`.

Closed automated behavior:
- explicit multi-file local PDF input;
- explicit output directory;
- existing `WatermarkUtilityRequest`, `WatermarkPlanner` and `PdfTextOverlayWriter` reused instead of duplicating a pipeline;
- `Todas las páginas` resolves independently against each PDF's own page count;
- current-page scope is unavailable in batch mode;
- sequential execution only;
- each file returns success/error independently, so one unreadable/missing PDF does not stop the remaining items;
- existing output is not silently overwritten;
- original input is preserved and output is written as a copy;
- empty batch and blank output directory are rejected at the batch request boundary;
- UI summary reports successes and item-level errors.

Explicitly not introduced:
- generic `BatchEngine`;
- workflow designer;
- database/job store;
- worker/scheduler/task queue;
- concurrency framework;
- new PDF engine, backend/cloud or dependency;
- new XAML surface.

Further batch operations such as Bates/page numbering, OCR, protection or organize actions require a separate value/capability decision; they are not implicitly part of the closed F12.5 MVP.

## F12.6 Code Generator capability gate

**Capability gate: COMPLETE. Implementation: BLOCKED pending explicit dependency approval.**

Repository evidence at `ea8486ec2aaf6ebe26dd8947c925f32eef092e2d`:
- `SGPdf.App.csproj` has no QR/barcode generation dependency;
- `packages.lock.json` contains no ZXing, QRCoder, SkiaSharp or ImageSharp package;
- repository search found no existing QR, Code128 or generic barcode generator owner to reuse;
- existing PDF/text rendering owners do not supply the missing symbol/matrix generation capability.

Frozen KISS decision:
1. do not hand-roll QR/Code128 encoding;
2. do not create a generic barcode studio or symbology plugin architecture;
3. do not add a dependency without explicit approval;
4. if dependency addition is approved, first MVP should be **QR only + standalone PNG export**, local/offline and deterministic;
5. Code128 is deferred to a separate value/capability decision after QR;
6. EAN/UPC/DataMatrix/PDF417 and bulk generation remain deferred;
7. PDF insertion is deferred until the standalone QR asset path is proven, avoiding two concerns in the first slice.

No production code, package reference or new runtime was added by this gate.

## Operational audit

### Architecture

**PASS.** The product architecture remains narrow and local-first:
- PDFium remains primary PDF inspection/render owner;
- PDFsharp/qpdf/Tesseract remain narrow, already-approved tools;
- WPF partials/features remain focused;
- no generalized enterprise architecture was introduced.

### Documentation drift

**CORRECTED through F12.6 capability gate.** Operational sources record that Code Generator cannot enter implementation until its new dependency is explicitly approved.

### Integration drift

**OPEN / HIGH PRIORITY BEFORE RELEASE.**

`main` remains intentionally untouched. No merge/rebase/branch cleanup is authorized by this document. Integration requires an explicit, separately approved consolidation gate.

### Manual/private/physical QA

**OPEN.** Automated PASS does not replace:
- representative private PDF/ZPL corpora;
- Windows end-to-end UX smoke;
- relevant physical printer/scanner/stylus/hardware checks;
- external reader interoperability where applicable;
- real certificates/signing corpus where applicable.

Compare and Batch still belong in later representative real-PDF/manual QA packs; automated closure is not a claim of universal PDF compatibility or throughput.

## Operating cadence

> **1 `continua` = 1 meaningful bounded product task closed.**

Inside that task, ChatGPT executes:
1. RED;
2. verify intended failure;
3. minimal GREEN;
4. focused verification;
5. exact-head full CI;
6. KISS/Ley del Ruido audit;
7. checkpoint + percentage/state update.

Stop early only for:
- a real product decision;
- architectural fork;
- new dependency/second engine;
- capability NO-GO;
- unsafe/irreversible Git integration;
- scope requiring explicit approval.

Operational docs update once per meaningful closed task/slice, not after every tiny implementation step.

## Next

```text
F12.6 Code Generator — dependency approval gate
→ recommended first slice if approved: QR only + standalone PNG export
→ keep Code128 and PDF insertion deferred
→ no generic barcode studio, plugin architecture or cloud/backend
```

No merge to `main` without explicit user approval.