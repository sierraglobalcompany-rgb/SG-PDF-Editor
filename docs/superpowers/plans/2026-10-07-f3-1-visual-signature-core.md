# F3.1 Visual Signature Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the first complete visual-signature loop: import a transparent PNG, place/move/resize/duplicate/delete it on the current PDF page, then save a validated copy with PDFium while preserving the original.

**Architecture:** Keep the active `PdfDocumentSession` read-only. Signature edits live in a small in-memory model using PDF page coordinates as authority; the WPF overlay is derived from an affine device→PDF transform captured during the same PDFium render that produced the visible bitmap. Saving uses a focused PDFium writer on a separately opened source document, writes to a destination-adjacent temporary file, closes native handles/releases the global gate, then reopens/renders the temporary PDF before replacing the destination.

**Tech Stack:** C# / .NET 10 / WPF / existing `bblanchon.PDFium.Win32` `156.0.8076`; no new NuGet/runtime dependency.

**Spec:** `docs/superpowers/specs/2026-10-07-f3-visual-signature-design.md`

## Global Constraints

- Windows x64, WPF, .NET 10.
- PDFium remains the only PDF writer/editor used by F3; PDFsharp stays limited to the already-approved label-PDF composition path.
- No new package, network call, cloud service, API key, telemetry, AI background removal, cryptographic-signing implementation, or generic editing framework.
- F3.1 accepts **transparent PNG only**. Photo cleanup is F3.2; drawing is F3.3; local library is F3.4. Those later sub-slices get separate plans.
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
2. **PNG memory/alpha safety:** corrupt, zero-size, oversized (>20,000,000 decoded pixels), or fully opaque PNG must not replace the current valid signature state; Task 2 pins these inputs.
3. **Native alpha/orientation:** transparent pixels must reveal underlying PDF content and an asymmetric PNG must not be vertically mirrored after save/reopen; Task 3 proves both with rendered pixels.
4. **Transactional destination safety:** save failure or reopen-validation failure must leave an existing destination unchanged and remove the `.sgpdf.tmp` file; Task 3 pins both paths.
5. **Dirty-workspace loss:** page navigation, PDF/ZPL open, mode exit, and window close must never silently discard pending signatures; Task 4 pins the guard outcomes, including cancellable `Window.Closing` rather than the too-late `Closed` event.

---

## File Structure

### New feature files

- `src/SGPdf.App/Features/Sign/SignatureAsset.cs` — immutable prepared BGRA/alpha signature pixels.
- `src/SGPdf.App/Features/Sign/SignaturePlacement.cs` — small PDF/device geometry records + one placed signature.
- `src/SGPdf.App/Features/Sign/SignatureEditState.cs` — current-page placement/selection/dirty operations.
- `src/SGPdf.App/Features/Sign/SignaturePngLoader.cs` — transparent-PNG decode/normalization/safety validation.
- `src/SGPdf.App/Features/Sign/SignatureCoordinateMapper.cs` — pure affine UI/device ↔ PDF rectangle mapping.
- `src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs` — PDFium insert/save/validate transaction.
- `src/SGPdf.App/Pdf/PdfPageDeviceTransform.cs` — immutable affine anchors captured from PDFium render coordinates.
- `src/SGPdf.App/MainWindow.Sign.cs` — FIRMAR-mode orchestration and overlay interaction only.

### Existing files modified

- `src/SGPdf.App/Pdf/PdfiumNative.cs` — minimal P/Invoke surface required by F3.1.
- `src/SGPdf.App/Pdf/PdfRenderedPage.cs` — optional `DeviceTransform` property; old constructor call sites remain source-compatible.
- `src/SGPdf.App/Pdf/PdfDocumentSession.cs` — capture render transform and expose cryptographic signature count.
- `src/SGPdf.App/MainWindow.xaml` — LEER/FIRMAR mode bar, signature overlay canvas, signature property controls, cancellable `Closing` hook.
- `src/SGPdf.App/MainWindow.xaml.cs` — render-state handoff + dirty guard hooks for PDF navigation/open/window close.
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
- Create: `src/SGPdf.App/Pdf/PdfPageDeviceTransform.cs`
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
- Consumes: existing `PdfDocumentSession.RenderPage(...)`, which renders with `(startX,startY,rotate)=(0,0,0)` and returned pixel width/height.
- Produces:
```csharp
internal readonly record struct PdfPageDeviceTransform(
    double OriginX, double OriginY,
    double XAxisX, double XAxisY,
    double YAxisX, double YAxisY,
    int DeviceWidth, int DeviceHeight);

internal readonly record struct PdfRect(double Left, double Bottom, double Width, double Height);
internal readonly record struct SignatureDeviceRect(double X, double Y, double Width, double Height);

public sealed class SignatureAsset
{
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public int Stride { get; }
    public ReadOnlyMemory<byte> BgraPixels { get; }
    public string? SourceName { get; }
    public double AspectRatio { get; }
}

public sealed record SignaturePlacement(
    Guid Id,
    int PageIndex,
    PdfRect Bounds,
    SignatureAsset Asset);

internal static class SignatureCoordinateMapper
{
    internal static PdfRect DeviceRectToPdfRect(SignatureDeviceRect rect, PdfPageDeviceTransform transform);
    internal static SignatureDeviceRect PdfRectToDeviceRect(PdfRect rect, PdfPageDeviceTransform transform);
    internal static PdfRect GetVisiblePdfBounds(PdfPageDeviceTransform transform);
}

internal sealed class SignatureEditState
{
    internal SignatureEditState(int pageIndex, PdfRect pageBounds);
    internal IReadOnlyList<SignaturePlacement> Placements { get; }
    internal Guid? SelectedId { get; }
    internal bool IsDirty { get; }
    internal SignaturePlacement AddCentered(SignatureAsset asset);
    internal bool Select(Guid id);
    internal SignaturePlacement SetSelectedBounds(PdfRect proposedBounds);
    internal SignaturePlacement? DuplicateSelected();
    internal bool DeleteSelected();
    internal void MarkSaved();
    internal void DiscardAll();
}
```

`SignatureAsset` copies caller pixel memory on construction; callers cannot mutate stored bytes through a public `byte[]` reference.

`PdfRenderedPage` becomes source-compatible with existing tests/helpers by adding an optional final constructor parameter:
```csharp
PdfPageDeviceTransform? DeviceTransform = null
```
Production `PdfDocumentSession.RenderPage()` always supplies a non-null transform. F3.1 refuses to edit a render without one.

- [ ] **Step 1: Write RED tests for render transform and coordinate round trips**

Assert:
- an arbitrary affine transform round-trips a rectangle within `0.01pt`;
- same PDF rect maps consistently for device sizes equivalent to 96-DPI and 192-DPI renders;
- non-zero PDF origin/crop-like transform round-trips;
- `GetVisiblePdfBounds()` encloses all four mapped device corners;
- real `PdfDocumentSession.RenderPage()` transform anchor values match direct `FPDF_DeviceToPage()` calls made with the same `size_x`, `size_y`, `rotate=0` as the render.

Run:
```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureCoordinateMapperTests"
```
Expected RED: only missing transform/mapper/PDFium conversion symbols.

- [ ] **Step 2: Add minimal `FPDF_DeviceToPage` + capture three affine anchors**

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

Inside the existing `RenderPage()` page/native-gate lifetime, after pixel dimensions are known, convert `(0,0)`, `(pixelWidth,0)`, `(0,pixelHeight)` and attach the resulting basis to `PdfRenderedPage`. Fail the render if any conversion returns false; do not invent a fallback transform.

- [ ] **Step 3: Implement mapper and verify GREEN**

Use the three anchors as a 2D affine basis. Map all four rectangle corners, derive the axis-aligned PDF bounding rectangle, and implement the inverse by solving the 2×2 basis matrix. Reject non-finite/singular transforms.

Run focused mapper tests; expected PASS.

- [ ] **Step 4: Write RED tests for edit-state behavior**

Pin:
- initial centered placement width = `min(144pt, 35% of visible page width)`; if resulting height exceeds `25%` of visible page height, reduce both dimensions proportionally;
- asset aspect ratio is preserved within `0.001`;
- `SetSelectedBounds()` clamps entire placement to `pageBounds`, enforces asset aspect ratio from requested width, and enforces minimum width `12pt`;
- duplicate applies a `12pt` diagonal PDF-space offset where possible, clamps otherwise, gets a new `Guid`, and becomes selected;
- delete only removes selected placement;
- add/bounds-change/duplicate/delete marks dirty;
- `MarkSaved()` clears dirty without deleting placements;
- `DiscardAll()` clears placements/selection/dirty.

Expected RED: missing models/state.

- [ ] **Step 5: Implement immutable models/state minimally**

`SignatureAsset` validates positive dimensions, `stride >= width*4`, sufficient pixel length and finite aspect ratio, copies the supplied bytes once, and exposes read-only memory. Keep state deterministic and UI-free.

- [ ] **Step 6: Task 1 regression verification + commit**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```
Expected locked restore PASS, build 0 warnings/0 errors, full suite PASS.

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
- Produces:
```csharp
internal static class SignaturePngLoader
{
    internal const long MaxDecodedPixels = 20_000_000;
    internal static SignatureAsset Load(string path);
}
```

- [ ] **Step 1: Write RED PNG-loader tests**

Use WPF bitmap encoders to create controlled inputs. Assert:
- transparent PNG normalizes to BGRA32 and preserves controlled alpha values;
- mixed opaque/transparent pixels accepted;
- corrupt/non-PNG data rejected cleanly;
- decoded dimensions invalid/zero rejected;
- `PixelWidth * PixelHeight > 20_000_000` rejected before allocating normalized BGRA storage when dimensions are known;
- image with no pixel having alpha `<255` rejected with a message that F3.1 requires a transparent PNG rather than silently removing its background;
- source file is not locked after `Load()` returns or throws.

Run:
```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignaturePngLoaderTests"
```
Expected RED: loader missing.

- [ ] **Step 2: Implement loader with WPF imaging only**

Use `BitmapDecoder`/`FormatConvertedBitmap`, `BitmapCacheOption.OnLoad`, `PixelFormats.Bgra32`, managed copy, then `SignatureAsset`. No recolor/background removal in F3.1.

- [ ] **Step 3: Focused + full verification and commit**

Run focused tests, then locked restore/build/full tests.

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
- Consumes: `SignaturePlacement` / `SignatureAsset`.
- Produces:
```csharp
public int PdfDocumentSession.GetCryptographicSignatureCount();

internal sealed class PdfVisualSignatureWriter
{
    internal void SaveAsCopy(
        string sourcePath,
        string destinationPath,
        IReadOnlyList<SignaturePlacement> placements,
        CancellationToken cancellationToken = default);
}
```

**Minimal native surface:** `FPDF_GetSignatureCount`, `FPDFBitmap_CreateEx` (`FPDFBitmap_BGRA = 4`), existing bitmap buffer/stride/destroy, `FPDFPageObj_NewImageObj`, `FPDFImageObj_SetBitmap`, `FPDFImageObj_SetMatrix`, `FPDFPage_InsertObject`, `FPDFPageObj_Destroy`, `FPDFPage_GenerateContent`, `FPDF_SaveAsCopy` and a minimal `FPDF_FILEWRITE` callback. No wrapper library.

- [ ] **Step 1: Write RED native-writer integration tests before UI exists**

A synthetic one-page fixture may use the already-present PDFsharp test helper to create known background content; **the writer under test must use PDFium only**.

Build an asymmetric `SignatureAsset` with opaque black, semi-transparent, and fully transparent regions. Place at a known `PdfRect`.

After `SaveAsCopy`:
- reopen using `PdfDocumentSession`;
- same page count;
- render at 300 DPI;
- sample controlled regions proving opaque ink, underlying content through transparent pixels, semi-transparent blending, no white box, and no vertical mirror;
- placement error `<=1.5pt`.

Also RED-test:
- source == destination rejected before write;
- page index out of range rejected;
- empty placements rejected/no output;
- non-finite/out-of-bounds placement rejected;
- existing destination survives forced native-write failure;
- existing destination survives forced post-write validation failure;
- temp `.*.sgpdf.tmp` residue removed on failure;
- unsigned synthetic PDF returns signature count `0`.

Use only small internal delegate seams needed to force failure; no application-wide interface hierarchy.

- [ ] **Step 2: Implement the smallest PDFium insert/save path**

Within one `NativeGate` section:
1. open source separately with `FPDF_LoadDocument`;
2. validate page count/placements;
3. load each affected page once;
4. allocate `FPDFBitmap_CreateEx(width,height,FPDFBitmap_BGRA,null,0)` and copy BGRA rows respecting destination stride;
5. create image object, call `FPDFImageObj_SetBitmap(IntPtr.Zero,0,...)`, then matrix `(Width,0,0,Height,Left,Bottom)`, then insert object;
6. ownership transfers to page after insert; destroy only not-yet-inserted objects on failure;
7. call `FPDFPage_GenerateContent` once per changed page;
8. save through `FPDF_SaveAsCopy` to destination-adjacent `.{fileName}.{Guid:N}.sgpdf.tmp`;
9. keep the native bitmaps alive through `FPDF_SaveAsCopy`, matching PDFium embedder-test lifetime, then destroy them;
10. close native page/document handles and release `NativeGate` in `finally`.

Check cancellation between native operations; do not claim mid-call cancellation of synchronous PDFium APIs.

- [ ] **Step 3: Validate outside `NativeGate`, then replace destination**

Only after all native handles are closed/gate released:
1. reopen temp with `PdfDocumentSession`;
2. assert same page count;
3. render each affected page;
4. close validation session;
5. if destination exists use replacement only now; if not, move temp into place;
6. any exception/cancel deletes temp best-effort and preserves source/existing destination.

Writer does not mutate/clear UI edit state.

- [ ] **Step 4: Implement cryptographic-signature count preflight**

`GetCryptographicSignatureCount()` runs `FPDF_GetSignatureCount` under `NativeGate`, returns `>=0`, throws controlled `InvalidOperationException` for `<0` or missing/failed export. UI treats that exception as save-blocking.

- [ ] **Step 5: Run alpha/orientation feasibility gate**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfVisualSignatureWriterTests"
```

**Hard stop:** if alpha/orientation/position cannot pass with pinned PDFium `156.0.8076` without page rasterization, another writer engine, or a new package, return to design review before any WPF signature UI is implemented.

- [ ] **Step 6: Full regression + commit**

Run locked restore, Release build, full tests; require 0 warnings/0 errors/all PASS.

Commit:
```bash
git add src/SGPdf.App/Pdf src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs tests/SGPdf.App.Tests/PdfVisualSignatureWriterTests.cs
git commit -m "feat(sign): save transparent visual signatures with PDFium"
```

---

### Task 4: FIRMAR mode, overlay interaction, dirty guard, save UX

**Files:**
- Create: `src/SGPdf.App/MainWindow.Sign.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.xaml.cs`
- Modify: `src/SGPdf.App/MainWindow.Labels.cs`
- Test: `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs`

**Interfaces:**
- Consumes prior tasks + current `PdfRenderedPage.DeviceTransform`.
- Test seams in `MainWindow.Sign.cs` only:
```csharp
private Func<string, SignatureAsset> _loadSignaturePng = SignaturePngLoader.Load;
private Action<string, string, IReadOnlyList<SignaturePlacement>, CancellationToken> _saveVisualSignatures =
    static (source, destination, placements, token) =>
        new PdfVisualSignatureWriter().SaveAsCopy(source, destination, placements, token);
private Func<PdfDocumentSession, int> _getCryptographicSignatureCount =
    static session => session.GetCryptographicSignatureCount();
```
A single focused decision seam for `Guardar como / Descartar / Cancelar` may be added for STA tests; do not create a dialog-service framework.

- [ ] **Step 1: Write RED STA tests for mode/UI/state**

Assert:
- no PDF → FIRMAR disabled;
- PDF open → LEER/FIRMAR functional; future modes not activated/scaffolded;
- enter FIRMAR → signature panel/overlay visible, label panel hidden;
- return LEER while clean → signature UI hidden, PDF unchanged;
- valid PNG → centered selected placement + dirty;
- invalid/opaque PNG → prior placements unchanged;
- zoom/refit rerender → same canonical `PdfRect`, overlay reprojected through new transform.

- [ ] **Step 2: Add minimal mode strip/panel/overlay**

`MainWindow.xaml`:
- functional `LEER` + `FIRMAR` only;
- `SignatureOverlayCanvas` exactly over displayed `PdfImage` page rectangle, not the whole `ScrollViewer`/margin;
- right panel with `Cargar PNG transparente...`, `Duplicar`, `Eliminar`, `Guardar como...`, help/status;
- add `Closing="Window_Closing"` while retaining existing `Closed="Window_Closed"` for final resource cleanup.

No dead F3.2/F3.3/F3.4 controls yet.

- [ ] **Step 3: Implement overlay projection/selection interaction**

`MainWindow.Sign.cs` creates visuals dynamically:
- map placement `PdfRect` → device rect via current transform;
- display alpha bitmap;
- selected outline + one bottom-right resize handle UI-only;
- body drag updates temporary device visual, then maps final rect and commits `SetSelectedBounds()` on mouse-up;
- resize handle preserves aspect ratio while dragging and commits on mouse-up;
- Delete key/panel button delete selected;
- duplicate button duplicates selected.

F3.1 never writes chrome/handles to PDF.

- [ ] **Step 4: Write RED tests for dirty guard + signed warning**

While dirty, pin these transitions:
- previous/next/go-to page;
- Open PDF;
- Open ZPL;
- FIRMAR → LEER;
- window `Closing` event.

For each:
- Cancel → action aborted, state unchanged; `CancelEventArgs.Cancel=true` for window close;
- Discard → state cleared, action proceeds;
- Save → action proceeds only if save callback succeeds; failure keeps edits dirty and action aborted.

Signed preflight:
- count 0 → normal save;
- count >0 → explicit warning that modifying/saving can invalidate existing cryptographic signatures; cancel leaves dirty state;
- count lookup throws → save blocked, dirty state remains.

- [ ] **Step 5: Implement one centralized dirty guard and hook destructive transitions before dialogs/state swaps**

Use one focused method such as:
```csharp
private bool TryResolvePendingSignatureEdits(SignatureGuardReason reason);
```

Call it **before** showing a new PDF/ZPL open dialog, before page navigation, before leaving FIRMAR, and from `Window_Closing`. `Window_Closed` remains cleanup-only and must never be used for a cancellable decision.

Save behavior:
- standard `SaveFileDialog` with PDF filter + overwrite confirmation;
- destination equal to source blocked;
- cryptographic count queried first; `>0` warns, failure blocks;
- set busy/status, call writer, restore busy in `finally`;
- `MarkSaved()` only after writer success;
- active source/session remains the original PDF after saving; do not auto-open the copy in F3.1.

- [ ] **Step 6: Focused WPF + full verification**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```
Expected focused PASS, build 0/0, all tests PASS.

- [ ] **Step 7: Commit Task 4**

```bash
git add src/SGPdf.App/MainWindow.xaml src/SGPdf.App/MainWindow.xaml.cs src/SGPdf.App/MainWindow.Labels.cs src/SGPdf.App/MainWindow.Sign.cs tests/SGPdf.App.Tests/MainWindowSignatureTests.cs
git commit -m "feat(sign): add visual signature placement workflow"
```

---

### Task 5: F3.1 audit, docs, and exact-head verification

**Files:**
- Create: `docs/history/2026-10-07-F3.1.md`
- Create/update concise phase companion under `.planning/phases/04-f3-visual-signature/` if project convention requires it; do not duplicate this full plan.
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `AGENTS.md`
- Draft PR: base `feat/f2-6-validation-hardening`, head `feat/f3-visual-signature`.

- [ ] **Step 1: Audit branch scope against F2.6**

Require no new package/lock mutation; no PDFsharp expansion for signing; no runtime HTTP; no F3.2 photo cleanup, F3.3 InkCanvas, F3.4 persistence, cryptographic signing, generic image editor, or multipage pending-edit session; source overwrite impossible; only minimal PDFium APIs added.

- [ ] **Step 2: Fresh full verification**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```
Record exact count; require 0 warnings/0 errors.

- [ ] **Step 3: Document acceptance boundaries**

Only after evidence:
- F3.1 automated geometry/native-save/alpha/transaction/UI tests = PASS;
- manual Windows drag/resize feel = NOT RUN until executed;
- real handwritten PNG appearance/transparency = NOT RUN until executed;
- real cryptographically signed PDF warning behavior = NOT RUN unless tested on a real signed document;
- F3.2/F3.3/F3.4 remain pending.

- [ ] **Step 4: Open/update stacked draft PR and verify exact closure head**

After closure docs commit, require both push and PR workflows PASS on the same SHA. Earlier functional CI is insufficient.

- [ ] **Step 5: Record final SHA/CI IDs in PR and stop**

PR stays draft/unmerged. Next architecture gate is **F3.2 — Photo/scan → transparent signature**, unless manual F3.1 evidence exposes a defect first.

---

## Execution Order / Stop Conditions

Execute Task 1 → 2 → 3 → 4 → 5. Stop and return to design review if implementation requires:

- page rasterization to preserve signature alpha;
- a second PDF writer/editor engine;
- a new runtime package;
- silent source-PDF modification/overwrite;
- durable placement stored in WPF pixels instead of PDF coordinates;
- unsaved signatures spanning multiple pages in F3.1;
- F3.2/F3.3/F3.4 work smuggled into F3.1;
- creation/modification of cryptographic signatures rather than warning about existing ones.
