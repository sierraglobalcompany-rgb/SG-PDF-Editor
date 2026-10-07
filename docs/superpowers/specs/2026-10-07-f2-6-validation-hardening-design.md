# F2.6 — Validation + Hardening — Design

**Date:** 2026-10-07  
**Status:** written design awaiting explicit review/approval  
**Base:** F2.5 final head `c003d6512a5df5be2f53aab2262d09c6dcbf92cf`  
**Branch:** `feat/f2-6-validation-hardening`

## Purpose

Close the ZPL feature block with reproducible evidence that the label pipeline preserves machine-readable Code128/QR content, that real private labels can be exercised locally without entering GitHub/CI/Graphify, and that the runtime remains offline/private with temporary files cleaned up.

F2.6 may achieve **automated PASS** in CI, but **F2 physical acceptance remains open** until real printer/ruler/scanner QA and the user's private corpus are actually run on a Windows machine with the required hardware/data.

## Existing baseline

F2.1–F2.5 already provide:

- parse/open of `.zpl/.txt/.prn`;
- Labelize 1.7.0 local/offline PNG rendering;
- quantity and physical dimensions/dpmm;
- pure-mm layouts and PDF export;
- exact-size Windows thermal-print workflow;
- runtime network guards;
- temp cleanup in Labelize paths;
- `tests/PrivateFixtures/` ignored by Git;
- `ZXing.Net 0.16.11` already pinned in **tests only**;
- existing Code128 + QR decode regression tests for Labelize PNG output.

Therefore F2.6 introduces **no runtime barcode decoder** and should require **no new production package**.

## Scope

### 1. Digital barcode/QR validation

Extend the existing test-only decode evidence rather than building a new validation subsystem.

Required synthetic cases:

1. Code128 rendered by Labelize decodes to the exact expected payload.
2. QR rendered by Labelize decodes to the exact expected payload.
3. A thermal PDF exported through the real F2.4 `LabelPdfExporter`, reopened/rendered through the existing PDFium path at a barcode-safe DPI, still decodes to the exact expected payload.
4. Repeat the PDF path for both Code128 and QR.
5. Include one 90° label-rotation case so rotation is proven not to corrupt machine readability.

Validation compares **decoded format + exact text**, not merely "decoder returned something".

Digital validation remains test-only. No scanner UI, no barcode-reading command, and no ZXing reference enters `src/SGPdf.App`.

### 2. Private real-label corpus

Private Mercado Libre/customer labels remain local and ignored.

Canonical local root:

`tests/PrivateFixtures/`

Recommended structure:

```text
tests/PrivateFixtures/
  labels/
    case-001.zpl
    case-001.expected.json
    case-002.prn
    case-002.expected.json
```

The sidecar expectation file is private together with the label. At minimum it may declare:

- expected design count;
- expected physical size/dpmm when known;
- expected Code128/QR payload(s) when present;
- optional notes for visual/manual comparison.

No real customer names, addresses, order IDs, ZPL, rendered PNGs, PDFs, expected JSON, screenshots, scanner captures or QA reports are committed to the public repository.

A **versioned local QA entry point** will document/run the private corpus from the ignored directory. Standard CI must not require the directory and must never download or synthesize private data.

Private QA must report clearly whether it was actually run; absence of private fixtures must never be interpreted as private acceptance PASS.

### 3. Offline / privacy / temporary-residue hardening

Reuse and strengthen current guards.

Automated evidence must cover:

- runtime project still has only the already-approved production package surface;
- no direct runtime HTTP/web/socket/navigation client is introduced;
- Labelize temporary working directories are removed after success;
- cleanup remains covered after renderer failure;
- cleanup remains covered after cancellation/process termination;
- PDF export transactional temporary files do not remain after success/failure paths already covered by F2.4, with any missing residue assertion added here;
- no `tests/PrivateFixtures/**` path is tracked by Git/CI artifacts.

Do not add telemetry, crash upload, cloud validation, web fonts, Labelary, remote barcode services or runtime downloads.

### 4. Physical QA protocol

Physical QA is deliberately a documented/manual gate, not faked by CI.

Minimum thermal sizes:

- 102 × 152 mm;
- 100 × 150 mm;
- 100 × 100 mm.

Minimum physical checks:

1. print through the F2.5 Windows thermal workflow;
2. measure label geometry with a ruler/caliper where practical;
3. verify no silent shrink-to-fit;
4. record clipping/feed anomalies if the driver/imageable area warns or behaves differently;
5. scan a representative Code128;
6. scan a representative QR;
7. compare scanner text exactly with expected payload;
8. repeat at a representative printer-native DPI available on the real printer.

If a real printer/scanner is unavailable, status is **NOT RUN**, never PASS.

## Private QA result model

The repository may contain a **template/checklist only**. Actual private results remain local/ignored.

A local run should end in one of these explicit states:

- `PASS` — all selected private cases rendered/validated as expected;
- `FAIL` — at least one case mismatched or errored;
- `NOT RUN` — fixtures/hardware were not supplied;
- `PARTIAL` — some required corpus/hardware checks were run but the full gate was not completed.

This prevents automated synthetic success from being confused with real-label or physical acceptance.

## Error handling

- A barcode decode failure in synthetic CI is a test failure with case/format context.
- A private-case failure reports the local filename/case identifier only in local output; it is never uploaded automatically.
- Invalid/missing private expectation files fail that requested private run clearly.
- Private fixture absence does not modify normal CI outcome; it is recorded as `NOT RUN` in project state/history.
- No failed validation mutates the source ZPL, current application workspace, exported PDF or printer configuration.

## Dependency policy

`ZXing.Net 0.16.11` is already present in `SGPdf.App.Tests` and is Apache-2.0. F2.6 reuses it.

Expected dependency outcome:

- no new runtime `PackageReference`;
- no ZXing reference in `src/SGPdf.App`;
- no new barcode package;
- no new network dependency;
- no lockfile mutation unless an implementation detail unexpectedly proves unavoidable and receives separate approval.

## Files / likely implementation surface

Expected minimal areas:

- extend or reuse `tests/SGPdf.App.Tests/LabelizeBarcodeRegressionTests.cs`;
- add focused PDF barcode integration tests rather than overloading geometry-only tests;
- strengthen existing renderer/export cleanup tests only where evidence is missing;
- add one small versioned private-QA launcher/instructions if required for reproducibility;
- add a physical QA checklist/template;
- update `STATE`, `ROADMAP`, `AGENTS` and history at closure.

No production UI change is expected in F2.6.

## Non-goals

F2.6 does **not** include:

- barcode/QR scanner UI in SG PDF Editor;
- runtime ZXing dependency;
- webcam scanning;
- RAW ZPL printing;
- printer calibration/darkness/speed controls;
- vendor SDKs;
- new label-layout behavior;
- OCR;
- PDF editing features outside label validation;
- storing private QA evidence in GitHub.

## TDD / execution shape

Implementation should remain small and staged:

1. **Digital export decode RED→GREEN** — prove PDF export + PDFium render retains exact Code128/QR data, including rotation.
2. **Private QA harness RED→GREEN** — reproducible local runner and expectation handling without CI/private-data coupling.
3. **Residue/privacy hardening RED→GREEN** — add only missing cleanup/offline assertions.
4. **Closure** — whole-branch audit, docs/history, exact-head push + PR CI.

Physical printer/scanner execution is a separate manual evidence gate and may remain `NOT RUN` after automated closure.

## Acceptance criteria

### Automated PASS

F2.6 automated acceptance requires:

- existing PNG Code128/QR regressions remain PASS;
- exported PDF Code128 and QR decode to exact expected payload after PDFium rendering;
- 90° rotation decode regression PASS;
- private-fixture mechanism cannot leak data to tracked repository paths or CI by design;
- offline/runtime package guards PASS;
- temp-residue tests PASS for the relevant renderer/export paths;
- Release build has 0 warnings / 0 errors;
- full automated suite PASS on both push and PR CI at the exact closure head;
- no new runtime package/dependency unless separately approved.

### Private acceptance

Requires an actual local run against the user's private real-label corpus. Until run, status remains `NOT RUN`.

### Physical acceptance

Requires actual printer + ruler + scanner evidence. Until run, status remains `NOT RUN`.

## Completion semantics

At the end of automated F2.6:

- `F2.6 automated PASS` may be declared if CI criteria pass;
- `F2 synthetic/automated pipeline PASS` may be declared;
- `F2 private corpus acceptance` remains separate;
- `F2 physical printer/scanner acceptance` remains separate;
- the project must not claim the full ZPL workflow is physically validated until both real gates are actually completed.

## Merge policy

F2.6 remains a draft stacked PR and is not merged automatically. Any merge/rebase/integration remains subject to explicit user approval.
