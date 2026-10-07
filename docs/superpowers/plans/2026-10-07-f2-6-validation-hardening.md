# F2.6 Validation + Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close F2.6 automated validation with exact Code128/QR decode evidence through Labelize → PDF export → PDFium, a reproducible private-corpus runner, and final offline/privacy/temp-residue hardening while keeping real corpus and physical printer/scanner acceptance explicitly separate.

**Architecture:** Keep new validation logic test/dev-only. Reuse the existing Labelize renderer, `LabelPdfExporter`, `PdfDocumentSession`, SkiaSharp and already-pinned ZXing.Net; do not add a runtime scanner or barcode subsystem. Standard CI remains synthetic/offline, while an explicit PowerShell launcher opts into ignored `tests/PrivateFixtures/` data for local private QA.

**Tech Stack:** C# / .NET 10 / xUnit 2.9.3 / SkiaSharp 3.119.1 / ZXing.Net 0.16.11 test-only / Labelize 1.7.0 / PDFsharp 6.2.4 / PDFium / PowerShell / GitHub Actions Windows.

**Spec:** `docs/superpowers/specs/2026-10-07-f2-6-validation-hardening-design.md`

## Global Constraints

- Base is F2.5 final head `c003d6512a5df5be2f53aab2262d09c6dcbf92cf`.
- Branch is `feat/f2-6-validation-hardening`.
- `ZXing.Net 0.16.11` already exists in `SGPdf.App.Tests`; do not add it to `src/SGPdf.App`.
- No new production `PackageReference` and no lockfile mutation unless a separately approved blocker proves it unavoidable.
- No Labelary, HTTP, sockets, cloud validation, telemetry, downloads, webcam scanning or scanner UI.
- Real Mercado Libre/customer files, expected JSON, rendered output, screenshots, scanner captures and QA reports remain private/ignored and never enter GitHub/CI/Graphify.
- Standard CI success means `F2.6 automated PASS` only; it does not mean private-corpus PASS or physical printer/scanner PASS.
- Physical QA remains manual and may finish as `NOT RUN`.
- No merge without explicit user approval.

## Review Focus

1. **PDF raster DPI too low for machine readability** — integration tests render exported thermal pages at 300 DPI and require exact Code128/QR payload matches.
2. **90° rotation corrupts or double-rotates a code** — a dedicated PDF export + PDFium + ZXing regression must decode the rotated payload exactly.
3. **Private fixtures absent in normal CI** — standard tests stay green, while the explicit private-QA launcher reports `NOT RUN` with non-zero exit when its requested root is absent.
4. **Malformed/missing private expectation metadata** — an explicitly requested private run fails clearly with local case filename context and never silently marks PASS.
5. **Temporary/private residue or runtime scope creep** — residue assertions and existing offline/package guards remain green; whole-branch audit shows no runtime ZXing/network/private fixture changes.

---

## File map

- Create `tests/SGPdf.App.Tests/BarcodeDecodeAssert.cs` — shared test-only decoder helpers for PNG and PDFium BGRA buffers.
- Create `tests/SGPdf.App.Tests/LabelPdfBarcodeIntegrationTests.cs` — real Labelize → PDF export → PDFium render → ZXing exact-payload tests.
- Modify `tests/SGPdf.App.Tests/LabelizeBarcodeRegressionTests.cs` — reuse shared decoder helper; preserve existing PNG regressions.
- Create `tests/SGPdf.App.Tests/PrivateLabelCorpusTests.cs` — opt-in private-corpus test entry point plus expectation parsing/validation.
- Create `tools/run-private-label-qa.ps1` — explicit local private-QA launcher.
- Modify `tests/SGPdf.App.Tests/LabelizeProcessRendererTests.cs` — add renderer-start failure residue coverage if missing.
- Modify `tests/SGPdf.App.Tests/LabelPdfExporterTests.cs` — assert no `.sgpdf.tmp` residue after successful export.
- Reuse `tests/SGPdf.App.Tests/OfflineRuntimeTests.cs` unchanged unless RED demonstrates a missing guard.
- Create `docs/qa/F2.6-PHYSICAL-QA.md` — public checklist/template only.
- Closure only: update `.planning/STATE.md`, `.planning/ROADMAP.md`, `AGENTS.md`, and create `docs/history/2026-10-07-F2.6.md`.

No `src/SGPdf.App` file is expected to change in F2.6. If a RED appears to require product code or a new dependency, stop and return to design review.

---

### Task 1: Exact barcode/QR decode after PDF export and PDFium render

**Files:**
- Create: `tests/SGPdf.App.Tests/BarcodeDecodeAssert.cs`
- Create: `tests/SGPdf.App.Tests/LabelPdfBarcodeIntegrationTests.cs`
- Modify: `tests/SGPdf.App.Tests/LabelizeBarcodeRegressionTests.cs`

**Interfaces:**
- Consumes: `LabelizeProcessRenderer.RenderAsync(...)`, `LabelLayoutPlanner.CreatePlan(...)`, `LabelPdfExporter.Export(...)`, `PdfDocumentSession.Open(...)`, `PdfDocumentSession.RenderPage(...)`.
- Produces test-only helper:
  - `internal static ZXing.Result DecodePng(byte[] pngBytes, BarcodeFormat expectedFormat)`
  - `internal static ZXing.Result DecodeBgra32(byte[] pixels, int width, int height, int stride, BarcodeFormat expectedFormat)`

- [ ] **Step 1: Write the failing integration tests**

Create:

```csharp
[Fact]
public async Task ExportedThermalPdf_Code128AndQrDecodeAfterPdfiumRender()
```

Use two printable designs rendered by real Labelize at `100 × 100 mm`, `8 dpmm`:
- Code128 payload `SGPDF-C128-2607`
- QR payload `SGPDF-QR-2607`

Use a Thermal layout with `OneEach`, export one label per PDF page, reopen with `PdfDocumentSession`, render each page at **300 DPI**, decode page 0 as `CODE_128` and page 1 as `QR_CODE`, and assert exact format + text.

Also create:

```csharp
[Fact]
public async Task ExportedThermalPdf_RotatedQrDecodesAfterPdfiumRender()
```

Use payload `SGPDF-QR-ROT90`, Thermal layout rotation `Degrees90`, render through PDFium at **300 DPI**, and require exact QR text.

Both tests should already call `BarcodeDecodeAssert`, which does not exist yet.

- [ ] **Step 2: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~LabelPdfBarcodeIntegrationTests"
```

Expected: compile failure because `BarcodeDecodeAssert` does not exist. Production code remains untouched.

- [ ] **Step 3: Implement `BarcodeDecodeAssert` minimally**

`DecodePng` uses SkiaSharp + ZXing with `TryHarder=true`, `AutoRotate=false`, `PossibleFormats=[expectedFormat]`.

`DecodeBgra32` respects `PdfRenderedPage.Stride`, converts PDFium BGRA rows to RGB24, then uses the same ZXing options. It must fail with expected-format context when no code is decoded.

- [ ] **Step 4: Refactor existing PNG regressions to the helper**

Preserve these existing exact assertions:
- Code128 `123456789012`;
- QR `SG-PDF-QR-12345`;
- `^FT` QR `SG-PDF-FT-QR` plus existing ink-origin assertions.

- [ ] **Step 5: Verify Task 1 GREEN**

Run the focused integration tests and existing `LabelizeBarcodeRegressionTests`, then the full suite.

Expected: all PASS; no product code or new package change.

- [ ] **Step 6: Commit Task 1**

```bash
git add tests/SGPdf.App.Tests/BarcodeDecodeAssert.cs tests/SGPdf.App.Tests/LabelPdfBarcodeIntegrationTests.cs tests/SGPdf.App.Tests/LabelizeBarcodeRegressionTests.cs
git commit -m "test(labels): validate barcode decode through exported PDF"
```

---

### Task 2: Explicit private-corpus QA runner without CI coupling

**Files:**
- Create: `tests/SGPdf.App.Tests/PrivateLabelCorpusTests.cs`
- Create: `tools/run-private-label-qa.ps1`

**Interfaces:**
- Consumes: Task 1 `BarcodeDecodeAssert`, `ZplDocumentParser`, `LabelizeProcessRenderer`, `ZplRenderOptions`.
- Environment contract: `SGPDF_PRIVATE_QA_ROOT` is set only by the explicit launcher.
- Test-only internal contracts:
  - `private static PrivateLabelExpectation ParseExpectation(string path)`
  - `private static IReadOnlyList<PrivateLabelCase> DiscoverCases(string rootPath)`
  - `private static Task ValidateCaseAsync(PrivateLabelCase testCase, CancellationToken cancellationToken = default)`
  - nested records `PrivateLabelCase(string SourcePath, string ExpectationPath, PrivateLabelExpectation Expectation)` and expectation/code records matching the schema below.

Private sibling expectation schema:

```json
{
  "designCount": 1,
  "widthMm": 102,
  "heightMm": 152,
  "dpmm": 8,
  "codes": [
    { "designIndex": 0, "format": "CODE_128", "text": "private-expected-value" }
  ]
}
```

`widthMm`, `heightMm`, `dpmm`, and `codes` may be omitted; `designCount` is required. Supported `format` values are exactly `CODE_128` and `QR_CODE`.

- [ ] **Step 1: Write synthetic contract tests**

Add tracked synthetic tests:

```csharp
[Fact]
public void ParseExpectation_MissingDesignCount_FailsClearly()

[Fact]
public void ParseExpectation_UnsupportedBarcodeFormat_FailsClearly()

[Fact]
public void DiscoverCases_RequiresMatchingExpectedJson()
```

Use temporary synthetic files only. Error assertions include the local filename where applicable.

Add opt-in entry point:

```csharp
[Trait("Category", "PrivateQA")]
[Fact]
public async Task PrivateCorpus_FromEnvironment_MatchesExpectations()
```

When `SGPDF_PRIVATE_QA_ROOT` is unset, return without touching filesystem/network and write `PRIVATE_QA: NOT RUN` to `ITestOutputHelper`. Never print fixture contents.

When it is set, missing directory or malformed expectations must fail.

- [ ] **Step 2: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PrivateLabelCorpusTests"
```

Expected: compile/test failure until parsing/discovery/validation contracts are implemented.

- [ ] **Step 3: Implement private corpus validation test-only**

For an opted-in local run:
1. enumerate top-level `*.zpl`, `*.txt`, `*.prn` only;
2. require sibling `<basename>.expected.json`;
3. parse with `ZplDocumentParser`;
4. assert `designCount`;
5. if width/height/dpmm are all present, use them; otherwise use `ZplRenderOptions.Default`;
6. render through real Labelize;
7. validate expected design index, format and exact decoded text via Task 1 helper;
8. failures may identify local filename/design index but must not dump entire ZPL/customer data.

- [ ] **Step 4: Add `tools/run-private-label-qa.ps1`**

```powershell
param([string]$Root = "tests/PrivateFixtures/labels")
```

Required behavior:
- missing `$Root` → print exactly `PRIVATE_QA: NOT RUN — no private fixture directory` and exit non-zero;
- resolve local root;
- stage Labelize through existing `tools/setup-labelize.ps1`;
- set `SGPDF_PRIVATE_QA_ROOT` only for the child filtered `dotnet test`;
- run only `FullyQualifiedName~PrivateCorpus_FromEnvironment_MatchesExpectations`;
- success → `PRIVATE_QA: PASS`;
- test failure → `PRIVATE_QA: FAIL` and non-zero exit;
- restore/remove the environment variable in `finally`;
- never upload/copy/archive the private root.

- [ ] **Step 5: Verify normal CI-style execution and explicit NOT RUN behavior**

With the environment variable unset, full standard tests must PASS. Then run the launcher against a guaranteed-missing path and require non-zero exit plus the exact NOT RUN message.

- [ ] **Step 6: Commit Task 2**

```bash
git add tests/SGPdf.App.Tests/PrivateLabelCorpusTests.cs tools/run-private-label-qa.ps1
git commit -m "test(labels): add private corpus QA runner"
```

---

### Task 3: Temp-residue and offline/privacy hardening

**Files:**
- Modify: `tests/SGPdf.App.Tests/LabelizeProcessRendererTests.cs`
- Modify: `tests/SGPdf.App.Tests/LabelPdfExporterTests.cs`
- Inspect/reuse: `tests/SGPdf.App.Tests/OfflineRuntimeTests.cs`
- Inspect/reuse: `.github/workflows/build.yml`
- Inspect/reuse: `.gitignore`

**Interfaces:** no new product API.

- [ ] **Step 1: Add missing residue evidence**

Add:

```csharp
[Fact]
public async Task RenderAsync_WhenProcessStartFails_CleansRequestTempDirectory()
```

Use a guaranteed-nonexistent executable path plus controlled `tempRoot`; assert exception and empty request residue.

Extend successful PDF export coverage to assert no `.<filename>.*.sgpdf.tmp` remains after success.

- [ ] **Step 2: Run focused tests**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~LabelizeProcessRendererTests|FullyQualifiedName~LabelPdfExporterTests"
```

If new assertions expose residue, RED is valid. If they pass immediately because cleanup is already correct, record evidence-hardening and do not fabricate a product change.

- [ ] **Step 3: Fix production cleanup only if RED proves a real bug**

Make the smallest owning `finally`/cleanup change and rerun. Otherwise make no `src/` change.

- [ ] **Step 4: Re-run offline/privacy/package guards**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~OfflineRuntimeTests"
git ls-files "tests/PrivateFixtures/**"
```

Expected:
- runtime packages exactly `bblanchon.PDFium.Win32`, `PDFsharp`;
- no forbidden runtime network surface;
- `git ls-files` output empty;
- `.gitignore` still contains `tests/PrivateFixtures/`;
- CI hygiene still rejects tracked private fixtures.

- [ ] **Step 5: Full Task 3 verification**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Expected: restore PASS, build 0 warnings / 0 errors, full suite PASS, no package/lock mutation.

- [ ] **Step 6: Commit Task 3 only if files changed**

If tests were added:

```bash
git add tests/SGPdf.App.Tests/LabelizeProcessRendererTests.cs tests/SGPdf.App.Tests/LabelPdfExporterTests.cs
git commit -m "test(labels): harden temporary residue checks"
```

No empty commit if existing evidence already covered everything.

---

### Task 4: Physical QA protocol, audit, and automated closure

**Files:**
- Create: `docs/qa/F2.6-PHYSICAL-QA.md`
- Create: `docs/history/2026-10-07-F2.6.md`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `AGENTS.md`
- Update/create stacked draft PR after implementation evidence exists.

**Interfaces:** consumes Task 1–3 evidence and exact CI run IDs; produces no code API.

- [ ] **Step 1: Write physical QA checklist/template**

Include:
- `PASS / FAIL / NOT RUN / PARTIAL`;
- printer model + driver version;
- `102×152`, `100×150`, `100×100 mm`;
- measured width/height;
- expected vs selected driver media size;
- clipping/imageable-area warning;
- representative native DPI;
- Code128 expected/actual scanner text;
- QR expected/actual scanner text;
- explicit warning not to commit completed private results/customer data.

If hardware has not been used, status remains `NOT RUN`.

- [ ] **Step 2: Open the stacked draft PR**

Base: `feat/f2-5-windows-thermal-print`  
Head: `feat/f2-6-validation-hardening`

PR stays draft/unmerged and separates automated, private-corpus and physical statuses.

- [ ] **Step 3: Audit the whole branch against F2.5**

Expected diff is tests/dev tooling/docs only unless Task 3 proved a real cleanup bug. Block closure on any unexpected runtime ZXing reference, production dependency/lock mutation, tracked private fixture, network/download behavior or new product feature.

- [ ] **Step 4: Run fresh functional CI**

Require repository hygiene, Labelize staging, locked restore, Release build 0 warnings/0 errors and all tests PASS. Record push + PR CI IDs.

- [ ] **Step 5: Update durable state/history**

`STATE`/`ROADMAP` may say F2.1–F2.6 automated PASS only after CI criteria pass. Private real-label acceptance and physical thermal/scanner acceptance remain `NOT RUN` unless actually executed. Next product slice becomes F3 visual signature design with its own gate.

`AGENTS.md` must preserve that ZXing is test/QA-only and CI cannot imply private/hardware PASS.

- [ ] **Step 6: Commit closure docs and verify exact closure head**

After the docs commit, require both push and PR CI on that same exact SHA. Earlier functional CI is insufficient for final closure.

- [ ] **Step 7: Update PR body and stop without merge**

Record final SHA, push CI ID, PR CI ID, test count, scope audit, private status and physical status. Do not merge/rebase/integrate without explicit user approval.

---

## Execution order / stop conditions

Execute Task 1 → 2 → 3 → 4. Stop and return to design review if:

- a production/runtime barcode decoder becomes necessary;
- a new runtime/NuGet dependency becomes necessary;
- PDF geometry must change merely to make decoding pass;
- private data would need to enter tracked files, CI artifacts or normal logs;
- physical behavior suggests RAW/vendor-specific transport is required.

Those are architecture changes, not F2.6 hardening details.
