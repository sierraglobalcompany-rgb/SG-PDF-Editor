# F6 — Task 1 checkpoint — capability gate + image discovery

**Date:** 2026-10-09  
**Branch:** `feat/f6-images`  
**Planning base:** `3d350de870733008413367ff9082058b8bc74723`  
**Functional head before this checkpoint:** `21b763353b22b3b39c024a9c6fca1b4ec3ceebce`  
**Scope:** Task 1 only — exact PDFium capability gate, synthetic image fixtures, managed image discovery/bitmap snapshots, and optional capability characterization.

## 1. Status

Task 1 functional code/tests are GREEN on Windows CI before this documentation commit.

- Workflow run: `38006722002`
- Head: `21b763353b22b3b39c024a9c6fca1b4ec3ceebce`
- Restore locked dependencies: PASS
- Release build: PASS
- Build warnings: 0
- Build errors: 0
- Tests: 560 passed / 0 failed / 0 skipped

A fresh exact-head CI is required after this checkpoint commit before Task 1 is considered closed.

## 2. Core exact-runtime capability gate

The pinned runtime remains `bblanchon.PDFium.Win32 156.0.8076`.

The exact packaged `pdfium.dll` exports all Task-1 core functions probed by `PdfiumImageEditApiAvailabilityTests`:

- `FPDFPage_CountObjects`
- `FPDFPage_GetObject`
- `FPDFPageObj_GetType`
- `FPDFPageObj_GetBounds`
- `FPDFPageObj_GetMatrix`
- `FPDFImageObj_GetImageMetadata`
- `FPDFImageObj_GetBitmap`
- `FPDFBitmap_GetWidth`
- `FPDFBitmap_GetHeight`
- `FPDFBitmap_GetFormat`
- `FPDFPageObj_SetMatrix`
- `FPDFPage_RemoveObject`
- `FPDFPage_GenerateContent`
- `FPDF_SaveAsCopy`

No Task-1 core stop condition was triggered.

## 3. Conditional exports

All four conditional candidate exports probed in this pinned runtime are present:

- `FPDFPageObj_SetFillColor`
- `FPDFPage_InsertObjectAtIndex`
- `FPDFPageObj_GetRotatedBounds`
- `FPDFImageObj_GetRenderedBitmap`

Presence alone is not treated as product support. Task 1 added representative save/reopen/render characterization before freezing the initial verdict below.

## 4. Managed image discovery

`PdfDocumentSession.Images.cs` now returns managed snapshots only:

- `PdfObjectMatrix`
- `PdfObjectBounds`
- `PdfImageObjectMetadata`
- `PdfImageObjectInfo`
- `PdfImageBitmap`

`GetImageObjects(...)`:

- enumerates page objects under `PdfiumRuntime.NativeGate`;
- returns only `FPDF_PAGEOBJ_IMAGE` objects;
- records original page-object ordinal;
- validates finite matrix/bounds and plausible image metadata;
- closes page handles in `finally`;
- exposes no native `IntPtr` as durable model identity.

`GetImageBitmap(...)`:

- validates page/object index and image type;
- obtains the PDFium image bitmap under `NativeGate`;
- copies pixels to managed memory before native cleanup;
- normalizes Gray/BGR/BGRx/BGRA inputs to managed BGRA output;
- destroys the native bitmap and closes the page handle in `finally`.

The optional rendered-object bitmap route is characterized separately in Task 1; product selection of rendered-vs-direct extraction remains a later F6.4 decision.

## 5. Synthetic fixtures and behavior evidence

Public synthetic fixtures cover:

- one image;
- image + vector neighbor;
- multiple images;
- rotated/scaled non-square image;
- overlapping images + vector object;
- image near a page edge.

The tests demonstrate:

- image-only discovery while a vector neighbor is ignored;
- finite/plausible original ordinal, affine matrix, bounds and metadata;
- managed BGRA bitmap extraction;
- rotated bounds return finite quad coordinates;
- rendered-image-object bitmap route returns non-zero dimensions;
- matrix mutation persists through `GenerateContent` + Save As + reopen and the output renders;
- representative opacity 255/128/0 survives save/reopen/render with expected visual alpha behavior;
- representative exact-index z-order mutation changes visible paint order across image/vector/image without duplicating or losing objects.

For the overlap fixture, source object enumeration is `[image, vector, image]` and the last image paints above the earlier objects. After moving the first image to the final index, enumeration becomes `[vector, image, image]` and the moved image paints on top. This is representative evidence for F6.2/F6.6, not a universal claim about every PDF construction.

## 6. Optional capability verdict after Task 1

Initial exact-runtime verdict for the representative synthetic corpus:

- rotated-bounds route: **PROVEN AVAILABLE / representative behavior PASS**;
- rendered-image-object bitmap route: **PROVEN AVAILABLE / representative behavior PASS**;
- opacity route: **PROVEN AVAILABLE / representative save-reopen-render PASS**;
- exact-index z-order route: **PROVEN AVAILABLE / representative save-reopen-render PASS**.

These verdicts authorize later slices to write RED product tests for these capabilities. They do not bypass later undo/redo, writer safety, preservation, or regression requirements.

## 7. Exact scope audit

Relative to planning head `3d350de870733008413367ff9082058b8bc74723`, Task 1 functional work touches only:

1. `src/SGPdf.App/Pdf/PdfDocumentSession.Images.cs`
2. `src/SGPdf.App/Pdf/PdfiumNative.cs`
3. `tests/SGPdf.App.Tests/ImageEditBehaviorCharacterizationTests.cs`
4. `tests/SGPdf.App.Tests/ImageEditCapabilityCharacterizationTests.cs`
5. `tests/SGPdf.App.Tests/ImageEditNativeCharacterizationHarness.cs`
6. `tests/SGPdf.App.Tests/ImageEditPdfFixtureFactory.cs`
7. `tests/SGPdf.App.Tests/PdfImageObjectDiscoveryTests.cs`
8. `tests/SGPdf.App.Tests/PdfiumImageEditApiAvailabilityTests.cs`

This checkpoint document is the ninth Task-1 file.

No WPF/editor surface, workspace/history model, writer, preservation policy, project dependency, lockfile, FIRMAR, ORGANIZAR, LEER or ZPL behavior is intentionally changed in Task 1.

## 8. Boundaries preserved

- PDFium remains the only PDF editor engine for F6.
- No runtime package added.
- No cloud/network dependency added.
- No customer/private fixtures committed.
- No persistent native handles in UI/domain models.
- No F6.2 UI implementation started.
- No merge to `main`.
- No F6 PR created yet.
- Manual Windows UX QA remains NOT RUN.

## 9. Next gate

After fresh exact-checkpoint CI passes, stop Task 1.

The next user-approved `continua` may start **Task 2 — F6.1 logical edit model, source fingerprint and command history**. Do not start Task 2 in the same closure step.
