---
gsd_state_version: '1.1'
status: f12-6-qr-closed-professional-in-progress
progress:
  total_phases: 13
  completed_phases: 12
  functional_percent_estimate: 99
  release_readiness_percent_estimate: 89
  overall_percent_estimate: 94
---

# Project State

## Source of truth

- Architecture/product scope: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.
- Operational state: this file + `.planning/ROADMAP.md`.
- Detailed acceptance: `.planning/REQUIREMENTS.md` + approved slice specs/plans.
- Reality check: GitHub exact heads and CI override stale snapshots.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.

## Current position — 2026-10-14

F12 Professional is **IN PROGRESS**.

Current functional branch: `feat/f12-6-qr-generator-ui`.

| Slice | Status |
|---|---|
| F0–F11 | CLOSED / AUTO PASS, with manual/private/physical gates still open where documented |
| F12.1 Safe Redaction | CLOSED / AUTO PASS |
| F12.2 Cryptographic Signature PFX/P12 MVP | CLOSED / AUTO PASS |
| F12.3 Existing AcroForm `/Tx` | CLOSED / AUTO PASS |
| F12.4 Compare | CLOSED / AUTO PASS — core + minimal UI |
| F12.5 Batch | CLOSED / AUTO PASS — TextWatermark MVP |
| F12.6 Code Generator | **CLOSED / AUTO PASS — QR PNG MVP** |
| F12.7 Audited Conversions | PLANNED / NOT STARTED |
| Release integration/stabilization | OPEN |

Current exact F12.6 user-facing implementation baseline before this documentation-closure commit:
- branch: `feat/f12-6-qr-generator-ui`;
- exact HEAD: `2be0fc5345160e500d78102d99a833c9b9729bae`;
- repository hygiene: **PASS**;
- locked restore/runtime staging: **PASS**;
- Windows Release build step: **PASS**;
- full Test step: **PASS**;
- exact-head CI run: `38513237239`, job `115686247754`;
- no merge to `main`.

## Engineering progress estimates

These are planning estimates, not telemetry.

- **Automated functional implementation: ~99%.**
- **Release readiness including manual/private/physical QA and integration: ~89%.**
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

Automated UI closure includes local B selection/cancel, existing service reuse, A/B summary, close/reset, existing mode guards, controlled errors and shared workspace reset. No second viewer/sidebar, ModeManager, cloud/backend, new PDF engine, global XAML architecture or new dependency was introduced.

UI design: `docs/superpowers/specs/2026-10-14-f12-4-compare-ui-design.md`.  
UI implementation plan: `docs/superpowers/plans/2026-10-14-f12-4-compare-ui.md`.  
Exact-head closure: `docs/history/2026-10-14-F12-4-compare-ui-closure.md`.

## F12.5 Batch closed scope

Closed MVP is deliberately narrow: **text watermark batch only**.

KISS flow:
`Herramientas → Marca de agua por lotes... → select local PDFs → reuse existing watermark dialog → select output folder → sequential processing → one result per input + final summary`.

Closed automated behavior:
- explicit multi-file local PDF input/output directory;
- existing watermark request/planner/writer reused;
- all-pages resolved per document;
- sequential execution only;
- per-item success/error without stopping the remaining items;
- originals protected and no silent output overwrite;
- final UI summary.

Explicitly not introduced: generic BatchEngine, workflow designer, database/job store, workers/scheduler/queue, concurrency framework, new PDF engine, backend/cloud or dependency.

## F12.6 Code Generator closed scope

Capability gate: **PASS for QR using the existing Labels owner**.

Closed KISS flow:
`Herramientas → Generar código QR... → texto/URL → QrCodeGenerator → existing ZPL parser + Labelize renderer → explicit Guardar PNG`.

Automated evidence proves:
- local/offline QR generation through already-approved Labelize 1.7.0;
- PNG output decodes back to the exact original value;
- UTF-8 content is preserved;
- ZPL-looking payload text such as `^XZ` and `~JA` remains field data rather than becoming commands;
- blank values are rejected;
- payload is bounded to 512 UTF-8 bytes;
- user-facing menu entry reuses the existing `Herramientas` owner;
- input is collected by one focused dialog;
- PNG destination is explicit and `OverwritePrompt=true` prevents silent replacement;
- cancel paths are controlled and do not alter an open document;
- generated QR does not require a PDF to be open.

KISS / anti-noise result:
- **0 new runtime dependencies**;
- no ZXing production dependency; existing test-only decoder is verification-only;
- no generic barcode studio, symbology factory/plugin architecture, cloud/backend or second renderer;
- no parallel top-level menu owner;
- Code128 remains outside baseline until separate value/capability approval.

Manual phone/scanner verification of generated QR belongs to the later ZPL/physical or professional QA pack; automated decoder PASS does not claim every physical scanner/environment.

## Operational audit

### Architecture

**PASS.** PDFium remains primary PDF owner; PDFsharp/qpdf/Tesseract stay narrow approved tools; Labelize remains the single approved ZPL/QR renderer; WPF feature partials remain focused; no generalized enterprise architecture was introduced.

### Documentation drift

**CORRECTED through F12.6.** Operational sources now reflect QR PNG MVP as CLOSED / AUTO PASS.

### Integration drift

**OPEN / HIGH PRIORITY BEFORE RELEASE.** `main` remains intentionally untouched. No merge/rebase/branch cleanup is authorized by this document.

### Manual/private/physical QA

**OPEN.** Automated PASS does not replace representative private corpora, Windows end-to-end UX smoke, relevant hardware checks, external-reader interoperability, real certificates/signing corpus or physical QR scanning where applicable.

## Operating cadence

> **1 `continua` = 1 meaningful bounded product task closed.**

Inside that task, ChatGPT executes RED → intended failure → minimal GREEN → focused verification → exact-head full CI → KISS/Ley del Ruido audit → checkpoint/state update.

Stop early only for a real product decision, architectural fork, new dependency/second engine, capability NO-GO, unsafe Git integration or scope requiring explicit approval.

## Next

```text
F12.7 Audited Conversions — capability/scope gate
→ inventory conversion requests against existing owners
→ approve one narrow conversion only if quality can be validated locally/offline
→ no universal converter, second PDF engine, LibreOffice/Office automation or cloud dependency without explicit decision
```

No merge to `main` without explicit user approval.