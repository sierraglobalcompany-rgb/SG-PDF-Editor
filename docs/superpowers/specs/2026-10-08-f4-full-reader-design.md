# F4 — Full Reader / Lector Completo — Design Specification

**Date:** 2026-10-08  
**Status:** WRITTEN — awaiting user review  
**Branch:** `feat/f4-full-reader`  
**Base:** F3.4 closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`  
**Product:** SG PDF Editor — Windows x64, C#/.NET 10, WPF, local-first/offline

## 1. Purpose

F4 turns the existing F0 single-page PDF reader into the normal daily reading experience for SG PDF Editor without replacing PDFium, introducing another PDF engine, or breaking the visual-signature workflow delivered in F3.

Success means a user can open a normal or password-protected PDF and read it naturally with continuous vertical scrolling, thumbnails, bookmarks, internal/external links, text search/copy, keyboard shortcuts and recent files while the application remains local-first, responsive and memory-bounded.

F4 is a reader phase. It does not add page organization, OCR, text editing, annotations, cryptographic signing, multi-document tabs or cloud features.

## 2. Existing baseline and constraints

The current application already has:

- `PdfDocumentSession` owning the native PDFium document handle;
- `PdfiumRuntime.NativeGate`, globally serializing native PDFium calls;
- `PdfRenderScheduler` with latest-request-wins behavior for the current page;
- `PageNavigationState`;
- `PdfZoomState` with 25–400% manual zoom plus Fit Page and Fit Width;
- a central WPF `ScrollViewer` containing one `PdfImage`;
- Windows printing based on the current PDF session;
- F3 visual signatures layered on the current single `PdfImage` through `SignatureEditState` and `SignatureOverlayCanvas`;
- F2 ZPL preview sharing the same central area through a separate canvas.

Non-negotiable project constraints remain:

1. Windows x64, C#/.NET 10 and WPF.
2. PDFium remains the primary PDF engine.
3. All PDFium calls remain behind `PdfiumRuntime.NativeGate`.
4. No cloud, account, API key, telemetry or network dependency for normal operation.
5. No new commercial runtime dependency.
6. KISS/YAGNI: no preventive MVVM framework, DI container, event bus, repository abstraction or plugin framework.
7. No automatic merge to `main`.
8. Existing F2 and F3 behavior must not be silently regressed.

## 3. Product UX model

The final product mode concept remains:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

F4 changes only `LEER`.

### 3.1 LEER mode

`LEER` becomes a continuous vertical PDF viewer. Multiple page cards may be visible. Only visible pages and a small neighbor window are rendered at full viewing resolution.

The left panel becomes reader navigation with:

```text
[Páginas] [Marcadores]
```

The existing top page/zoom controls remain familiar rather than being redesigned wholesale.

### 3.2 FIRMAR compatibility boundary

F3 visual signing remains a single-active-page editor in F4.

When the user enters `FIRMAR` from the continuous reader:

1. the current reader page becomes the active signing page;
2. the continuous reader surface is hidden;
3. the existing single-page `PdfImage` surface is shown/rendered;
4. the existing `SignatureOverlayCanvas`, `SignatureEditState`, coordinate mapping and `AddSignatureAsset(...)` path remain authoritative.

When the user returns to `LEER`:

1. unresolved-signature guards keep their current behavior;
2. the single-page signing surface is hidden;
3. the continuous reader returns to the same active page;
4. exact sub-page pixel offset restoration is desirable but is not a hard F4 acceptance requirement.

This boundary is deliberate. F4 must not create a second signature placement model or migrate signature overlays into every continuous page card.

While `FIRMAR` is active, existing previous/next/page-number navigation continues to use the single-page path and existing dirty guards. Continuous scrolling is a `LEER` behavior.

### 3.3 ZPL compatibility boundary

Opening ZPL continues to switch to the existing ZPL surface. Both the continuous PDF reader and single-page PDF editor are hidden while ZPL preview is active. F4 must not move label rendering into the reader architecture.

## 4. Recommended architecture

F4 evolves the existing application rather than replacing it.

### 4.1 Central surfaces

The center area will contain three mutually exclusive surfaces:

```text
PDF read  -> continuous virtualized reader
PDF edit  -> existing single-page PdfImage + overlays
ZPL       -> existing LabelSheetCanvas / label preview
```

A new continuous reader control/list is added beside the existing `PdfImage`; the existing `PdfImage` is retained for F3 and future page-local editing modes.

### 4.2 Feature structure

Feature-specific reader code may live under:

```text
src/SGPdf.App/Features/Reader/
```

Small WPF integration may remain in a `MainWindow.Reader.cs` partial, following the existing `MainWindow.Sign.cs` / `MainWindow.Labels.cs` pattern.

Do not create new projects or framework layers.

### 4.3 PdfDocumentSession growth

`PdfDocumentSession` remains the sole owner of the native document handle. F4 may make it `partial` and split native-reader operations by concern, for example:

```text
PdfDocumentSession.cs
PdfDocumentSession.Text.cs
PdfDocumentSession.Navigation.cs
```

This is preferred over exposing the raw `IntPtr` document handle to reader services.

All new P/Invoke declarations remain minimal and feature-driven in `PdfiumNative.cs` or narrowly split native files if the single file becomes materially difficult to maintain.

## 5. F4.1 — Continuous Reader Core

### 5.1 Goal

Replace single-page reading in `LEER` with smooth continuous vertical scrolling while preserving navigation, zoom, printing and the single-page editor used by F3.

### 5.2 Page metadata

On successful document open, the reader obtains lightweight geometry for every page using PDFium page-size metadata, preferably `FPDF_GetPageSizeByIndexF` rather than fully loading/rendering every page.

Each reader page slot needs only:

- zero-based page index;
- PDF width/height in points;
- calculated display width/height for the active zoom mode;
- current full-page bitmap, if realized/rendered;
- current `PdfPageDeviceTransform`, if rendered;
- render state: placeholder / loading / ready / error.

No full document bitmap pre-render is allowed.

### 5.3 WPF virtualization

Use WPF virtualization/recycling rather than one permanent `Image` per page. A standard WPF virtualized list/panel is preferred over a custom scrolling engine.

Page slots render as white page cards over the existing neutral viewer background. Placeholders preserve page geometry so the scrollbar remains stable before the bitmap arrives.

The virtualized view must support mixed portrait/landscape/page sizes.

### 5.4 Current-page definition

In continuous mode, the current page is the page whose displayed page rectangle contains the vertical center of the viewport. If the viewport center falls in an inter-page gap, choose the nearest page rectangle.

The current page drives:

- `PageNavigationState` / page-number display;
- status text;
- selected thumbnail;
- Print -> Current page;
- the page chosen when entering `FIRMAR`.

Scrolling updates current-page state without triggering a replacement render of the whole viewer.

### 5.5 Navigation semantics

In `LEER`:

- Previous = scroll to the previous page;
- Next = scroll to the next page;
- typed page number + Enter = scroll to that page;
- programmatic navigation aligns the target page predictably near the top of the viewport with normal page margin;
- navigation does not recreate the document session.

In `FIRMAR`, these controls continue through the existing single-page navigation path.

### 5.6 Zoom semantics

The existing `PdfZoomState` and 25–400% limits remain authoritative.

Manual zoom / 100%:
- one common DPI/scale applies across pages.

Fit Width:
- resolve display scale per page from that page's own width and the current viewport width;
- pages of different physical widths may therefore have different resolved DPI while each fits the reader width.

Fit Page:
- resolve display scale per page from its own dimensions and the current viewport dimensions;
- continuous mode remains active; this button does not switch back to single-page reading.

The visible zoom percentage shown by the UI in a fit mode reflects the current page's resolved DPI.

A zoom or fit change:

1. updates all page placeholder geometry;
2. invalidates stale full-page reader bitmaps;
3. preserves the current page as the navigation anchor;
4. rerenders only the active render window.

### 5.7 Full-page render window and memory policy

F4 does not introduce an unbounded bitmap cache.

At full viewing resolution, retain only:

- pages currently visible; plus
- at most one adjacent page before the visible range; plus
- at most one adjacent page after the visible range.

When a page leaves that window, its full-resolution `BitmapSource` is released from the page slot and may be rerendered if the user returns.

This count-bounded policy is intentionally simpler than an LRU/byte-budget subsystem. If real QA shows that very high zoom still causes unacceptable memory pressure, add a measured pixel/byte guard in the implementation plan rather than inventing a cache framework now.

### 5.8 Render scheduling

Reader rendering follows latest-view-wins behavior:

- scrolling/zoom/opening produces a new reader generation/request;
- stale pending page renders are canceled or ignored before UI publication;
- native `FPDF_RenderPageBitmap` remains synchronous and globally serialized by `NativeGate`;
- do not launch an unbounded Task per page;
- render visible pages before adjacent prefetch pages;
- thumbnail background work must never starve the visible full-page render path.

Progressive PDFium rendering remains out of scope unless physical performance evidence proves it necessary.

### 5.9 Per-page render failure

After a document has opened successfully, one page failing to render must not discard the whole document session. That page card shows a controlled error state and navigation can continue to other pages.

Opening a document remains candidate-first: failure to load the document/session leaves the previous document intact.

## 6. F4.2 — Thumbnails and left navigation

### 6.1 Páginas tab

The current left placeholder becomes a virtualized thumbnail list.

Each item shows:

- lazy thumbnail;
- page number;
- current-page selection state.

Clicking a thumbnail scrolls the continuous reader to that page. Reader scrolling updates thumbnail selection without recursively causing another navigation loop.

### 6.2 Thumbnail rendering

Thumbnails are low-resolution and independently lazy. Target display width should remain small (approximately 120–150 WPF pixels); the exact visual width belongs to implementation/UI tuning.

Do not pre-render thumbnails for the whole document.

Only realized/near-visible thumbnail rows request thumbnail renders. Stale thumbnail requests may be dropped. Full-page viewing renders have higher practical priority than thumbnail work.

Thumbnail bitmap lifetime is tied to a small realized/near-realized window; F4 does not maintain thousands of thumbnail bitmaps permanently.

### 6.3 Marcadores tab empty state

Before F4.5 bookmark support lands, the tab may be absent or show a controlled empty/coming-later state only inside the development slice. The final F4 UI must provide both `Páginas` and `Marcadores`.

## 7. F4.3 — Password-protected PDFs

### 7.1 Error classification

PDFium error code `FPDF_ERR_PASSWORD` (`4`) means password required or incorrect. F4 must surface this separately from malformed PDF/file errors.

`PdfDocumentSession.Open(...)` may retain its current public shape or gain a controlled exception/result type, but the UI must not parse human-readable exception text to detect password failure.

### 7.2 Open flow

```text
open without password
  -> success: publish candidate session
  -> password error: show password dialog
       -> retry with entered password
       -> success: publish candidate session
       -> password error: keep dialog open with controlled validation message
       -> cancel: keep prior document unchanged
  -> other error: show normal open error, keep prior document unchanged
```

The password field is masked.

### 7.3 Password privacy

F4 never writes a password to:

- recent-files storage;
- logs/status text;
- JSON/settings;
- crash/diagnostic artifacts intentionally created by the app.

The password exists only as needed for the current open attempt/session flow. It is not persisted across application restarts.

F4.3 guarantees reading/navigation/search/printing of a successfully opened protected document to the extent allowed by PDFium. It does **not** redesign encrypted-PDF save/edit/sign semantics. If an existing writer cannot reopen an encrypted source safely, that write path must fail in a controlled manner rather than storing credentials or silently bypassing security.

Owner-permission enforcement for editing is a later write-feature concern; F4 does not claim to unlock restricted editing.

## 8. F4.4 — Text search, selection and copy

### 8.1 PDFium APIs

Use PDFium text APIs, including the necessary subset of:

- `FPDFText_LoadPage` / `FPDFText_ClosePage`;
- `FPDFText_FindStart` / `FindNext` / `FindPrev` / `FindClose`;
- `FPDFText_GetSchResultIndex` / `FPDFText_GetSchCount`;
- `FPDFText_GetCharIndexAtPos`;
- `FPDFText_GetText`;
- `FPDFText_CountRects` / `FPDFText_GetRect`.

No OCR library enters F4. Image-only/scanned PDFs without a text layer legitimately return no searchable/selectable text; OCR remains F10.

### 8.2 Find UI

`Ctrl+F` opens a compact find bar in `LEER`.

Minimum controls:

- query box;
- previous match;
- next match;
- close.

Enter = next match. Shift+Enter = previous match. Escape closes the bar.

F4 uses case-insensitive, non-whole-word search by default. Case/whole-word/regex options are not part of F4.

### 8.3 Search strategy

Do not build a permanent whole-document search index.

Search is on-demand from the current page and current direction:

1. search the current page;
2. continue page-by-page in the requested direction;
3. wrap at most once at document ends;
4. stop when a match is found or the full document has been visited;
5. cancellation/query change invalidates the prior search generation.

The user does not need an all-results panel or exact global `N of M` count in F4. A controlled `Sin resultados` state is sufficient.

The active match scrolls into view and is highlighted. F4 does not require highlighting every match on the current page simultaneously.

### 8.4 Text selection

Mouse drag may select text **within one page at a time** in F4.

Flow:

1. map mouse/device coordinates to PDF page coordinates using the rendered page transform;
2. hit-test start/end character indexes;
3. normalize the character range;
4. obtain highlight rectangles with PDFium text-rect APIs;
5. render a selection overlay above that page;
6. `Ctrl+C` copies Unicode text returned by PDFium to the Windows clipboard.

Cross-page drag selection, double-click word semantics, paragraph selection and custom selection handles are explicitly deferred.

Clicking elsewhere or changing document clears the selection. Changing zoom rerenders the selection rectangles from PDF coordinates/character range rather than rasterizing the highlight into the page image.

### 8.5 Search/selection and FIRMAR

Text search/selection belongs to `LEER`. Entering `FIRMAR` hides/clears the reader selection UI; it does not mutate the document or signature state.

## 9. F4.5 — Bookmarks and links

### 9.1 Bookmarks

The `Marcadores` tab displays the PDF outline as a WPF tree.

Use PDFium bookmark APIs, including:

- `FPDFBookmark_GetFirstChild`;
- `FPDFBookmark_GetNextSibling`;
- `FPDFBookmark_GetTitle`;
- `FPDFBookmark_GetDest` and/or bookmark action APIs;
- `FPDFDest_GetDestPageIndex`.

Bookmark loading is read-only.

Malformed PDFs may contain circular bookmark references. Traversal must be bounded and cycle-safe. The implementation must track visited native bookmark handles and apply conservative maximum node/depth limits rather than recursing forever. Suggested design limits are 10,000 nodes and depth 128; implementation tests may tighten them but must not remove bounded traversal.

A bookmark with a valid in-document destination scrolls to that page. Unsupported bookmark actions remain visible if their title is valid but are not executed.

### 9.2 Explicit PDF links

F4.5 supports explicit PDF link annotations.

Minimum supported actions:

- internal document destination/GOTO -> scroll to target page;
- URI action -> explicit user confirmation, then optional OS browser launch.

Do not execute:

- `Launch` actions;
- remote-document GOTO actions;
- arbitrary file/shell actions;
- JavaScript actions;
- unsupported action types.

Plain-text URL auto-detection through `FPDFLink_LoadWebLinks` is optional/deferred and is not required for READER-03. Explicit PDF link annotations are the F4 acceptance target.

### 9.3 External URI safety

External navigation is never automatic.

Before opening an external URI:

1. require a user click on the link;
2. show the destination URI in a confirmation prompt;
3. allow only `http` and `https` schemes in F4;
4. only after confirmation call the OS shell/browser.

Cancel performs no external action.

This explicit action does not violate the product's offline-first rule: SG PDF Editor itself has no network dependency and never initiates link traffic without the user's request.

### 9.4 Link hit areas

Link annotation rectangles/quads are mapped through each rendered page's device transform and exposed as transparent/cursor-aware overlays. They must not alter the page bitmap.

When text selection drag is active, selection interaction takes precedence over accidental link activation.

## 10. F4.6 — Keyboard shortcuts, recent files and hardening

### 10.1 Required shortcuts

Minimum F4 shortcuts:

- `Ctrl+O` — open PDF;
- `Ctrl+P` — print;
- `Ctrl+F` — find;
- `Ctrl+C` — copy active reader text selection;
- `Ctrl++` / `Ctrl+-` — zoom in/out;
- `Ctrl+0` — 100% actual size;
- `Home` / `End` — first/last page when focus is not inside an editable text field;
- `PageUp` / `PageDown` — normal reader viewport/page scrolling without stealing keystrokes from text-entry controls;
- `F3` / `Shift+F3` may be used for next/previous find if it does not conflict with existing app behavior.

Existing text-entry behavior in page number, search, custom ZPL fields and dialogs takes precedence over global shortcuts where appropriate.

### 10.2 Recent files

Add `Archivo -> Recientes` with at most 10 successful PDF opens, newest first.

Persist only:

```json
{
  "version": 1,
  "items": [
    {
      "path": "C:\\...\\documento.pdf",
      "lastOpenedUtc": "2026-10-08T20:00:00Z"
    }
  ]
}
```

Storage is local app data, not the repository or the PDF directory.

Rules:

- add/update only after a PDF opens successfully;
- full normalized Windows path is the identity, compared case-insensitively;
- move an existing item to the top on reopen;
- trim to 10;
- do not store passwords, PDF contents, search history or thumbnails;
- no database;
- provide `Borrar recientes`;
- corrupt recent-file JSON degrades to an empty/unavailable recent list without blocking PDF open.

Do not probe every recent path at application startup. In particular, a stale UNC/network path must not trigger background network access merely because the menu exists. Existence/opening is checked when the user explicitly chooses that recent item.

If a selected recent file no longer exists, show a controlled message and allow/remove that stale entry without affecting the current document.

### 10.3 Offline/privacy hardening

Automated guards must continue to ensure normal reader operations do not require network access. No telemetry is introduced.

Recent paths are the only new persisted reader metadata in F4.

## 11. Reader state and data flow

The conceptual data flow is:

```text
Open PDF
  -> candidate PdfDocumentSession
  -> page count + lightweight page metrics
  -> publish session
  -> Reader document state
       -> virtualized page slots
       -> visible-range detector
       -> bounded render window
       -> page bitmap + transform
       -> overlays (selection/search/links)

Left panel
  -> thumbnail requests (lazy)
  -> bookmark tree (read-only)

Search
  -> current session
  -> one text page at a time
  -> active match
  -> navigate + highlight

FIRMAR
  -> current reader page
  -> existing single-page PdfImage path
  -> existing SignatureEditState / AddSignatureAsset
```

There is one current PDF session, not one session per page.

## 12. State ownership

F4 should keep state small and explicit. The exact type names may be adjusted during planning, but responsibilities are frozen:

### Document/session state
Owned by `PdfDocumentSession` and `MainWindow` lifecycle:
- native document handle;
- file path;
- page count;
- PDFium operations.

### Reader navigation state
- current page;
- zoom mode;
- page metrics;
- continuous viewport anchor;
- visible page range.

### Render state
- page slot placeholder/loading/bitmap/error;
- generation/cancellation identity;
- visible + one-neighbor retention policy.

### Reader interaction state
- find query/direction/current match;
- one-page text selection;
- link overlays;
- selected left-panel tab/item.

Do not merge signature dirty state into reader state. Do not put recent-files persistence inside `PdfDocumentSession`.

## 13. Error handling

### Open failure
Previous document stays usable. Candidate session is disposed.

### Password failure
Prompt/retry without replacing the current document until success.

### Page render failure
Page card reports error; rest of document remains navigable.

### Thumbnail failure
Show placeholder/error thumbnail; full reader remains usable.

### Text extraction/search failure on one page
Skip/report that page in a controlled manner; do not crash or dispose the document.

### Bookmark corruption/cycle
Stop bounded traversal; keep any already-valid bookmark nodes; do not hang.

### Unsupported link action
Do nothing external; optionally show a non-blocking/controlled status.

### Recent-files corruption
Do not block startup or PDF opening; recent list may reset logically/appear unavailable. Never delete arbitrary user files.

## 14. Performance and responsiveness rules

F4 acceptance is behavioral rather than tied to a synthetic benchmark number, but the following are hard architectural rules:

1. Opening/scrolling/zooming must never intentionally render every full page.
2. Scrolling rapidly must invalidate stale publication.
3. Native calls remain globally serialized.
4. Long reader work happens off the WPF UI thread except small state/UI updates.
5. No unbounded Task creation per page.
6. Bitmap lifetime is bounded by visible/neighbor windows.
7. Thumbnail work is lazy and lower practical priority than visible page work.
8. Search can be canceled/restarted between page operations when the query changes.
9. No startup access to every recent file path.
10. Progressive rendering is deferred until measured need.

## 15. Testing strategy

Every slice follows RED -> GREEN and final full regression.

### 15.1 Pure/unit tests

Cover at minimum:

- current-page selection from viewport geometry;
- previous/next/go-to-page continuous navigation semantics;
- zoom display geometry for manual/Fit Width/Fit Page with mixed page sizes;
- visible + neighbor render-window calculation;
- stale generation rejection;
- recent-files ordering/dedupe/cap/corruption;
- bounded bookmark traversal/cycle detection;
- search wrap behavior;
- text-range normalization.

### 15.2 PDFium integration tests

Use synthetic/public-safe fixtures only.

Cover:

- page size by index;
- password required/incorrect/correct;
- text extraction and Unicode;
- search index/count/rects;
- bookmark title/destination;
- internal link destination;
- URI extraction;
- scanned/image-only page returning no useful text without OCR.

All native test calls remain serialized.

### 15.3 WPF/STA tests

Cover:

- continuous reader surface shown in LEER;
- single-page surface shown in FIRMAR;
- entering FIRMAR uses current reader page;
- leaving FIRMAR returns to same page;
- thumbnail click navigates exactly once;
- scrolling updates page-number UI;
- find bar shortcut/open/close;
- selection + Ctrl+C seam;
- external URI requires confirmation;
- recent menu does not probe paths on construction/startup;
- existing PNG/photo/draw/library signature actions remain available.

### 15.4 Full regression

Every completed F4 slice runs the full existing suite. F2 ZPL and F3 visual-signature tests are regression gates, not optional collateral coverage.

## 16. Manual Windows QA — separate NOT RUN gate

Automated PASS does not imply physical UX/performance PASS.

F4 manual QA should include:

- 1-page, ~100-page and very large/many-page PDFs;
- mixed portrait/landscape/page sizes;
- rapid mouse-wheel/touchpad scrolling;
- rapid zoom/refit/resize;
- memory observation while scrolling forward/back through many pages;
- thumbnails while fast scrolling;
- password required / wrong / correct / cancel;
- accented Spanish text search/copy;
- image-only PDF no-results behavior;
- nested bookmarks;
- internal link navigation;
- external `https` link confirm/cancel, including network-disabled smoke;
- missing recent file;
- recent UNC/network-looking path does not get probed at startup;
- LEER -> FIRMAR -> LEER transition;
- dirty signature navigation guards;
- printing current page from continuous reader;
- open ZPL after PDF and return to PDF flows.

Manual findings may justify tuning render prefetch, thumbnail size or cache policy, but they do not justify adding another PDF engine without a separate evidence gate.

## 17. Slice boundaries and implementation order

### F4.1 — Continuous Reader Core

- page metrics;
- virtualized continuous page list;
- current-page tracking;
- navigation integration;
- zoom/fit integration;
- visible + one-neighbor render window;
- LEER/FIRMAR surface transition;
- per-page render errors.

### F4.2 — Thumbnails

- `Páginas` left tab;
- lazy virtualized thumbnails;
- selection synchronization;
- click-to-page.

### F4.3 — Password PDFs

- PDFium password error classification;
- password dialog/retry/cancel;
- candidate-first safety/privacy.

### F4.4 — Search + Copy Text

- PDFium text bindings;
- find bar;
- next/previous/wrap;
- active-match highlight;
- single-page mouse selection;
- clipboard copy.

### F4.5 — Bookmarks + Links

- `Marcadores` tree;
- cycle-safe traversal;
- bookmark destinations;
- explicit link annotations;
- internal GOTO;
- confirmed `http/https` URI launch;
- unsupported dangerous actions ignored.

### F4.6 — Shortcuts + Recent Files + Hardening

- shortcut routing;
- local recent-files manifest;
- stale/corrupt recent handling;
- integrated regression/performance/offline hardening;
- closure docs and draft stacked PR.

Each slice gets its own RED/GREEN evidence and may use a stacked branch/PR pattern consistent with previous phases. No merge to `main` without explicit user approval.

## 18. Explicit non-goals

F4 does **not** include:

- multi-document tabs;
- side-by-side/two-page/spread view;
- page reorder/delete/rotate/insert/merge/split;
- OCR;
- text editing;
- comments/highlights saved into the PDF;
- forms editing;
- attachments UI;
- cryptographic signature UI;
- external remote-GOTO/Launch/JavaScript execution;
- automatic plain-text URL detection as a required feature;
- cross-page text drag selection;
- regex/advanced search/indexing;
- saved search history;
- password persistence;
- cloud sync;
- WebView2/browser PDF viewer;
- another PDF rendering engine;
- database/SQLite;
- MVVM/DI framework migration;
- progressive PDFium rendering without measured evidence.

Tabs may be reconsidered after F4 only if the stable reader demonstrates a real product need and the added multi-session state is justified.

## 19. Stop conditions / return to design

Stop implementation and return to design if any slice appears to require:

1. replacing PDFium;
2. a new runtime PDF SDK/package for functionality PDFium already exposes;
3. moving F3 signature placement into a new continuous-page editing model;
4. storing passwords persistently;
5. executing unsafe PDF actions;
6. unbounded full-document bitmap or thumbnail caching;
7. a custom scrolling/layout engine when standard WPF virtualization can meet the need;
8. app-wide MVVM/DI/repository refactor;
9. network service/API for search, text, thumbnails or bookmarks;
10. scope expansion into F5+ editing/organization features.

## 20. Acceptance summary

F4 automated acceptance requires all of the following:

- `READER-01`: continuous vertical reader + lazy thumbnails, without all-page full rendering;
- `READER-02`: local PDFium search and single-page selection/copy;
- `READER-03`: bookmarks + explicit internal/URI links with safe action handling;
- `READER-04`: password-protected PDF open/retry/cancel flow;
- `READER-05`: shortcuts + local recent files; no tabs in F4;
- existing print behavior still works, with continuous current-page semantics;
- F3 LEER/FIRMAR transition preserves the existing single-page signature architecture;
- F2 ZPL remains operational;
- no new normal-operation network dependency;
- exact-head Windows CI passes full regression;
- manual Windows QA remains separately reported as PASS/FAIL/NOT RUN, never inferred from CI.

## 21. Upstream API evidence used by this design

Current PDFium public headers confirm the required primitives:

- `public/fpdfview.h`: `FPDF_GetPageSizeByIndexF`, `FPDF_LoadDocument`, `FPDF_GetLastError`, `FPDF_ERR_PASSWORD`;
- `public/fpdf_text.h`: text-page load/close, hit testing, Unicode extraction, search and text rectangles;
- `public/fpdf_doc.h`: bookmarks, destinations, link actions and URI extraction.

The implementation plan must pin tests to the PDFium binary version already used by SG PDF Editor rather than assuming every upstream-main experimental API is present. Only APIs confirmed exported by the pinned runtime may enter product code.

## 22. Design decisions frozen for planning

1. PDFium only; no second PDF engine.
2. LEER is continuous/virtualized; FIRMAR stays single-active-page.
3. Visible pages + one neighbor each side; no general full-page LRU cache in F4 design.
4. Thumbnails are lazy/virtualized, never all eagerly rendered.
5. Current page is viewport-center based.
6. Existing zoom modes remain; fit modes resolve per page in continuous view.
7. Search is on-demand and page-by-page, not permanently indexed.
8. Text drag selection is one page at a time.
9. Bookmarks are read-only and cycle-bounded.
10. Explicit PDF link annotations only are required; URI launch requires confirmation and `http/https`.
11. Passwords are never persisted.
12. Recent files store at most 10 paths + timestamp locally and do not probe them at startup.
13. No tabs in F4.
14. No product code starts until this written spec is reviewed and approved, followed by a separate written TDD implementation plan.