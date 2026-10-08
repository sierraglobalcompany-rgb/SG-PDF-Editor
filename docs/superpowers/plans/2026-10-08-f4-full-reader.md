# F4 Full Reader Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn SG PDF Editor's existing single-page F0 reader into a continuous, virtualized, local/offline reader with thumbnails, password support, text search/copy, bookmarks/links, shortcuts and recent files while preserving F2 ZPL and the existing F3 single-active-page signing architecture.

**Architecture:** Keep one `PdfDocumentSession` and PDFium as the only PDF engine. `LEER` gets a new virtualized continuous surface driven by lightweight page geometry and a bounded visible+neighbor render window; `FIRMAR` continues to use the existing single `PdfImage` + `SignatureEditState` surface. New reader code stays feature-local under `Features/Reader` plus focused `MainWindow.Reader*.cs` partials; all PDFium calls remain serialized by `PdfiumRuntime.NativeGate`.

**Tech Stack:** C# / .NET 10 / WPF / PDFium `bblanchon.PDFium.Win32` 156.0.8076 / xUnit / existing PDFsharp test fixture support. No new runtime NuGet dependency.

**Spec:** `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`

## Global Constraints

- Windows x64 + C#/.NET 10 + WPF.
- PDFium remains the only PDF engine; verify needed exports against the pinned binary before depending on them.
- Every PDFium call remains behind `PdfiumRuntime.NativeGate`.
- No new commercial runtime dependency and no new runtime NuGet package without returning to design.
- Normal reader operation remains offline; no account, API key, telemetry, HTTP client, WebView2 or background network dependency.
- `LEER` = continuous virtualized reader; `FIRMAR` = existing single-active-page `PdfImage` / `SignatureEditState` editor.
- Full-resolution reader bitmap retention = visible pages + at most one neighbor before and after.
- Thumbnails are lazy/virtualized; no eager whole-document thumbnail render.
- Search is on-demand page-by-page; no permanent document index and no OCR in F4.
- Text drag selection is one page at a time.
- Bookmarks are read-only and traversal is cycle-safe with max 10,000 nodes / depth 128.
- Only explicit PDF internal GOTO and confirmed `http`/`https` URI actions execute; no Launch/JavaScript/remote-GOTO/arbitrary shell action.
- Passwords are never persisted.
- Recents = max 10 paths + UTC timestamp, atomic LocalAppData JSON, no startup/menu path probing.
- PageUp/PageDown = one viewport-height scroll in continuous `LEER`.
- No multi-document tabs in F4.
- Existing F2/F3 tests remain full-regression gates.
- No merge to `main` without explicit user approval.

## Review Focus

1. **Rapid scroll/zoom while an older render finishes** — stale bitmap/transform must never publish into the new viewport. Pin in Task 2 with `StaleReaderGeneration_DoesNotPublishBitmapOrTransform`.
2. **One pathological/failed page render** — only that page becomes an error card; current session and other pages remain usable. Pin in Task 2 with `PerPageRenderFailure_IsolatedAndSessionRemainsActive`.
3. **Wrong password/cancel while another PDF is already open** — previous document/session/navigation must remain unchanged. Pin in Task 4 with `PasswordWrongOrCancel_PreservesExistingWorkspace`.
4. **Malformed/cyclic bookmark graph** — traversal must stop at cycle/depth/node bounds and preserve already valid nodes. Pin in Task 7 with `BookmarkTraversal_CycleDepthAndNodeLimitsAreBounded`.
5. **Stale/UNC-looking recent path** — building `Archivo -> Recientes` must not probe it or remove it until the user explicitly chooses it. Pin in Task 8 with `RecentMenu_LoadsStoredPathWithoutFileSystemProbe`.

---

## File Structure Locked by This Plan

### Reader feature files

- `src/SGPdf.App/Features/Reader/ReaderPageGeometry.cs` — immutable page layout/result contracts.
- `src/SGPdf.App/Features/Reader/ReaderLayoutPlanner.cs` — pure page geometry, current-page, visible-range and retained-window calculations.
- `src/SGPdf.App/Features/Reader/ReaderPageItem.cs` — WPF-facing page slot state only.
- `src/SGPdf.App/Features/Reader/ReaderThumbnailItem.cs` — WPF-facing thumbnail state only.
- `src/SGPdf.App/Features/Reader/ReaderSearchNavigator.cs` — pure page-by-page next/previous/wrap search state.
- `src/SGPdf.App/Features/Reader/ReaderTextSelection.cs` — reader selection state/normalization only.
- `src/SGPdf.App/Features/Reader/ReaderBookmarkTraversalGuard.cs` — pure bounded traversal guard.
- `src/SGPdf.App/Features/Reader/RecentPdfStore.cs` — versioned LocalAppData recents only.
- `src/SGPdf.App/Features/Reader/RecentPdfFileOps.cs` — narrow deterministic file-operation seam for recents tests.

### PDF layer additions

- `src/SGPdf.App/Pdf/PdfPageSize.cs` — neutral PDF page size contract.
- `src/SGPdf.App/Pdf/PdfTextModels.cs` — neutral text-match/text-rect contracts.
- `src/SGPdf.App/Pdf/PdfNavigationModels.cs` — neutral bookmark/link contracts.
- `src/SGPdf.App/Pdf/PdfDocumentOpenException.cs` — controlled PDFium open error classification.
- `src/SGPdf.App/Pdf/PdfDocumentSession.Text.cs` — text operations; same native document owner.
- `src/SGPdf.App/Pdf/PdfDocumentSession.Navigation.cs` — bookmarks/links; same native document owner.

### MainWindow partials

- `src/SGPdf.App/MainWindow.Reader.cs` — continuous surface/open/navigation/zoom/render-window integration.
- `src/SGPdf.App/MainWindow.ReaderThumbnails.cs` — thumbnail panel synchronization.
- `src/SGPdf.App/MainWindow.ReaderSearch.cs` — find bar and active-match UI.
- `src/SGPdf.App/MainWindow.ReaderSelection.cs` — page-local mouse selection/copy overlay.
- `src/SGPdf.App/MainWindow.ReaderLinks.cs` — bookmark/link navigation and safe external URI seam.
- `src/SGPdf.App/MainWindow.ReaderShortcuts.cs` — reader shortcut routing only.

### Dialog

- `src/SGPdf.App/PdfPasswordDialog.xaml`
- `src/SGPdf.App/PdfPasswordDialog.xaml.cs`

Do not create a ViewModel framework, reader service container, repository layer, event bus or second PDF project.

---

### Task 1: F4.1 Native Capability Gate + Pure Continuous Layout

**Files:**
- Create: `src/SGPdf.App/Pdf/PdfPageSize.cs`
- Create: `src/SGPdf.App/Features/Reader/ReaderPageGeometry.cs`
- Create: `src/SGPdf.App/Features/Reader/ReaderLayoutPlanner.cs`
- Modify: `src/SGPdf.App/Pdf/PdfiumNative.cs`
- Modify: `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- Create test: `tests/SGPdf.App.Tests/PdfiumReaderApiAvailabilityTests.cs`
- Create test: `tests/SGPdf.App.Tests/ReaderLayoutPlannerTests.cs`
- Modify test: `tests/SGPdf.App.Tests/PdfRenderTests.cs`

**Interfaces:**

```csharp
public readonly record struct PdfPageSize(double WidthPoints, double HeightPoints);

public sealed partial class PdfDocumentSession
{
    public IReadOnlyList<PdfPageSize> GetPageSizes(CancellationToken cancellationToken = default);
}

internal readonly record struct ReaderPageGeometry(
    int PageIndex,
    double PdfWidthPoints,
    double PdfHeightPoints,
    double DisplayWidth,
    double DisplayHeight,
    double Top,
    double Bottom,
    double ResolvedDpi);

internal readonly record struct ReaderRenderWindow(
    int FirstVisiblePageIndex,
    int LastVisiblePageIndex,
    int FirstRetainedPageIndex,
    int LastRetainedPageIndex,
    IReadOnlyList<int> RenderPriority);

internal static class ReaderLayoutPlanner
{
    internal static IReadOnlyList<ReaderPageGeometry> Build(
        IReadOnlyList<PdfPageSize> pages,
        PdfZoomState zoom,
        double viewportWidth,
        double viewportHeight,
        double horizontalPadding = 48d,
        double pageGap = 16d);

    internal static int FindCurrentPageIndex(
        IReadOnlyList<ReaderPageGeometry> pages,
        double verticalOffset,
        double viewportHeight);

    internal static ReaderRenderWindow GetRenderWindow(
        IReadOnlyList<ReaderPageGeometry> pages,
        double verticalOffset,
        double viewportHeight);
}
```

`Build` keeps one common DPI for manual zoom, resolves Fit Width/Fit Page per page through existing `PdfZoomState.ResolveDpi`, and builds stable cumulative `Top`/`Bottom` geometry including `pageGap`. `GetRenderWindow` returns visible pages first in `RenderPriority`, then at most one neighbor before and one after.

- [ ] **Step 1: Write the pinned-PDFium capability RED test.**

`PdfiumReaderApiAvailabilityTests.PinnedPdfium_ExportsAllF4RequiredStableFunctions` must load the already-packaged `pdfium` native library and assert exports for:

`FPDF_GetPageSizeByIndexF`, `FPDFText_LoadPage`, `FPDFText_ClosePage`, `FPDFText_FindStart`, `FPDFText_FindNext`, `FPDFText_FindPrev`, `FPDFText_FindClose`, `FPDFText_GetSchResultIndex`, `FPDFText_GetSchCount`, `FPDFText_GetCharIndexAtPos`, `FPDFText_GetText`, `FPDFText_CountRects`, `FPDFText_GetRect`, `FPDFBookmark_GetFirstChild`, `FPDFBookmark_GetNextSibling`, `FPDFBookmark_GetTitle`, `FPDFBookmark_GetDest`, `FPDFDest_GetDestPageIndex`, `FPDFLink_Enumerate`, `FPDFLink_GetAnnotRect`, `FPDFLink_GetDest`, `FPDFLink_GetAction`, `FPDFAction_GetType`, `FPDFAction_GetDest`, `FPDFAction_GetURIPath`.

If any required export is absent, **stop implementation and return to design before adding product code or another engine**.

- [ ] **Step 2: Run the capability test.**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfiumReaderApiAvailabilityTests"
```

Expected: PASS against the pinned binary. If FAIL because an export is absent, this is a design stop condition, not a test to weaken.

- [ ] **Step 3: Write RED tests for page-size metadata and pure layout.**

Tests:

- `GetPageSizes_MixedPortraitLandscape_ReturnsIndexOrderedPointSizes`
- `GetPageSizes_PreCanceledToken_ThrowsWithoutPartialResult`
- `Build_ManualZoom_UsesOneDpiAcrossMixedPageSizes`
- `Build_FitWidth_ResolvesDpiPerPageWidth`
- `Build_FitPage_ResolvesDpiPerPageDimensions`
- `FindCurrentPage_CenterInsidePage_SelectsThatPage`
- `FindCurrentPage_CenterInGap_SelectsNearestPage`
- `RenderWindow_VisiblePagesPlusOneNeighborEachSideOnly`
- `RenderWindow_PrioritizesVisibleBeforeNeighbors`
- `InvalidViewportOrPageSize_IsRejected`

- [ ] **Step 4: Run Task 1 RED tests.**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReaderLayoutPlannerTests|FullyQualifiedName~PdfRenderTests"
```

Expected: FAIL because `PdfPageSize`, `GetPageSizes`, `ReaderPageGeometry` and `ReaderLayoutPlanner` do not exist yet.

- [ ] **Step 5: Implement minimum PDFium page-size binding and layout contracts.**

Add only the `FPDF_GetPageSizeByIndexF` binding/size struct needed now. `GetPageSizes` must serialize native access through `NativeGate`, validate positive finite dimensions, preserve page order, and honor cancellation between page metadata reads. Do not add text/bookmark/link declarations yet; Task 1 capability test checks raw exports independently.

- [ ] **Step 6: Run focused GREEN + full regression.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfiumReaderApiAvailabilityTests|FullyQualifiedName~ReaderLayoutPlannerTests|FullyQualifiedName~PdfRenderTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Expected: all focused tests PASS; full build 0 errors; existing suite PASS.

- [ ] **Step 7: Commit Task 1.**

```bash
git add src/SGPdf.App/Pdf src/SGPdf.App/Features/Reader tests/SGPdf.App.Tests/PdfiumReaderApiAvailabilityTests.cs tests/SGPdf.App.Tests/ReaderLayoutPlannerTests.cs tests/SGPdf.App.Tests/PdfRenderTests.cs
git commit -m "feat(reader): add continuous layout primitives"
```

---

### Task 2: F4.1 Continuous WPF Surface + Bounded Rendering + FIRMAR Boundary

**Files:**
- Create: `src/SGPdf.App/Features/Reader/ReaderPageItem.cs`
- Create: `src/SGPdf.App/MainWindow.Reader.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.xaml.cs`
- Modify narrowly: `src/SGPdf.App/MainWindow.Sign.cs`
- Create test: `tests/SGPdf.App.Tests/MainWindowReaderTests.cs`
- Create test: `tests/SGPdf.App.Tests/ReaderPageItemTests.cs`
- Regression: existing `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs`
- Regression: existing ZPL UI tests

**Interfaces:**

```csharp
internal enum ReaderPageRenderState { Placeholder, Loading, Ready, Error }

internal sealed class ReaderPageItem : INotifyPropertyChanged
{
    internal int PageIndex { get; }
    internal ReaderPageGeometry Geometry { get; private set; }
    internal ReaderPageRenderState RenderState { get; }
    internal BitmapSource? Bitmap { get; }
    internal PdfPageDeviceTransform? DeviceTransform { get; }
    internal string? ErrorMessage { get; }

    internal void ApplyGeometry(ReaderPageGeometry geometry);
    internal void MarkLoading();
    internal void Publish(PdfRenderedPage rendered, BitmapSource bitmap);
    internal void MarkError(string message);
    internal void ReleaseBitmap();
}
```

MainWindow reader seams/methods:

```csharp
private Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>> _getReaderPageSizes =
    static (session, token) => session.GetPageSizes(token);

private Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage> _renderReaderPage =
    static (session, pageIndex, dpi, token) => session.RenderPage(pageIndex, dpi, token);

private Task<bool> TryOpenPdfPathAsync(string path, string? password = null);
private void InitializeContinuousReader(IReadOnlyList<PdfPageSize> pageSizes);
private void RebuildReaderGeometry(bool preserveCurrentPageAnchor);
private Task RefreshReaderRenderWindowAsync();
private void UpdateReaderCurrentPageFromViewport();
private void ScrollReaderToPage(int pageIndex);
private Task EnsureEditSurfaceForCurrentPageAsync();
```

Use one dedicated `PdfRenderScheduler _readerRenderScheduler`; a refresh obtains one request and processes `ReaderRenderWindow.RenderPriority` **sequentially**, checking `IsCurrent(request)` before publishing each bitmap. Never spawn Task-per-page fanout.

`MainWindow.xaml` adds a named continuous list/surface inside the existing center grid, using standard WPF virtualization/recycling (`VirtualizingPanel.IsVirtualizing=True`, `VirtualizationMode=Recycling`, pixel scrolling where supported). Keep the existing `PdfScrollViewer` + `PdfImage` as the edit/sign surface; do not repurpose it into the continuous viewer.

- [ ] **Step 1: Write RED tests for page-slot lifecycle.**

Tests:

- `PageItem_DefaultsToPlaceholderWithoutBitmap`
- `Publish_SetsReadyBitmapAndTransform`
- `ReleaseBitmap_ClearsFullResolutionDataButPreservesGeometry`
- `MarkError_ClearsBitmapAndKeepsPageNavigable`

- [ ] **Step 2: Write RED STA tests for continuous reader behavior.**

Tests:

- `LeerMode_ShowsContinuousSurfaceAndHidesSinglePageEditSurface`
- `ContinuousSurface_UsesRecyclingVirtualization`
- `OpenPdf_PublishesSessionAfterPageMetricsWithoutEagerAllPageRender`
- `ScrollCenter_UpdatesNavigationPageNumberAndPrintCurrentSemantics`
- `PreviousNextAndPageNumber_ScrollWithoutRecreatingSession`
- `Zoom_RebuildsGeometryKeepsCurrentPageAnchorAndReleasesStaleBitmaps`
- `StaleReaderGeneration_DoesNotPublishBitmapOrTransform`
- `PerPageRenderFailure_IsolatedAndSessionRemainsActive`
- `ReaderRetention_ReleasesBitmapOutsideVisiblePlusNeighborWindow`
- `EnterFirmar_UsesCurrentReaderPageAndExistingSinglePageSurface`
- `ReturnLeer_PreservesCurrentPageAndExistingSignatureDirtyGuard`
- `PdfToZpl_HidesBothPdfReaderSurfacesAndKeepsExistingZplBehavior`
- `PrintCurrentPage_UsesContinuousCurrentNavigationIndex`

Use the existing reflection + `RunInSta` style from `MainWindowSignatureTests`; inject `_getReaderPageSizes` / `_renderReaderPage` for deterministic UI tests rather than requiring real long renders.

- [ ] **Step 3: Run Task 2 RED.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReaderPageItemTests|FullyQualifiedName~MainWindowReaderTests"
```

Expected: FAIL because the continuous surface, `ReaderPageItem`, seams and reader methods are absent.

- [ ] **Step 4: Implement `ReaderPageItem` and continuous surface with minimal MainWindow split.**

Move only reader-specific logic into `MainWindow.Reader.cs`. Refactor `OpenPdf_Click`, navigation, zoom and resize to route by mode:

- `LEER`: scroll/rebuild/render continuous reader.
- `FIRMAR`: retain current single-page behavior and guards.

`TryOpenPdfPathAsync` remains candidate-first: create candidate session + page sizes; only then replace `_session`. Do not dispose the prior session on candidate failure.

- [ ] **Step 5: Integrate F3 without changing F3 data model.**

`SignMode_Click` may become async only as needed to call `EnsureEditSurfaceForCurrentPageAsync`. It must not alter `SignatureEditState`, `SignatureCoordinateMapper`, `AddSignatureAsset(...)` or writer semantics. Returning to `LEER` hides the edit surface and restores the continuous page anchor after existing dirty resolution succeeds.

- [ ] **Step 6: Run focused GREEN, F3/ZPL regressions, then full suite.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReaderPageItemTests|FullyQualifiedName~MainWindowReaderTests|FullyQualifiedName~MainWindowSignatureTests|FullyQualifiedName~MainWindowZpl"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 7: Commit Task 2.**

```bash
git add src/SGPdf.App/MainWindow.xaml src/SGPdf.App/MainWindow.xaml.cs src/SGPdf.App/MainWindow.Reader.cs src/SGPdf.App/MainWindow.Sign.cs src/SGPdf.App/Features/Reader/ReaderPageItem.cs tests/SGPdf.App.Tests/ReaderPageItemTests.cs tests/SGPdf.App.Tests/MainWindowReaderTests.cs
git commit -m "feat(reader): add continuous virtualized reading"
```

---

### Task 3: F4.2 Lazy Thumbnails + Left Navigation Sync

**Files:**
- Create: `src/SGPdf.App/Features/Reader/ReaderThumbnailItem.cs`
- Create: `src/SGPdf.App/MainWindow.ReaderThumbnails.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Create test: `tests/SGPdf.App.Tests/ReaderThumbnailTests.cs`
- Create test: `tests/SGPdf.App.Tests/MainWindowReaderThumbnailTests.cs`

**Interfaces:**

```csharp
internal sealed class ReaderThumbnailItem : INotifyPropertyChanged
{
    internal int PageIndex { get; }
    internal PdfPageSize PageSize { get; }
    internal BitmapSource? Bitmap { get; }
    internal bool IsLoading { get; }
    internal bool HasError { get; }
    internal void Publish(BitmapSource bitmap);
    internal void MarkError();
    internal void ReleaseBitmap();
}

internal static double ResolveThumbnailDpi(PdfPageSize size, double targetWidthPixels = 132d);
```

Add named UI controls:

- `ReaderNavigationTabs`
- `ReaderPagesTab`
- `ReaderBookmarksTab`
- `ReaderThumbnailList`
- `ReaderBookmarksTree` (empty/disabled until Task 7)

Use a separate `PdfRenderScheduler _thumbnailRenderScheduler`; render only realized/near-visible thumbnail rows and process sequentially. Main full-page reader scheduling always starts independently and is not blocked by a queued eager thumbnail sweep.

- [ ] **Step 1: Write RED tests.**

Tests:

- `ThumbnailDpi_Targets132PixelsWithoutUpscalingBeyondViewingNeed`
- `ThumbnailItem_ReleaseDropsBitmap`
- `PagesTab_IsVirtualizedAndBookmarksTabExists`
- `ThumbnailRequests_AreLazyForRealizedWindowOnly`
- `ThumbnailClick_NavigatesExactlyOnce`
- `ReaderScroll_UpdatesSelectedThumbnailWithoutRecursiveNavigation`
- `ThumbnailRenderFailure_LeavesMainReaderUsable`
- `RapidThumbnailScroll_DropsStalePublication`

- [ ] **Step 2: Run RED.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReaderThumbnailTests|FullyQualifiedName~MainWindowReaderThumbnailTests"
```

Expected: FAIL because thumbnail model/panel integration does not exist.

- [ ] **Step 3: Implement lazy thumbnail model and virtualized Páginas tab.**

Do not keep a bitmap for every page. Page number and `PdfPageSize` may exist for all items; thumbnail `BitmapSource` lifetime is tied to realized/near-realized rows.

- [ ] **Step 4: Run focused GREEN + Task 2 + full regression.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReaderThumbnail|FullyQualifiedName~MainWindowReader"
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 5: Commit Task 3.**

```bash
git add src/SGPdf.App/Features/Reader/ReaderThumbnailItem.cs src/SGPdf.App/MainWindow.ReaderThumbnails.cs src/SGPdf.App/MainWindow.xaml tests/SGPdf.App.Tests/ReaderThumbnailTests.cs tests/SGPdf.App.Tests/MainWindowReaderThumbnailTests.cs
git commit -m "feat(reader): add lazy page thumbnails"
```

---

### Task 4: F4.3 Password-Protected PDFs

**Files:**
- Create: `src/SGPdf.App/Pdf/PdfDocumentOpenException.cs`
- Modify: `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- Create: `src/SGPdf.App/PdfPasswordDialog.xaml`
- Create: `src/SGPdf.App/PdfPasswordDialog.xaml.cs`
- Modify: `src/SGPdf.App/MainWindow.Reader.cs`
- Create test: `tests/SGPdf.App.Tests/PdfPasswordTests.cs`
- Create test: `tests/SGPdf.App.Tests/PdfPasswordDialogTests.cs`
- Modify/create STA test: `tests/SGPdf.App.Tests/MainWindowReaderPasswordTests.cs`

**Interfaces:**

```csharp
public enum PdfDocumentOpenError
{
    PasswordRequiredOrIncorrect,
    OtherPdfiumError
}

public sealed class PdfDocumentOpenException : InvalidOperationException
{
    public PdfDocumentOpenError Error { get; }
    public uint PdfiumErrorCode { get; }
}

public partial class PdfDocumentSession
{
    public static PdfDocumentSession Open(string filePath, string? password = null);
}

public partial class PdfPasswordDialog : Window
{
    internal static string? Prompt(Window owner, string fileName);
}
```

MainWindow seam:

```csharp
private Func<Window, string, string?> _requestPdfPassword =
    static (owner, fileName) => PdfPasswordDialog.Prompt(owner, fileName);
```

`PdfDocumentSession.Open` maps PDFium error code `4` to `PasswordRequiredOrIncorrect`; UI must inspect the typed exception, never message text.

- [ ] **Step 1: Write RED PDFium password integration tests.**

Create a test PDF with PDFsharp security settings and user password `secret`. Tests:

- `ProtectedPdf_OpenWithoutPassword_ThrowsTypedPasswordError`
- `ProtectedPdf_OpenWrongPassword_ThrowsTypedPasswordError`
- `ProtectedPdf_OpenCorrectPassword_SucceedsAndRenders`
- `InvalidPdf_StillClassifiesAsOtherPdfiumError`
- `PasswordException_DoesNotExposePasswordValue`

- [ ] **Step 2: Write RED dialog/MainWindow candidate-first tests.**

Tests:

- `PasswordDialog_UsesMaskedPasswordBox`
- `PasswordPrompt_CancelReturnsNull`
- `PasswordWrongOrCancel_PreservesExistingWorkspace`
- `PasswordRetryCorrect_PublishesCandidateOnce`
- `PasswordValue_IsNotStoredInStatusTitleRecentsOrFieldsAfterOpen`

- [ ] **Step 3: Run RED.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfPassword|FullyQualifiedName~MainWindowReaderPassword"
```

- [ ] **Step 4: Implement typed open classification, masked dialog and retry loop.**

Keep prior candidate-first behavior. Wrong password keeps the password dialog flow active; Cancel leaves the previous document/session/navigation untouched. Do not add “remember password”.

- [ ] **Step 5: Run focused GREEN + full regression.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfPassword|FullyQualifiedName~MainWindowReaderPassword|FullyQualifiedName~PdfRenderTests"
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 6: Commit Task 4.**

```bash
git add src/SGPdf.App/Pdf/PdfDocumentOpenException.cs src/SGPdf.App/Pdf/PdfDocumentSession.cs src/SGPdf.App/PdfPasswordDialog.xaml src/SGPdf.App/PdfPasswordDialog.xaml.cs src/SGPdf.App/MainWindow.Reader.cs tests/SGPdf.App.Tests/PdfPasswordTests.cs tests/SGPdf.App.Tests/PdfPasswordDialogTests.cs tests/SGPdf.App.Tests/MainWindowReaderPasswordTests.cs
git commit -m "feat(reader): support password protected PDFs"
```

---

### Task 5: F4.4 PDFium Text Core + Find Navigation

**Files:**
- Create: `src/SGPdf.App/Pdf/PdfTextModels.cs`
- Create: `src/SGPdf.App/Pdf/PdfDocumentSession.Text.cs`
- Modify: `src/SGPdf.App/Pdf/PdfiumNative.cs`
- Ensure: `src/SGPdf.App/Pdf/PdfDocumentSession.cs` is `partial`
- Create: `src/SGPdf.App/Features/Reader/ReaderSearchNavigator.cs`
- Create: `src/SGPdf.App/MainWindow.ReaderSearch.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Create test: `tests/SGPdf.App.Tests/PdfTextTests.cs`
- Create test: `tests/SGPdf.App.Tests/ReaderSearchNavigatorTests.cs`
- Create test: `tests/SGPdf.App.Tests/MainWindowReaderSearchTests.cs`

**Interfaces:**

```csharp
public readonly record struct PdfTextRect(double Left, double Bottom, double Right, double Top);

public sealed record PdfTextMatch(
    int PageIndex,
    int StartIndex,
    int CharacterCount,
    IReadOnlyList<PdfTextRect> Rects);

public partial class PdfDocumentSession
{
    public IReadOnlyList<PdfTextMatch> FindTextOnPage(
        int pageIndex,
        string query,
        CancellationToken cancellationToken = default);

    public int GetCharacterIndexAtPoint(
        int pageIndex,
        double pdfX,
        double pdfY,
        double xTolerance,
        double yTolerance,
        CancellationToken cancellationToken = default);

    public string GetTextRange(
        int pageIndex,
        int startIndex,
        int characterCount,
        CancellationToken cancellationToken = default);

    public IReadOnlyList<PdfTextRect> GetTextRangeRects(
        int pageIndex,
        int startIndex,
        int characterCount,
        CancellationToken cancellationToken = default);
}

enum ReaderSearchDirection { Forward, Backward }

internal sealed record ReaderSearchCursor(
    string Query,
    int PageIndex,
    int MatchIndexWithinPage);

internal static class ReaderSearchNavigator
{
    internal static ReaderSearchResult? Find(
        string query,
        int startPageIndex,
        ReaderSearchCursor? current,
        ReaderSearchDirection direction,
        int pageCount,
        Func<int, string, CancellationToken, IReadOnlyList<PdfTextMatch>> findOnPage,
        CancellationToken cancellationToken);
}
```

`ReaderSearchNavigator` visits each page at most once per action, wraps at most once, and returns first next/previous match. Query changes ignore the prior cursor. No whole-document result cache.

- [ ] **Step 1: Write RED PDFium text integration tests using public-safe synthetic PDFs.**

Tests:

- `UnicodeText_ExtractsAccentsAndEnye`
- `FindTextOnPage_IsCaseInsensitiveAndNonWholeWordByDefault`
- `FindTextOnPage_ReturnsStartCountAndRects`
- `ImageOnlyPage_ReturnsNoTextMatches`
- `GetTextRange_ReturnsUnicodeWithoutTrailingGarbage`
- `GetCharacterIndexAtPoint_OutsideTextReturnsMinusOne`
- `TextOperations_PreCanceledToken_AbortBeforePublication`

Do not add OCR.

- [ ] **Step 2: Write RED pure search navigator tests.**

Tests:

- `Forward_FindsNextMatchOnCurrentPage`
- `Forward_ContinuesToLaterPageThenWrapsOnce`
- `Backward_UsesPreviousMatchAndWrapsOnce`
- `NoResult_VisitsEveryPageAtMostOnce`
- `QueryChange_ResetsPriorCursor`
- `Cancellation_StopsBetweenPageCalls`

- [ ] **Step 3: Write RED STA find-bar tests.**

Named controls:

- `ReaderFindBar`
- `ReaderFindTextBox`
- `ReaderFindPreviousButton`
- `ReaderFindNextButton`
- `ReaderFindCloseButton`
- `ReaderFindStatusText`

Tests:

- `CtrlF_OpensFindBarAndFocusesQuery`
- `Enter_FindsNext_ShiftEnterFindsPrevious`
- `Escape_ClosesFindBar`
- `ActiveMatch_NavigatesPageAndCreatesHighlightOnlyForActiveMatch`
- `NoResults_ShowsControlledStatusWithoutChangingDocument`
- `NewQuery_CancelsStaleSearchGeneration`
- `EnteringFirmar_HidesSearchUiWithoutMutatingSignatureState`

- [ ] **Step 4: Run RED.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfTextTests|FullyQualifiedName~ReaderSearchNavigatorTests|FullyQualifiedName~MainWindowReaderSearchTests"
```

- [ ] **Step 5: Implement PDFium text bindings and managed search navigation.**

Open/close PDFium text-page handles inside each operation under `NativeGate`; no native text handle escapes `PdfDocumentSession`. Use PDFium match rectangles as PDF-space data; WPF mapping happens in reader UI.

- [ ] **Step 6: Implement find bar and active match overlay only.**

Search runs off UI thread page-by-page; cancellation/query generation is checked before UI publication. Do not build all-results UI or global N-of-M count.

- [ ] **Step 7: Run focused GREEN + full regression and commit.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfTextTests|FullyQualifiedName~ReaderSearchNavigatorTests|FullyQualifiedName~MainWindowReaderSearchTests"
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
git add src/SGPdf.App/Pdf src/SGPdf.App/Features/Reader/ReaderSearchNavigator.cs src/SGPdf.App/MainWindow.ReaderSearch.cs src/SGPdf.App/MainWindow.xaml tests/SGPdf.App.Tests/PdfTextTests.cs tests/SGPdf.App.Tests/ReaderSearchNavigatorTests.cs tests/SGPdf.App.Tests/MainWindowReaderSearchTests.cs
git commit -m "feat(reader): add PDF text search"
```

---

### Task 6: F4.4 One-Page Text Selection + Clipboard Copy

**Files:**
- Create: `src/SGPdf.App/Features/Reader/ReaderTextSelection.cs`
- Create: `src/SGPdf.App/MainWindow.ReaderSelection.cs`
- Modify: `src/SGPdf.App/MainWindow.Reader.cs` or page template event wiring only
- Create test: `tests/SGPdf.App.Tests/ReaderTextSelectionTests.cs`
- Create test: `tests/SGPdf.App.Tests/MainWindowReaderSelectionTests.cs`

**Interfaces:**

```csharp
internal sealed record ReaderTextSelection(
    int PageIndex,
    int StartIndex,
    int CharacterCount,
    IReadOnlyList<PdfTextRect> PdfRects);

internal static class ReaderTextSelectionRange
{
    internal static (int StartIndex, int CharacterCount)? Normalize(int firstCharIndex, int lastCharIndex);
}
```

MainWindow seam:

```csharp
private Action<string> _setReaderClipboardText = static text => Clipboard.SetText(text);
```

Selection drag starts and ends on one page card only. Device point -> PDF point uses that page's existing `PdfPageDeviceTransform`; do not use F3's `SignatureCoordinateMapper` or move `PdfRect` across feature boundaries. Reader selection overlay uses neutral `PdfTextRect` + page transform.

- [ ] **Step 1: Write RED pure range tests.**

Tests:

- `Normalize_ForwardAndBackwardDragProduceSameRange`
- `Normalize_MissingHitReturnsNull`
- `Normalize_SameCharacterProducesOneCharacterSelection`

- [ ] **Step 2: Write RED STA selection/copy tests.**

Tests:

- `MouseDragWithinOnePage_CreatesSelectionAndRectOverlay`
- `CrossPageDrag_DoesNotCreateCrossPageSelection`
- `ClickElsewhere_ClearsSelection`
- `Zoom_ReprojectsSelectionFromPdfRectsNotRasterizedHighlight`
- `CtrlC_WithSelectionCopiesExactPdfiumUnicodeText`
- `CtrlC_WithoutSelectionDoesNothing`
- `EnteringFirmar_ClearsReaderSelectionOnly`
- `TextSelectionDrag_TakesPrecedenceOverLinkActivationPlaceholder`

- [ ] **Step 3: Run RED.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReaderTextSelectionTests|FullyQualifiedName~MainWindowReaderSelectionTests"
```

- [ ] **Step 4: Implement selection state, mouse wiring and overlay.**

Only page-local selection. Use `GetCharacterIndexAtPoint`, `GetTextRangeRects`, `GetTextRange`; clipboard write happens only after explicit `Ctrl+C`/copy command.

- [ ] **Step 5: Run focused GREEN + search/F3 regression + full suite; commit.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReaderSelection|FullyQualifiedName~ReaderSearch|FullyQualifiedName~MainWindowSignature"
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
git add src/SGPdf.App/Features/Reader/ReaderTextSelection.cs src/SGPdf.App/MainWindow.ReaderSelection.cs src/SGPdf.App/MainWindow.Reader.cs tests/SGPdf.App.Tests/ReaderTextSelectionTests.cs tests/SGPdf.App.Tests/MainWindowReaderSelectionTests.cs
git commit -m "feat(reader): add page text selection and copy"
```

---

### Task 7: F4.5 Bookmarks + Explicit PDF Links

**Files:**
- Create: `src/SGPdf.App/Pdf/PdfNavigationModels.cs`
- Create: `src/SGPdf.App/Pdf/PdfDocumentSession.Navigation.cs`
- Modify: `src/SGPdf.App/Pdf/PdfiumNative.cs`
- Create: `src/SGPdf.App/Features/Reader/ReaderBookmarkTraversalGuard.cs`
- Create: `src/SGPdf.App/MainWindow.ReaderLinks.cs`
- Modify: `src/SGPdf.App/MainWindow.ReaderThumbnails.cs` / `MainWindow.xaml` only to populate existing `ReaderBookmarksTree`
- Create test: `tests/SGPdf.App.Tests/PdfNavigationTests.cs`
- Create test: `tests/SGPdf.App.Tests/ReaderBookmarkTraversalTests.cs`
- Create test: `tests/SGPdf.App.Tests/MainWindowReaderLinksTests.cs`

**Interfaces:**

```csharp
public sealed record PdfBookmarkNode(
    string Title,
    int? DestinationPageIndex,
    IReadOnlyList<PdfBookmarkNode> Children);

public enum PdfLinkActionKind
{
    None,
    InternalGoto,
    Uri,
    Unsupported
}

public sealed record PdfPageLink(
    int PageIndex,
    PdfTextRect Rect,
    PdfLinkActionKind ActionKind,
    int? DestinationPageIndex,
    string? Uri);

public partial class PdfDocumentSession
{
    public IReadOnlyList<PdfBookmarkNode> GetBookmarks(CancellationToken cancellationToken = default);
    public IReadOnlyList<PdfPageLink> GetPageLinks(int pageIndex, CancellationToken cancellationToken = default);
}

internal sealed class ReaderBookmarkTraversalGuard
{
    internal const int MaxNodes = 10_000;
    internal const int MaxDepth = 128;
    internal bool TryEnter(nint nativeHandle, int depth);
}
```

MainWindow seams:

```csharp
private Func<Window, string, bool> _confirmExternalUri;
private Action<Uri> _openExternalUri;
```

Default confirmation displays the full destination. Default opener uses OS shell only after confirmation. Scheme check uses `Uri.UriSchemeHttp` / `Uri.UriSchemeHttps`; do not introduce literal remote URLs or `HttpClient`, preserving existing offline-source guards.

- [ ] **Step 1: Write RED bounded traversal tests.**

Tests:

- `BookmarkTraversal_CycleDepthAndNodeLimitsAreBounded`
- `RepeatedNativeHandle_IsRejectedAsCycle`
- `Depth128Allowed_Depth129Rejected`
- `Node10000Allowed_Node10001Rejected`

- [ ] **Step 2: Write RED PDFium bookmark/link integration tests.**

Use synthetic test PDFs with outline and annotations. Tests:

- `Bookmarks_ReturnTitleHierarchyAndInternalDestination`
- `BookmarkUnsupportedAction_RemainsVisibleWithoutExecutableDestination`
- `InternalLink_ReturnsTargetPageAndRect`
- `UriLink_ReturnsUriWithoutLaunchingAnything`
- `UnsupportedLaunchOrJavaScript_IsClassifiedUnsupported`
- `MalformedBookmarkTraversal_ReturnsValidPrefixWithoutInfiniteLoop`

- [ ] **Step 3: Write RED STA UI safety tests.**

Tests:

- `BookmarkClick_ScrollsReaderToDestinationPage`
- `InternalLink_ClickNavigatesWithoutExternalPrompt`
- `UriLink_RequiresExplicitConfirmationBeforeOpenSeam`
- `UriCancel_PerformsNoExternalAction`
- `NonHttpHttpsUri_IsNeverOpened`
- `UnsupportedAction_PerformsNoAction`
- `SelectionDrag_SuppressesLinkActivation`

- [ ] **Step 4: Run RED.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfNavigationTests|FullyQualifiedName~ReaderBookmarkTraversalTests|FullyQualifiedName~MainWindowReaderLinksTests"
```

- [ ] **Step 5: Implement minimal bookmark/link bindings + bounded traversal.**

No plain-text URL detection, remote GOTO, Launch, JavaScript or arbitrary file action execution. Native handles stay inside session operations.

- [ ] **Step 6: Populate `Marcadores` and page link overlays; run full regression.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfNavigation|FullyQualifiedName~ReaderBookmark|FullyQualifiedName~MainWindowReaderLinks"
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 7: Commit Task 7.**

```bash
git add src/SGPdf.App/Pdf src/SGPdf.App/Features/Reader/ReaderBookmarkTraversalGuard.cs src/SGPdf.App/MainWindow.ReaderLinks.cs src/SGPdf.App/MainWindow.ReaderThumbnails.cs src/SGPdf.App/MainWindow.xaml tests/SGPdf.App.Tests/PdfNavigationTests.cs tests/SGPdf.App.Tests/ReaderBookmarkTraversalTests.cs tests/SGPdf.App.Tests/MainWindowReaderLinksTests.cs
git commit -m "feat(reader): add bookmarks and safe PDF links"
```

---

### Task 8: F4.6 Shortcuts + Atomic Recent Files + Hardening

**Files:**
- Create: `src/SGPdf.App/Features/Reader/RecentPdfStore.cs`
- Create: `src/SGPdf.App/Features/Reader/RecentPdfFileOps.cs`
- Create: `src/SGPdf.App/MainWindow.ReaderShortcuts.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify narrowly: `src/SGPdf.App/MainWindow.Sign.cs` (`OnPreviewKeyDown` delegates to reader shortcut handler after signature-specific guards)
- Modify: `src/SGPdf.App/MainWindow.Reader.cs` successful-open hook + recent-entry open
- Modify test: `tests/SGPdf.App.Tests/OfflineRuntimeTests.cs`
- Create test: `tests/SGPdf.App.Tests/RecentPdfStoreTests.cs`
- Create test: `tests/SGPdf.App.Tests/MainWindowReaderShortcutTests.cs`
- Create test: `tests/SGPdf.App.Tests/MainWindowRecentFilesTests.cs`

**Interfaces:**

```csharp
internal sealed record RecentPdfEntry(string FullPath, DateTimeOffset LastOpenedUtc);

internal sealed class RecentPdfFileOps
{
    internal Action<string, string> PublishNewFile { get; set; }
    internal Action<string, string> ReplaceFile { get; set; }
    internal Action<string> DeleteFile { get; set; }
}

internal sealed class RecentPdfStore
{
    internal RecentPdfStore(string? rootDirectory = null, RecentPdfFileOps? fileOps = null);
    internal string RootDirectory { get; }
    internal IReadOnlyList<RecentPdfEntry> Load();
    internal IReadOnlyList<RecentPdfEntry> RecordSuccessfulOpen(string path, DateTimeOffset openedUtc);
    internal IReadOnlyList<RecentPdfEntry> Remove(string path);
    internal void Clear();
}
```

Storage: `%LOCALAPPDATA%\SG PDF Editor\recent-files.json`, version `1`, max 10 entries. `Load()` never calls `File.Exists`/opens each stored PDF path; it reads only the manifest file itself. Corrupt manifest returns unavailable/empty for that read and is not rewritten merely by loading. A later successful open may rebuild a valid manifest through the normal atomic temp->publish path.

Reader shortcut entry point:

```csharp
private bool TryHandleReaderShortcut(KeyEventArgs e);
```

It returns `true` only when it handled the key. It must not override normal editing keys when `e.OriginalSource` is a `TextBoxBase`, `PasswordBox`, ComboBox editable text part, or other text-entry control.

- [ ] **Step 1: Write RED recent-store tests.**

Tests:

- `Load_MissingManifest_ReturnsEmptyWithoutCreatingFiles`
- `RecordSuccessfulOpen_NormalizesPathMovesExistingToTopAndUsesOrdinalIgnoreCase`
- `RecordSuccessfulOpen_CapsAtTenNewest`
- `RecordSuccessfulOpen_StoresOnlyPathAndUtcTimestamp`
- `Load_CorruptManifest_ReturnsEmptyAndDoesNotRewrite`
- `SuccessfulOpen_AfterCorruptManifest_RebuildsValidManifestAtomically`
- `ManifestPublicationFailure_LeavesOldManifestAuthoritative`
- `Clear_RemovesMetadataOnly`
- `Load_DoesNotFilterMissingOrUncLookingPaths`

- [ ] **Step 2: Write RED STA recent-menu tests.**

Named menu controls:

- `RecentFilesMenuItem`
- `ClearRecentFilesMenuItem`

Tests:

- `RecentMenu_LoadsStoredPathWithoutFileSystemProbe`
- `SuccessfulPdfOpen_RecordsRecentOnlyAfterCandidatePublished`
- `FailedOrCanceledOpen_DoesNotRecordRecent`
- `RecentClick_UsesNormalCandidateFirstOpenFlow`
- `MissingRecentSelected_ShowsControlledFailureKeepsCurrentDocumentAndCanRemoveEntry`
- `ClearRecents_ClearsMetadataWithoutTouchingPdfFiles`

- [ ] **Step 3: Write RED shortcut tests.**

Tests:

- `CtrlO_InvokesOpenPdfAction`
- `CtrlP_InvokesPrintWhenPdfOpen`
- `CtrlF_OpensFindBar`
- `CtrlC_UsesReaderSelectionOnlyWhenPresent`
- `CtrlPlusMinusAndZero_UseExistingZoomState`
- `HomeEnd_NavigateFirstLastPageOutsideTextEntry`
- `PageUpPageDown_ScrollExactlyOneViewportHeight`
- `EditableTextControl_KeepsNormalHomeEndCopyAndTypingSemantics`
- `SignatureDeleteAndDirtyEnterGuards_StillWinBeforeReaderShortcuts`

Do not bind F3/Shift+F3 unless an explicit no-conflict test first proves they are free; they are optional in the spec.

- [ ] **Step 4: Run RED.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~RecentPdfStoreTests|FullyQualifiedName~MainWindowRecentFilesTests|FullyQualifiedName~MainWindowReaderShortcutTests"
```

- [ ] **Step 5: Implement atomic recents and shortcut routing.**

Reuse the same KISS same-volume temp publication principles as F3.4, but keep the recents store feature-local; do not generalize an app-wide repository/file transaction abstraction.

- [ ] **Step 6: Extend offline/hardening regression.**

`OfflineRuntimeTests` must continue to reject network client dependencies. Add assertions that reader runtime adds no `HttpClient`, WebView/browser control, remote URL literal dependency or new runtime package. External URI opening is only user-triggered OS shell after confirmation and does not make the app itself a network client.

- [ ] **Step 7: Run focused GREEN + complete suite.**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~RecentPdf|FullyQualifiedName~MainWindowRecent|FullyQualifiedName~MainWindowReaderShortcut|FullyQualifiedName~OfflineRuntimeTests|FullyQualifiedName~MainWindowSignature"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 8: Commit Task 8.**

```bash
git add src/SGPdf.App/Features/Reader/RecentPdfStore.cs src/SGPdf.App/Features/Reader/RecentPdfFileOps.cs src/SGPdf.App/MainWindow.ReaderShortcuts.cs src/SGPdf.App/MainWindow.Reader.cs src/SGPdf.App/MainWindow.Sign.cs src/SGPdf.App/MainWindow.xaml tests/SGPdf.App.Tests/RecentPdfStoreTests.cs tests/SGPdf.App.Tests/MainWindowRecentFilesTests.cs tests/SGPdf.App.Tests/MainWindowReaderShortcutTests.cs tests/SGPdf.App.Tests/OfflineRuntimeTests.cs
git commit -m "feat(reader): add shortcuts and recent files"
```

---

### Task 9: F4 Closure Audit + Documentation + Exact-Head CI + Draft PR

**Files:**
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `.planning/REQUIREMENTS.md`
- Create/modify: `.planning/phases/05-f4-full-reader/PLAN.md`
- Create: `docs/history/2026-10-08-F4.md`
- No product change unless audit finds a real spec/test defect; if it does, return to the owning task and re-run RED/GREEN before closure.

**Interfaces:** none; closure/evidence only.

- [ ] **Step 1: Run final exact local/CI-equivalent commands from the functional head.**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require 0 build errors; existing + F4 tests all PASS.

- [ ] **Step 2: Audit scope against F3.4 closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.**

Verify:

- no new runtime NuGet/package lock changes unless explicitly approved after design return;
- no second PDF engine;
- no WebView/HTTP client/network service;
- `PdfVisualSignatureWriter`, `SignatureEditState`, `SignatureCoordinateMapper`, F3 photo/ink/library semantics remain unchanged except narrowly necessary mode/shortcut integration;
- ZPL/Labelize/PDFsharp label runtime code unchanged;
- no multi-document tabs;
- no eager all-page full-resolution or thumbnail cache;
- no password persistence;
- no unsafe PDF action execution.

- [ ] **Step 3: Write closure evidence and requirement status.**

Mark READER-01..07 automated PASS only when their owning tests and exact-head CI exist. Manual Windows QA remains **NOT RUN** unless actually performed.

History must record each slice RED/green CI run IDs, final functional head, final test count, scope audit and unresolved manual QA.

- [ ] **Step 4: Commit documentation only.**

```bash
git add .planning/STATE.md .planning/ROADMAP.md .planning/REQUIREMENTS.md .planning/phases/05-f4-full-reader/PLAN.md docs/history/2026-10-08-F4.md
git commit -m "docs(reader): close F4 full reader"
```

- [ ] **Step 5: Require exact-head GitHub Actions PASS on the closure-doc commit.**

Require hygiene + Labelize staging + locked restore + Release build + full test step all PASS against the exact closure SHA.

- [ ] **Step 6: Open one draft stacked PR.**

- Base: `feat/f3-4-local-signature-library`
- Head: `feat/f4-full-reader`
- Title: `F4 — Full Reader`
- Draft: `true`
- Body: summarize slices, exact functional/closure heads, CI evidence, automated PASS vs manual Windows QA **NOT RUN**, and explicit “do not merge to main without user approval”.

- [ ] **Step 7: Require PR-triggered CI PASS on the same closure head.**

Keep PR draft/open/unmerged.

---

## Slice Execution Order / Approval Gates

Implementation is deliberately incremental:

```text
Task 1  native capability + pure geometry
  -> review/CI gate
Task 2  continuous WPF reader + FIRMAR boundary
  -> review/CI gate
Task 3  thumbnails
  -> review/CI gate
Task 4  password PDFs
  -> review/CI gate
Task 5  text core + find
  -> review/CI gate
Task 6  selection/copy
  -> review/CI gate
Task 7  bookmarks/links
  -> review/CI gate
Task 8  shortcuts/recents/hardening
  -> review/CI gate
Task 9  closure docs + exact-head CI + draft PR
```

Do not bundle multiple tasks into one uncontrolled implementation batch. The project convention remains medium-sized `continúa` gates: after plan approval, execute **Task 1 only**, report RED/GREEN evidence, then wait for the next `continúa` before Task 2 unless the user explicitly changes that cadence.

## Self-Review Results

### Spec coverage

- Section 3 LEER/FIRMAR/ZPL boundary -> Task 2 regressions.
- F4.1 page metrics/layout/current page/navigation/zoom/memory/scheduling/per-page error -> Tasks 1–2.
- F4.2 thumbnails -> Task 3.
- F4.3 password classification/privacy/candidate-first -> Task 4.
- F4.4 PDFium search/find UI/one-page selection/copy -> Tasks 5–6.
- F4.5 bookmark bounds + explicit safe links -> Task 7.
- F4.6 shortcuts/recents/corruption/offline hardening -> Task 8.
- Closure/manual separation/no merge -> Task 9.
- No spec requirement is intentionally left without an owning task.

### Type consistency

- Neutral PDF types (`PdfPageSize`, `PdfTextRect`, `PdfTextMatch`, bookmark/link models) live in `Pdf`, not under Sign/Reader, preventing dependency from PDF layer into a feature.
- Reader-specific geometry/search/selection state consumes PDF contracts but does not own native handles.
- MainWindow seams use the exact contracts produced by earlier tasks.
- F3 keeps `SignatureEditState` and `PdfRect`; F4 does not reuse or relocate those Sign-specific types.

### Review-focus coverage

All five Review Focus items have named tests in their owning tasks.

### Proportion / YAGNI

The plan intentionally does **not** add tabs, OCR, all-results search, regex, cross-page drag selection, plain-text web-link detection, LRU cache framework, custom scrolling engine, app-wide MVVM/DI, progressive PDFium rendering or another PDF SDK. Any requirement for those is a stop-and-return-to-design condition.

## Execution Method

Use the project's established **Native / task-by-task** workflow in this session: TDD RED -> confirm expected failure -> minimal GREEN -> focused regression -> full CI -> report evidence -> next user `continúa`. This preserves the user's requested medium-size cadence and the existing stacked-branch audit trail.
