# F2.5 Windows Thermal Print Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Print the current ZPL output through an installed Windows thermal-printer driver at the exact F2.4 physical label size, with driver preflight, no silent scaling, and no duplicate copy multiplication.

**Architecture:** Reuse `LabelLayoutPlan` as the single geometry authority. Keep driver-independent acceptance rules in a small pure `ThermalPrintPreflightPolicy`, render one exact-size label per page through `LabelPrintPaginator`, and keep `PrintQueue`/`PrintTicket` interaction in one Windows-specific workflow. Do not reuse the PDF paginator because it intentionally uses fit-to-printable-area scaling.

**Tech Stack:** C# / .NET 10 Windows / WPF, `System.Printing`, WPF `PrintDialog` + `DocumentPaginator`, existing Labelize PNG bytes and F2.4 layout classes. No new NuGet package.

**Spec:** `docs/superpowers/specs/2026-10-07-f2-5-windows-thermal-print-design.md`

## Global Constraints

- Windows x64 + WPF + .NET 10 remain unchanged.
- `LabelLayoutPlan` from F2.4 is the only label geometry authority; do not create a second layout engine.
- Thermal output is one label per physical page.
- Never `FitInsidePage`, shrink-to-fit, stretch-to-printable-area, or auto-correct geometry from `PageImageableArea`.
- Media acceptance tolerance is exactly **0.5 mm per dimension**, only for driver/transport quantization; it never authorizes scaling content.
- F2.4 rotation semantics remain authoritative: if Thermal rotation is 90°, the plan already owns the swapped occupied page dimensions; the print layer must not swap them again.
- Quantities come only from `LabelOutputSequence` / current `LabelLayoutPlan`; force Windows `CopyCount = 1`.
- `DocumentPaginator.PageCount` is `int`; block totals above `Int32.MaxValue` before paginator construction/spooling.
- Use installed Windows drivers only; no RAW ZPL, vendor SDK, direct USB/serial/network transport, driver download, PDF intermediary, or runtime Internet.
- Reuse current in-memory `ZplRenderedLabel.PngBytes`; normal printing must not call Labelize.
- No new runtime package is expected.
- Physical printer/scanner validation remains manual/later; CI must not pretend to prove hardware behavior.
- No merge to `main` without explicit user approval.

## Review Focus

1. **Driver omits validated media width/height** — block before spool with a specific reason; Task 1 pins this behavior.
2. **4×6-class driver quantization** — 101.6×152.4 mm must be acceptable for a 102×152 mm request while 101.4×152.6 mm must be blocked because one dimension exceeds 0.5 mm; Task 1 pins both boundaries.
3. **Thermal 90° rotation** — use F2.4 plan page dimensions and placement rotation exactly once; Task 2 verifies page size/orientation and rendered orientation.
4. **Huge quantity at paginator boundary** — exactly `Int32.MaxValue` pages is representable; `Int32.MaxValue + 1` is blocked without overflow/allocation explosion; Task 2 pins both.
5. **Windows dialog copies / missing imageable area / unsupported DPI** — copies normalize to one, absent imageable area is not a blocker, unsupported DPI warns rather than mutating workspace; Tasks 1 and 3 pin these cases.

---

## File Map

### New product files

- `src/SGPdf.App/Printing/ThermalPrintPreflightPolicy.cs` — pure size/resolution/copy/imageable-area decision logic, no printer APIs.
- `src/SGPdf.App/Printing/LabelPrintPaginator.cs` — one exact physical label page from F2.4 plan + existing PNG bytes.
- `src/SGPdf.App/Printing/WindowsThermalPrintPreflight.cs` — thin `PrintQueue`/`PrintTicket` capability + merge/validate adapter.
- `src/SGPdf.App/Printing/WindowsThermalPrintWorkflow.cs` — standard PrintDialog, confirmation summary, paginator submission.
- `src/SGPdf.App/MainWindow.LabelPrinting.cs` — small WPF label-print handler/test seam.

### Existing product files modified

- `src/SGPdf.App/MainWindow.LabelLayout.cs` — include print-button enabled state in the existing label-property UI refresh.
- `src/SGPdf.App/MainWindow.xaml` — add `Imprimir etiquetas...` below PDF export.

### New/focused tests

- `tests/SGPdf.App.Tests/ThermalPrintPreflightPolicyTests.cs`
- `tests/SGPdf.App.Tests/LabelPrintPaginatorTests.cs`
- `tests/SGPdf.App.Tests/WindowsThermalPrintPreflightTests.cs`
- `tests/SGPdf.App.Tests/MainWindowZplPrintUiTests.cs`

### Closure docs

- `.planning/phases/02-f1-zpl-gate/F2.5-PLAN.md`
- `docs/history/2026-10-07-F2.5.md`
- `.planning/STATE.md`
- `.planning/ROADMAP.md`
- `AGENTS.md`

---

### Task 1: Pure Thermal Print Preflight Policy

**Files:**
- Create: `src/SGPdf.App/Printing/ThermalPrintPreflightPolicy.cs`
- Create: `tests/SGPdf.App.Tests/ThermalPrintPreflightPolicyTests.cs`

**Interfaces:**
- Consumes: requested physical page width/height from `LabelLayoutPlan.PageWidthMm/PageHeightMm`; requested DPI derived from current applied dpmm.
- Produces:
  - `public sealed record ThermalImageableAreaMm(double OriginX, double OriginY, double ExtentWidth, double ExtentHeight);`
  - `public sealed record ThermalPrinterResolution(int X, int Y);`
  - `public sealed record ThermalPrintPreflightInput(double RequestedWidthMm, double RequestedHeightMm, double? ValidatedWidthMm, double? ValidatedHeightMm, int RequestedDpi, int? ValidatedDpi, int? DialogCopyCount, ThermalImageableAreaMm? ImageableArea);`
  - `public sealed record ThermalPrintPreflightDecision(bool CanPrint, string? BlockReason, IReadOnlyList<string> Warnings, int EffectiveCopyCount);`
  - `public static class ThermalPrintPreflightPolicy` with `public const double MediaToleranceMm = 0.5d;`, `public static ThermalPrintPreflightDecision Evaluate(ThermalPrintPreflightInput input)`, and `public static ThermalPrinterResolution? SelectMatchingResolution(int requestedDpi, IReadOnlyList<ThermalPrinterResolution> capabilities)`.

- [ ] **Step 1: Write the failing policy tests**

Add tests named at minimum:

```csharp
ExactValidatedMedia_IsAccepted()
QuantizedFourBySixWithinHalfMillimeter_IsAcceptedWithoutScalingPermission()
ValidatedMediaBeyondHalfMillimeter_IsBlocked()
MissingValidatedMediaDimension_IsBlocked()
SwappedDimensions_AreBlockedForUnrotatedRequest()
ImageableAreaShortfall_AddsWarningButDoesNotBlock()
MissingImageableArea_DoesNotBlock()
MatchingResolution_IsSelected()
UnsupportedResolution_AddsWarning()
DialogCopyCountAboveOne_NormalizesEffectiveCopiesToOneAndWarns()
```

Pin exact examples:

```text
requested 102.0 × 152.0
accepted  101.6 × 152.4
blocked   101.4 × 152.4
```

For imageable-area shortfall, assert `CanPrint == true`, warning contains `recorte`, and no result field authorizes scale/translation. For copy count 3, assert `EffectiveCopyCount == 1` and warning mentions SG PDF Editor controls quantity.

- [ ] **Step 2: Run only the new tests and verify RED**

Run:

```bash
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter FullyQualifiedName~ThermalPrintPreflightPolicyTests
```

Expected: compile failure because `ThermalPrintPreflightPolicy` and its records do not exist.

- [ ] **Step 3: Implement the pure policy**

Implement the exact interfaces above. `Evaluate` must validate finite positive requested dimensions, require finite positive validated dimensions, compare each absolute dimension difference independently against `0.5d`, warn (not block) for imageable-area shortfall/unsupported DPI/copy normalization, and always report `EffectiveCopyCount = 1` when printable.

`SelectMatchingResolution` chooses an entry only when both X and Y DPI are within **±2 dpi** of `requestedDpi`; if multiple match, choose the smallest absolute X+Y difference, preserving capability order on ties. This ±2 dpi matching tolerance is only for capability-class matching and does not alter label geometry.

- [ ] **Step 4: Run Task 1 tests and full locked build/test**

Run:

```bash
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Expected: build 0 warnings / 0 errors; all tests PASS.

- [ ] **Step 5: Commit Task 1**

```bash
git add src/SGPdf.App/Printing/ThermalPrintPreflightPolicy.cs tests/SGPdf.App.Tests/ThermalPrintPreflightPolicyTests.cs
git commit -m "feat(print): add thermal preflight policy"
```

---

### Task 2: Exact-Size LabelPrintPaginator

**Files:**
- Create: `src/SGPdf.App/Printing/LabelPrintPaginator.cs`
- Create: `tests/SGPdf.App.Tests/LabelPrintPaginatorTests.cs`

**Interfaces:**
- Consumes: `LabelLayoutPlan`, `IReadOnlyList<ZplRenderedLabel>`.
- Produces: `public sealed class LabelPrintPaginator : DocumentPaginator` with constructor `public LabelPrintPaginator(LabelLayoutPlan plan, IReadOnlyList<ZplRenderedLabel> renderedLabels)`.
- Contract: requires `plan.LabelsPerPage == 1`; requires `renderedLabels.Count == plan.DesignCount`; blocks `plan.PageCount > int.MaxValue`; `PageCount == checked((int)plan.PageCount)`; `PageSize` is exact plan page size converted by `96d / 25.4d`; `GetPage(n)` uses `plan.GetPage(n).Placements.Single()` and the matching `ZplRenderedLabel.PngBytes`.

- [ ] **Step 1: Write failing paginator tests**

Add STA-safe tests named at minimum:

```csharp
Constructor_RejectsMultiUpPlan()
Constructor_RejectsRenderedDesignCountMismatch()
PageSize_UsesExactPlanMillimetersWithoutPrintableAreaFit()
RepeatedQuantities_ResolveCorrectRenderedDesignPerPage()
RotatedThermalPlan_UsesAlreadyRotatedF2_4PageDimensionsExactlyOnce()
PageCount_AllowsInt32MaxValue_AndRejectsOneAboveWithoutMaterializingPages()
```

Use tiny generated PNGs with different solid colors for two designs; render returned `DocumentPage.Visual` into a `RenderTargetBitmap` and sample the center pixel so pages A,A,B prove lazy repeated-design mapping. For 90° use a non-square source pattern and assert both the physical `DocumentPage.Size` and one orientation-sensitive pixel location.

For huge-count tests, construct a `ZplDocument`/quantity combination whose `LabelLayoutPlan.PageCount` reaches the boundary without iterating pages; do not allocate a list proportional to output copies.

- [ ] **Step 2: Run Task 2 tests and verify RED**

Run:

```bash
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter FullyQualifiedName~LabelPrintPaginatorTests
```

Expected: compile failure because `LabelPrintPaginator` does not exist.

- [ ] **Step 3: Implement `LabelPrintPaginator` minimally**

Use WPF `BitmapImage`/`BitmapSource` loaded fully from managed PNG bytes (`CacheOption=OnLoad` or equivalent). Draw a white background and the label image into the exact full physical page rectangle from the plan. For `LabelRotation.Degrees90`, rotate the bitmap/visual once using the F2.4 placement rotation; **do not swap page dimensions again**. Do not read printer printable-area dimensions anywhere in this class.

- [ ] **Step 4: Run Task 2 tests + full suite**

Run the Task 2 filter, then locked restore/build/full test as in Task 1.

Expected: all PASS, build 0/0.

- [ ] **Step 5: Commit Task 2**

```bash
git add src/SGPdf.App/Printing/LabelPrintPaginator.cs tests/SGPdf.App.Tests/LabelPrintPaginatorTests.cs
git commit -m "feat(print): add exact-size label paginator"
```

---

### Task 3: Windows PrintQueue / PrintTicket Preflight and Workflow

**Files:**
- Create: `src/SGPdf.App/Printing/WindowsThermalPrintPreflight.cs`
- Create: `src/SGPdf.App/Printing/WindowsThermalPrintWorkflow.cs`
- Create: `tests/SGPdf.App.Tests/WindowsThermalPrintPreflightTests.cs`

**Interfaces:**
- Consumes: Task 1 policy, Task 2 paginator, selected `PrintQueue`, dialog `PrintTicket`, current `LabelLayoutPlan`, rendered PNG list, requested DPI.
- Produces:
  - `public sealed record WindowsThermalPrintPreflightResult(bool CanPrint, PrintTicket? ValidatedPrintTicket, int? ValidatedDpi, IReadOnlyList<string> Warnings, string? BlockReason);`
  - `public static class WindowsThermalPrintPreflight` with `public static WindowsThermalPrintPreflightResult Run(PrintQueue queue, PrintTicket dialogTicket, double requestedWidthMm, double requestedHeightMm, int requestedDpi)` and internal helper `internal static PrintTicket CreateCandidateTicket(PrintTicket dialogTicket, double requestedWidthMm, double requestedHeightMm, PageResolution? matchingResolution)`.
  - `public static class WindowsThermalPrintWorkflow` with `public static bool TryPrint(Window owner, LabelLayoutPlan plan, IReadOnlyList<ZplRenderedLabel> renderedLabels, int requestedDpi)`.

- [ ] **Step 1: Write failing deterministic Windows-adapter tests**

Without requiring a physical printer, test `CreateCandidateTicket` and any internal conversion helpers:

```csharp
CreateCandidateTicket_SetsExactRequestedMediaInWpfUnits()
CreateCandidateTicket_ForcesCopyCountOne()
CreateCandidateTicket_PreservesSelectedMatchingResolution()
ConvertImageableArea_ToMillimeters_PreservesOriginAndExtent()
BuildSummary_IncludesPrinterSizeResolutionQuantityAndNoScaling()
```

Also add a test asserting no new PackageReference is needed for `System.Printing`/WPF APIs.

- [ ] **Step 2: Run Task 3 tests and verify RED**

Expected: compile failure for missing `WindowsThermalPrintPreflight`/workflow helpers.

- [ ] **Step 3: Implement the thin Windows adapter**

`Run` must:

1. null-check queue/ticket and call `queue.GetPrintCapabilities(dialogTicket)`;
2. map `PageResolutionCapability` to Task 1 resolution records and choose a ±2 dpi match;
3. build a delta/candidate ticket requesting exact page dimensions, `CopyCount=1`, and matching resolution when one exists;
4. call `queue.MergeAndValidatePrintTicket(dialogTicket, candidateTicket)`;
5. read the validated media dimensions, validated DPI and `PageImageableArea`;
6. call `ThermalPrintPreflightPolicy.Evaluate`;
7. return the validated ticket only when `CanPrint`.

If capabilities or validation throw/return unusable media, return a blocked result with a user-readable reason; do not silently fall back to defaults.

- [ ] **Step 4: Implement `WindowsThermalPrintWorkflow.TryPrint`**

Flow must be exactly:

1. show standard `PrintDialog`;
2. on cancel return `false` without spool;
3. require selected `PrintQueue` + dialog ticket;
4. call preflight;
5. if blocked, show specific error and return `false`;
6. build confirmation text containing printer name, exact mm size, validated DPI (or driver/default wording), total label count, Windows copies=1, and `Escalado SG PDF Editor: ninguno`; append warnings;
7. confirmation uses `MessageBoxButton.OKCancel` (or equally simple existing WPF surface), and cancel returns `false`;
8. set the dialog's `PrintTicket` to the validated ticket;
9. instantiate `LabelPrintPaginator(plan, renderedLabels)` and call `PrintDocument` once;
10. return `true` only after `PrintDocument` returns without exception.

Do not create a PDF, temp file, RAW job, printer-discovery service, or background queue abstraction.

- [ ] **Step 5: Verify Task 3 and full suite**

Run filtered tests plus locked restore/build/full tests.

Expected: all PASS; no package-lock changes.

- [ ] **Step 6: Commit Task 3**

```bash
git add src/SGPdf.App/Printing/WindowsThermalPrintPreflight.cs src/SGPdf.App/Printing/WindowsThermalPrintWorkflow.cs tests/SGPdf.App.Tests/WindowsThermalPrintPreflightTests.cs
git commit -m "feat(print): add Windows thermal print preflight"
```

---

### Task 4: WPF Print Entry Point, Regression Guard, and Slice Closure

**Files:**
- Create: `src/SGPdf.App/MainWindow.LabelPrinting.cs`
- Modify: `src/SGPdf.App/MainWindow.LabelLayout.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Create: `tests/SGPdf.App.Tests/MainWindowZplPrintUiTests.cs`
- Modify if needed: `tests/SGPdf.App.Tests/OfflineRuntimeTests.cs`
- Create/update closure docs listed in File Map.

**Interfaces:**
- Consumes: `WindowsThermalPrintWorkflow.TryPrint`, current `_labelLayoutPlan`, `_labelLayoutSettings`, `_renderedZplLabels`, `_zplRenderOptions`.
- Produces: XAML button `PrintLabelButton`; handler `PrintLabels_Click`; local test seam `private Func<Window, LabelLayoutPlan, IReadOnlyList<ZplRenderedLabel>, int, bool> _printThermalLabels = WindowsThermalPrintWorkflow.TryPrint;`.
- Requested DPI mapping is fixed: 6→152, 8→203, 12→300, 24→600; unexpected dpmm uses checked nearest integer `dpmm * 25.4` only if a future valid setting reaches this handler.

- [ ] **Step 1: Write failing WPF UI tests**

Add tests named at minimum:

```csharp
PrintButton_IsDisabledWithoutLoadedZpl()
PrintButton_IsEnabledOnlyForValidThermalLayout()
A4OrLetterLayout_DisablesThermalPrintButton()
Click_UsesCurrentPlanRenderedLabelsAndRequestedDpi()
WorkflowCancel_DoesNotChangeWorkspaceOrReportSubmittedJob()
WorkflowSuccess_ReportsSubmittedJobWithoutRerenderingLabelize()
```

Inject `_printThermalLabels` by reflection as existing UI tests do for PDF export. Capture the exact plan/list/DPI passed. Snapshot selected design index, quantity selection, render options and image source before cancel/failure and assert unchanged afterward.

- [ ] **Step 2: Run UI tests and verify RED**

Expected: failure because `PrintLabelButton`/handler/test seam do not exist.

- [ ] **Step 3: Add the WPF button and handler**

Add `Imprimir etiquetas...` directly below `Guardar como PDF...`. Enable only when `_zplDocument != null`, rendered labels exist, `_labelLayoutPlan != null`, `_labelLayoutSettings.MediaKind == LabelMediaKind.Thermal`, and `!_isBusy`.

Handler must capture current plan/rendered list/DPI, set busy around workflow execution, call the injected delegate once, and update status only to `Trabajo de impresión enviado.` on `true`; cancellation leaves a neutral/cancelled status and never mutates workspace state.

- [ ] **Step 4: Add regression/offline guards**

Confirm:

- no new PackageReference;
- no `HttpClient`, web URI, RAW printer P/Invoke, direct socket/USB/serial code;
- existing PDF printing tests still pass unchanged;
- F2.4 PDF export tests still pass;
- runtime package allowlist remains PDFium + PDFsharp only.

Update `OfflineRuntimeTests` only if its source-scan needs an explicit assertion for the new print files; do not weaken existing guards.

- [ ] **Step 5: Fresh exact-head CI before docs closure**

Run/push fresh Windows CI. Required evidence:

```text
hygiene PASS
Labelize staging PASS
locked restore PASS
Release build 0 warnings / 0 errors
all tests PASS / 0 FAIL / 0 SKIPPED
```

Audit diff against F2.4 and explicitly confirm: no package addition, no PDF paginator scaling reuse, no RAW ZPL, no private fixtures, no runtime network, no second layout engine.

- [ ] **Step 6: Update closure documentation in one commit**

Update/create:

- `.planning/phases/02-f1-zpl-gate/F2.5-PLAN.md` with RED/GREEN/CI evidence;
- `docs/history/2026-10-07-F2.5.md` portable handoff;
- `.planning/STATE.md` → F2.5 automated PASS, next F2.6;
- `.planning/ROADMAP.md`;
- `AGENTS.md` execution order.

State prominently that real thermal-printer dimensions/scanner behavior remain **NOT RUN** unless actual hardware QA was performed.

- [ ] **Step 7: Final exact-head push + PR CI**

Require both push and PR workflow success on the closure-doc head. Record head SHA and run IDs in the draft PR body. Keep the PR draft/unmerged.

- [ ] **Step 8: Commit Task 4 / closure**

Use focused functional commit(s) followed by one closure-doc commit; do not squash or merge automatically.

---

## Self-Review Result

**Spec coverage:** Complete. Exact media, driver validation, imageable-area warning, resolution request, copy normalization, lazy quantities, exact paginator, standard dialog/confirmation/spool, offline/privacy, no new package, and hardware-QA boundary each map to a task.

**Step scan:** Each task owns an independently reviewable deliverable and explicit RED→GREEN check. No implementation body is prescribed beyond algorithms/policies fixed by the approved spec.

**Type consistency:** `LabelLayoutPlan` + `IReadOnlyList<ZplRenderedLabel>` flow unchanged from Tasks 2→4. DPI enters the Windows workflow as `int requestedDpi`; validated DPI is nullable because drivers may omit it. `PrintTicket` exists only in the Windows-specific adapter/result.

**Review Focus coverage:** All five focus items have explicit tests/tasks above.

**Proportion:** Four implementation tasks; no new package or architecture layer; hardware-specific claims remain outside automated closure.
