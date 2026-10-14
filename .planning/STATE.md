---
gsd_state_version: '1.1'
status: f12-7-conversions-gate-passed-in-progress
progress:
  total_phases: 13
  completed_phases: 12
  functional_percent_estimate: 99
  release_readiness_percent_estimate: 88
  overall_percent_estimate: 94
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

Current functional branch: `feat/f12-7-conversions-gate`.

| Slice | Status |
|---|---|
| F0–F11 | CLOSED / AUTO PASS, with manual/private/physical gates still open where documented |
| F12.1 Safe Redaction | CLOSED / AUTO PASS |
| F12.2 Cryptographic Signature PFX/P12 MVP | CLOSED / AUTO PASS |
| F12.3 Existing AcroForm `/Tx` | CLOSED / AUTO PASS |
| F12.4 Compare | CLOSED / AUTO PASS — core + minimal UI |
| F12.5 Batch | CLOSED / AUTO PASS — TextWatermark MVP |
| F12.6 Code Generator | CLOSED / AUTO PASS — QR PNG MVP |
| F12.7 Audited Conversions | **IN PROGRESS / GATE PASS — PDF→PNG MVP next** |
| Release integration/stabilization | OPEN |

Current exact F12.6 closure baseline:
- branch: `feat/f12-6-qr-generator`;
- exact final HEAD: `6e82e18d60c6c3e8eee74b4e96769a2cb8892e08`;
- Windows Release build: **PASS / 0 warnings / 0 errors**;
- tests: **1197/1197 PASS**, 0 failed, 0 skipped;
- exact-head CI run: `38534984770`, job `115755569827`;
- no merge to `main`.

An earlier F12.6 implementation run exposed `NU1902`/`NU1903` for test dependency `System.Security.Cryptography.Pkcs 8.0.1`; the final exact-head locked restore/build did not reproduce those warnings. Package/advisory review remains a stabilization concern rather than part of F12.7 conversion scope.

## Engineering progress estimates

These are planning estimates, not telemetry.

- **Automated functional implementation: ~99%.**
- **Release readiness including manual/private/physical QA and integration: ~88%.**
- **Headline weighted project state: ~94%.**

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

## F12.6 Code Generator — closed scope

Capability gate: **PASS for QR by reusing the existing Labels/ZPL/Labelize owner**.

Closed KISS flow:
`Herramientas → Generar código QR... → enter text/URL → Guardar PNG → QrCodeGenerator → existing Labelize renderer → local PNG`.

Automated evidence proves:
- local/offline QR generation through already-approved Labelize 1.7.0;
- PNG output decodes back to the exact original value;
- UTF-8 content is preserved;
- ZPL-looking payload text such as `^XZ` and `~JA` remains data rather than becoming commands;
- blank values are rejected;
- payload is bounded to 512 UTF-8 bytes;
- minimal WPF surface exposes value entry, explicit PNG destination and the existing QR owner;
- cancel and controlled errors do not create hidden output.

KISS / anti-noise result:
- **0 new runtime dependencies**;
- no QRCoder or ZXing production dependency was added; existing test-only decode infrastructure remains verification-only;
- no generic barcode studio, symbology factory, plugin architecture, cloud/backend or second renderer;
- no preview, scanner, batch QR, PDF insertion, size/color/ECC studio or history/catalog;
- Code128 and other symbologies remain outside this MVP until value is demonstrated.

Manual Windows end-to-end UX smoke remains part of the later representative QA packs; automated closure is not a physical/mobile scanner interoperability claim.

## F12.7 Audited Conversions — capability gate

Gate result: **PASS for one narrow first conversion: PDF→PNG**.

Why PDF→PNG is the selected first MVP:
- the authoritative baseline already has `PdfDocumentSession` as the PDFium render owner;
- conversion is an export of an existing render capability, not a second PDF engine;
- it remains local/offline and deterministic;
- output validity, dimensions, naming, overwrite protection and page count are directly testable;
- a divergent historical prototype branch (`feat/f12-7-pdf-to-png`) independently demonstrates feasibility through the same PDFium owner, but it is **not an operational base** because it diverges from the closed F12.6 lineage and carries unrelated work.

Frozen next MVP flow:
`open local PDF → choose PDF→PNG export → select output folder → existing PdfDocumentSession/PDFium renders every page → one PNG per page`.

Frozen MVP contract:
- current/open PDF only;
- all pages only;
- deterministic names such as `<basename>-pagina-001.png`;
- refuse silent overwrite;
- original PDF untouched;
- controlled error/cancel behavior;
- cancellation propagated;
- reuse `PdfDocumentSession`; no duplicate PDFium wrapper.

Explicitly excluded from this first conversion slice:
- JPEG/WebP/TIFF or generic image-format selector;
- page-range selector;
- custom DPI/quality studio unless separately justified;
- OCR or content interpretation;
- image→PDF in the same slice;
- Office formats;
- cloud/backend;
- second renderer/PDF engine;
- generic converter framework, registry, plugin system or workflow engine.

Conversion classification after the gate:
- **GO:** PDF→PNG, next bounded MVP.
- **DEFER:** image→PDF; useful potential direction, but it creates/assembles PDF from raster input and is a separate capability/quality contract rather than the same render-export path.
- **NO-GO for baseline F12.7:** DOCX/XLSX/PPTX↔PDF, PDF→Office and other conversions that would require LibreOffice, Office automation, cloud conversion, a second document engine, or broad fidelity claims. They require a separate value/capability decision and explicit approval.

## Operational audit

### Architecture

**PASS.** The product architecture remains narrow and local-first:
- PDFium remains primary PDF inspection/render owner and is the approved owner for PDF→PNG;
- PDFsharp/qpdf/Tesseract remain narrow, already-approved tools;
- Labelize remains the single approved ZPL renderer;
- WPF partials/features remain focused;
- no generalized enterprise/conversion architecture was introduced.

### Documentation drift

**CORRECTED through the F12.7 capability gate.** Operational sources now freeze PDF→PNG as the next narrow MVP instead of treating F12.7 as a universal conversion phase.

### Branch drift

`feat/f12-7-pdf-to-png` is a **divergent/non-authoritative prototype** relative to the exact F12.6 closure baseline. It must not be merged or used as the implementation base. Current authoritative continuation is `feat/f12-7-conversions-gate`, created directly from `6e82e18d60c6c3e8eee74b4e96769a2cb8892e08`.

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

Compare, Batch and QR still belong in later representative real/manual QA packs. PDF→PNG will require representative visual/export QA after its automated MVP closes.

## Operating cadence

> **1 `continua` = 1 meaningful bounded product task closed.**

Inside an implementation task, ChatGPT executes:
1. RED;
2. verify intended failure;
3. minimal GREEN;
4. focused verification;
5. exact-head full CI;
6. KISS/Ley del Ruido audit;
7. checkpoint + percentage/state update.

Capability gates may close with evidence + frozen scope before production code, by design.

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
F12.7 PDF→PNG core MVP
→ existing PdfDocumentSession/PDFium owner
→ current PDF, all pages
→ deterministic PNG names + refuse overwrite
→ original untouched + cancellation/error contract
→ RED → minimal GREEN → exact-head CI
→ no generic conversion framework or extra formats
```

No merge to `main` without explicit user approval.