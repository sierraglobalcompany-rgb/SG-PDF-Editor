# F3.3 — Draw Signature with WPF InkCanvas Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a user draw a visual signature locally with mouse/touch/stylus, correct it with small dialog-local history controls, convert it to the existing transparent `SignatureAsset`, and place it through the proven F3.1 signing flow.

**Architecture:** F3.3 is only a new signature-source adapter. WPF `InkCanvas` collects `StrokeCollection`; a focused renderer converts strokes to transparent BGRA at 300-DPI-equivalent sampling; a tiny history helper owns Undo/Redo/Clear; `MainWindow` receives only `SignatureAsset?` and reuses `AddSignatureAsset(...)`. No PDF writer, coordinate, F3.2 processor, ZPL, or persistence changes.

**Tech Stack:** C# / .NET 10 / WPF `InkCanvas`, `StrokeCollection`, `DrawingVisual`, `RenderTargetBitmap`; existing `SignatureAsset`, `SignatureImageLimits`, xUnit STA tests. No new runtime package.

**Spec:** `docs/superpowers/specs/2026-10-08-f3-3-drawn-signature-design.md`

## Global Constraints

- Base exactly F3.2 final head `2aa1f58a6e01397e84d8f8cfcf7eb6e0e168cf62`.
- Branch `feat/f3-3-drawn-signature`; PR remains draft/unmerged until explicit user approval.
- Windows x64 + WPF + .NET 10 remain unchanged.
- Input authority is WPF `StrokeCollection`; do not render the visible `InkCanvas` background/chrome into the signature.
- Output authority is the existing F3.1 `SignatureAsset`; no second asset/placement/writer hierarchy.
- Fixed ink colors: Black `#000000`; Blue `#194196`.
- Fixed logical widths: Thin `2.0 DIP`, Medium `3.5 DIP`, Thick `5.0 DIP`; Medium default.
- Final raster sampling scale is `300 / 96 = 3.125` relative to WPF DIPs.
- Reuse `SignatureImageLimits.MaxDecodedPixels = 20_000_000`; validate output geometry before allocating the full pixel buffer where practical.
- Useful drawing requires at least one stroke with at least two stylus points and union bounds at least `4 DIP × 4 DIP`.
- Transparent padding: `clamp(max(8, maxStrokeWidth * 2), 8, 24)` DIP on each side.
- Undo/Redo/Clear are dialog-local only; no application-wide command system.
- `Clear` is intentionally not undoable in F3.3 MVP and clears both history stacks.
- Color/width changes affect only newly drawn strokes; existing strokes keep their original `DrawingAttributes`.
- Cancel/failure must not modify existing placements, selection, dirty state, active PDF, or current page.
- No temporary signature image files, clipboard requirement, telemetry, cloud/network, AI/ML, pressure engine, custom stylus framework, F3.4 persistence, or new runtime dependency.
- Do not modify `PdfVisualSignatureWriter`, PDFium P/Invoke, signature coordinate mapping, F3.2 photo processing, ZPL/Labelize, PDFsharp scope, `.csproj`, or lockfiles unless a failing test proves the approved architecture impossible and design is revisited first.

## Review Focus

1. **Tiny taps / one-point strokes:** Apply stays disabled and renderer rejects them without clearing the canvas. Covered in Task 1 and Task 2.
2. **Mixed stroke attributes:** black/blue and thin/medium/thick strokes drawn in one signature preserve each stroke's original attributes after later selector changes. Covered in Task 1 and Task 2.
3. **History mutation:** Undo → Redo must not accidentally enter the "new user stroke" path or clear redo; a genuinely new `StrokeCollected` after Undo must clear redo. Covered in Task 2.
4. **Very large geometric bounds:** renderer rejects >20M output pixels before full BGRA allocation and leaves dialog strokes intact. Covered in Task 1 and Task 2.
5. **WPF raster variation:** tests verify geometry, transparency, color presence, crop/padding and relative width behavior; they do not assert the entire antialiased bitmap byte-for-byte across Windows renderer versions. Covered in Task 1.

---

### Task 1: Transparent stroke renderer + fixed ink presets

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureInkStyle.cs`
- Create: `src/SGPdf.App/Features/Sign/SignatureInkRenderer.cs`
- Test: `tests/SGPdf.App.Tests/SignatureInkRendererTests.cs`

**Interfaces:**

Produces:

```csharp
internal enum SignatureInkColor
{
    Black,
    Blue
}

internal enum SignatureInkWidth
{
    Thin,
    Medium,
    Thick
}

internal static class SignatureInkStyle
{
    internal static Color GetColor(SignatureInkColor color);
    internal static double GetWidthDip(SignatureInkWidth width);
    internal static DrawingAttributes CreateDrawingAttributes(
        SignatureInkColor color,
        SignatureInkWidth width);
}

internal static class SignatureInkRenderer
{
    internal const double RasterScale = 300d / 96d;
    internal static bool HasUsefulInk(StrokeCollection strokes);
    internal static SignatureAsset Render(
        StrokeCollection strokes,
        string? sourceName = "drawn-signature");
}
```

`CreateDrawingAttributes` returns normal WPF ink attributes with fixed color and width/height equal to the selected width. Do not add pressure-width logic.

`HasUsefulInk` and `Render` repeat the same useful-ink rule from the spec so UI enablement is not the only validation boundary.

`Render` must:

- clone/snapshot the strokes before drawing so the renderer does not mutate the dialog collection;
- compute union bounds from stroke geometry;
- calculate padding from the maximum `DrawingAttributes.Width/Height` present in the snapshot;
- compute pixel width/height as `ceil(paddedDip * 3.125)` with minimum 1;
- call `SignatureImageLimits.ValidatePixelCount(...)` before allocating/copying the final normalized BGRA buffer;
- render only the snapshot strokes into a transparent `DrawingVisual`/`DrawingContext` using `StrokeCollection.Draw(DrawingContext)` and a transform from source DIP bounds to the raster target;
- use `RenderTargetBitmap` with a transparent target; normalize the resulting premultiplied WPF pixels to `PixelFormats.Bgra32` before constructing `SignatureAsset`;
- never render a white canvas/background rectangle.

- [ ] **Step 1: Write RED renderer/preset tests**

Add focused STA tests named:

```text
InkStyle_UsesFrozenColorsAndWidths
HasUsefulInk_RejectsEmptyOnePointAndTooSmallBounds
HasUsefulInk_AcceptsNormalSignatureStroke
Render_BlackStroke_HasTransparentBackgroundAndVisibleBlackInk
Render_BlueStroke_Uses194196WithAlpha
Render_CropsAroundInkAndIncludesBoundedPadding
Render_MixedAttributes_PreserveEachStrokeColorAndRelativeWidth
Render_ThinMediumThick_HaveIncreasingVisibleCoverage
Render_DoesNotMutateSourceStrokesOrDrawingAttributes
Render_RejectsGeometryBeyondTwentyMillionPixelsBeforeOutputAllocation
EquivalentStrokeGeometry_ProducesStableDimensionsBoundsAndInkColors
```

Use synthetic `StylusPointCollection` / `Stroke` objects; no real signature data. For antialiasing, assert transparent corner/background samples, expected ink-color samples/tolerances and coverage/bounds rather than full bitmap byte equality.

- [ ] **Step 2: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkRendererTests"
```

Expected: fail only because `SignatureInkStyle` / `SignatureInkRenderer` contracts do not exist.

- [ ] **Step 3: Implement minimum preset + renderer contracts**

Use WPF built-ins only. Do not add a generic image/vector rendering abstraction.

- [ ] **Step 4: Run focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkRendererTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require focused PASS, locked restore PASS, Release build 0 warnings/0 errors, all tests PASS.

- [ ] **Step 5: Commit Task 1**

```bash
git add src/SGPdf.App/Features/Sign/SignatureInkStyle.cs src/SGPdf.App/Features/Sign/SignatureInkRenderer.cs tests/SGPdf.App.Tests/SignatureInkRendererTests.cs
git commit -m "feat(sign): render drawn signatures transparently"
```

---

### Task 2: Dialog-local stroke history + `Dibujar firma` WPF dialog

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureInkHistory.cs`
- Create: `src/SGPdf.App/SignatureDrawDialog.xaml`
- Create: `src/SGPdf.App/SignatureDrawDialog.xaml.cs`
- Test: `tests/SGPdf.App.Tests/SignatureInkHistoryTests.cs`
- Test: `tests/SGPdf.App.Tests/SignatureDrawDialogTests.cs`

**Interfaces:**

Consumes Task 1:

```csharp
SignatureInkStyle.CreateDrawingAttributes(SignatureInkColor, SignatureInkWidth)
SignatureInkRenderer.HasUsefulInk(StrokeCollection)
SignatureInkRenderer.Render(StrokeCollection, string?)
```

Produces:

```csharp
internal sealed class SignatureInkHistory
{
    internal bool CanUndo { get; }
    internal bool CanRedo { get; }
    internal void RecordUserStroke(Stroke stroke);
    internal Stroke? Undo(StrokeCollection current);
    internal Stroke? Redo(StrokeCollection current);
    internal void Clear(StrokeCollection current);
}

public partial class SignatureDrawDialog : Window
{
    internal SignatureDrawDialog();
    internal SignatureAsset? PreparedAsset { get; }
    internal static SignatureAsset? Draw(Window owner);
}
```

History semantics:

- `RecordUserStroke` is called only from the InkCanvas user `StrokeCollected` event;
- it appends the new user stroke to undo order and clears redo;
- `Undo` removes the most recently undoable stroke from `current` and pushes it to redo;
- `Redo` adds the most recently undone stroke back to `current` without calling `RecordUserStroke`, preserving the same `Stroke` / attributes;
- `Clear` empties the collection and both stacks; Clear is not undoable.

Dialog named controls:

```text
SignatureDrawInkCanvas
SignatureDrawBlackButton
SignatureDrawBlueButton
SignatureDrawWidthComboBox
SignatureDrawUndoButton
SignatureDrawRedoButton
SignatureDrawClearButton
SignatureDrawCancelButton
SignatureDrawApplyButton
SignatureDrawStatusText
```

Dialog defaults:

- `InkCanvas.EditingMode = InkCanvasEditingMode.Ink`;
- Black selected;
- Medium selected;
- Apply/Undo/Redo/Clear disabled on empty canvas;
- selector changes update only `InkCanvas.DefaultDrawingAttributes`, never existing strokes.

For testability, keep rendering behind one narrow internal delegate or virtual-free seam, e.g.:

```csharp
private Func<StrokeCollection, SignatureAsset> _renderInk =
    static strokes => SignatureInkRenderer.Render(strokes);
```

Do not introduce a dialog-service framework.

- [ ] **Step 1: Write RED pure history tests**

Tests:

```text
RecordUserStroke_EnablesUndoAndClearsRedo
Undo_RemovesOnlyNewestStrokeAndEnablesRedo
Redo_RestoresSameStrokeAndAttributesInOrder
RepeatedUndoRedo_PreservesStrokeOrder
NewUserStrokeAfterUndo_ClearsRedo
Clear_RemovesAllStrokesAndResetsHistory
```

- [ ] **Step 2: Write RED STA dialog tests**

Tests:

```text
Dialog_OpensEmptyWithBlackMediumDefaults
Dialog_ApplyDisabledUntilUsefulInkExists
StrokeCollected_UpdatesHistoryAndButtonStates
ChangingColorOrWidth_ChangesOnlyFutureDefaultDrawingAttributes
UndoRedoClear_OperateOnInkCanvasWithoutGlobalState
Cancel_LeavesPreparedAssetNull
Apply_ValidInkReturnsSignatureAsset
Apply_RenderFailure_PreservesStrokesHistoryAndDialogState
ClosingWindow_IsEquivalentToCancel
```

For test-generated strokes, add them through a narrow internal helper only if raising the real routed `StrokeCollected` event is impractical in headless STA tests. The helper must execute the same `RecordUserStroke` + button-state path used by the event; do not fork product behavior for tests.

- [ ] **Step 3: Run Task 2 focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkHistoryTests|FullyQualifiedName~SignatureDrawDialogTests"
```

Expected: fail because history/dialog contracts do not exist.

- [ ] **Step 4: Implement history + focused WPF dialog**

Keep the dialog conventional WPF. Mouse/touch/stylus collection is delegated to `InkCanvas`; no custom pointer capture or brush engine.

On Apply:

1. call renderer on current strokes;
2. set `PreparedAsset` only on success;
3. close with successful dialog result only when the window is actually visible;
4. on controlled render/validation errors, keep the dialog open, preserve strokes/history, set status text and return no asset.

- [ ] **Step 5: Run focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkHistoryTests|FullyQualifiedName~SignatureDrawDialogTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require 0 warnings/0 errors and all PASS.

- [ ] **Step 6: Commit Task 2**

```bash
git add src/SGPdf.App/Features/Sign/SignatureInkHistory.cs src/SGPdf.App/SignatureDrawDialog.xaml src/SGPdf.App/SignatureDrawDialog.xaml.cs tests/SGPdf.App.Tests/SignatureInkHistoryTests.cs tests/SGPdf.App.Tests/SignatureDrawDialogTests.cs
git commit -m "feat(sign): add drawn signature dialog"
```

---

### Task 3: Integrate `Dibujar firma...` into existing FIRMAR source flow

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Sign.cs`
- Create: `tests/SGPdf.App.Tests/MainWindowSignatureDrawTests.cs`

**Interfaces:**

Consumes Task 2:

```csharp
SignatureDrawDialog.Draw(Window owner) -> SignatureAsset?
```

Add one narrow seam beside `_loadSignaturePng` and `_prepareSignaturePhoto`:

```csharp
private Func<Window, SignatureAsset?> _drawSignature =
    static owner => SignatureDrawDialog.Draw(owner);
```

Add a named FIRMAR action:

```text
DrawSignatureButton
Content = "Dibujar firma..."
```

Add a focused method:

```csharp
private bool TryDrawSignature()
```

Behavior:

- `null` result = user cancel; return false without changing placement state;
- thrown exception = controlled status/message; return false without changing placement state;
- valid `SignatureAsset` = call existing `AddSignatureAsset(asset)` exactly once and report normal success status;
- do not create any new PDF/save path.

Update the informational copy in FIRMAR to mention all three supported sources: transparent PNG, photo/scan, or drawn signature.

- [ ] **Step 1: Write RED MainWindow integration tests**

Use the existing `MainWindowSignaturePhotoTests` reflection/STA pattern in a new focused test file.

Tests:

```text
DrawSignature_ActionExistsAndIsVisibleOnlyWithFirmarPanel
DrawCancel_KeepsPriorPlacementSelectionAndDirtyState
DrawFailure_KeepsPriorPlacementSelectionAndDirtyState
DrawApply_AddsExactlyOneNormalSelectedDirtyPlacement
DrawApply_UsesSameAddSignatureAssetFlowAsOtherSources
ExistingPngAndPhotoSourceActionsRemainAvailable
```

Do not open the real modal dialog in tests; replace `_drawSignature` through the narrow seam.

- [ ] **Step 2: Run focused tests and confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureDrawTests"
```

Expected: fail because `_drawSignature`, `DrawSignatureButton`, and `TryDrawSignature` do not exist.

- [ ] **Step 3: Implement minimum MainWindow integration**

Preserve the existing imperative F3 mode-panel ruling in `MainWindow.Sign.cs`; do not migrate the whole FIRMAR panel into another framework/XAML shell during this slice.

- [ ] **Step 4: Run focused GREEN + complete regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureDrawTests|FullyQualifiedName~MainWindowSignaturePhotoTests|FullyQualifiedName~MainWindowSignatureTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require build 0 warnings/0 errors and all tests PASS.

- [ ] **Step 5: Commit Task 3**

```bash
git add src/SGPdf.App/MainWindow.Sign.cs tests/SGPdf.App.Tests/MainWindowSignatureDrawTests.cs
git commit -m "feat(sign): integrate drawn signatures into FIRMAR"
```

---

### Task 4: Scope audit, documentation, exact-head CI, and automated closure

**Files:**
- Create: `docs/history/2026-10-08-F3.3.md`
- Create: `.planning/phases/04-f3-visual-signature/F3.3-PLAN.md`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `AGENTS.md`
- Open/update: stacked draft PR for `feat/f3-3-drawn-signature` with base `feat/f3-2-photo-preparation`

- [ ] **Step 1: Audit the complete branch against F3.2 final**

Require:

- changes limited to F3.3 spec/plan, feature-local ink renderer/history/dialog, focused `MainWindow.Sign.cs` integration, tests, and closure docs;
- no `.csproj` or lockfile change;
- no new runtime package;
- no PDFium writer/PInvoke/coordinate change;
- no F3.2 processor behavior change;
- no ZPL/Labelize change;
- no runtime HTTP/network/temp signature file;
- no F3.4 persistence/library behavior.

If any excluded surface changed, explain and return to design/review rather than normalizing the scope expansion.

- [ ] **Step 2: Run fresh functional verification before closure docs**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require locked restore PASS, Release build 0 warnings/0 errors, all tests PASS.

- [ ] **Step 3: Write closure docs**

Record separately:

- automated rendering/history/dialog/MainWindow PASS evidence;
- exact final head and CI IDs;
- manual mouse/touch/stylus hardware QA = `NOT RUN` unless actually performed;
- F3.4 local signature library remains next approved-but-unimplemented slice;
- branch/PR remains draft/unmerged.

- [ ] **Step 4: Open/update stacked draft PR**

Base: `feat/f3-2-photo-preparation`.

PR body must distinguish:

- automated PASS;
- manual device QA NOT RUN;
- no new dependency/network/PDF writer scope;
- no merge without explicit user approval.

- [ ] **Step 5: Freeze closure head and require push + PR CI on that exact SHA**

After the single closure-doc commit, make no more branch mutations. Require both push CI and pull-request CI to pass on that same head.

- [ ] **Step 6: Verify exact-head evidence before claiming completion**

Read CI logs and record:

- build warning/error count;
- passed/failed/skipped test counts;
- repository hygiene/locked restore success;
- PR still open, draft, unmerged.

Only then mark **F3.3 automated PASS**.

- [ ] **Step 7: Commit Task 4**

```bash
git add docs/history/2026-10-08-F3.3.md .planning/phases/04-f3-visual-signature/F3.3-PLAN.md .planning/STATE.md .planning/ROADMAP.md AGENTS.md
git commit -m "docs(sign): close F3.3 automated slice"
```

---

## Self-review outcome

- **Spec coverage:** renderer/transparency, 300-DPI-equivalent rasterization, fixed colors/widths, useful-ink validation, dialog-local Undo/Redo/Clear, Cancel/Apply safety, MainWindow convergence, privacy/offline boundaries and manual hardware QA all map to Tasks 1–4.
- **Type consistency:** `SignatureInkColor`, `SignatureInkWidth`, `SignatureInkStyle`, `SignatureInkRenderer`, `SignatureInkHistory`, `SignatureDrawDialog`, `_drawSignature` and `TryDrawSignature()` are introduced once and consumed with matching signatures.
- **Review Focus:** tiny taps, mixed attributes, history mutation, oversized bounds and renderer-version antialias variance each have an owning test task.
- **KISS/YAGNI:** no general command stack, vector format, pressure engine, pointer abstraction, new package, persistence or PDF changes.
- **Execution size:** four reviewable tasks; Tasks 1–3 each close a RED→GREEN product boundary, Task 4 only audits/closes. This is intentionally smaller than F3.2 execution context.

## Stop conditions

Stop and return to design if implementation demonstrates that any of these are necessary:

- new runtime drawing/image package;
- custom low-level stylus/pointer engine;
- PDFium/PDF writer changes for drawn signatures;
- temp image files for correctness;
- pressure-sensitive brush behavior required for acceptable MVP quality;
- application-wide Undo/Redo framework;
- F3.4 persistence to make F3.3 usable;
- cloud/network service.
