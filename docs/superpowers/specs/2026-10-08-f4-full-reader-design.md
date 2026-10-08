# F4 — Full Reader / Lector Completo — Design Specification

**Date:** 2026-10-08  
**Status:** WRITTEN + SELF-REVIEWED — awaiting user approval  
**Branch:** `feat/f4-full-reader`  
**Base:** F3.4 closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`  
**Product:** SG PDF Editor — Windows x64, C#/.NET 10, WPF, local-first/offline

## 1. Purpose

F4 turns the existing F0 single-page PDF reader into the normal daily reading experience for SG PDF Editor without replacing PDFium, introducing another PDF engine, or breaking F2 ZPL and F3 visual signatures.

Success means a user can open a normal or password-protected PDF and read it naturally with continuous vertical scrolling, thumbnails, bookmarks, safe links, text search/copy, keyboard shortcuts and recent files while the application remains responsive, private and memory-bounded.

F4 is a **reader** phase. It does not add page organization, OCR, text editing, saved annotations, cryptographic signing, multi-document tabs or cloud features.

## 2. Existing baseline and non-negotiables

The current application already has:

- `PdfDocumentSession` owning the native PDFium document handle;
- `PdfiumRuntime.NativeGate`, globally serializing PDFium calls;
- `PdfRenderScheduler` latest-request-wins behavior;
- `PageNavigationState`;
- `PdfZoomState` with 25–400% manual zoom, Fit Page and Fit Width;
- one central `PdfImage` inside a WPF `ScrollViewer`;
- Windows printing using the current PDF session;
- F3 visual signing layered on the active single `PdfImage` via `SignatureEditState` + `SignatureOverlayCanvas`;
- F2 ZPL preview using its own existing central surface.

Constraints remain:

1. Windows x64 + C#/.NET 10 + WPF.
2. PDFium remains the primary PDF engine.
3. Every PDFium call remains behind `PdfiumRuntime.NativeGate`.
4. No normal-operation cloud, API key, account, telemetry or required network connection.
5. No new commercial runtime dependency.
6. KISS/YAGNI: no preventive MVVM framework, DI container, event bus, repository abstraction or plugin framework.
7. No automatic merge to `main`.
8. Existing F2/F3 tests are regression gates.

## 3. Product UX boundary

The long-term mode concept remains:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

F4 changes `LEER` only.

### 3.1 LEER

`LEER` becomes a continuous vertical viewer. Multiple page cards may be visible, but only the visible region and a small neighbor window are rendered at full viewing resolution.

The left panel becomes:

```text
[Páginas] [Marcadores]
```

The existing top page and zoom controls remain recognizable.

### 3.2 FIRMAR compatibility — frozen decision

F3 remains a **single-active-page editor**.

Entering `FIRMAR` from continuous reading:

1. current reader page becomes active signing page;
2. continuous surface is hidden;
3. existing single-page `PdfImage` is shown/rendered;
4. existing `SignatureOverlayCanvas`, `SignatureEditState`, coordinate mapping and `AddSignatureAsset(...)` remain authoritative.

Returning to `LEER`:

1. existing unresolved-signature guard remains authoritative;
2. single-page signing surface is hidden;
3. continuous reader returns to the same active page.

Exact sub-page pixel offset restoration is desirable but not a hard F4 requirement.

While `FIRMAR` is active, previous/next/page-number navigation keeps the existing single-page path and dirty guards. Continuous scrolling is a `LEER` behavior.

F4 must not create a second signature placement model or put signature editing overlays on every continuous page card.

### 3.3 ZPL compatibility

Opening ZPL continues to activate the existing ZPL surface. The continuous reader and single-page PDF editor are hidden while ZPL preview is active. F4 does not move label rendering into reader code.

## 4. Architecture

### 4.1 Three mutually exclusive center surfaces

```text
PDF read  -> new continuous virtualized reader
PDF edit  -> existing single PdfImage + edit/sign overlays
ZPL       -> existing label preview/sheet surface
```

The existing `PdfImage` is retained specifically so F3 and later page-local editing do not need to be rebuilt for F4.

### 4.2 Feature structure

Reader-specific code may live in:

```text
src/SGPdf.App/Features/Reader/
```

WPF integration may use `MainWindow.Reader.cs`, matching the existing partial-window pattern. No new project or app-wide framework is introduced.

### 4.3 Native document ownership

`PdfDocumentSession` remains the only owner of the native document handle. It may become `partial` and split reader concerns into focused files such as text/navigation helpers rather than exposing the raw `IntPtr`.

New P/Invoke declarations stay minimal and feature-driven.

## 5. F4.1 — Continuous Reader Core

### 5.1 Lightweight page model

After a document loads, obtain page count and page geometry without rendering every page. Prefer `FPDF_GetPageSizeByIndexF` for geometry.

Each reader page slot needs only:

- page index;
- width/height in PDF points;
- calculated display size for current zoom mode;
- full-page bitmap only while in the render window;
- `PdfPageDeviceTransform` only while rendered;
- placeholder/loading/ready/error state.

No eager full-document bitmap render is allowed.

### 5.2 Standard WPF virtualization

Use standard WPF virtualization/recycling before considering a custom scrolling engine. Page placeholders preserve calculated geometry so scrollbar extent does not depend on bitmap completion.

Mixed page sizes and portrait/landscape combinations are required.

### 5.3 Current page

Current page is determined by the **vertical center of the viewport**:

- if center is inside a page rectangle, that page is current;
- if center is in a page gap, choose the nearest page.

Current page drives page-number UI, status, thumbnail selection, Print -> Current page, and the page selected when entering `FIRMAR`.

### 5.4 Navigation

In `LEER`:

- Previous -> scroll to previous page;
- Next -> scroll to next page;
- page number + Enter -> scroll to target page;
- explicit page navigation aligns target near the top with normal page margin;
- document session is not recreated.

In `FIRMAR`, the same controls retain existing single-page behavior.

### 5.5 Zoom

Existing 25–400% limits remain.

**Manual / 100%:** one common DPI/scale across pages.

**Fit Width:** resolve scale per page from that page's width and current viewport width.

**Fit Page:** resolve scale per page from that page's dimensions and viewport dimensions; continuous mode remains active.

In fit modes the displayed percentage reflects the current page's resolved DPI.

Zoom/refit:

1. recomputes placeholder geometry;
2. invalidates stale full-page reader bitmaps;
3. preserves current page as navigation anchor;
4. rerenders only the active render window.

### 5.6 Memory policy

Full-resolution reader bitmaps are retained only for:

- currently visible pages;
- at most one neighboring page immediately before the visible range;
- at most one neighboring page immediately after the visible range.

When a page leaves this window, its full-resolution bitmap is released and may be rerendered later.

This simple count-bounded policy is preferred over an LRU/cache framework. If real Windows QA proves high-zoom memory is still unacceptable, the implementation plan may add a measured pixel/byte guard.

### 5.7 Render scheduling

Reader rendering is latest-view-wins:

- scroll/zoom/open creates a new generation;
- stale work is canceled where possible or ignored before UI publication;
- `FPDF_RenderPageBitmap` remains synchronous and globally serialized;
- no unbounded Task-per-page fanout;
- visible pages precede neighbor prefetch;
- thumbnail background work must not starve visible full-page work.

Progressive PDFium rendering remains deferred until measured evidence requires it.

### 5.8 Per-page failure

Once the document session itself opened successfully, one page failing to render shows a controlled error card; the rest of the document remains usable.

Document open remains candidate-first: a failed new open leaves the previous document untouched.

## 6. F4.2 — Thumbnails

The left `Páginas` tab is a virtualized lazy thumbnail list.

Each item shows thumbnail, page number and current-page selection. Clicking exactly once navigates to that page; scrolling the reader updates selection without recursive navigation.

Thumbnail rules:

- approximately 120–150 WPF-pixel display width;
- rendered lazily for realized/near-visible rows;
- never eagerly render all thumbnails;
- stale requests may be discarded;
- thumbnail work is lower practical priority than visible full-page rendering;
- do not permanently retain thousands of thumbnail bitmaps.

The final F4 UI has both `Páginas` and `Marcadores`; during earlier slices Marcadores may be empty/hidden until F4.5.

## 7. F4.3 — Password PDFs

### 7.1 Classification

`FPDF_ERR_PASSWORD` (`4`) is the authoritative PDFium classification for password required/incorrect. UI code must not detect password cases by parsing exception text.

### 7.2 Candidate-first flow

```text
open with no password
  -> success: publish candidate
  -> password error: masked password dialog
       -> retry
       -> success: publish candidate
       -> password error: controlled invalid-password message, stay in dialog
       -> cancel: keep previous document
  -> other error: normal open error, keep previous document
```

### 7.3 Privacy and scope

Passwords are never written to recents, logs, status text, JSON/settings or app-created diagnostics. They are not persisted across restarts.

F4 guarantees reading/navigation/search/printing after a protected file opens successfully to the extent PDFium permits. F4 does **not** redesign encrypted-PDF save/edit/sign behavior. Existing write paths that cannot safely reopen an encrypted source must fail in a controlled way rather than persist credentials or bypass security.

Owner-permission enforcement for editing belongs to later write features.

## 8. F4.4 — Search, selection and copy

### 8.1 PDFium text APIs

Use the needed subset of:

- `FPDFText_LoadPage` / `FPDFText_ClosePage`;
- `FPDFText_FindStart` / `FindNext` / `FindPrev` / `FindClose`;
- `FPDFText_GetSchResultIndex` / `FPDFText_GetSchCount`;
- `FPDFText_GetCharIndexAtPos`;
- `FPDFText_GetText`;
- `FPDFText_CountRects` / `FPDFText_GetRect`.

No OCR enters F4. Image-only PDFs with no text layer legitimately return no text; OCR remains F10.

### 8.2 Find UI

`Ctrl+F` opens a compact find bar with query, previous, next and close.

- Enter -> next;
- Shift+Enter -> previous;
- Escape -> close;
- default search = case-insensitive, non-whole-word;
- no regex/case/whole-word UI in F4.

### 8.3 Search strategy

No permanent whole-document index.

On-demand search starts from the current page, continues one page at a time in the requested direction, wraps at most once, and stops at first match or after every page was visited. Query change/cancel invalidates the previous search generation.

F4 does not require an all-results panel or exact global `N of M` count. `Sin resultados` is sufficient.

Only the active match must be highlighted.

### 8.4 Text selection

Mouse drag selects text **within one page at a time**.

1. map device point -> PDF point using that rendered page's transform;
2. hit-test start/end character index;
3. normalize range;
4. obtain text rectangles;
5. draw selection overlay over the page;
6. `Ctrl+C` copies PDFium Unicode text to Windows clipboard.

Cross-page drag selection, double-click word semantics, paragraph selection and selection handles are deferred.

Selection is interaction state, not rasterized into the page image. Zoom recomputes overlay geometry from PDF/text coordinates.

Entering `FIRMAR` clears/hides reader selection without altering signature/document state.

## 9. F4.5 — Bookmarks and links

### 9.1 Bookmarks

`Marcadores` shows a read-only WPF tree using the necessary PDFium bookmark/destination APIs, including `FPDFBookmark_GetFirstChild`, `FPDFBookmark_GetNextSibling`, `FPDFBookmark_GetTitle`, destination/action lookup and `FPDFDest_GetDestPageIndex`.

Malformed outline traversal must be cycle-safe and bounded. Track visited bookmark handles and enforce conservative maximums: **10,000 nodes and depth 128** unless planning chooses stricter limits. Never recurse indefinitely.

A valid in-document destination scrolls to its page. Unsupported actions may remain visible by title but are not executed.

### 9.2 Explicit PDF links

Required F4 links are explicit PDF link annotations.

Support:

- internal GOTO/destination -> reader navigation;
- URI -> confirmation, then optional browser launch.

Do **not** execute Launch actions, remote GOTO, arbitrary file/shell actions, JavaScript or unknown action types.

Automatic detection of plain-text URLs via `FPDFLink_LoadWebLinks` is not required for READER-03 and is deferred.

### 9.3 External URI safety

External URI opening requires all of:

1. explicit user click;
2. confirmation showing destination;
3. scheme exactly `http` or `https`;
4. OS browser launch only after confirmation.

Cancel = no external action. SG PDF Editor itself never requires network access.

Link rectangles/quads are overlays mapped through the page transform. Active text-selection drag wins over link activation.

## 10. F4.6 — Shortcuts, recent files and hardening

### 10.1 Shortcuts

Minimum:

- `Ctrl+O` open PDF;
- `Ctrl+P` print;
- `Ctrl+F` find;
- `Ctrl+C` copy active reader selection;
- `Ctrl++` / `Ctrl+-` zoom;
- `Ctrl+0` 100%;
- `Home` / `End` go to first/last page when focus is not in an editable text control;
- `PageUp` / `PageDown` scroll **one viewport height** up/down in continuous `LEER`; they do not directly force previous/next page selection;
- `F3` / `Shift+F3` may map to next/previous find only if no existing app conflict is found.

Text-entry controls (page box, find box, ZPL fields, dialogs) keep normal editing semantics where they conflict with global shortcuts.

### 10.2 Recent files

`Archivo -> Recientes` stores at most 10 successful PDF opens, newest first, under LocalAppData. Format is small versioned JSON containing only full path and UTC last-open time.

Rules:

- update only after successful PDF open;
- normalized Windows full path is identity, compared `OrdinalIgnoreCase`;
- reopen moves item to top;
- cap = 10;
- never store passwords, PDF content, search history or thumbnails;
- `Borrar recientes` clears metadata only;
- writes use complete temp -> same-volume publish/replace, never in-place truncation;
- do not probe all paths at startup/menu construction;
- choosing an entry is the point where the app attempts to open/check it;
- stale/missing chosen file shows controlled message and may be removed from recents without changing current document.

**Corrupt `recent-files.json` policy:** treat recents as unavailable/empty for that load and do not delete or rewrite the corrupt file merely by opening the app/menu. A later **successful PDF open** may rebuild a fresh valid recent manifest containing that newly opened path through the normal atomic write path. Corrupt recents must never block PDF opening.

This avoids background access to stale UNC/network paths and keeps offline/privacy behavior deterministic.

## 11. Conceptual data flow

```text
Open PDF
  -> candidate PdfDocumentSession
  -> page count + lightweight page metrics
  -> publish session
  -> reader state
       -> virtualized page slots
       -> visible-range detector
       -> visible + one-neighbor render window
       -> bitmap + transform
       -> overlays (search/selection/links)

Left panel
  -> lazy thumbnails
  -> read-only bookmark tree

Search
  -> current session
  -> one text page at a time
  -> active match
  -> navigate + highlight

FIRMAR
  -> current reader page
  -> existing single PdfImage
  -> existing SignatureEditState / AddSignatureAsset
```

There is exactly one current PDF document session, not a session per page.

## 12. State ownership

Keep responsibilities explicit:

**Document/session:** native handle, path, page count, native operations.

**Reader navigation/layout:** current page, zoom mode, page metrics, viewport anchor/range.

**Reader rendering:** page render state, generation/cancellation identity, visible+neighbor bitmap lifetime.

**Reader interaction:** find query/current match, one-page text selection, link overlays, left-panel selection.

**Recents:** separate LocalAppData metadata store.

Signature dirty/placement state remains separate. Recent-file persistence does not belong in `PdfDocumentSession`.

## 13. Error handling

- Failed new document open -> dispose candidate; keep previous document.
- Password error -> prompt/retry/cancel without replacing previous document.
- One page render failure -> error card only; rest remains navigable.
- Thumbnail failure -> placeholder/error thumbnail; main reader works.
- Text extraction/search failure on one page -> controlled skip/error; do not dispose session.
- Bookmark cycle/corruption -> stop bounded traversal, retain already-valid nodes.
- Unsupported link action -> no external execution.
- Corrupt recents -> reader startup/open still works; do not destructively repair merely on read.

## 14. Performance rules

Hard architecture rules:

1. Never intentionally full-render every page on open/zoom.
2. Rapid scroll/zoom rejects stale UI publication.
3. Native PDFium calls remain globally serialized.
4. Long reader work is off the WPF UI thread except small UI/state publication.
5. No unbounded Task creation per page.
6. Full-resolution bitmap lifetime is visible+one-neighbor bounded.
7. Thumbnail rendering is lazy.
8. Search can restart/cancel between page operations.
9. Startup/menu creation does not touch every recent file path.
10. Progressive rendering remains deferred without measured need.

## 15. Testing strategy

Every slice is RED -> GREEN plus full regression.

### 15.1 Pure/unit tests

At minimum:

- current-page choice from viewport geometry;
- continuous previous/next/go-to semantics;
- manual/Fit Width/Fit Page page geometry with mixed sizes;
- visible+neighbor render-window calculation;
- stale generation rejection;
- search forward/back/wrap behavior;
- text-range normalization;
- bookmark cycle/node/depth bounds;
- recents ordering, dedupe, cap, corruption and no-startup-probe behavior.

### 15.2 PDFium integration tests

Synthetic/public-safe fixtures only:

- page size by index;
- password required/wrong/correct;
- text Unicode extraction;
- search index/count/rectangles;
- bookmark title/destination;
- internal link destination;
- URI extraction;
- image-only page with no searchable text.

All native calls remain serialized.

### 15.3 WPF/STA tests

At minimum:

- continuous surface visible in LEER;
- single-page surface visible in FIRMAR;
- entering FIRMAR uses current reader page;
- leaving FIRMAR returns to same page;
- thumbnail click navigates once;
- scroll updates page-number state;
- find bar shortcut/open/close;
- selection + copy seam;
- URI requires confirmation;
- recent menu construction does not probe stored paths;
- existing PNG/photo/draw/library signature actions remain available.

### 15.4 Full regression

Every F4 slice runs the complete existing suite. F2 ZPL and F3 visual-signature tests remain required gates.

## 16. Manual Windows QA — separate gate

Automated PASS does not imply UX/performance PASS. Manual F4 QA should include:

- 1-page, ~100-page and large/many-page PDFs;
- mixed page sizes/orientations;
- rapid wheel/touchpad scroll;
- rapid zoom/refit/window resize;
- memory observation after long forward/back scrolling;
- lazy thumbnail behavior;
- password required/wrong/correct/cancel;
- Spanish accented text search/copy;
- image-only PDF no-results behavior;
- nested bookmarks;
- internal links;
- external HTTPS confirm/cancel and network-disabled smoke;
- stale recent file;
- stored UNC-looking recent path not probed at startup;
- LEER -> FIRMAR -> LEER;
- dirty signature guards;
- Print Current Page from continuous view;
- PDF -> ZPL transitions.

Manual results may tune prefetch/thumbnail sizes but do not justify another PDF engine without a separate evidence gate.

## 17. Slice order

### F4.1 Continuous Reader Core

Page metrics, virtualized list, current-page tracking, navigation, zoom/fit, bounded render window, LEER/FIRMAR surface transition, per-page render errors.

### F4.2 Thumbnails

`Páginas` tab, lazy thumbnails, selection sync, click-to-page.

### F4.3 Password PDFs

Error classification, masked prompt, retry/cancel, candidate-first/privacy.

### F4.4 Search + Copy

Text bindings, find bar, next/previous/wrap, active highlight, one-page selection, clipboard copy.

### F4.5 Bookmarks + Links

`Marcadores`, cycle-safe outline, destinations, explicit link annotations, internal GOTO, confirmed HTTP/HTTPS URI.

### F4.6 Shortcuts + Recents + Hardening

Shortcut routing, atomic local recents, stale/corrupt handling, regression/performance/offline hardening, closure docs and draft stacked PR.

Each slice gets independent RED/GREEN evidence. No merge to `main` without explicit user approval.

## 18. Explicit non-goals

F4 excludes:

- multiple-document tabs;
- two-page/spread/side-by-side view;
- reorder/rotate/delete/insert/merge/split pages;
- OCR;
- text editing;
- saved PDF comments/highlights;
- forms editing;
- attachments UI;
- cryptographic-signature UI;
- remote GOTO, Launch, JavaScript or arbitrary shell/file action execution;
- required auto-detection of plain-text URLs;
- cross-page text drag selection;
- regex/advanced search or persistent indexing;
- search history;
- password persistence;
- cloud sync;
- WebView2/browser PDF viewer;
- second PDF engine;
- database/SQLite;
- app-wide MVVM/DI migration;
- progressive PDFium rendering without evidence.

Tabs may be reconsidered after a stable F4 only if real product value justifies multi-session complexity.

## 19. Stop conditions

Return to design if implementation appears to require:

1. replacing PDFium;
2. adding a second runtime PDF SDK for an API PDFium already exposes;
3. moving F3 signing into a new multi-page edit model;
4. persisting passwords;
5. executing unsafe PDF actions;
6. unbounded full-document bitmap/thumbnail caching;
7. a custom scrolling engine before standard WPF virtualization is proven insufficient;
8. app-wide framework/refactor work;
9. a network service for reader functions;
10. scope expansion into F5+ editing/organizing.

## 20. Acceptance mapping

F4 automated acceptance requires:

- **READER-01:** continuous vertical reader + lazy thumbnails without eager full-document rendering;
- **READER-02:** PDFium local search + one-page text selection/copy;
- **READER-03:** read-only bookmarks + explicit safe internal/URI links;
- **READER-04:** password open/retry/cancel;
- **READER-05:** shortcuts + max-10 local recent files; **no tabs in F4**;
- Print Current Page uses continuous current-page semantics;
- F3 LEER/FIRMAR transition preserves existing signing architecture;
- F2 ZPL remains operational;
- no new normal-operation network dependency;
- exact-head Windows CI passes full regression;
- manual Windows QA is reported separately as PASS/FAIL/NOT RUN.

## 21. Upstream API evidence

Current PDFium public headers expose the primitives this design relies on:

- `public/fpdfview.h`: `FPDF_GetPageSizeByIndexF`, `FPDF_LoadDocument`, `FPDF_GetLastError`, `FPDF_ERR_PASSWORD`;
- `public/fpdf_text.h`: text page load/close, hit testing, Unicode extraction, search and text rectangles;
- `public/fpdf_doc.h`: bookmarks, destinations, link actions and URI extraction.

Implementation must verify exports against the **pinned PDFium binary already used by SG PDF Editor**. Do not assume every experimental API in upstream `main` exists in the pinned runtime.

## 22. Frozen decisions for the implementation-plan gate

1. PDFium only.
2. LEER = continuous virtualized; FIRMAR = existing single-active-page editor.
3. Full-page retention = visible pages + one neighbor each side; no generic LRU in F4 design.
4. Thumbnails = lazy/virtualized, never eager whole-document render.
5. Current page = viewport vertical center / nearest page.
6. Existing zoom modes remain; fit scale resolves per page in continuous view.
7. Search = on-demand page-by-page; no permanent document index.
8. Text drag selection = one page at a time.
9. Bookmarks = read-only, cycle-safe, max 10,000 nodes / depth 128.
10. Required links = explicit PDF annotations; only internal GOTO and confirmed HTTP/HTTPS URI execute.
11. Passwords are never persisted.
12. Recents = max 10 paths + timestamp, atomic local JSON, no startup path probing.
13. PageUp/PageDown = one viewport-height scroll in continuous LEER.
14. No tabs in F4.
15. No product code begins until this written spec is approved and then a separate written TDD implementation plan is reviewed/approved.