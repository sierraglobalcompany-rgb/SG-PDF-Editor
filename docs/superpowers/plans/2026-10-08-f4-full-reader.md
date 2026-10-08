# F4 Full Reader Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evolve the existing F0 single-page reader into a continuous, virtualized, offline reader with thumbnails, password support, text search/copy, bookmarks/links, shortcuts and recent files while preserving F2 ZPL and the existing F3 single-active-page signing architecture.

**Architecture:** Keep one `PdfDocumentSession` and PDFium as the only PDF engine. `LEER` receives a virtualized continuous surface driven by lightweight page geometry and a visible+one-neighbor render window; `FIRMAR` keeps the existing single `PdfImage` + `SignatureEditState` path. Reader code stays feature-local under `Features/Reader` plus focused `MainWindow.Reader*.cs` partials.

**Tech Stack:** C# / .NET 10 / WPF / PDFium `bblanchon.PDFium.Win32` 156.0.8076 / xUnit. No new runtime NuGet dependency.

**Spec:** `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`

## Global Constraints

- Windows x64 + C#/.NET 10 + WPF.
- PDFium remains the only PDF engine; verify required exports against the pinned binary before using them.
- Every PDFium call stays behind `PdfiumRuntime.NativeGate`.
- No HTTP client, WebView2, account, telemetry, network service, database or new commercial runtime dependency.
- `LEER` = continuous virtualized reader; `FIRMAR` = existing single-active-page editor.
- Full-resolution page retention = visible pages + at most one neighbor before and after.
- Thumbnails are lazy; never eagerly render the whole document.
- Search is page-by-page on demand; no OCR or permanent index.
- Text drag selection is one page at a time.
- Bookmarks are read-only and bounded to 10,000 nodes / depth 128.
- Execute only internal GOTO and confirmed `http`/`https` URI links; never Launch, JavaScript, remote-GOTO or arbitrary shell/file actions.
- Passwords are never persisted.
- Recents = max 10 paths + UTC timestamp, LocalAppData, atomic JSON, no startup/menu path probing.
- PageUp/PageDown = one viewport-height scroll in continuous `LEER`.
- No multi-document tabs in F4.
- Existing F2/F3 tests remain regression gates.
- No merge to `main` without explicit user approval.

## Review Focus

1. **Rapid scroll/zoom with stale render completion** -> stale bitmap/transform must not publish. Test in Task 2: `StaleReaderGeneration_DoesNotPublishBitmapOrTransform`.
2. **One page render fails** -> only that page becomes an error card; session/other pages remain usable. Test in Task 2: `PerPageRenderFailure_IsolatedAndSessionRemainsActive`.
3. **Wrong password/cancel over an existing document** -> prior workspace remains unchanged. Test in Task 4: `PasswordWrongOrCancel_PreservesExistingWorkspace`.
4. **Malformed/cyclic bookmarks** -> traversal stops at cycle/depth/node bounds while prior valid nodes remain. Test in Task 7: `BookmarkTraversal_CycleDepthAndNodeLimitsAreBounded`.
5. **Stale/UNC-looking recent path** -> building the menu must not probe it. Test in Task 8: `RecentMenu_LoadsStoredPathWithoutFileSystemProbe`.

---

## Locked File Boundaries

Reader feature files:

- `Features/Reader/ReaderPageGeometry.cs` — immutable layout/window contracts.
- `Features/Reader/ReaderLayoutPlanner.cs` — pure geometry/current-page/render-window logic.
- `Features/Reader/ReaderPageItem.cs` — WPF-facing full-page slot state.
- `Features/Reader/ReaderThumbnailItem.cs` — WPF-facing thumbnail state.
- `Features/Reader/ReaderSearchNavigator.cs` — pure next/previous/wrap search state.
- `Features/Reader/ReaderTextSelection.cs` — selection range/state only.
- `Features/Reader/ReaderBookmarkTraversalGuard.cs` — cycle/depth/node guard.
- `Features/Reader/RecentPdfStore.cs` + `RecentPdfFileOps.cs` — recents only.

PDF additions:

- `Pdf/PdfPageSize.cs`
- `Pdf/PdfTextModels.cs`
- `Pdf/PdfNavigationModels.cs`
- `Pdf/PdfDocumentOpenException.cs`
- `Pdf/PdfDocumentSession.Text.cs`
- `Pdf/PdfDocumentSession.Navigation.cs`

WPF integration:

- `MainWindow.Reader.cs`
- `MainWindow.ReaderThumbnails.cs`
- `MainWindow.ReaderSearch.cs`
- `MainWindow.ReaderSelection.cs`
- `MainWindow.ReaderLinks.cs`
- `MainWindow.ReaderShortcuts.cs`
- `PdfPasswordDialog.xaml/.cs`

Do not add ViewModel/service-container/repository/event-bus layers.

---

### Task 1 — F4.1 Native Capability Gate + Pure Continuous Layout

**Files**
- Create: `src/SGPdf.App/Pdf/PdfPageSize.cs`
- Create: `src/SGPdf.App/Features/Reader/ReaderPageGeometry.cs`
- Create: `src/SGPdf.App/Features/Reader/ReaderLayoutPlanner.cs`
- Modify: `src/SGPdf.App/Pdf/PdfiumNative.cs`
- Modify: `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- Test: `tests/SGPdf.App.Tests/PdfiumReaderApiAvailabilityTests.cs`
- Test: `tests/SGPdf.App.Tests/ReaderLayoutPlannerTests.cs`
- Modify test: `tests/SGPdf.App.Tests/PdfRenderTests.cs`

**Produces**

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

- [ ] **Step 1: Add pinned-PDFium capability test before product changes.**

`PinnedPdfium_ExportsAllF4RequiredStableFunctions` asserts exports for `FPDF_GetPageSizeByIndexF`, required `FPDFText_*`, bookmark/destination functions, `FPDFLink_Enumerate/GetAnnotRect/GetDest/GetAction`, and `FPDFAction_GetType/GetDest/GetURIPath`.

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PdfiumReaderApiAvailabilityTests"
```

Expected: PASS. Missing required export = **stop and return to design**, not a reason to add another engine or weaken the test.

- [ ] **Step 2: Write RED tests.**

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

Run focused tests and require expected missing-contract failure.

- [ ] **Step 3: Implement minimum GREEN.**

Add only page-size native binding needed here. `GetPageSizes` keeps page order, validates positive finite sizes, honors cancellation between reads, and uses `NativeGate`. `ReaderLayoutPlanner` uses existing `PdfZoomState.ResolveDpi`; visible pages precede neighbors in render priority.

- [ ] **Step 4: Verify + commit.**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
git commit -am "feat(reader): add continuous layout primitives"
```

---

### Task 2 — F4.1 Continuous WPF Surface + Bounded Rendering + FIRMAR Boundary

**Files**
- Create: `src/SGPdf.App/Features/Reader/ReaderPageItem.cs`
- Create: `src/SGPdf.App/MainWindow.Reader.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.xaml.cs`
- Modify narrowly: `src/SGPdf.App/MainWindow.Sign.cs`
- Test: `tests/SGPdf.App.Tests/ReaderPageItemTests.cs`
- Test: `tests/SGPdf.App.Tests/MainWindowReaderTests.cs`

**Produces**

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

Named WPF controls fixed by the plan:
- `ReaderContinuousSurface` — container shown in `LEER`.
- `ReaderPageList` — virtualized `ListBox` with its own scrolling; **do not wrap it in an external `ScrollViewer`**.

Use recycling virtualization and pixel scrolling. Keep existing `PdfScrollViewer` + `PdfImage` exclusively for page-local edit/sign.

MainWindow seams:

```csharp
private Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>> _getReaderPageSizes;
private Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage> _renderReaderPage;
private Task<bool> TryOpenPdfPathAsync(string path, string? password = null);
private void InitializeContinuousReader(IReadOnlyList<PdfPageSize> sizes);
private void RebuildReaderGeometry(bool preserveCurrentPageAnchor);
private Task RefreshReaderRenderWindowAsync();
private void UpdateReaderCurrentPageFromViewport();
private void ScrollReaderToPage(int pageIndex);
private Task EnsureEditSurfaceForCurrentPageAsync();
```

Use one `_readerRenderScheduler`; process `RenderPriority` sequentially. Never Task-per-page fanout.

- [ ] **Step 1: Write RED page-item tests.**

`PageItem_DefaultsToPlaceholderWithoutBitmap`, `Publish_SetsReadyBitmapAndTransform`, `ReleaseBitmap_ClearsFullResolutionDataButPreservesGeometry`, `MarkError_ClearsBitmapAndKeepsPageNavigable`.

- [ ] **Step 2: Write RED STA integration tests.**

`LeerMode_ShowsContinuousSurfaceAndHidesSinglePageEditSurface`, `ContinuousSurface_UsesRecyclingVirtualization`, `OpenPdf_PublishesSessionAfterPageMetricsWithoutEagerAllPageRender`, `ScrollCenter_UpdatesNavigationPageNumberAndPrintCurrentSemantics`, `PreviousNextAndPageNumber_ScrollWithoutRecreatingSession`, `Zoom_RebuildsGeometryKeepsCurrentPageAnchorAndReleasesStaleBitmaps`, `StaleReaderGeneration_DoesNotPublishBitmapOrTransform`, `PerPageRenderFailure_IsolatedAndSessionRemainsActive`, `ReaderRetention_ReleasesBitmapOutsideVisiblePlusNeighborWindow`, `EnterFirmar_UsesCurrentReaderPageAndExistingSinglePageSurface`, `ReturnLeer_PreservesCurrentPageAndExistingSignatureDirtyGuard`, `PdfToZpl_HidesBothPdfReaderSurfacesAndKeepsExistingZplBehavior`.

Use existing reflection + STA style; inject render/page-size seams.

- [ ] **Step 3: Implement minimum GREEN.**

Route navigation/zoom by mode. Candidate PDF open publishes only after session + lightweight page metrics succeed. Entering `FIRMAR` renders current reader page onto existing `PdfImage`; do not modify `SignatureEditState`, `SignatureCoordinateMapper`, `AddSignatureAsset(...)` or writer semantics.

- [ ] **Step 4: Verify focused F4.1 + F3/ZPL + full suite; commit.**

Commit: `feat(reader): add continuous virtualized reading`.

---

### Task 3 — F4.2 Lazy Thumbnails + Left Navigation

**Files**
- Create: `Features/Reader/ReaderThumbnailItem.cs`
- Create: `MainWindow.ReaderThumbnails.cs`
- Modify: `MainWindow.xaml`
- Test: `ReaderThumbnailTests.cs`
- Test: `MainWindowReaderThumbnailTests.cs`

**Produces**

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

Named controls: `ReaderNavigationTabs`, `ReaderPagesTab`, `ReaderBookmarksTab`, `ReaderThumbnailList`, `ReaderBookmarksTree`.

- [ ] **Step 1: RED tests.**

`ThumbnailDpi_Targets132PixelDisplayWidth`, `ThumbnailItem_ReleaseDropsBitmap`, `PagesTab_IsVirtualizedAndBookmarksTabExists`, `ThumbnailRequests_AreLazyForRealizedWindowOnly`, `ThumbnailClick_NavigatesExactlyOnce`, `ReaderScroll_UpdatesSelectedThumbnailWithoutRecursiveNavigation`, `ThumbnailRenderFailure_LeavesMainReaderUsable`, `RapidThumbnailScroll_DropsStalePublication`.

- [ ] **Step 2: GREEN.**

Separate `_thumbnailRenderScheduler`; render realized/near-visible rows sequentially. Never iterate every page to render thumbnails. Full-page reader work remains independent/higher priority.

- [ ] **Step 3: Full regression; commit `feat(reader): add lazy page thumbnails`.**

---

### Task 4 — F4.3 Password-Protected PDFs

**Files**
- Create: `Pdf/PdfDocumentOpenException.cs`
- Modify: `Pdf/PdfDocumentSession.cs`
- Create: `PdfPasswordDialog.xaml/.cs`
- Modify: `MainWindow.Reader.cs`
- Test: `PdfPasswordTests.cs`, `PdfPasswordDialogTests.cs`, `MainWindowReaderPasswordTests.cs`

**Produces**

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

private Func<Window, string, string?> _requestPdfPassword;
```

- [ ] **Step 1: RED native/open tests.**

Using a protected synthetic fixture with user password `secret`: `ProtectedPdf_OpenWithoutPassword_ThrowsTypedPasswordError`, `ProtectedPdf_OpenWrongPassword_ThrowsTypedPasswordError`, `ProtectedPdf_OpenCorrectPassword_SucceedsAndRenders`, `InvalidPdf_StillClassifiesAsOtherPdfiumError`, `PasswordException_DoesNotExposePasswordValue`.

- [ ] **Step 2: RED UI tests.**

`PasswordDialog_UsesMaskedPasswordBox`, `PasswordPrompt_CancelReturnsNull`, `PasswordWrongOrCancel_PreservesExistingWorkspace`, `PasswordRetryCorrect_PublishesCandidateOnce`, `PasswordValue_IsNotStoredInStatusTitleOrPersistentState`.

- [ ] **Step 3: GREEN.**

Map PDFium error `4` to typed password classification. Prompt/retry until success or cancel. Candidate failure/cancel never replaces prior session/navigation. No remember-password feature.

- [ ] **Step 4: Full regression; commit `feat(reader): support password protected PDFs`.**

---

### Task 5 — F4.4 PDFium Text Core + Find Navigation

**Files**
- Create: `Pdf/PdfTextModels.cs`, `Pdf/PdfDocumentSession.Text.cs`
- Modify: `Pdf/PdfiumNative.cs`, make session partial as needed
- Create: `Features/Reader/ReaderSearchNavigator.cs`
- Create: `MainWindow.ReaderSearch.cs`
- Modify: `MainWindow.xaml`
- Test: `PdfTextTests.cs`, `ReaderSearchNavigatorTests.cs`, `MainWindowReaderSearchTests.cs`

**Produces**

```csharp
public readonly record struct PdfTextRect(double Left, double Bottom, double Right, double Top);

public sealed record PdfTextMatch(
    int PageIndex,
    int StartIndex,
    int CharacterCount,
    IReadOnlyList<PdfTextRect> Rects);

public partial class PdfDocumentSession
{
    public IReadOnlyList<PdfTextMatch> FindTextOnPage(int pageIndex, string query, CancellationToken cancellationToken = default);
    public int GetCharacterIndexAtPoint(int pageIndex, double pdfX, double pdfY, double xTolerance, double yTolerance, CancellationToken cancellationToken = default);
    public string GetTextRange(int pageIndex, int startIndex, int characterCount, CancellationToken cancellationToken = default);
    public IReadOnlyList<PdfTextRect> GetTextRangeRects(int pageIndex, int startIndex, int characterCount, CancellationToken cancellationToken = default);
}

internal enum ReaderSearchDirection { Forward, Backward }
internal sealed record ReaderSearchCursor(string Query, int PageIndex, int MatchIndexWithinPage);
internal sealed record ReaderSearchResult(PdfTextMatch Match, ReaderSearchCursor Cursor);

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

Named controls: `ReaderFindBar`, `ReaderFindTextBox`, `ReaderFindPreviousButton`, `ReaderFindNextButton`, `ReaderFindCloseButton`, `ReaderFindStatusText`.

- [ ] **Step 1: RED PDFium text tests.**

`UnicodeText_ExtractsAccentsAndEnye`, `FindTextOnPage_IsCaseInsensitiveAndNonWholeWordByDefault`, `FindTextOnPage_ReturnsStartCountAndRects`, `ImageOnlyPage_ReturnsNoTextMatches`, `GetTextRange_ReturnsUnicodeWithoutTrailingGarbage`, `GetCharacterIndexAtPoint_OutsideTextReturnsMinusOne`, `TextOperations_PreCanceledToken_AbortBeforePublication`.

- [ ] **Step 2: RED pure navigator tests.**

Forward/back current-page, later-page, single wrap, no-result visits each page at most once, query change resets cursor, cancellation stops between pages.

- [ ] **Step 3: RED find-bar STA tests.**

Ctrl+F open/focus, Enter next, Shift+Enter previous, Escape close, active match navigates/highlights only active result, no-results controlled state, query generation rejects stale result, entering FIRMAR hides reader search UI without changing signature state.

- [ ] **Step 4: GREEN.**

Native text handles never escape `PdfDocumentSession`; all operations under `NativeGate`. PDFium search flags = case-insensitive + non-whole-word (no match-case/whole-word flag). Search runs page-by-page off UI thread; no global result cache.

- [ ] **Step 5: Full regression; commit `feat(reader): add PDF text search`.**

---

### Task 6 — F4.4 One-Page Text Selection + Clipboard Copy

**Files**
- Create: `Features/Reader/ReaderTextSelection.cs`
- Create: `MainWindow.ReaderSelection.cs`
- Modify narrowly: `MainWindow.Reader.cs`
- Test: `ReaderTextSelectionTests.cs`, `MainWindowReaderSelectionTests.cs`

**Produces**

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

private Action<string> _setReaderClipboardText = static text => Clipboard.SetText(text);
```

Reader selection maps using the page's `PdfPageDeviceTransform`; do not reuse/move Sign-specific `PdfRect` or `SignatureCoordinateMapper`.

- [ ] **Step 1: RED pure tests.**

Forward/back drag same range, missing hit -> null, same char -> one-character range.

- [ ] **Step 2: RED STA tests.**

One-page drag creates selection/rect overlay; cross-page drag does not; click elsewhere clears; zoom reprojects from PDF rects; Ctrl+C copies exact Unicode; no selection does nothing; entering FIRMAR clears reader selection only; selection drag takes precedence over link activation.

- [ ] **Step 3: GREEN + full regression; commit `feat(reader): add page text selection and copy`.**

---

### Task 7 — F4.5 Bookmarks + Explicit Safe Links

**Files**
- Create: `Pdf/PdfNavigationModels.cs`, `Pdf/PdfDocumentSession.Navigation.cs`
- Modify: `Pdf/PdfiumNative.cs`
- Create: `Features/Reader/ReaderBookmarkTraversalGuard.cs`
- Create: `MainWindow.ReaderLinks.cs`
- Populate existing bookmarks tree in reader navigation UI
- Test: `PdfNavigationTests.cs`, `ReaderBookmarkTraversalTests.cs`, `MainWindowReaderLinksTests.cs`

**Produces**

```csharp
public sealed record PdfBookmarkNode(string Title, int? DestinationPageIndex, IReadOnlyList<PdfBookmarkNode> Children);

public enum PdfLinkActionKind { None, InternalGoto, Uri, Unsupported }

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

MainWindow seams: `_confirmExternalUri` and `_openExternalUri`. Default scheme checks use `Uri.UriSchemeHttp` / `Uri.UriSchemeHttps`, avoiding literal remote URLs and network-client dependencies.

- [ ] **Step 1: RED bounded traversal tests.**

`BookmarkTraversal_CycleDepthAndNodeLimitsAreBounded`, repeated handle rejected, depth 129 rejected, node 10001 rejected, and rejecting a later cyclic handle does not discard already accepted valid nodes.

- [ ] **Step 2: RED PDF navigation tests.**

Bookmark hierarchy/title/destination; unsupported bookmark remains visible; internal link target+rect; URI extracted without launching; Launch/JavaScript classified unsupported.

- [ ] **Step 3: RED UI safety tests.**

Bookmark/internal-link navigation, URI explicit confirmation, cancel no action, non-http/https never opens, unsupported action no-op, selection drag suppresses link activation.

- [ ] **Step 4: GREEN + full regression; commit `feat(reader): add bookmarks and safe PDF links`.**

No plain-text URL auto-detection, remote GOTO, Launch or JavaScript.

---

### Task 8 — F4.6 Shortcuts + Atomic Recent Files + Hardening

**Files**
- Create: `Features/Reader/RecentPdfStore.cs`, `RecentPdfFileOps.cs`
- Create: `MainWindow.ReaderShortcuts.cs`
- Modify: `MainWindow.xaml`, `MainWindow.Reader.cs`, narrowly `MainWindow.Sign.cs`
- Modify test: `OfflineRuntimeTests.cs`
- Test: `RecentPdfStoreTests.cs`, `MainWindowReaderShortcutTests.cs`, `MainWindowRecentFilesTests.cs`

**Produces**

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

Storage path: `%LOCALAPPDATA%\SG PDF Editor\recent-files.json`. Root is created lazily only on a successful mutation.

Exact manifest v1:

```json
{
  "version": 1,
  "items": [
    {
      "fullPath": "C:\\Docs\\sample.pdf",
      "lastOpenedUtc": "2026-10-08T20:00:00Z"
    }
  ]
}
```

Identity = normalized full Windows path, `OrdinalIgnoreCase`; max 10; newest first. `Load()` reads only this JSON and never probes stored target paths. Corrupt JSON returns empty/unavailable for that read and is not rewritten merely by load; a later successful open may rebuild valid v1 atomically.

Named controls: `RecentFilesMenuItem`, `ClearRecentFilesMenuItem`.

Shortcut entry:

```csharp
private bool TryHandleReaderShortcut(KeyEventArgs e);
```

Existing signature dirty Enter/Delete guards run first. Reader shortcuts must not override editable text controls.

- [ ] **Step 1: RED recents tests.**

Missing manifest no creation; normalize/dedupe/move-to-top; cap 10; stores only path+UTC; corrupt non-destructive load; successful open after corruption rebuilds atomically; publication failure leaves old manifest; clear metadata only; missing/UNC-looking stored paths remain unprobed.

- [ ] **Step 2: RED menu tests.**

`RecentMenu_LoadsStoredPathWithoutFileSystemProbe`, record only after successful candidate publication, failed/cancelled open not recorded, recent click reuses candidate-first open, selected missing recent leaves current doc and can remove entry, clear recents does not touch PDFs.

- [ ] **Step 3: RED shortcut tests.**

Ctrl+O/P/F/C; Ctrl +/-/0; Home/End; PageUp/PageDown exactly one viewport; editable controls preserve normal semantics; signature Delete and dirty-Enter guards still win. Do not bind F3/Shift+F3 unless a separate no-conflict test proves them free.

- [ ] **Step 4: GREEN + offline hardening.**

Extend `OfflineRuntimeTests`: runtime packages unchanged; no HttpClient/WebView/network client/remote URL dependency. External browser opening remains explicit OS shell after confirmation.

- [ ] **Step 5: Full regression; commit `feat(reader): add shortcuts and recent files`.**

---

### Task 9 — F4 Closure Audit + Documentation + Exact-Head CI + Draft PR

**Files**
- Modify: `.planning/STATE.md`, `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md`
- Create/update: `.planning/phases/05-f4-full-reader/PLAN.md`
- Create: `docs/history/2026-10-08-F4.md`

- [ ] **Step 1: Run final functional verification.**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 2: Audit diff against F3.4 closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.**

Require: no new runtime package/second PDF engine/WebView/network service/tabs/eager whole-document bitmap cache/password persistence/unsafe PDF actions. Confirm F3 placement/writer/photo/ink/library semantics and ZPL runtime remain unchanged except narrow mode/shortcut integration.

- [ ] **Step 3: Close docs only after evidence exists.**

Mark READER-01..07 automated PASS only with corresponding tests + CI. Record every slice RED/GREEN run, functional head, test count and unresolved manual Windows QA as **NOT RUN**.

- [ ] **Step 4: Commit closure docs and require exact-head push CI PASS.**

- [ ] **Step 5: Open draft stacked PR.**

Base `feat/f3-4-local-signature-library`, head `feat/f4-full-reader`, title `F4 — Full Reader`, draft=true, no merge.

- [ ] **Step 6: Require PR-triggered CI PASS on the same closure SHA.**

---

## Execution Order / User Gates

```text
Task 1  capability + pure geometry
Task 2  continuous WPF reader + FIRMAR boundary
Task 3  thumbnails
Task 4  password PDFs
Task 5  text core + find
Task 6  selection/copy
Task 7  bookmarks/links
Task 8  shortcuts/recents/hardening
Task 9  closure docs + CI + draft PR
```

Each task is independent RED -> confirm expected failure -> minimal GREEN -> focused regression -> full CI -> evidence. Do not bundle tasks. Per established project cadence, after this plan is approved, execute **Task 1 only** and wait for the next `continúa` before Task 2 unless the user explicitly changes cadence.

## Self-Review Results

**Spec coverage:** Sections 3–4 and F4.1 through F4.6 all map to Tasks 1–8; closure/manual separation maps to Task 9. No spec requirement is intentionally unowned.

**Type consistency:** `PdfPageSize`, `PdfTextRect`, `PdfTextMatch`, `PdfBookmarkNode` and `PdfPageLink` stay neutral in `Pdf`; reader state consumes them. `ReaderSearchResult` is explicitly defined with its cursor. F3 retains its own `PdfRect`/`SignatureEditState`; reader does not move or reuse them.

**Review Focus:** all five high-risk cases have named tests in the owning task.

**YAGNI:** no tabs, OCR, all-results search, regex, cross-page selection, plain-text URL detection, LRU framework, custom scrolling engine, progressive rendering, app-wide MVVM/DI or second PDF SDK.

## Execution Method

Use the established **Native / task-by-task** workflow in this session: TDD RED -> verify expected failure -> minimal GREEN -> regression -> exact-head CI -> report evidence -> next user `continúa`.
