---
gsd_state_version: '1.0'
status: executing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 9
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 3 — F2 Etiquetas. **F2.1–F2.5 tienen cierre automatizado PASS. Siguiente: F2.6 Validation + Hardening.** F0 physical smoke y F1 private-corpus acceptance continúan abiertos.

## Current Position

F2.5 — **Windows Thermal Print: automated PASS / physical QA NOT RUN**.  
Branch: `feat/f2-5-windows-thermal-print`.  
PR: #17 draft, base `feat/f2-4-layout-pdf-export`, no merge.  
Functional head before closure docs: `228d045ed34c1eecdcc3892ee1322f9db56fce4d` → push CI `37682279620` PASS + PR CI `37682285176` PASS; Release build 0 warnings / 0 errors; **136 PASS / 0 FAIL / 0 SKIPPED**.  
Closure docs are the final mutation; exact-head push/PR CI must be recorded in PR #17.

Parallel acceptance gates still open:

- F0 PDF Base: F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.
- F1 Gate ZPL-A: synthetic comparison complete and engine selected **Labelize 1.7.0**; private Mercado Libre corpus still required for formal close.
- F2 manual/private: real ZPL corpus, exact physical thermal printing, clipping behavior, barcode/QR scanner and offline/privacy/temp-residue smoke remain **NOT RUN**.

## Development Tooling

- GSD Core `1.15.0`, project-scoped; `.codex/` ignored.
- Graphify `0.9.77`, project-scoped; `graphify-out/` ignored/regenerable.
- GSD/Graphify are dev-only and never runtime/build dependencies.

## Accumulated Decisions

### Runtime / PDF

- Windows x64 + C# + .NET 10 LTS + WPF remain frozen.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium remains the primary PDF reader/render engine and native calls remain globally serialized.
- PDFsharp 6.2.4 is used only for F2.4 label PDF composition/export, not reading/editing.

### ZPL / Labels

- Labelize 1.7.0 is the sole ZPL runtime renderer; BinaryKits remains historical Gate evidence only.
- Runtime remains local/offline; no Labelary/HTTP/server/download.
- `^PQ` is quantity metadata, never a render multiplier; output ordering/counts remain lazy via `LabelOutputSequence`.
- F2.3 physical dimensions/dpmm are authoritative for rendering.
- F2.4 `LabelLayoutPlan` is the single physical layout authority in millimeters for preview/export/thermal print.
- Labels are never silently shrunk to fit.
- F2.5 uses standard Windows installed-driver printing via `PrintQueue` / `PrintTicket` / `PrintCapabilities`.
- F2.5 requests exact thermal `PageMediaSize`, normalizes `CopyCount=1`, requests matching DPI when exposed, then validates via `MergeAndValidatePrintTicket`.
- Driver media quantization tolerance is 0.5 mm per dimension for acceptance only; it never authorizes content scaling.
- `PageImageableArea` can produce clipping warnings but never changes geometry.
- `LabelPrintPaginator` is separate from PDF `PdfDocumentPaginator` because PDF printing intentionally uses fit-to-printable-area while labels require exact physical geometry.
- Thermal print is one label per page; F2.4 rotation is applied exactly once.
- Paginator blocks page counts above `Int32.MaxValue` rather than overflowing.
- No RAW ZPL, vendor SDK, direct USB/serial/socket protocol or PDF intermediary was added.
- `System.Printing` required no new NuGet package.
- Printing does not rerender Labelize or mutate current workspace state.
- Real customer/Mercado Libre ZPL remains private/local and never enters repo/CI/Graphify.
- No merge to `main` without explicit user approval.

## Evidence

### F0 / F1

- F0.1–F0.6 automated PASS; F0.6 CI `37575306423`.
- F1 synthetic Gate head `2df2f374ae6124729389640425dc8334d0647f9f` → `37577735748` PASS; private corpus pending.

### F2.1–F2.4

- F2.1 final history head `21fc4e515677b4870fe6319b246d7329bda39ce1` → `37584180581` PASS.
- F2.2 final head `9af600ce19812adb67a11718a47734c346b51bfc` → push `37656292018` + PR `37656303399` PASS; 73 tests.
- F2.3 final head `1f33f48390681c6e4000c17329f1763eb1551047` → push `37659728415` + PR `37659736033` PASS; 83 tests.
- F2.4 final head `1355ddf1ea92c7b3b25addf05f7722dec6fe57b9` → push `37673096522` + PR `37673103906` PASS; 108 tests.

### F2.5 — Windows Thermal Print

- Approved spec: `docs/superpowers/specs/2026-10-07-f2-5-windows-thermal-print-design.md`.
- Approved TDD plan: `docs/superpowers/plans/2026-10-07-f2-5-windows-thermal-print.md`.
- Task 1 RED `57e286b169bc43576f9453d48b5c73f8ea9c05a4` → `37679965628`; GREEN `731935c3b9533de349cd307d4f3b00e32a9b2f87` → `37680108237`, 118 PASS.
- Task 2 RED `bb75e3138c6c133fc7f5fad2ab6916635c3d38ad` → `37680438244`; GREEN `450e26f1a664355150694f2bf9241ebd2b86ad06` → `37680626270`, 124 PASS.
- Task 3 RED `02152926ef99759c5ad863df556473bb4f63b64d` → `37680987425`; GREEN `fd04e5b106977e48bf03a36853cee2ebb2a067fa` → `37681449064`, 130 PASS.
- Task 4 RED `b8432ff29911b6fe2ebf97bf8e242f507c45c8f2` → `37681780601`: build 0/0; 130 PASS / 6 expected FAIL.
- Functional GREEN `228d045ed34c1eecdcc3892ee1322f9db56fce4d` → push `37682279620` PASS + PR `37682285176` PASS; build 0/0; **136 PASS**.
- Scope audit from F2.4: 13 changed files, no `.csproj`, lockfile, third-party runtime, PDF paginator or private fixture changes.

## Manual / Private QA Pending

1. private real Mercado Libre `.zpl/.txt/.prn` corpus;
2. visual label/sheet/PDF comparison on Windows;
3. real save/cancel/overwrite PDF UX;
4. network-disabled runtime + temp-residue/privacy smoke;
5. physical thermal printer: 102×152, 100×150, 100×100 + custom;
6. physical DPI/clipping/feed behavior on real Windows driver;
7. barcode/QR automatic decode + scanner validation.

## Blockers / Concerns

- No automated technical blocker in F2.5.
- **Physical print correctness is not proven until real hardware QA.** CI validates geometry/tickets/paginator only.
- F0 physical smoke remains open.
- F1 formal close waits private real labels.
- `ZplGSCustom.ttf` provenance must be re-audited before a public installer.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| ZPL | Parse/Open | Automated PASS | private QA |
| ZPL | Labelize preview | Automated PASS | private QA |
| ZPL | Quantity + physical dimensions/dpmm | Automated PASS | private QA |
| ZPL | Layout + PDF export | Automated PASS | manual/private QA |
| ZPL | Windows thermal print | **Automated PASS / physical NOT RUN** | F2.6/manual |
| ZPL | Validation + hardening | **Next** | F2.6 |
| PDF | Progressive native rendering | Deferred | heavy-PDF QA |
| Tooling | Automatic GSD↔Graphify integration | Deferred | later |

## Session Continuity

Last session: 2026-10-07  
Stopped at: **F2.5 automated complete**, PR #17 draft/unmerged; final closure-head CI still to record after this documentation commit.  
Next technical slice: **F2.6 Validation + Hardening**. It owns automatic barcode/QR decode, private corpus, real printer/scanner testing and final offline/privacy/temp audit.  
Resume files: `docs/history/2026-10-07-F2.5.md`, `.planning/phases/02-f1-zpl-gate/F2.5-PLAN.md`, `docs/superpowers/specs/2026-10-07-f2-5-windows-thermal-print-design.md`.
