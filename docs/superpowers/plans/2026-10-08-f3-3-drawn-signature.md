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
- Input authority is WPF `StrokeCollection`; never rasterize the visible `InkCanvas` background/chrome into the signature.
- Output authority is the existing `SignatureAsset`; no second asset/placement/writer hierarchy.
- Fixed colors: Black `#000000`, Blue `#194196`.
- Fixed widths: Thin `2.0 DIP`, Medium `3.5 DIP`, Thick `5.0 DIP`; Medium default.
- Raster sampling scale is `300 / 96 = 3.125` relative to WPF DIPs.
- Reuse the shared **20,000,000 pixel** safety limit and validate output geometry before full allocation where practical.
- Useful drawing requires at least one stroke with >=2 stylus points and union bounds >=`4 DIP × 4 DIP`.
- Transparent padding: `clamp(max(8, maxStrokeWidth * 2), 8, 24)` DIP.
- Undo/Redo/Clear are dialog-local. Clear is not undoable and empties both history stacks.
- Selector changes affect only future strokes; existing strokes keep their original `DrawingAttributes`.
- Cancel/failure must preserve existing placements, selection, dirty state, active PDF and current page.
- No temp image file, clipboard requirement, telemetry, cloud/network, pressure engine, custom stylus framework, F3.4 persistence, or new runtime dependency.
- Do not modify `PdfVisualSignatureWriter`, PDFium P/Invoke, signature coordinate mapping, F3.2 processing, ZPL/Labelize, PDFsharp scope, `.csproj`, or lockfiles without returning to design first.

## Review Focus

1. Tiny taps / one-point strokes are rejected without clearing user ink.
2. Mixed black/blue and thin/medium/thick strokes preserve per-stroke attributes after later selector changes.
3. Undo→Redo must not masquerade as a new user stroke; a genuinely new stroke after Undo must clear Redo.
4. Oversized bounds must hit the 20M guard before full BGRA allocation and leave dialog ink intact.
5. Renderer tests verify geometry, alpha, color presence, crop/padding and relative width — not every antialiased pixel across Windows versions.

---

### Task 1: Transparent stroke renderer + fixed ink presets

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureInkStyle.cs`
- Create: `src/SGPdf.App/Features/Sign/SignatureInkRenderer.cs`
- Test: `tests/SGPdf.App.Tests/SignatureInkRendererTests.cs`

**Interfaces:**

```csharp
internal enum SignatureInkColor { Black, Blue }
internal enum SignatureInkWidth { Thin, Medium, Thick }

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

Renderer rules:

- snapshot/clone strokes before drawing;
- union stroke bounds + padding from maximum stroke width;
- `pixelWidth/Height = ceil(paddedDip * 3.125)`, minimum 1;
- call `SignatureImageLimits.ValidatePixelCount(...)` before final allocation;
- draw only strokes into a transparent `DrawingVisual`/`DrawingContext` using WPF `StrokeCollection.Draw(DrawingContext)` and an explicit bounds→raster transform;
- normalize WPF premultiplied output to `PixelFormats.Bgra32` before creating `SignatureAsset`;
- never draw a canvas/background rectangle.

- [ ] **Step 1: Write RED tests**

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

Use synthetic `StylusPointCollection` / `Stroke` objects only. For antialiasing, assert transparent background, expected ink colors/tolerances, coverage and geometry rather than full bitmap byte equality.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkRendererTests"
```

Expected: failure only because renderer/style contracts do not exist.

- [ ] **Step 3: Implement minimum contracts with WPF built-ins only.**

- [ ] **Step 4: Focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkRendererTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require focused PASS, locked restore PASS, build 0 warnings/0 errors, all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/Features/Sign/SignatureInkStyle.cs src/SGPdf.App/Features/Sign/SignatureInkRenderer.cs tests/SGPdf.App.Tests/SignatureInkRendererTests.cs
git commit -m "feat(sign): render drawn signatures transparently"
```

---

### Task 2: Dialog-local stroke history + `Dibujar firma` dialog

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureInkHistory.cs`
- Create: `src/SGPdf.App/SignatureDrawDialog.xaml`
- Create: `src/SGPdf.App/SignatureDrawDialog.xaml.cs`
- Test: `tests/SGPdf.App.Tests/SignatureInkHistoryTests.cs`
- Test: `tests/SGPdf.App.Tests/SignatureDrawDialogTests.cs`

**Interfaces:**

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

History contract:

- call `RecordUserStroke` only from the user `StrokeCollected` path;
- new user stroke clears Redo;
- Undo removes newest undoable stroke and pushes it to Redo;
- Redo restores the same stroke/attributes without calling `RecordUserStroke`;
- Clear empties strokes plus both stacks and is not undoable.

Named controls:

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

Defaults: `Ink` mode, Black + Medium, Apply/Undo/Redo/Clear disabled while empty. Selector changes update only `InkCanvas.DefaultDrawingAttributes`.

Narrow test seam:

```csharp
private Func<StrokeCollection, SignatureAsset> _renderInk =
    static strokes => SignatureInkRenderer.Render(strokes);
```

No dialog-service framework.

- [ ] **Step 1: Write RED history tests**

```text
RecordUserStroke_EnablesUndoAndClearsRedo
Undo_RemovesOnlyNewestStrokeAndEnablesRedo
Redo_RestoresSameStrokeAndAttributesInOrder
RepeatedUndoRedo_PreservesStrokeOrder
NewUserStrokeAfterUndo_ClearsRedo
Clear_RemovesAllStrokesAndResetsHistory
```

- [ ] **Step 2: Write RED STA dialog tests**

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

If raising real `StrokeCollected` is impractical in headless tests, use one internal helper that executes the exact same record/update path; do not fork product behavior.

- [ ] **Step 3: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkHistoryTests|FullyQualifiedName~SignatureDrawDialogTests"
```

- [ ] **Step 4: Implement history + focused WPF dialog.**

Mouse/touch/stylus collection stays delegated to `InkCanvas`; no custom pointer engine. On render failure keep dialog open and preserve strokes/history. Set `DialogResult` only when the window is actually visible so STA tests remain noninteractive.

- [ ] **Step 5: Focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureInkHistoryTests|FullyQualifiedName~SignatureDrawDialogTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require build 0/0 and all tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/SGPdf.App/Features/Sign/SignatureInkHistory.cs src/SGPdf.App/SignatureDrawDialog.xaml src/SGPdf.App/SignatureDrawDialog.xaml.cs tests/SGPdf.App.Tests/SignatureInkHistoryTests.cs tests/SGPdf.App.Tests/SignatureDrawDialogTests.cs
git commit -m "feat(sign): add drawn signature dialog"
```

---

### Task 3: Integrate `Dibujar firma...` into FIRMAR

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Sign.cs`
- Create: `tests/SGPdf.App.Tests/MainWindowSignatureDrawTests.cs`

**Interfaces:**

```csharp
private Func<Window, SignatureAsset?> _drawSignature =
    static owner => SignatureDrawDialog.Draw(owner);

private bool TryDrawSignature();
```

Named action:

```text
DrawSignatureButton
Content = "Dibujar firma..."
```

Behavior:

- null = cancel, no state change;
- exception = controlled status/message, no state change;
- valid asset = call existing `AddSignatureAsset(asset)` exactly once;
- no new PDF/save path;
- informational FIRMAR copy mentions PNG, photo/scan and drawn signature.

- [ ] **Step 1: Write RED STA MainWindow tests using the existing F3.2 reflection pattern**

```text
DrawSignature_ActionExistsAndIsVisibleOnlyWithFirmarPanel
DrawCancel_KeepsPriorPlacementSelectionAndDirtyState
DrawFailure_KeepsPriorPlacementSelectionAndDirtyState
DrawApply_AddsExactlyOneNormalSelectedDirtyPlacement
DrawApply_UsesSameAddSignatureAssetFlowAsOtherSources
ExistingPngAndPhotoSourceActionsRemainAvailable
```

Replace `_drawSignature` in tests; never open the real modal dialog.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureDrawTests"
```

Expected: missing seam/button/method only.

- [ ] **Step 3: Implement minimum integration in `MainWindow.Sign.cs`.**

Preserve the existing imperative F3 panel ruling; do not migrate the whole shell/XAML in this slice.

- [ ] **Step 4: Focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureDrawTests|FullyQualifiedName~MainWindowSignaturePhotoTests|FullyQualifiedName~MainWindowSignatureTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require build 0/0 and all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/MainWindow.Sign.cs tests/SGPdf.App.Tests/MainWindowSignatureDrawTests.cs
git commit -m "feat(sign): integrate drawn signatures into FIRMAR"
```

---

### Task 4: Audit, documentation, exact-head CI, automated closure

**Files:**
- Create: `docs/history/2026-10-08-F3.3.md`
- Create: `.planning/phases/04-f3-visual-signature/F3.3-PLAN.md`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `AGENTS.md`
- Open/update: stacked draft PR `feat/f3-3-drawn-signature` → base `feat/f3-2-photo-preparation`

- [ ] **Step 1: Audit whole branch against F3.2 final**

Require changes only in F3.3 spec/plan, feature-local ink renderer/history/dialog, focused `MainWindow.Sign.cs`, tests and closure docs. Require no `.csproj`/lockfile/new package, PDFium writer/PInvoke/coordinates, F3.2 processor, ZPL/Labelize, runtime network/temp signature file or F3.4 persistence changes.

- [ ] **Step 2: Run fresh functional verification**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require locked restore PASS, build 0 warnings/0 errors and all tests PASS.

- [ ] **Step 3: Write closure docs and PR body**

Record automated evidence separately from manual device QA. Mouse/touch/stylus hardware QA remains `NOT RUN` unless actually performed. F3.4 remains deferred. PR remains draft/unmerged.

- [ ] **Step 4: Create exactly one closure-doc commit**

```bash
git add docs/history/2026-10-08-F3.3.md .planning/phases/04-f3-visual-signature/F3.3-PLAN.md .planning/STATE.md .planning/ROADMAP.md AGENTS.md
git commit -m "docs(sign): close F3.3 automated slice"
```

This commit becomes the **candidate final head**. From this point, make no more branch mutations.

- [ ] **Step 5: Open/update stacked draft PR if not already open**

Base `feat/f3-2-photo-preparation`; body distinguishes automated PASS, hardware QA NOT RUN, no dependency/network/PDF-writer scope, and no merge without explicit user approval. Updating PR metadata/comments does not change the branch SHA.

- [ ] **Step 6: Require push CI + PR CI on the same closure SHA**

Both workflows must run against the exact candidate final head from Step 4.

- [ ] **Step 7: Read exact-head logs and verify before completion claim**

Record:

- build warning/error count;
- passed/failed/skipped tests;
- repository hygiene + locked restore success;
- PR open, draft, unmerged;
- exact head SHA and both CI IDs.

Only then mark **F3.3 automated PASS**. Do not make another docs commit merely to record CI IDs; place final CI IDs in the PR body/comment and next-slice state update so the verified SHA remains exact.

---

## Self-review outcome

- **Spec coverage:** renderer/transparency, 300-DPI-equivalent sampling, colors/widths, useful-ink validation, local history, Apply/Cancel safety, MainWindow convergence, offline/privacy and manual hardware QA all map to Tasks 1–4.
- **Type consistency:** `SignatureInkColor`, `SignatureInkWidth`, `SignatureInkStyle`, `SignatureInkRenderer`, `SignatureInkHistory`, `SignatureDrawDialog`, `_drawSignature`, `TryDrawSignature()` are introduced once and consumed consistently.
- **Review Focus:** tiny taps, mixed attributes, history mutation, oversized bounds and antialias variance all have tests.
- **KISS/YAGNI:** no generic command system, vector format, pressure engine, pointer abstraction, new package, persistence or PDF changes.
- **Closure ordering:** docs commit precedes exact-head CI; no post-CI branch mutation can invalidate evidence.
- **Execution size:** four reviewable tasks. Tasks 1–3 each close one RED→GREEN boundary; Task 4 audits/closes.

## Stop conditions

Return to design if implementation requires a new runtime drawing/image package, custom low-level stylus engine, PDFium/PDF writer changes, temp image files, pressure-sensitive brush engine, application-wide Undo/Redo, F3.4 persistence, or cloud/network service.
