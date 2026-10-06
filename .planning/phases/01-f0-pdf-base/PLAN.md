# F0 PDF Base — Plan

**Phase:** 1 of 13  
**Status:** in progress  
**Current slice:** F0.1 — Open PDF + render page 1 in WPF

## F0.1 goal

Deliver the smallest usable PDF vertical slice: choose a local PDF from the WPF UI, open it with the existing PDFium session, render page 1 to a real bitmap, and display it without blocking the UI thread.

## In scope

- wire `Archivo > Abrir...` to `OpenFileDialog`;
- open the selected local PDF through `PdfDocumentSession`;
- add only the PDFium bitmap/render P/Invokes required by this slice;
- rasterize page 1 at a deterministic DPI;
- copy the native BGRA buffer into managed memory;
- create/display a WPF `BitmapSource`;
- keep native PDFium access globally serialized;
- dispose page/bitmap/document handles on every path;
- replace the previous session only after the new document rendered successfully;
- show a clear local error on open/render failure;
- automated render integration test with a generated one-page PDF;
- Windows build/tests/CI.

## Out of scope

- previous/next/go-to-page navigation (F0.2);
- zoom/fit page/fit width (F0.3);
- scheduler priorities/cancellation/progressive rendering (F0.4);
- printing (F0.5);
- thumbnails/search/ZPL/sign/edit/OCR.

## TDD

1. RED: integration test requires `PdfDocumentSession.RenderPage(0, 96)` and validates Letter output `816×1056`, stride, buffer size and non-white rendered content.
2. GREEN: add minimum native/render implementation to satisfy the test.
3. UI: connect the already-existing `Abrir...` command and display the successful result.
4. VERIFY: full Windows restore/build/test plus final fresh GitHub Actions run.

## Expected files

- `src/SGPdf.App/Pdf/PdfiumNative.cs`
- `src/SGPdf.App/Pdf/PdfiumRuntime.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- `src/SGPdf.App/Pdf/PdfRenderedPage.cs` (only if the result object remains clearer than a tuple)
- `src/SGPdf.App/MainWindow.xaml`
- `src/SGPdf.App/MainWindow.xaml.cs`
- `tests/SGPdf.App.Tests/PdfRenderTests.cs`
- `.planning/STATE.md`
- `docs/history/2026-10-06-F0.1.md`

## Acceptance

F0.1 is complete only when a real PDF can be selected from the desktop UI, page 1 visibly renders in WPF, the render test is green, handles are released correctly, the app remains offline-only, and fresh Windows CI is green.

## Merge rule

No merge to `main` without explicit user approval.
