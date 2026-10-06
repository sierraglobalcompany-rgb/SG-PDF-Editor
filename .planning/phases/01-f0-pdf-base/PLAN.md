# F0 PDF Base — Plan

**Phase:** 1 of 13  
**Status:** in progress  
**Current slice:** F0.1 — Open PDF + render page 1 in WPF  
**F0.1 automated status:** PASS  
**Remaining F0.1 gate:** manual Windows UI smoke (select PDF and visually confirm page 1)

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

## TDD evidence

1. RED: GitHub Actions run `37536987398` built successfully and then failed exactly because `PdfDocumentSession` did not contain `RenderPage`.
2. GREEN: `RenderPage(0, 96)` renders a generated real Letter PDF at `816×1056` with a valid BGRA buffer and non-white content.
3. UI: the existing `Abrir...` command opens a local PDF in a background task and creates a WPF `BitmapSource` on the UI thread.
4. VERIFY: GitHub Actions run `37537454239` completed restore/build/tests successfully on Windows for the functional head `a9a6e0e23c86d10cbe9bd20f50a5db09aa1cfffa`.

## Files

- `src/SGPdf.App/Pdf/PdfiumNative.cs`
- `src/SGPdf.App/Pdf/PdfiumRuntime.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- `src/SGPdf.App/Pdf/PdfRenderedPage.cs`
- `src/SGPdf.App/MainWindow.xaml`
- `src/SGPdf.App/MainWindow.xaml.cs`
- `tests/SGPdf.App.Tests/PdfRenderTests.cs`
- `.planning/STATE.md`
- `docs/history/2026-10-06-F0.1.md`

## Acceptance status

Automated acceptance is satisfied: the real PDFium render test is green, resources are released, UI rendering work is dispatched off the WPF thread, no online dependency was added, and Windows CI is green.

The final visual desktop assertion — choose a PDF in the running Windows app and visually confirm page 1 — is intentionally recorded as **NOT RUN** in this connector-only session. It must be performed before treating F0.1 as physically QA-closed.

## Next after manual smoke

F0.2 — previous/next navigation, current page and boundaries.

## Merge rule

No merge to `main` without explicit user approval.
