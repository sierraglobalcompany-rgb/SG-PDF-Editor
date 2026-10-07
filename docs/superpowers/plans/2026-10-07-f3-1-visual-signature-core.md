# F3.1 Visual Signature Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the first complete visual-signature loop: import a transparent PNG, place/move/resize/duplicate/delete it on the current PDF page, then save a validated copy with PDFium while preserving the original.

**Architecture:** Keep the active `PdfDocumentSession` read-only. Signature edits live in a small in-memory model using PDF page coordinates as authority; the WPF overlay is derived from the current render geometry. Saving uses a focused PDFium writer on a separately opened source document, writes to a destination-adjacent temporary file, closes native handles/releases the global gate, then reopens/renders the temporary PDF before replacing the destination.

**Tech Stack:** C# / .NET 10 / WPF / existing `bblanchon.PDFium.Win32` `156.0.8076`; no new NuGet/runtime dependency.

**Spec:** `docs/superpowers/specs/2026-10-07-f3-visual-signature-design.md`

## Global Constraints

- Windows x64, WPF, .NET 10.
- PDFium remains the only PDF writer/editor used by F3; PDFsharp stays limited to the already-approved label-PDF composition path.
- No new package, network call, cloud service, API key, telemetry, AI background removal, cryptographic-signing implementation, or generic editing framework.
- F3.1 accepts **transparent PNG only**. Photo cleanup is F3.2; drawing is F3.3; local library is F3.4.
- `Guardar como...` only. The source PDF must never be overwritten by F3.1.
- Pending F3.1 edits are limited to one current PDF page. Navigation/open/close/mode-exit must offer save/discard/cancel while dirty.
- PDF-space points are authoritative; WPF pixels/DIPs are derived display state only.
- Existing global `PdfiumRuntime.NativeGate` remains the single PDFium serialization guard.
- Never reopen/render the saved temporary PDF while still holding `NativeGate`.
- If insertion cannot preserve alpha with the pinned PDFium runtime, stop the slice and return to design review; never flatten/rasterize the whole page as a fallback.
- If cryptographic-signature preflight cannot be evaluated, do not claim the document is safe to modify; block save with a controlled message.
- No merge to `main` without explicit user approval.

## Review Focus

1. **Crop/rotation/zoom mapping:** a placement created at one zoom must map to the same PDF rectangle after zoom/refit, including a page whose visible bounds do not begin at PDF `(0,0)`; Task 1 pins the render transform and round-trip mapping.
2. **PNG memory/alpha safety:** corrupt, zero-size, oversized (>20,000,000 decoded pixels), or fully opaque white-background PNG must not replace the current valid signature state; Task 2 pins these inputs.
3. **Native alpha/orientation:** transparent pixels must reveal underlying PDF content and an asymmetric PNG must not be vertically mirrored after save/reopen; Task 3 proves both with rendered pixels.
4. **Transactional destination safety:** save failure or reopen-validation failure must leave an existing destination unchanged and remove the `.sgpdf.tmp` file; Task 3 pins both paths.
5. **Dirty-workspace loss:** page navigation, PDF/ZPL open, mode exit, and window close must never silently discard pending signatures; Task 4 pins the guard outcomes.

---

## File Structure

### New feature files

- `src/SGPdf.App/Features/Sign/SignatureAsset.cs` — immutable prepared BGRA/alpha signature pixels.
- `src/SGPdf.App/Features/Sign/SignaturePlacement.cs` — one placed signature in PDF-space points.
- `src/SGPdf.App/Features/Sign/SignatureEditState.cs` — current-page placement/selection/dirty operations.
- `src/SGPdf.App/Features/Sign/SignaturePngLoader.cs` — transparent-PNG decode/normalization/safety validation.
- `src/SGPdf.App/Features/Sign/SignatureCoordinateMapper.cs` — pure affine UI/device ↔ PDF rectangle mapping.
- `src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs` — PDFium insert/save/validate transaction.
- `src/SGPdf.App/MainWindow.Sign.cs` — FIRMAR-mode orchestration and overlay interaction only.

### Existing files modified

- `src/SGPdf.App/Pdf/PdfiumNative.cs` — minimal P/Invoke surface required by F3.1.
- `src/SGPdf.App/Pdf/PdfRenderedPage.cs` — carry the device→PDF affine transform captured during render.
- `src/SGPdf.App/Pdf/PdfDocumentSession.cs` — capture render transform and expose cryptographic signature count.
- `src/SGPdf.App/MainWindow.xaml` — LEER/FIRMAR mode bar, signature overlay canvas, signature property controls.
- `src/SGPdf.App/MainWindow.xaml.cs` — render-state handoff + dirty guard hooks for PDF navigation/open/close.
- `src/SGPdf.App/MainWindow.Labels.cs` — dirty guard before switching from PDF signature workspace to ZPL.

### Tests

- `tests/SGPdf.App.Tests/SignatureCoordinateMapperTests.cs`
- `tests/SGPdf.App.Tests/SignatureEditStateTests.cs`
- `tests/SGPdf.App.Tests/SignaturePngLoaderTests.cs`
- `tests/SGPdf.App.Tests/PdfVisualSignatureWriterTests.cs`
- `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs`

---

### Task 1: PDF coordinate authority + edit model

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureAsset.cs`
- Create: `src/SGPdf.App/Features/Sign/SignaturePlacement.cs`
- Create: `src/SGPdf.App/Features/Sign/SignatureEditState.cs`
- Create: `src/SGPdf.App/Features/Sign/SignatureCoordinateMapper.cs`
- Modify: `src/SGPdf.App/Pdf/PdfiumNative.cs`
- Modify: `src/SGPdf.App/Pdf/PdfRenderedPage.cs`
- Modify: `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- Test: `tests/SGPdf.App.Tests/SignatureCoordinateMapperTests.cs`
- Test: `tests/SGPdf.App.Tests/SignatureEditStateTests.cs`

**Interfaces:**
- Consumes: existing `PdfDocumentSession.RenderPage(...)`, which renders with `(startX,startY,rotate)=(0,0,0)` and the returned pixel width/height.
- Produces:
  - `internal readonly record struct PdfPageDeviceTransform(double OriginX, double OriginY, double XAxisX, double XAxisY, double YAxisX, double YAxisY, int DeviceWidth, int DeviceHeight)` in `PdfRenderedPage.cs` or a focused adjacent file.
  - `PdfRenderedPage.DeviceTransform`.
  - `public sealed record SignatureAsset(int PixelWidth, int PixelHeight, int Stride, byte[] BgraPixels, string? SourceName = null)` with `AspectRatio` derived from dimensions.
  - `public sealed record SignaturePlacement(Guid Id, int PageIndex, double Left, double Bottom, double Width, double Height, SignatureAsset Asset)`.
  - `internal static SignatureCoordinateMapper.DeviceRectToPdfRect(...)` and `PdfRectToDeviceRect(...)` using `PdfPageDeviceTransform`.
  - `internal sealed SignatureEditState` with `Placements`, `SelectedId`, `IsDirty`, `AddCentered(...)`, `Move(...)`, `ResizeProportional(...)`, `DuplicateSelected(...)`, `DeleteSelected()`, `Select(...)`, `MarkSaved()`, `DiscardAll()`.

- [ ] **Step 1: Write RED tests for render transform and coordinate round trips**

Add tests that assert:
- a synthetic transform mapping device `(0,0)`, `(W,0)`, `(0,H)` to arbitrary PDF points round-trips a rectangle within `0.01` point;
- mapping is unchanged when the same PDF rectangle is expressed against 96-DPI and 192-DPI device sizes;
- non-zero PDF origin/crop-like transform round-trips correctly;
- `PdfDocumentSession.RenderPage()` returns a transform whose three anchor points match direct `FPDF_DeviceToPage()` calls made with the same `size_x`, `size_y`, `rotate=0` as the render.

Run:
```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureCoordinateMapperTests"
```
Expected RED: missing transform/mapper/PDFium conversion symbols only.

- [ ] **Step 2: Add the minimal render-transform P/Invoke + capture**

Add to `PdfiumNative.cs`:
```csharp
internal static extern int FPDF_DeviceToPage(
    IntPtr page,
    int startX,
    int startY,
    int sizeX,
    int sizeY,
    int rotate,
    int deviceX,
    int deviceY,
    out double pageX,
    out double pageY);
```

Inside the existing `RenderPage()` native-gate/page lifetime, after pixel dimensions are known, convert the three device anchors `(0,0)`, `(pixelWidth,0)`, `(0,pixelHeight)` and attach them to the returned `PdfRenderedPage`. Fail the render if any conversion returns false; do not guess a fallback transform.

- [ ] **Step 3: Implement `SignatureCoordinateMapper` and verify GREEN**

Use the three anchors as a 2D affine basis. `DeviceRectToPdfRect` maps all four UI/device rectangle corners and returns axis-aligned PDF `left/bottom/width/height`; inverse mapping solves the same affine basis and returns device coordinates. Reject singular/non-finite transforms.

Run the focused mapper tests; expected PASS.

- [ ] **Step 4: Write RED tests for edit-state behavior**

Pin these behaviors:
- initial centered placement uses `min(144pt, 35% of visible page width)` then reduces proportionally if height would exceed `25%` of visible page height;
- aspect ratio always equals `SignatureAsset.AspectRatio` within tolerance;
- move clamps the entire placement within current PDF page bounds;
- proportional resize has a `12pt` minimum width and never leaves page bounds;
- duplicate uses a `12pt` right/down visual offset when space permits, clamps otherwise, receives a new `Guid`, and becomes selected;
- delete removes only selected placement;
- any add/move/resize/duplicate/delete sets `IsDirty=true`;
- `MarkSaved()` clears dirty without deleting placements; `DiscardAll()` clears placements/selection/dirty.

Run the focused state tests; expected RED because the models/state are missing.

- [ ] **Step 5: Implement models/state minimally and rerun focused tests**

Keep all geometry methods deterministic and UI-free. `SignatureAsset` must defensively validate positive dimensions, `stride >= width*4`, exact/adequate buffer length, and finite aspect ratio.

Expected: mapper + edit-state tests PASS.

- [ ] **Step 6: Run Task 1 regression suite and commit**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```
Expected: locked restore PASS, build 0 warnings/0 errors, full suite PASS.

Commit:
```bash
git add src/SGPdf.App/Pdf src/SGPdf.App/Features/Sign tests/SGPdf.App.Tests/SignatureCoordinateMapperTests.cs tests/SGPdf.App.Tests/SignatureEditStateTests.cs
git commit -m "feat(sign): add PDF-space signature edit model"
```

---

### Task 2: Transparent PNG import

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignaturePngLoader.cs`
- Test: `tests/SGPdf.App.Tests/SignaturePngLoaderTests.cs`

**Interfaces:**
- Consumes: `SignatureAsset` from Task 1.
- Produces: `internal static SignatureAsset SignaturePngLoader.Load(string path)`.

- [ ] **Step 1: Write RED PNG-loader tests**

Create synthetic PNG bytes in the test using WPF bitmap encoders. Assert:
- valid transparent PNG normalizes to `PixelFormats.Bgra32`, expected width/height/stride and preserves alpha values exactly for controlled pixels;
- PNG with opaque and transparent pixels is accepted;
- corrupt data throws controlled `InvalidDataException`/`NotSupportedException` and does not return an asset;
- zero-size decode is rejected;
- decoded pixel count above `20_000_000` is rejected before allocating the normalized BGRA buffer when dimensions are available;
- an image with **no transparent pixel at all** is rejected by F3.1 with an error explaining that this slice requires a transparent PNG rather than silently removing a white background.

Run:
```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignaturePngLoaderTests"
```
Expected RED: `SignaturePngLoader` missing.

- [ ] **Step 2: Implement loader with existing WPF imaging only**

Use `BitmapDecoder`/`FormatConvertedBitmap` with `BitmapCacheOption.OnLoad`, close the file/stream after decode, convert to `PixelFormats.Bgra32`, copy pixels to managed memory, then construct `SignatureAsset`. Do not keep the source file locked. Do not recolor/remove backgrounds in F3.1.

- [ ] **Step 3: Verify focused + full suite and commit**

Focused test must PASS, then run locked restore/build/full tests.

Commit:
```bash
git add src/SGPdf.App/Features/Sign/SignaturePngLoader.cs tests/SGPdf.App.Tests/SignaturePngLoaderTests.cs
git commit -m "feat(sign): import transparent PNG signatures"
```

---

### Task 3: PDFium visual-signature writer + alpha feasibility gate

**Files:**
- Create: `src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs`
- Modify: `src/SGPdf.App/Pdf/PdfiumNative.cs`
- Modify: `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- Test: `tests/SGPdf.App.Tests/PdfVisualSignatureWriterTests.cs`

**Interfaces:**
- Consumes: `SignaturePlacement` / `SignatureAsset` from Task 1.
- Produces:
  - `public int PdfDocumentSession.GetCryptographicSignatureCount()`; throws if PDFium returns `<0`.
  - `internal sealed class PdfVisualSignatureWriter`.
  - `public void SaveAsCopy(string sourcePath, string destinationPath, IReadOnlyList<SignaturePlacement> placements, CancellationToken cancellationToken = default)`.

**Native surface to add only as proven by tests:**
- `FPDF_GetSignatureCount`.
- `FPDFBitmap_CreateEx` with format constant `FPDFBitmap_BGRA = 4`.
- existing `FPDFBitmap_GetBuffer`, `FPDFBitmap_GetStride`, `FPDFBitmap_Destroy` reused.
- `FPDFPageObj_NewImageObj`.
- `FPDFImageObj_SetBitmap`.
- `FPDFImageObj_SetMatrix`.
- `FPDFPage_InsertObject`.
- `FPDFPageObj_Destroy` for not-yet-inserted failure cleanup.
- `FPDFPage_GenerateContent`.
- `FPDF_SaveAsCopy` plus the minimal `FPDF_FILEWRITE` callback structure/delegate.

- [ ] **Step 1: Write RED native-writer integration tests before UI exists**

Build a synthetic one-page PDF with known colored background content. Build an asymmetric `SignatureAsset` containing:
- opaque black region in one known corner;
- semi-transparent pixels;
- fully transparent region.

Place it at a known PDF rectangle and call `SaveAsCopy`.

After save:
- reopen via `PdfDocumentSession`;
- assert page count unchanged;
- render at 300 DPI;
- sample controlled regions to prove: opaque ink is present, underlying background is visible through transparent pixels, semi-transparent area blends rather than becoming opaque white, and the asymmetric corner proves vertical orientation is not mirrored;
- assert signature position is within a tolerance equivalent to `<=1.5pt`.

Also add RED tests for:
- source path == destination path → reject before write;
- placement page index out of range → reject;
- empty placement list → reject/no output;
- existing destination survives simulated write failure;
- existing destination survives simulated post-write validation failure;
- all `.*.sgpdf.tmp` files are removed on both failure paths;
- unsigned synthetic PDF returns cryptographic signature count `0`.

Use small internal delegates/seams only where needed to deterministically force write/validation failure; do not introduce an application-wide interface hierarchy.

Run focused writer tests. Expected RED: missing writer/native symbols.

- [ ] **Step 2: Implement the smallest PDFium image-insertion path**

Inside one `NativeGate` section:
1. open source separately with `FPDF_LoadDocument`;
2. validate page count and all placement bounds;
3. for each affected page, load it once;
4. for each placement create `FPDFBitmap_CreateEx(width,height,FPDFBitmap_BGRA,null,0)`, copy BGRA rows into PDFium-owned bitmap memory respecting destination stride, create image object, set bitmap, set matrix `(Width,0,0,Height,Left,Bottom)`, insert object, transfer object ownership to the page;
5. call `FPDFPage_GenerateContent` once per changed page;
6. save with `FPDF_SaveAsCopy` to a destination-adjacent unique `.{fileName}.{Guid:N}.sgpdf.tmp` via a synchronous file-write callback;
7. close pages/document/bitmaps and release `NativeGate` in `finally`.

If an image object has not been inserted yet when failure occurs, destroy it explicitly. Keep the bitmap alive through `FPDF_SaveAsCopy` in this first implementation, matching PDFium's own embedder-test lifetime pattern; destroy it only after save completes/fails.

- [ ] **Step 3: Implement validation outside `NativeGate` and transactional destination replace**

After the native write section has fully released the gate:
1. `PdfDocumentSession.Open(temp)`;
2. assert page count unchanged;
3. render each affected page once to ensure the saved document is readable;
4. close validation session;
5. if destination exists, replace it only now; otherwise move temp to destination;
6. on any exception/cancel, delete temp best-effort and preserve source/existing destination.

The writer itself does not clear the UI edit state; caller does that only after successful return.

- [ ] **Step 4: Implement cryptographic-signature count preflight**

Add `FPDF_GetSignatureCount` to the existing session under `NativeGate`. Return count `>=0`; throw controlled `InvalidOperationException` for `-1` so the UI cannot silently assume unsigned.

- [ ] **Step 5: Run the alpha/orientation feasibility gate**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfVisualSignatureWriterTests"
```

**Hard stop:** if alpha, orientation, or positional tests cannot pass with pinned PDFium `156.0.8076` without page rasterization/second writer engine/new package, stop implementation and return to design review. Do not continue to WPF UI.

- [ ] **Step 6: Full regression + commit**

Run locked restore, Release build, full tests. Expected 0 warnings/0 errors/all PASS.

Commit:
```bash
git add src/SGPdf.App/Pdf src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs tests/SGPdf.App.Tests/PdfVisualSignatureWriterTests.cs
git commit -m "feat(sign): save transparent visual signatures with PDFium"
```

---

### Task 4: FIRMAR mode, overlay interactions, dirty-state guard, and save UX

**Files:**
- Create: `src/SGPdf.App/MainWindow.Sign.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.xaml.cs`
- Modify: `src/SGPdf.App/MainWindow.Labels.cs`
- Test: `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs`

**Interfaces:**
- Consumes: `SignatureEditState`, `SignatureCoordinateMapper`, `SignaturePngLoader`, `PdfVisualSignatureWriter`, current `PdfRenderedPage.DeviceTransform`, current navigation/zoom state.
- Produces UI behaviors only; no new cross-feature framework.
- Test seams allowed in `MainWindow.Sign.cs`:
  - `Func<string, SignatureAsset> _loadSignaturePng` defaults to `SignaturePngLoader.Load`.
  - `Action<string,string,IReadOnlyList<SignaturePlacement>,CancellationToken> _saveVisualSignatures` defaults to `new PdfVisualSignatureWriter().SaveAsCopy`.
  - `Func<PdfDocumentSession,int> _getCryptographicSignatureCount` defaults to `session => session.GetCryptographicSignatureCount()`.
  - one focused decision delegate for save/discard/cancel guard may be used so STA tests do not open real message boxes.

- [ ] **Step 1: Write RED STA tests for mode/UI/state visibility**

Assert:
- with no PDF open, `FIRMAR` is disabled;
- valid PDF enables `LEER` + `FIRMAR` only; EDITAR/ORGANIZAR/COMENTAR are not scaffolded as active features;
- entering FIRMAR shows signature panel + overlay and hides label properties;
- returning to LEER with clean state hides selection chrome/overlay controls without mutating PDF;
- loading a valid PNG adds one centered placement, selects it, and marks dirty;
- invalid/opaque PNG leaves prior placements unchanged;
- zoom/refit rerender recreates overlay from the same PDF-space rectangle rather than changing placement coordinates.

Expected RED: missing UI names/handlers/state.

- [ ] **Step 2: Add minimal mode strip and signature panel**

In `MainWindow.xaml` add only functional `LEER` and `FIRMAR` controls; do not add active placeholder buttons for future modes. Add `SignatureOverlayCanvas` exactly over the displayed `PdfImage` rectangle (same page margin/alignment, dimensions updated from the current rendered bitmap) and a right-panel `SignaturePropertiesPanel` containing:
- `Cargar PNG transparente...`;
- `Duplicar`;
- `Eliminar`;
- `Guardar como...`;
- short status/help text.

F3.2/F3.3 buttons may be absent until those slices; do not ship dead controls.

- [ ] **Step 3: Implement overlay projection and selection rendering**

`MainWindow.Sign.cs` owns dynamic overlay visuals. For each placement:
- project PDF rectangle through current `PdfRenderedPage.DeviceTransform`;
- render asset as WPF `BitmapSource` with alpha;
- selected item gets UI-only border + resize handle;
- body drag commits `SignatureEditState.Move(...)`;
- one bottom-right handle commits `ResizeProportional(...)`;
- pointer interactions update the temporary visual continuously but commit canonical PDF-space state on mouse-up;
- Delete key and panel button call `DeleteSelected()`; duplicate button calls `DuplicateSelected()`.

Do not write selection border/handle to PDF.

- [ ] **Step 4: Write RED tests for dirty guard + signed-document warning**

Pin each action while dirty:
- page previous/next/go-to;
- `Abrir PDF...`;
- `Abrir etiquetas ZPL...`;
- exit FIRMAR to LEER;
- window close.

For each action test decision outcomes:
- **Cancelar** → action aborted, placements unchanged;
- **Descartar** → placements cleared, action proceeds;
- **Guardar como...** → action proceeds only after save callback succeeds; failure keeps current page/workspace dirty.

Signed-preflight tests:
- count `0` → normal save confirmation path;
- count `>0` → explicit warning that modifying/saving can invalidate existing cryptographic signatures; user can cancel;
- count lookup throws → save blocked and placements remain dirty.

Expected RED before guard integration.

- [ ] **Step 5: Implement one centralized guard method and hook existing actions**

Create one focused method, e.g.:
```csharp
private bool TryResolvePendingSignatureEdits(SignatureGuardReason reason)
```
(or async only if save implementation genuinely requires it).

It must be called before state-destructive transitions in the existing PDF navigation/open/ZPL-open/window-close/mode-exit handlers. Avoid duplicating message-box logic in each handler.

`Guardar como...`:
- standard `SaveFileDialog`, PDF filter;
- reject destination equal to source path;
- query cryptographic signature count first;
- show warning when count > 0;
- call writer with current placements;
- only after success call `MarkSaved()` and keep source PDF/session open unchanged;
- update status with saved-copy path; do not automatically replace active source in F3.1.

- [ ] **Step 6: Run focused WPF tests, then full suite**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```
Expected: focused PASS; build 0/0; full suite PASS.

- [ ] **Step 7: Commit Task 4**

```bash
git add src/SGPdf.App/MainWindow.xaml src/SGPdf.App/MainWindow.xaml.cs src/SGPdf.App/MainWindow.Labels.cs src/SGPdf.App/MainWindow.Sign.cs tests/SGPdf.App.Tests/MainWindowSignatureTests.cs
git commit -m "feat(sign): add visual signature placement workflow"
```

---

### Task 5: F3.1 audit, documentation, and exact-head verification

**Files:**
- Create: `docs/history/2026-10-07-F3.1.md`
- Create: `.planning/phases/04-f3-visual-signature/F3.1-PLAN.md` as a concise pointer/status companion if project phase convention requires it; do not duplicate the full superpowers plan.
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `AGENTS.md`
- Draft PR: base `feat/f2-6-validation-hardening`, head `feat/f3-visual-signature`.

**Interfaces:** no code API; consumes all prior task evidence.

- [ ] **Step 1: Audit diff against F2.6**

Require:
- no new package/lock mutation;
- no PDFsharp expansion for visual-signature writing;
- no runtime HTTP/network;
- no F3.2 photo processing, F3.3 InkCanvas, F3.4 persistence, cryptographic signing, generic image editor, or multipage pending-edit session;
- source PDF overwrite remains impossible from F3.1;
- only minimal PDFium APIs required by F3.1 were added.

- [ ] **Step 2: Fresh full verification**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```
Record exact test count and ensure 0 warnings/0 errors.

- [ ] **Step 3: Document automated vs manual acceptance honestly**

`STATE`/history must separate:
- F3.1 automated geometry/native-save/alpha/transaction/UI tests = PASS only if fresh CI proves it;
- manual Windows drag/resize visual feel = NOT RUN until executed;
- real handwritten PNG appearance/transparency = NOT RUN until executed;
- cryptographically signed real-world PDF warning behavior = NOT RUN unless a real signed fixture is tested;
- F3.2/F3.3/F3.4 = pending.

- [ ] **Step 4: Open/update stacked draft PR and run exact-head push + PR CI**

PR remains draft/unmerged. After closure docs land, require push and PR workflows to PASS on the same closure SHA; earlier functional CI is not enough.

- [ ] **Step 5: Record final SHA/CI IDs in PR and stop**

No merge/rebase/integration without explicit user approval. Next architecture gate after F3.1 is **F3.2 — Photo/scan → transparent signature** unless manual F3.1 evidence exposes a defect first.

---

## Execution Order / Stop Conditions

Execute Task 1 → 2 → 3 → 4 → 5. Stop and return to design review if any of these becomes necessary:

- page rasterization to preserve signature alpha;
- a second PDF writer/editor engine for visual signatures;
- a new runtime package;
- silent modification/overwrite of the source PDF;
- storing placement in WPF pixels instead of PDF coordinates;
- allowing unsaved signatures across multiple pages in F3.1;
- implementing F3.2/F3.3/F3.4 opportunistically inside this slice;
- creating/modifying cryptographic signatures rather than merely warning about existing ones.
