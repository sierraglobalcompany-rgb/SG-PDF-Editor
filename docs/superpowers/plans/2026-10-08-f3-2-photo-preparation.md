# F3.2 — Photo/Scan Signature Preparation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert a local PNG/JPG/JPEG photo or scan of a signature on white/near-white paper into the existing transparent `SignatureAsset`, with simple local cleanup controls and no PDF/runtime-network changes.

**Architecture:** F3.2 is a preprocessing slice only. A feature-local loader normalizes PNG/JPEG to immutable managed BGRA, a pure deterministic processor estimates paper/removes it with soft alpha/recolors/crops, and a focused WPF dialog provides reduced interactive preview while final Apply always reprocesses the full source. MainWindow receives only `SignatureAsset?` and passes successful output into the existing F3.1 `AddSignatureAsset(...)` path.

**Tech Stack:** C# / .NET 10 / WPF built-in bitmap codecs, existing F3.1 `SignatureAsset`, xUnit STA tests. No new runtime package.

**Spec:** `docs/superpowers/specs/2026-10-08-f3-2-photo-preparation-design.md`

## Global Constraints

- Base exactly F3.1 final head `d559169280f9d9ee2c19f1c245f6657d1b598c6d`.
- Branch `feat/f3-2-photo-preparation`; PR stays draft/unmerged until explicit user approval.
- Windows x64 + WPF + .NET 10 remain unchanged.
- Input only `.png`, `.jpg`, `.jpeg`; decode locally with Windows/WPF facilities.
- Decoded safety limit remains exactly **20,000,000 pixels**.
- Normalize source to immutable top-to-bottom BGRA managed pixels; preserve source alpha.
- Output authority is the existing F3.1 `SignatureAsset`; no second asset/placement/writer hierarchy.
- No changes to `PdfVisualSignatureWriter`, coordinate mapping, PDFium P/Invoke, ZPL/labels, PDFsharp scope, or cryptographic-signature behavior.
- No AI/ML, cloud/background-removal API, OCR, OpenCV/ImageSharp, runtime HTTP, telemetry, upload, or temporary signature-image file.
- No F3.3 InkCanvas/drawing and no F3.4 persistent library.
- Processing always derives from immutable decoded source; previews never become final assets.
- Preview maximum side is **1200 px**.
- UI settings ranges: `BackgroundRemoval 0..100`, `Brightness -100..100`, `Contrast -100..100`.
- Ink styles: `Original`, `Black`, `Blue`; blue is frozen as RGB `#194196` (BGRA bytes `96 41 19 A` hex / `150,65,25,A` decimal).
- Auto crop defaults ON; no manual crop/mask/eraser.
- Failure/cancel must not change current PDF, existing placements, selection, or dirty state.
- Real photo-quality QA is separate from automated PASS.

## Review Focus

1. **Very small or edge-dimension images:** valid 1-pixel dimensions must not cause perimeter/crop arithmetic crashes; blank/non-useful content still blocks Apply. Covered in Task 2 processor tests.
2. **Source with existing transparency:** cleanup alpha must multiply, never replace, original alpha; transparent pixels can never become opaque. Covered in Task 2.
3. **Stale async preview:** an older slider result must never replace a newer preview or be used by Apply. Covered in Task 3 coordinator tests.
4. **Large decoded geometry:** `20,000,001` pixels must be rejected before F3.2 allocates its normalized BGRA output buffer where practical; existing F3.1 direct PNG loader must retain the same limit. Covered in Task 1.
5. **A valid prior placement followed by photo cancel/failure:** placement list, selection and dirty state must remain byte-for-byte/logically unchanged. Covered in Task 4.

---

### Task 1: Shared image safety + local photo source loader

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureImageLimits.cs`
- Create: `src/SGPdf.App/Features/Sign/SignaturePhotoSource.cs`
- Create: `src/SGPdf.App/Features/Sign/SignaturePhotoLoader.cs`
- Modify: `src/SGPdf.App/Features/Sign/SignaturePngLoader.cs`
- Test: `tests/SGPdf.App.Tests/SignaturePhotoLoaderTests.cs`
- Test: `tests/SGPdf.App.Tests/SignaturePngLoaderTests.cs`

**Interfaces:**

```csharp
internal static class SignatureImageLimits
{
    internal const long MaxDecodedPixels = 20_000_000;
    internal static void ValidatePixelCount(int width, int height);
}

internal sealed class SignaturePhotoSource
{
    internal SignaturePhotoSource(int pixelWidth, int pixelHeight, int stride, byte[] bgraPixels, string? sourceName = null);
    internal int PixelWidth { get; }
    internal int PixelHeight { get; }
    internal int Stride { get; }
    internal ReadOnlyMemory<byte> BgraPixels { get; }
    internal string? SourceName { get; }
}

internal static class SignaturePhotoLoader
{
    internal static SignaturePhotoSource Load(string path);
}
```

`SignaturePhotoSource` validates positive dimensions, stride `>= width*4`, sufficient buffer, copies only the declared buffer, and exposes immutable memory semantics matching `SignatureAsset`.

`SignaturePhotoLoader.Load` accepts extension case-insensitively only for `.png/.jpg/.jpeg`, uses built-in WPF decoding with `BitmapCacheOption.OnLoad`, validates geometry via `SignatureImageLimits` before allocating the normalized output buffer, converts to `PixelFormats.Bgra32`, and preserves opaque or transparent input. Unlike `SignaturePngLoader`, it does **not** require useful transparency.

- [ ] **Step 1: Write RED loader/safety tests**

Add tests named:

```text
Load_Jpeg_NormalizesToBgraAndPreservesDimensions
Load_OpaquePng_IsAcceptedForPhotoPreparation
Load_TransparentPng_PreservesSourceAlpha
Load_UnsupportedExtension_IsRejectedBeforeDecode
Load_CorruptAcceptedExtension_IsRejectedWithoutPartialAsset
ValidatePixelCount_AllowsExactlyTwentyMillion_AndRejectsTwentyMillionAndOne
PhotoSource_CopiesInputBufferAndValidatesGeometry
ExistingDirectPngLoader_StillRejectsOpaquePngAndUsesSharedPixelLimit
```

Use only synthetic WPF-generated PNG/JPEG fixtures in temporary test directories. For the limit test call the small shared validation seam; do not allocate a 20 MP fixture merely to prove arithmetic.

- [ ] **Step 2: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignaturePhotoLoaderTests|FullyQualifiedName~SignaturePngLoaderTests"
```

Expected: new tests fail because `SignaturePhotoSource`, `SignaturePhotoLoader`, and shared `SignatureImageLimits` do not exist.

- [ ] **Step 3: Implement the minimum loader/safety contracts**

Move only the 20M policy from `SignaturePngLoader` into `SignatureImageLimits`; preserve direct PNG behavior exactly, including transparency requirement and Spanish controlled failures. Implement photo decode without introducing a generic image-loader framework.

- [ ] **Step 4: Run focused tests GREEN, then full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignaturePhotoLoaderTests|FullyQualifiedName~SignaturePngLoaderTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require focused PASS, locked restore PASS, Release build 0 warnings/0 errors, all tests PASS.

- [ ] **Step 5: Commit Task 1**

```bash
git add src/SGPdf.App/Features/Sign/SignatureImageLimits.cs src/SGPdf.App/Features/Sign/SignaturePhotoSource.cs src/SGPdf.App/Features/Sign/SignaturePhotoLoader.cs src/SGPdf.App/Features/Sign/SignaturePngLoader.cs tests/SGPdf.App.Tests/SignaturePhotoLoaderTests.cs tests/SGPdf.App.Tests/SignaturePngLoaderTests.cs
git commit -m "feat(sign): load signature photos safely"
```

---

### Task 2: Deterministic paper removal, recolor, validation, and auto-crop

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureImageProcessingSettings.cs`
- Create: `src/SGPdf.App/Features/Sign/SignatureImageProcessor.cs`
- Test: `tests/SGPdf.App.Tests/SignatureImageProcessorTests.cs`

**Interfaces:**

```csharp
internal enum SignatureInkStyle
{
    Original,
    Black,
    Blue
}

internal readonly record struct SignatureImageProcessingSettings(
    int BackgroundRemoval,
    int Brightness,
    int Contrast,
    SignatureInkStyle InkStyle,
    bool AutoCrop)
{
    internal static SignatureImageProcessingSettings Automatic { get; }
    internal void Validate();
}

internal readonly record struct SignaturePaperColor(byte R, byte G, byte B);

internal static class SignatureImageProcessor
{
    internal static SignaturePaperColor EstimatePaper(SignaturePhotoSource source);
    internal static SignatureAsset Process(
        SignaturePhotoSource source,
        SignatureImageProcessingSettings settings,
        SignaturePaperColor? paper = null);
}
```

Freeze `Automatic` at:

```text
BackgroundRemoval = 65
Brightness        = 0
Contrast          = 20
InkStyle          = Original
AutoCrop          = true
```

#### Paper estimate

KISS deterministic rule:

- perimeter band = `max(1, round(min(width,height) * 0.05))`, clamped not to overlap past the image center;
- candidates are perimeter pixels with original alpha `>= 32` and Rec.709 luminance `>= 180`;
- if fewer than 8 candidates exist, throw controlled `InvalidDataException` indicating no plausible light paper background;
- paper RGB is the per-channel median of candidates (for even count use the integer average of the two middle values).

Small images that have fewer than 8 distinct perimeter pixels may reuse available perimeter pixels; the processor must not crash. If fewer than 8 candidate samples still exist, the light-background failure is correct.

#### Soft removal

For original RGB and estimated paper RGB:

```text
distance = sqrt((R-Pr)^2 + (G-Pg)^2 + (B-Pb)^2)
low      = 8  + BackgroundRemoval * 0.32
high     = low + 32
cleanup  = smoothstep(low, high, distance)   // 0..1
finalA   = round(originalA * cleanup)
```

`smoothstep(low,high,x)` is clamped `t=(x-low)/(high-low)` then `t*t*(3-2*t)`.

This intentionally removes exact paper even at strength 0 while increasing the near-paper removal band monotonically as strength rises.

#### RGB adjustment + ink styles

- Compute alpha from **original** RGB/paper distance first.
- Then apply brightness/contrast to RGB for visible output; these controls never alter the frozen paper estimate.
- Brightness: add `round(Brightness * 255 / 100.0)` per channel with byte clamp.
- Contrast factor: `(100 + Contrast) / 100.0`; adjusted channel = `128 + (channel - 128) * factor`, rounded/clamped.
- `Original`: keep adjusted RGB.
- `Black`: output RGB `0,0,0` with computed alpha.
- `Blue`: output RGB `25,65,150` (`#194196`) with computed alpha.

#### Usable-content rule

After alpha computation, usable signature requires both:

- at least **32 pixels** with alpha `>= 64`; and
- their alpha>=64 bounding box width `>= 4` and height `>= 4` pixels.

Otherwise `Process` throws controlled `InvalidDataException` with the Spanish no-usable-signature guidance from the spec.

#### Auto-crop

Bounds are calculated from pixels with alpha `>= 16`. Padding:

```text
padding = clamp(round(max(contentWidth, contentHeight) * 0.02), 4, 32)
```

Clamp crop rectangle to source bounds. `AutoCrop=false` preserves processed source dimensions.

- [ ] **Step 1: Write RED pure processor tests**

Add tests named:

```text
AutomaticSettings_AreFrozenAndRangesValidate
EstimatePaper_ToleratesWhiteGrayWarmAndCoolPaperFixtures
EstimatePaper_RejectsNoPlausibleLightBackground
PurePaper_BecomesTransparent
NearPaper_ProducesIntermediateSoftAlpha
BlackAndBlueInk_RemainVisibleInOriginalMode
BlackRecolor_PreservesComputedAlpha
BlueRecolor_Uses194196AndPreservesAlpha
OriginalAlpha_MultipliesCleanupAlphaAndNeverBecomesMoreOpaque
StrongerBackgroundRemoval_NeverRestoresPaperOpacity
BrightnessAndContrast_ClampAtSupportedExtremes
SameInputAndSettings_AreByteDeterministic
BlankWhiteImage_IsRejectedAsNoUsableSignature
TinyIsolatedNoise_IsRejected
AutoCrop_FindsContentAndAddsBoundedPadding
CropDisabled_PreservesSourceDimensions
OnePixelEdgeGeometry_DoesNotCrashAndRejectsBlankContentCleanly
```

Fixtures are tiny synthetic BGRA buffers; no real signatures.

- [ ] **Step 2: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureImageProcessorTests"
```

Expected: compile/test failure only because processor/settings contracts do not yet exist.

- [ ] **Step 3: Implement minimal pure processor**

Keep all math in managed code and no WPF/PDF/file dependencies in `SignatureImageProcessor`. Do not optimize with unsafe/SIMD unless profiling later proves necessary.

- [ ] **Step 4: Run focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureImageProcessorTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require 0 warnings/0 errors and all PASS.

- [ ] **Step 5: Commit Task 2**

```bash
git add src/SGPdf.App/Features/Sign/SignatureImageProcessingSettings.cs src/SGPdf.App/Features/Sign/SignatureImageProcessor.cs tests/SGPdf.App.Tests/SignatureImageProcessorTests.cs
git commit -m "feat(sign): clean photo signatures locally"
```

---

### Task 3: Reduced preview coordinator + focused preparation dialog

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignaturePhotoPreview.cs`
- Create: `src/SGPdf.App/SignaturePhotoDialog.xaml`
- Create: `src/SGPdf.App/SignaturePhotoDialog.xaml.cs`
- Test: `tests/SGPdf.App.Tests/SignaturePhotoPreviewTests.cs`
- Test: `tests/SGPdf.App.Tests/SignaturePhotoDialogTests.cs`

**Interfaces:**

```csharp
internal static class SignaturePhotoPreview
{
    internal const int MaxSide = 1200;
    internal static SignaturePhotoSource CreateReducedSource(SignaturePhotoSource source);
}

internal sealed class SignaturePhotoPreviewVersion
{
    internal long BeginRequest();
    internal bool IsCurrent(long version);
}

public partial class SignaturePhotoDialog : Window
{
    internal SignaturePhotoDialog(SignaturePhotoSource source);
    internal SignatureAsset? PreparedAsset { get; }

    internal static SignatureAsset? Prepare(Window owner, string path);
}
```

`Prepare(owner,path)` loads once via `SignaturePhotoLoader`, constructs the modal dialog, and returns `PreparedAsset` only when dialog result is true. No temp image file.

#### Preview behavior

- `CreateReducedSource`: if max side `<=1200`, return an equivalent immutable source copy; otherwise use WPF `BitmapSource`/`TransformedBitmap` bilinear scaling, scale factor `1200/max(width,height)`, dimensions rounded to nearest integer with minimum 1, then copy back to BGRA `SignaturePhotoSource`.
- Dialog stores the full immutable source and a reduced preview source separately.
- Estimate paper separately for full and preview source only if scaling materially changes sampled perimeter; settings semantics remain identical.
- Each control change calls `BeginRequest()`, snapshots settings, processes the reduced source asynchronously, and updates `PreviewImage` only if `IsCurrent(version)` when it returns.
- Apply never consumes `_lastPreviewAsset`. It processes the **full source** with current settings and sets `PreparedAsset` only after full-resolution success.

#### UI contract

Named controls for testability:

```text
SignaturePhotoPreviewImage
SignaturePhotoAutomaticButton
SignaturePhotoBackgroundSlider
SignaturePhotoBrightnessSlider
SignaturePhotoContrastSlider
SignaturePhotoInkComboBox
SignaturePhotoAutoCropCheckBox
SignaturePhotoResetButton
SignaturePhotoApplyButton
SignaturePhotoCancelButton
SignaturePhotoStatusText
```

Checkerboard can be a local WPF `DrawingBrush`/tile brush; no image resource/network asset.

- [ ] **Step 1: Write RED preview tests**

Tests:

```text
CreateReducedSource_PreservesSmallSourceDimensionsAndPixels
CreateReducedSource_LimitsLongestSideTo1200AndPreservesAspectRatio
PreviewVersion_OlderRequestBecomesStaleAfterNewerRequest
FinalProcessingContract_UsesFullSourceRatherThanReducedPreview
```

The final-processing contract test can exercise a small internal dialog helper/seam if needed; do not require actual `ShowDialog()`.

- [ ] **Step 2: Write RED STA dialog tests**

Tests:

```text
Dialog_DefaultControlsReflectAutomaticSettings
AutomaticAndReset_RestoreFrozenAutomaticSettings
Apply_ValidSourceSetsPreparedAssetAndDialogSuccess
Apply_NoUsableSignatureKeepsDialogOpenAndPreparedAssetNull
Cancel_LeavesPreparedAssetNull
StalePreviewCompletion_DoesNotReplaceCurrentPreview
```

Use the established `RunInSta` style. Add only narrow internal delegates if needed to prevent real modal interaction/asynchrony in headless tests; no dialog-service framework.

- [ ] **Step 3: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignaturePhotoPreviewTests|FullyQualifiedName~SignaturePhotoDialogTests"
```

Expected: failures because preview/dialog contracts do not exist.

- [ ] **Step 4: Implement reduced preview + modal WPF dialog**

Keep XAML focused and conventional. Slider changes may debounce by a small local timer/async version gate, but do not add an application-wide scheduler. The dialog must remain usable if preview processing fails: show controlled status, preserve original source/settings, allow retry/cancel.

- [ ] **Step 5: Run focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignaturePhotoPreviewTests|FullyQualifiedName~SignaturePhotoDialogTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require 0 warnings/0 errors and all PASS.

- [ ] **Step 6: Commit Task 3**

```bash
git add src/SGPdf.App/Features/Sign/SignaturePhotoPreview.cs src/SGPdf.App/SignaturePhotoDialog.xaml src/SGPdf.App/SignaturePhotoDialog.xaml.cs tests/SGPdf.App.Tests/SignaturePhotoPreviewTests.cs tests/SGPdf.App.Tests/SignaturePhotoDialogTests.cs
git commit -m "feat(sign): add photo signature preparation dialog"
```

---

### Task 4: Integrate `Crear desde foto...` into existing FIRMAR flow

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Sign.cs`
- Modify: `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs`

**Interfaces:**

Add one narrow test seam beside existing F3.1 seams:

```csharp
private Func<Window, string, SignatureAsset?> _prepareSignaturePhoto =
    static (owner, path) => SignaturePhotoDialog.Prepare(owner, path);
```

Add focused methods:

```csharp
private void CreateSignatureFromPhoto_Click(object sender, RoutedEventArgs e);
private bool TryCreateSignatureFromPhotoPath(string path);
```

`TryCreateSignatureFromPhotoPath(path)` calls `_prepareSignaturePhoto(this,path)`. `null` means cancel/no apply and returns false without touching edit state. Valid asset is passed to the already-existing `AddSignatureAsset(asset)` exactly once.

The existing imperative `BuildSignaturePropertiesPanel()` receives a real button named/registered as `CreateSignatureFromPhotoButton` with content `Crear desde foto...`. Remove/replace the old F3.1 explanatory text that says cleanup is future; do not add F3.3/F3.4 dead controls.

- [ ] **Step 1: Write RED MainWindow integration tests**

Add tests:

```text
CreateFromPhotoButton_IsAvailableOnlyInUsableFirmarPdfContext
PhotoCancel_AddsNoPlacementAndPreservesPriorSelectionDirtyState
PhotoFailure_AddsNoPlacementAndPreservesPriorPlacements
PhotoApply_AddsExactlyOneCenteredSelectedDirtyPlacementThroughExistingPath
PhotoApply_DoesNotInvokePdfWriterOrChangeActivePdf
```

For cancel/failure test, first create at least one valid existing placement and snapshot placement IDs/bounds/selection/dirty state. Inject `_prepareSignaturePhoto` rather than showing a dialog.

- [ ] **Step 2: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureTests"
```

Expected: new tests fail because F3.2 button/seam/methods do not exist.

- [ ] **Step 3: Implement minimal MainWindow integration**

The file picker accepts `PNG/JPG/JPEG`. Cancel at file picker and cancel from preparation dialog are no-op. Controlled exceptions set concise Spanish status/message only; existing placements are never cleared. Reuse F3.1's headless-message behavior so tests cannot hang on a real `MessageBox`.

- [ ] **Step 4: Focused GREEN + complete verification**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require all PASS and Release 0 warnings/0 errors.

- [ ] **Step 5: Commit Task 4**

```bash
git add src/SGPdf.App/MainWindow.Sign.cs tests/SGPdf.App.Tests/MainWindowSignatureTests.cs
git commit -m "feat(sign): create signatures from photos"
```

---

### Task 5: F3.2 scope audit, documentation, and exact-head verification

**Files:**
- Create: `docs/history/2026-10-08-F3.2.md`
- Create/update concise phase companion: `.planning/phases/04-f3-visual-signature/F3.2-PLAN.md`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `AGENTS.md`

**PR:** draft; base `feat/f3-visual-signature`, head `feat/f3-2-photo-preparation`; never merge without explicit user approval.

- [ ] **Step 1: Audit branch diff against exact F3.1 head**

Require:

```text
no new PackageReference / packages.lock mutation
no PDFium/PdfVisualSignatureWriter/coordinate changes
no ZPL/label changes
no runtime HTTP/socket/cloud/AI code
no temp signature-image persistence
no F3.3 InkCanvas / F3.4 library
no real signature/photo fixture committed
```

Confirm expected scope is only F3.2 feature-local loader/settings/processor/preview/dialog, narrow `MainWindow.Sign.cs` integration, tests, and docs.

- [ ] **Step 2: Fresh full exact-head verification**

Run fresh on Windows CI/local Windows executor:

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Record exact test count, 0 warnings/0 errors, and exact branch SHA. Do not infer manual photo quality from synthetic tests.

- [ ] **Step 3: Update closure docs**

Document separately:

```text
F3.2 automated = PASS only if exact-head CI is green
real phone/scanner photo quality = NOT RUN unless actually executed
F3.1 automated remains PASS
F3.3 Draw signature = next design/approval slice
F3.4 library = later
```

Record any implementation ruling, especially if preview tuning differs from initial numeric assumptions while preserving frozen behavioral invariants.

- [ ] **Step 4: Commit closure docs as final branch mutation**

```bash
git add docs/history/2026-10-08-F3.2.md .planning/phases/04-f3-visual-signature/F3.2-PLAN.md .planning/STATE.md .planning/ROADMAP.md AGENTS.md
git commit -m "docs(sign): close F3.2 automated photo preparation"
```

- [ ] **Step 5: Verify final documentation head again**

Require **both** push CI and PR CI PASS on the same final documentation SHA. Record their IDs in the draft PR body/comment without mutating the branch afterward.

- [ ] **Step 6: Leave branch draft/unmerged**

No merge, rebase, squash, or deletion without explicit user approval.

---

## Execution Notes

Recommended method: **Native**. Tasks 1–4 share a tight chain of small feature-local contracts (`SignaturePhotoSource` → processor → preview/dialog → existing `AddSignatureAsset`) and this harness has no visible subagent executor; keeping the implementation in one session is cheaper and reduces interface drift. Use `superpowers:executing-plans`, `superpowers:test-driven-development`, and `superpowers:verification-before-completion` during execution.

Stop and return to design before proceeding if implementation appears to require OpenCV/ImageSharp, AI/cloud removal, a general masking/editor framework, changes to PDF writing/coordinates, persistent intermediate photo files, or any new runtime package.