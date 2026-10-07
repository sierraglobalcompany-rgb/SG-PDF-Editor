# F2.6 Validation + Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close F2.6 automated validation with exact Code128/QR decode evidence through Labelize → PDF export → PDFium, a reproducible private-corpus runner, and final offline/privacy/temp-residue hardening while keeping real corpus and physical printer/scanner acceptance explicitly separate.

**Architecture:** Keep all new validation logic test/dev-only. Reuse the existing Labelize renderer, `LabelPdfExporter`, `PdfDocumentSession`, SkiaSharp and already-pinned ZXing.Net; do not add a runtime scanner or barcode subsystem. Standard CI remains synthetic and offline, while a versioned PowerShell launcher explicitly opts into ignored `tests/PrivateFixtures/` data for local private QA.

**Tech Stack:** C# / .NET 10 / xUnit 2.9.3 / SkiaSharp 3.119.1 / ZXing.Net 0.16.11 test-only / Labelize 1.7.0 / PDFsharp 6.2.4 / PDFium / PowerShell / GitHub Actions Windows.

**Spec:** `docs/superpowers/specs/2026-10-07-f2-6-validation-hardening-design.md`

## Global Constraints

- Base is F2.5 final head `c003d6512a5df5be2f53aab2262d09c6dcbf92cf`.
- Branch is `feat/f2-6-validation-hardening`.
- `ZXing.Net 0.16.11` already exists in `SGPdf.App.Tests`; do not add it to `src/SGPdf.App`.
- No new production `PackageReference` and no lockfile mutation unless a separately approved blocker proves it unavoidable.
- No Labelary, HTTP, sockets, cloud validation, telemetry, downloads, webcam scanning or scanner UI.
- Real Mercado Libre/customer files, expected JSON, rendered output, screenshots, scanner captures and QA reports remain under ignored/private paths and never enter GitHub/CI/Graphify.
- Standard CI success means `F2.6 automated PASS` only; it does not mean private-corpus PASS or physical printer/scanner PASS.
- Physical QA remains manual and may finish as `NOT RUN`.
- No merge without explicit user approval.

## Review Focus

1. **PDF raster DPI too low for machine readability** — integration tests render exported thermal pages at 300 DPI and require exact Code128/QR payload matches.
2. **90° rotation corrupts or double-rotates a code** — a dedicated PDF export + PDFium + ZXing regression must decode the rotated payload exactly.
3. **Private fixtures absent in normal CI** — standard test execution must remain green, while the explicit private-QA launcher must report `NOT RUN` and non-zero status when its requested root is absent.
4. **Malformed/missing private expectation metadata** — an explicitly requested private run must fail clearly with the local case filename and must never silently mark PASS.
5. **Temporary/private residue or runtime scope creep** — failure/success residue assertions and existing offline/package guards must remain green; whole-branch audit must show no runtime ZXing/network/private fixture changes.

---

## File map

- Create `tests/SGPdf.App.Tests/BarcodeDecodeAssert.cs` — shared test-only decoder helpers for PNG and PDFium BGRA buffers.
- Create `tests/SGPdf.App.Tests/LabelPdfBarcodeIntegrationTests.cs` — real Labelize → PDFsharp export → PDFium render → ZXing exact-payload tests.
- Modify `tests/SGPdf.App.Tests/LabelizeBarcodeRegressionTests.cs` — reuse the shared decoder helper; preserve existing PNG regressions.
- Create `tests/SGPdf.App.Tests/PrivateLabelCorpusTests.cs` — opt-in private-corpus test entry point plus expectation parsing/validation.
- Create `tools/run-private-label-qa.ps1` — explicit local launcher that returns `NOT RUN`, `PASS`, or failure without uploading data.
- Modify `tests/SGPdf.App.Tests/LabelizeProcessRendererTests.cs` — add renderer-start failure residue coverage if missing.
- Modify `tests/SGPdf.App.Tests/LabelPdfExporterTests.cs` — assert no `.sgpdf.tmp` residue after successful export in addition to existing failure coverage.
- Reuse `tests/SGPdf.App.Tests/OfflineRuntimeTests.cs` unchanged unless a missing assertion is demonstrated by RED.
- Create `docs/qa/F2.6-PHYSICAL-QA.md` — public checklist/template only, never real private results.
- Closure only: update `.planning/STATE.md`, `.planning/ROADMAP.md`, `AGENTS.md`, and create `docs/history/2026-10-07-F2.6.md`.

No `src/SGPdf.App` file is expected to change in F2.6. If a RED appears to require product code or a new dependency, stop and reclassify before implementing it.

---

### Task 1: Exact barcode/QR decode after PDF export and PDFium render

**Files:**
- Create: `tests/SGPdf.App.Tests/BarcodeDecodeAssert.cs`
- Create: `tests/SGPdf.App.Tests/LabelPdfBarcodeIntegrationTests.cs`
- Modify: `tests/SGPdf.App.Tests/LabelizeBarcodeRegressionTests.cs`

**Interfaces:**
- Consumes: `LabelizeProcessRenderer.RenderAsync(ZplDocument, ZplRenderOptions, CancellationToken)`, `LabelLayoutPlanner.CreatePlan(...)`, `LabelPdfExporter.Export(string, LabelLayoutPlan, IReadOnlyList<ZplRenderedLabel>)`, `PdfDocumentSession.Open(string)`, `PdfDocumentSession.RenderPage(int, double)`.
- Produces test-only helper:
  - `internal static ZXing.Result DecodePng(byte[] pngBytes, BarcodeFormat expectedFormat)`
  - `internal static ZXing.Result DecodeBgra32(byte[] pixels, int width, int height, int stride, BarcodeFormat expectedFormat)`

- [ ] **Step 1: Add failing PDF barcode integration tests**

Create tests with these exact behaviors:

```csharp
[Fact]
public async Task ExportedThermalPdf_Code128AndQrDecodeAfterPdfiumRender()
```

Use two printable ZPL designs rendered by real Labelize at `100 × 100 mm`, `8 dpmm`:
- Code128 payload: `SGPDF-C128-2607`
- QR payload: `SGPDF-QR-2607`

Create a Thermal layout with `OneEach`, export one label per PDF page, reopen through `PdfDocumentSession`, render each page at **300 DPI**, decode page 0 as `CODE_128` and page 1 as `QR_CODE`, and assert both format and exact text.

Also add:

```csharp
[Fact]
public async Task ExportedThermalPdf_RotatedQrDecodesAfterPdfiumRender()
```

Use payload `SGPDF-QR-ROT90`, Thermal layout rotation `Degrees90`, render the exported page through PDFium at **300 DPI**, and require exact QR text. This test owns the review-focus case for accidental double rotation.

- [ ] **Step 2: Run the focused tests and confirm RED**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~LabelPdfBarcodeIntegrationTests"
```

Expected: compile/test failure because `BarcodeDecodeAssert` / the new integration test implementation does not yet exist. Do not alter production code to make RED easier.

- [ ] **Step 3: Implement `BarcodeDecodeAssert` test helper**

`DecodePng` must preserve the existing Labelize PNG behavior using SkiaSharp and ZXing with `TryHarder=true`, `AutoRotate=false`, and `PossibleFormats=[expectedFormat]`.

`DecodeBgra32` must respect `PdfRenderedPage.Stride`, convert each PDFium BGRA pixel to an RGB24 buffer without assuming tightly packed rows, then decode through ZXing with the same options. Throw/assert with format context if no result is returned.

- [ ] **Step 4: Refactor existing PNG regressions to the shared helper without changing their assertions**

`LabelizeBarcodeRegressionTests` must still prove:
- Code128 `123456789012`;
- QR `SG-PDF-QR-12345`;
- `^FT` QR `SG-PDF-FT-QR` plus its ink-origin assertions.

No behavioral change beyond removing duplicate decode code.

- [ ] **Step 5: Implement the two real pipeline integration tests minimally**

Use actual `LabelizeProcessRenderer`, actual `LabelPdfExporter`, actual `PdfDocumentSession`, and temporary files/directories cleaned in `finally`. Do not mock the export or PDFium path.

- [ ] **Step 6: Verify Task 1 GREEN**

Run the focused tests above, then:

```powershell
dotnet test SGPdf.slnx --configuration Release --no-build
```

Expected: all tests PASS, including the pre-existing PNG regressions; no skipped test added for this task.

- [ ] **Step 7: Commit Task 1**

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
- Consumes: Task 1 `BarcodeDecodeAssert`, existing `ZplDocumentParser`, `LabelizeProcessRenderer`, `ZplRenderOptions`.
- Environment contract: `SGPDF_PRIVATE_QA_ROOT` is set only by the explicit launcher.
- Private expectation schema, stored beside each private `.zpl/.txt/.prn` as `<basename>.expected.json`:

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

`widthMm`, `heightMm`, `dpmm`, and `codes` may be omitted when unknown/not applicable; `designCount` is required. Supported code format names in F2.6 are exactly `CODE_128` and `QR_CODE`.

- [ ] **Step 1: Write synthetic tests for the private expectation/runner contract**

The test class must include normal tracked synthetic temp-directory tests that do **not** contain customer data:

```csharp
[Fact]
public void ParseExpectation_MissingDesignCount_FailsClearly()

[Fact]
public void ParseExpectation_UnsupportedBarcodeFormat_FailsClearly()

[Fact]
public void DiscoverCases_RequiresMatchingExpectedJson()
```

Assertions must include the local case filename in the error message where applicable.

Add one opt-in entry test:

```csharp
[Trait("Category", "PrivateQA")]
[Fact]
public async Task PrivateCorpus_FromEnvironment_MatchesExpectations()
```

When `SGPDF_PRIVATE_QA_ROOT` is **unset**, this test must return without touching filesystem/network and write `PRIVATE_QA: NOT RUN` to test output. It must never print fixture contents.

When the variable is set, absence of the directory or invalid expectations is a test failure, not a skip/pass interpretation by the launcher.

- [ ] **Step 2: Run the focused contract tests and confirm RED**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PrivateLabelCorpusTests"
```

Expected: FAIL/compile failure because the private corpus parsing/discovery code has not been implemented.

- [ ] **Step 3: Implement the minimal private-corpus test runner inside the test project**

Keep it test-only. Required behavior for an opted-in local run:

1. enumerate only top-level `*.zpl`, `*.txt`, `*.prn` under the supplied root;
2. require sibling `<basename>.expected.json`;
3. parse source through `ZplDocumentParser`;
4. assert `designCount`;
5. choose render options from expectation values when all physical values are supplied, otherwise `ZplRenderOptions.Default`;
6. render through real Labelize;
7. for each expected code, validate `designIndex` range, map `CODE_128` / `QR_CODE`, decode rendered PNG via Task 1 helper, and compare exact text;
8. include only local case filename/design index in failures — never dump complete ZPL or decoded customer payload into normal logs unless needed for the direct local assertion message.

- [ ] **Step 4: Add the explicit PowerShell launcher**

`tools/run-private-label-qa.ps1` parameters:

```powershell
param([string]$Root = "tests/PrivateFixtures/labels")
```

Behavior:
- if `$Root` does not exist: print exactly `PRIVATE_QA: NOT RUN — no private fixture directory` and exit non-zero;
- resolve the root locally, set `SGPDF_PRIVATE_QA_ROOT` only for the child `dotnet test` invocation;
- stage Labelize through existing `tools/setup-labelize.ps1` before the filtered test;
- run only `FullyQualifiedName~PrivateCorpus_FromEnvironment_MatchesExpectations`;
- on success print `PRIVATE_QA: PASS`;
- on test failure print `PRIVATE_QA: FAIL` and propagate non-zero exit;
- clear/restore the environment variable in `finally`;
- never upload, copy, zip or artifact the private directory.

- [ ] **Step 5: Verify normal CI-style execution does not require private data**

With `SGPDF_PRIVATE_QA_ROOT` unset:

```powershell
dotnet test SGPdf.slnx --configuration Release --no-build
```

Expected: full standard suite PASS; output may contain `PRIVATE_QA: NOT RUN`, but F2 private acceptance remains separately documented as NOT RUN.

Then run the launcher against a guaranteed-missing temporary path and verify non-zero exit plus the exact `PRIVATE_QA: NOT RUN` message.

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
- Inspect/reuse without expected modification: `tests/SGPdf.App.Tests/OfflineRuntimeTests.cs`
- Inspect/reuse: `.github/workflows/build.yml`
- Inspect/reuse: `.gitignore`

**Interfaces:**
- Consumes existing renderer/export behavior only.
- Produces no product API.

- [ ] **Step 1: Add missing residue tests first**

Add:

```csharp
[Fact]
public async Task RenderAsync_WhenProcessStartFails_CleansRequestTempDirectory()
```

Construct `LabelizeProcessRenderer` with a guaranteed-nonexistent executable path and a controlled `tempRoot`, assert an exception, then assert `tempRoot` contains no request residue.

Extend successful PDF export coverage with an assertion that the destination directory contains no `.<filename>.*.sgpdf.tmp` file after success.

- [ ] **Step 2: Run focused tests and confirm RED if a gap exists**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~LabelizeProcessRendererTests|FullyQualifiedName~LabelPdfExporterTests"
```

Expected: the new assertions either expose a real cleanup gap (RED) or pass immediately because production cleanup is already correct. If they pass immediately, record this task as evidence-hardening rather than fabricate a product change.

- [ ] **Step 3: Implement only a demonstrated production cleanup fix, if RED proves one**

If and only if Step 2 exposes residue, make the smallest `finally`/cleanup change in the owning existing production class and rerun the focused test. If no RED exists, make **no** production change.

- [ ] **Step 4: Re-run offline/privacy/package guards**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~OfflineRuntimeTests"
```

Expected:
- runtime package list remains exactly `bblanchon.PDFium.Win32`, `PDFsharp`;
- app assembly/source still has no forbidden runtime network surface.

Also verify repository hygiene directly:

```powershell
git ls-files "tests/PrivateFixtures/**"
```

Expected: empty output.

Check `.gitignore` still contains `tests/PrivateFixtures/` and CI hygiene still rejects tracked private fixtures. Do not add a second redundant runtime-network mechanism.

- [ ] **Step 5: Full Task 3 verification**

Run:

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Expected: locked restore PASS, build 0 warnings / 0 errors, full suite PASS, no package/lock changes.

- [ ] **Step 6: Commit Task 3**

Commit only files that actually changed. If this task needed tests only:

```bash
git add tests/SGPdf.App.Tests/LabelizeProcessRendererTests.cs tests/SGPdf.App.Tests/LabelPdfExporterTests.cs
git commit -m "test(labels): harden temporary residue checks"
```

If no file needed changing because all required evidence already existed, do not create an empty commit; record that fact in the closure history.

---

### Task 4: Physical QA protocol, whole-branch audit, and automated closure

**Files:**
- Create: `docs/qa/F2.6-PHYSICAL-QA.md`
- Create: `docs/history/2026-10-07-F2.6.md`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `AGENTS.md`
- Update PR body after implementation CI evidence exists.

**Interfaces:**
- Consumes Task 1–3 evidence and exact CI run IDs.
- Produces the durable handoff to the next product phase; no code API.

- [ ] **Step 1: Write the public physical-QA checklist/template**

The checklist must contain, without real customer data:
- statuses `PASS / FAIL / NOT RUN / PARTIAL`;
- printer model + driver version fields;
- media tests `102×152`, `100×150`, `100×100 mm`;
- ruler/caliper measured width/height fields;
- expected vs selected driver media size;
- clipping/imageable-area warning field;
- representative native DPI field;
- Code128 scanner expected/actual fields;
- QR scanner expected/actual fields;
- explicit instruction: do not commit completed private results, customer labels or scanner captures.

Until hardware is used, closure docs must say physical QA **NOT RUN**.

- [ ] **Step 2: Open/update the stacked draft PR**

Create PR from `feat/f2-6-validation-hardening` to `feat/f2-5-windows-thermal-print` if it does not already exist. Keep it draft and unmerged.

PR body must distinguish:
- automated synthetic result;
- private corpus status;
- physical status;
- no-new-runtime-dependency audit.

- [ ] **Step 3: Run whole-branch scope audit against F2.5**

Verify changed files and diff. Expected scope:
- tests/dev tooling/docs only unless Task 3 demonstrated a real cleanup bug;
- no runtime ZXing reference;
- no `.csproj`/lockfile production dependency change;
- no tracked `tests/PrivateFixtures/**`;
- no network/runtime download;
- no new label layout, print, reader or editor feature.

Any unexpected product/API/package change blocks closure and requires review.

- [ ] **Step 4: Run fresh functional verification before closure docs**

Run CI on the latest functional head. Require:
- repository hygiene PASS;
- Labelize staging PASS;
- locked restore PASS;
- Release build 0 warnings / 0 errors;
- all tests PASS.

Record push and PR run IDs.

- [ ] **Step 5: Update durable project state/history**

`.planning/STATE.md` and `.planning/ROADMAP.md` must say:
- F2.1–F2.6 automated PASS only if exact CI criteria pass;
- F1/F2 private real-label acceptance remains `NOT RUN` unless actually executed;
- F2 physical thermal/scanner acceptance remains `NOT RUN` unless actually executed;
- next product slice is F3 visual signature design, subject to its own gate.

`AGENTS.md` must preserve:
- ZXing is test/QA-only;
- no runtime barcode validation subsystem exists;
- private/hardware PASS cannot be inferred from CI.

History file must include exact commits/run IDs and any Task 3 finding.

- [ ] **Step 6: Commit closure docs and verify the exact closure head**

After the docs commit, require both push and PR CI on that **exact same head**. Do not claim closure from the earlier functional head.

Expected final automated status:
- build 0 warnings / 0 errors;
- full suite 0 failures;
- PR draft/open/unmerged;
- `F2.6 automated PASS`;
- private corpus `NOT RUN` unless actually supplied;
- physical printer/scanner `NOT RUN` unless actually performed.

- [ ] **Step 7: Update PR body with exact-head evidence and stop without merge**

Record final head SHA, push CI ID, PR CI ID, test count, scope audit, private status and physical status. No merge/rebase/integration without explicit user approval.

---

## Execution order / stop conditions

Execute Tasks 1 → 2 → 3 → 4 in order. Stop and return to design review if any of these occurs:

- a production/runtime barcode decoder becomes necessary;
- a new NuGet/runtime dependency appears necessary;
- PDF export must change physical geometry to make decode pass;
- private data would need to enter tracked files, CI artifacts or logs;
- physical printer behavior suggests RAW/vendor-specific transport is required.

Those are architecture changes, not F2.6 hardening details.
