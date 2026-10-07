---
gsd_state_version: '1.0'
status: executing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 10
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 3 — F2 Etiquetas tiene **F2.1–F2.6 automated PASS**. Los gates privados/físicos permanecen abiertos en paralelo. **Siguiente slice de producto: F3 Firma Visual, diseño/aprobación antes de implementar.**

## Current Position

F2.6 — **Validation + Hardening: automated PASS / private corpus NOT RUN / physical printer-scanner NOT RUN**.  
Branch: `feat/f2-6-validation-hardening`.  
PR: #18 draft, base `feat/f2-5-windows-thermal-print`, no merge.  
Functional head before closure docs: `b69195ae0fe09edffdba9aeb8829fc9e225c54d5` → PR CI `37689872152` PASS; Release build 0 warnings / 0 errors; **144 PASS / 0 FAIL / 0 SKIPPED**.  
Closure docs are the final branch mutation; exact-head push/PR CI is recorded in PR #18 after this documentation commit.

Parallel acceptance gates still open:

- F0 PDF Base: F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.
- F1 Gate ZPL-A: synthetic comparison complete and engine selected **Labelize 1.7.0**; private Mercado Libre corpus still required for formal close.
- F2 private real-label corpus: **NOT RUN**. Use `tools/run-private-label-qa.ps1` locally with ignored `tests/PrivateFixtures/labels`.
- F2 physical thermal printer/ruler/scanner: **NOT RUN**. Follow `docs/qa/F2.6-PHYSICAL-QA.md`.

## Development Tooling

- GSD Core `1.15.0`, project-scoped; `.codex/` ignored.
- Graphify `0.9.77`, project-scoped; `graphify-out/` ignored/regenerable.
- GSD/Graphify are dev-only and never runtime/build dependencies.

## Accumulated Decisions

### Runtime / PDF

- Windows x64 + C# + .NET 10 LTS + WPF remain frozen.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium remains the primary PDF reader/render engine and native calls remain globally serialized.
- PDFsharp 6.2.4 is used only for label PDF composition/export, not reading/editing.

### ZPL / Labels

- Labelize 1.7.0 is the sole ZPL runtime renderer; BinaryKits remains historical Gate evidence only.
- Runtime remains local/offline; no Labelary/HTTP/server/download.
- `^PQ` is quantity metadata, never a render multiplier; output ordering/counts remain lazy via `LabelOutputSequence`.
- Physical dimensions in millimeters and selected dpmm are authoritative; labels are never silently shrunk to fit.
- F2.4 `LabelLayoutPlan` is the single layout authority for preview/export/thermal printing.
- F2.5 uses installed Windows drivers through `PrintQueue` / `PrintTicket` / `PrintCapabilities`; `CopyCount=1`; imageable area warns only.
- No RAW ZPL, vendor SDK, direct USB/serial/socket transport or PDF intermediary was added.
- F2.6 barcode validation is **test/QA-only**. `ZXing.Net 0.16.11` remains in `SGPdf.App.Tests`; it is not a runtime dependency.
- F2.6 validates real pipeline output: Labelize PNG → PDFsharp export → PDFium raster at 300 DPI → exact ZXing Code128/QR decode, including 90° label rotation.
- Private QA is explicit opt-in through `SGPDF_PRIVATE_QA_ROOT` / `tools/run-private-label-qa.ps1`; absence of fixtures is `NOT RUN`, never private PASS.
- Private/customer ZPL, expectation JSON, renders, screenshots, scanner captures and completed private QA reports never enter repo/CI/Graphify.
- F2.6 added no `src/SGPdf.App`, `.csproj`, package or lockfile changes.
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

- Final head `c003d6512a5df5be2f53aab2262d09c6dcbf92cf` → push `37682952554` + PR `37682957831` PASS; **136 tests**.
- Physical printer/ruler/scanner acceptance remains NOT RUN.

### F2.6 — Validation + Hardening

- Approved spec: `docs/superpowers/specs/2026-10-07-f2-6-validation-hardening-design.md`.
- Approved plan: `docs/superpowers/plans/2026-10-07-f2-6-validation-hardening.md`.
- Task 1 PDF decode RED `1b511172fc91b413ae98be112943e29c90fcf3ab` → PR CI `37688599916`: expected 3 `CS0103` because `BarcodeDecodeAssert` did not exist.
- Task 1 GREEN `e862144c9794795b9c72cfbfb9458c39409ea960` → PR CI `37688842446` PASS; build 0/0; **138 tests**.
- Task 2 private corpus RED `8b1a7449a1959b2d07959f149b7e5c8d98f6b741` → PR CI `37689085073`: expected 5 errors because `PrivateLabelCorpusQa` did not exist.
- Task 2 GREEN `1c065d718268c9cfcddbf1f2531fe98fc20e9a53` → PR CI `37689526677` PASS; build 0/0; **143 tests**.
- Task 3 residue/privacy evidence head `b69195ae0fe09edffdba9aeb8829fc9e225c54d5` → PR CI `37689872152` PASS; build 0/0; **144 tests**. New residue assertions passed without production cleanup changes.
- Functional scope audit against F2.5: tests/dev tooling/docs only; no runtime source/package/lock/private-fixture changes.

## Manual / Private QA Pending

1. Run the user's private real Mercado Libre `.zpl/.txt/.prn` corpus with `tools/run-private-label-qa.ps1`.
2. Physical thermal print at 102×152, 100×150 and 100×100 mm using a real installed driver.
3. Measure real printed dimensions and observe clipping/feed/alignment behavior.
4. Scan representative Code128 and QR physically and compare exact payload text.
5. F0 broader Windows UI/print/offline physical smoke.

## Blockers / Concerns

- No automated technical blocker in F2.6.
- **Automated barcode decode does not prove scanner/paper behavior.** Physical acceptance remains separate.
- F1 formal close waits private real labels.
- `ZplGSCustom.ttf` provenance must be re-audited before a public installer.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| ZPL | Parse/Open through validation/hardening | **Automated PASS** | private/physical QA |
| ZPL | Private real-label corpus | **NOT RUN** | local QA |
| ZPL | Physical thermal + scanner | **NOT RUN** | hardware QA |
| PDF | F3 Firma Visual | **Next — design first** | F3 |
| PDF | Progressive native rendering | Deferred | heavy-PDF QA |
| Tooling | Automatic GSD↔Graphify integration | Deferred | later |

## Session Continuity

Last session: 2026-10-07  
Stopped at: **F2.6 automated functional complete**, PR #18 draft/unmerged; closure docs are landing and exact closure-head CI must be recorded in PR #18.  
Next product slice after closure: **F3 Firma Visual**. Classify/design/approve before implementation. F2 private and physical gates remain open in parallel and must never be inferred from synthetic CI.  
Resume files: `docs/history/2026-10-07-F2.6.md`, `docs/qa/F2.6-PHYSICAL-QA.md`, `docs/superpowers/specs/2026-10-07-f2-6-validation-hardening-design.md`, `docs/superpowers/plans/2026-10-07-f2-6-validation-hardening.md`.
