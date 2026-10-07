---
gsd_state_version: '1.0'
status: executing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 8
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 3 — F2 Etiquetas. F2.1–F2.4 tienen cierre automatizado PASS. **Siguiente: F2.5 Thermal Print**, que requiere su propio diseño/aprobación antes de implementar. F1 private-corpus acceptance permanece abierta.

## Current Position

F2.4 — **Layout + PDF Export: automated PASS**.  
Branch: `feat/f2-4-layout-pdf-export`.  
PR: #16 draft, base `feat/f2-3-quantity-dimensions`, no merge.  
Functional head before closure docs: `039e43ddee048757b4b71724606c987d19bb0c11` → Windows push CI `37672485887` PASS; Release build 0 warnings / 0 errors; **108 PASS / 0 FAIL / 0 SKIPPED**.  
Closure docs are the final branch mutation; exact-head push/PR CI after that mutation must be recorded in PR #16.

Parallel acceptance gates still open:

- F0 PDF Base: F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.
- F1 Gate ZPL-A: synthetic comparison complete and engine selection approved as **Labelize 1.7.0**; private Mercado Libre ZPL corpus still required for formal F1 close.
- F2 private/manual acceptance: real labels, visual sheet/export comparison, network-disabled/temp-residue smoke, exact thermal printing and scanner QA remain **NOT RUN**.

## Development Tooling

- GSD Core `1.15.0`, project-scoped; `.codex/` ignored.
- Graphify `0.9.77`, project-scoped; `graphify-out/` ignored/regenerable.
- GSD/Graphify are dev-only and never runtime/build dependencies.

## Accumulated Decisions

### Runtime / PDF

- Windows x64 + C# + .NET 10 LTS + WPF remain frozen.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`; no preventive architecture layers.
- PDFium remains the primary PDF reader/render engine and native calls remain globally serialized.
- PDFsharp 6.2.4 entered **only in F2.4** as the local PDF composition/export dependency; it is not the reader/editor engine.
- PDFsharp is pinned in lockfiles, MIT license is stored under `third_party/licenses/PDFsharp-MIT.txt`, and runtime offline guards allow only the approved PDFium + PDFsharp package surface.

### ZPL / Labels

- Gate ZPL-A selected **Labelize 1.7.0**; BinaryKits remains historical Gate evidence only.
- Labelize is a pinned local sidecar; no runtime HTTP/server/download.
- `^PQ` remains metadata, never a render multiplier; quantities use `long` and large output counts are handled lazily.
- F2.3 quantity changes do not rerender Labelize; physical size/dpmm changes do.
- F2.4 `LabelOutputSequence` maps output indexes lazily without allocating one copy object per requested label.
- F2.4 layout math is pure managed C# in millimeters; WPF preview and PDF export consume the same `LabelLayoutPlan`.
- Media: Thermal exact-size, A4 210×297 mm, Letter 215.9×279.4 mm, Custom; layouts 1/2/3/4/6/8/10/12/custom rows×columns.
- Margins/gaps are explicit millimeters; rotation is explicit 0°/90°.
- Impossible layouts fail and disable sheet/export behavior; labels are never silently shrunk to fit.
- Thermal layout remains exactly one label per page.
- Sheet preview is viewport-only display scaling and does not change physical export geometry or call Labelize.
- PDF export is transactional: same-directory temporary file, publish destination only after successful PDF creation; failures preserve an existing destination.
- Exported PDFs are reopened through existing PDFium tests for page count, physical dimensions and renderability.
- WPF test execution is serialized because concurrent `Application.LoadComponent` calls exposed a framework resource-package race; this is test-host hardening, not product synchronization.
- Real Mercado Libre/customer ZPL remains private/local and never enters repo/CI/Graphify.
- No merge to `main` without explicit user approval.

## Evidence

### F0

- F0.1–F0.6 automated PASS; final F0.6 CI `37575306423`.

### F1

- Synthetic Gate head `2df2f374ae6124729389640425dc8334d0647f9f` → `37577735748` PASS; Labelize 1.7.0 selected. Formal private-corpus acceptance pending.

### F2.1 / F2.2 / F2.3

- F2.1 final history head `21fc4e515677b4870fe6319b246d7329bda39ce1` → `37584180581` PASS.
- F2.2 final head `9af600ce19812adb67a11718a47734c346b51bfc` → push `37656292018` PASS + PR `37656303399` PASS; 73 tests.
- F2.3 final head `1f33f48390681c6e4000c17329f1763eb1551047` → push `37659728415` PASS + PR `37659736033` PASS; 83 tests.

### F2.4 — Layout + PDF Export

- Approved design/spec: `docs/superpowers/specs/2026-10-07-f2-4-layout-pdf-export-design.md`.
- Geometry + lazy output sequence and sheet preview were completed before Task 3; preview fixes include `9584885c493219422c3723e877b5e7790c3fb823` and `72e43860215dd8b2815b117f28fe412a3e9bdb34`.
- Task 3 RED `838975b2edfd86383a6537096f8de53b02a9ad9d` → CI `37665150697`: expected compile failure because `LabelPdfExporter` did not exist.
- PDFsharp 6.2.4 lock graph was generated on isolated `tmp/f2-4-lock-probe`, then copied exactly into product lockfiles; feature workflow stayed locked-mode.
- Exporter/dependency baseline head `3ddcf771f6969ea91d748cfc6f712d8e81fac709` → PR CI `37671363640` PASS; 106 tests.
- Export UI RED `3cb60fa50e9c0f28650758e3af90a808c143fd77` → `37671767136`: build 0/0, 106 PASS / 1 expected FAIL because `ExportLabelPdfButton` did not exist.
- Export UI GREEN `f94566f6bd8c2820eb9b7903261ef1270297489c` → PR CI `37672248416` PASS; build 0/0; 107 tests.
- PDFium reopen validation head `039e43ddee048757b4b71724606c987d19bb0c11` → push CI `37672485887` PASS; build 0/0; **108 tests PASS**.
- Whole-branch scope audit: no print-driver code, no runtime network, no BinaryKits fallback, no private fixtures, no silent label scaling; PDFsharp 6.2.4 is the only new runtime package.

## Manual / Private QA Pending

1. private real Mercado Libre `.zpl/.txt/.prn` corpus;
2. visual comparison of label preview vs sheet preview vs exported PDF on Windows;
3. quantities/layouts/custom dimensions against real labels;
4. PDF export to real user-selected paths and overwrite/cancel interaction;
5. network-disabled runtime + temp-residue/privacy smoke;
6. F2.5 exact-size thermal print with real driver/printer;
7. F2.6 barcode/QR decode + physical scanner QA.

## Blockers / Concerns

- No automated technical blocker in F2.4.
- F0 physical smoke remains open.
- F1 formal close waits for private real labels.
- `ZplGSCustom.ttf` provenance must be re-audited before a public production installer.
- Very large output counts are intentionally lazy in planning, but actual PDF generation of extreme page counts is still inherently expensive and should be measured before adding optimization/limits.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| ZPL | Parse/Open | Automated PASS | private QA later |
| ZPL | Labelize preview | Automated PASS | private QA later |
| ZPL | Quantity + physical dimensions/dpmm | Automated PASS | private QA later |
| ZPL | Layout + PDF export | **Automated PASS** | manual/private QA later |
| ZPL | Exact thermal print | **Next — design first** | F2.5 |
| ZPL | Decode/private/physical hardening | Planned | F2.6 |
| PDF | Progressive native rendering | Deferred until measured need | heavy-PDF physical QA |
| Tooling | Automatic GSD↔Graphify integration | Deferred | later tooling upgrade |

## Session Continuity

Last session: 2026-10-07  
Stopped at: **F2.4 automated complete**, PR #16 draft, no merge; final exact-head closure CI must be recorded in PR #16.  
Next technical slice: **F2.5 Windows Thermal Print**. Classify/design/approve it before implementation; do not start printer capability or spooler changes from F2.4.  
Resume files: `docs/history/2026-10-07-F2.4.md`, `.planning/phases/02-f1-zpl-gate/F2.4-PLAN.md`, `docs/superpowers/specs/2026-10-07-f2-4-layout-pdf-export-design.md`.
