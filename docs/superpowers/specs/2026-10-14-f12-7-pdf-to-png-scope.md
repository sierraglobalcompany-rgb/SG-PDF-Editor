# F12.7 Audited Conversions — capability/scope gate

Date: 2026-10-14

## Decision

Approve exactly one baseline conversion slice: **PDF → PNG, one PNG per page**.

## Why this slice is GO

The repository already has the complete rendering owner needed for a faithful raster export:

- `PdfDocumentSession.Open(...)` for local PDF input;
- `PdfDocumentSession.RenderPage(pageIndex, dpi, ...)` using the pinned PDFium runtime;
- `PdfRenderedPage` with BGRA32 pixels, width, height and stride;
- WPF imaging support already used by the reader, so PNG encoding needs no new runtime dependency.

This keeps the conversion local/offline and reuses the same renderer the product already trusts for viewing.

## MVP flow

`PDF already open → Herramientas → Convertir PDF a PNG... → choose output folder → render pages sequentially at 150 DPI → save page-0001.png, page-0002.png, ... → summary`.

## Acceptance

1. Uses the currently open local PDF; no second document workflow.
2. Renders every page sequentially at a fixed 150 DPI baseline.
3. Creates one PNG per page with deterministic zero-padded names.
4. Output folder is explicit.
5. Existing output files are never silently overwritten; preflight fails before writing if any target exists.
6. Original PDF is read-only and never modified.
7. Cancellation/error leaves the original untouched and reports a controlled failure.
8. Representative automated evidence validates PNG signature, dimensions and page count/output count.
9. No new package/runtime/cloud/backend/second PDF engine.

## Honest limits

- Raster conversion only; text/searchability, links, annotations, forms and vector semantics do not survive as interactive PDF structures in PNG.
- Fixed 150 DPI baseline; no quality-preserving claim beyond the rendered pixels at that resolution.
- Password-protected PDFs are supported only when already open through the existing reader/session flow.
- No universal color-management or print-production fidelity claim.

## Explicit NO-GO / not baseline

- DOCX/PPTX/XLSX → PDF: would require a new Office/LibreOffice/conversion owner; not approved.
- PDF → DOCX/PPTX/XLSX: no existing semantic reconstruction owner; not approved.
- HTML/web/cloud conversion: not approved.
- Generic conversion framework, plugin architecture or format registry: not approved.
- JPEG/WebP/TIFF selectors, per-page format settings, DPI presets and batch conversion: future value decisions only.

## KISS / anti-noise audit

Use one focused `PdfToPngConverter` owner plus one existing-menu UI hook. Do not create `ConversionEngine`, format factories, workers, queues or generalized pipelines. Extend only after a second conversion proves a shared abstraction is actually needed.
