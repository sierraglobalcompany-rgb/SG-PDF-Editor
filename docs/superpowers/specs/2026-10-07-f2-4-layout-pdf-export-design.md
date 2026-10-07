# F2.4 — Layout + PDF Export Design

**Date:** 2026-10-07  
**Status:** approved in chat  
**Base:** F2.3 final head `1f33f48390681c6e4000c17329f1763eb1551047`

## Goal

Convert the rendered ZPL labels from F2.2/F2.3 into physically correct sheet/page layouts and export them as local PDFs without stretching barcodes or depending on a printer subsystem.

## Scope

- output media: Thermal, A4 (210 × 297 mm), Letter (215.9 × 279.4 mm), Custom;
- orientation: portrait / landscape for non-thermal media;
- layout capacity presets: 1/2/3/4/6/8/10/12 plus custom rows × columns;
- horizontal/vertical margins and horizontal/vertical gaps in millimeters;
- explicit label rotation 0° / 90°;
- WPF sheet preview driven by the same physical geometry used by export;
- PDF export through PDFsharp only after the pure geometry and preview slices are green;
- export must preserve exact physical label rectangles; never silently shrink-to-fit.

## Non-goals

- no Windows/thermal printing (F2.5);
- no printer capability discovery (F2.5);
- no automatic barcode/QR decode hardening or scanner QA (F2.6);
- no runtime network access;
- no BinaryKits fallback;
- no automatic merge.

## Core model

### Output sequence

F2.3 quantities may be very large, so F2.4 must not expand copies into a giant in-memory list. `LabelOutputSequence` exposes a checked `long TotalCount` and resolves an output index to a printable design lazily from `ZplDocument` + `ZplQuantitySelection`.

### Physical geometry

All layout calculations use millimeters. A placement stores page-relative `X`, `Y`, `Width`, `Height`, design index and rotation. Rotation 90° swaps the occupied width/height but does not rescale the rendered label.

`LabelLayoutSettings` owns media size, orientation, margins, gaps, capacity/grid and label rotation. `LabelLayoutPlanner` validates whether the requested layout physically fits and returns deterministic page geometry.

Preset capacity `N` searches factor-pair grids whose `rows × columns == N`; among fitting grids choose the one whose grid aspect ratio is closest to the usable page aspect ratio. Custom layout uses the requested rows/columns directly. Thermal output is always one label per page with page size equal to the current label's physical size and zero layout scaling.

If the requested geometry does not fit, planning fails with a user-readable reason. It never scales labels to force a fit.

### Preview

The WPF sheet preview consumes planner placements and existing managed PNG bytes. It scales the *whole sheet view* to the viewport for display only. It does not mutate export geometry or rerender Labelize when only layout/margins/gaps change.

Keep an explicit `Etiqueta | Hoja` preview mode so F2.3 individual-design navigation remains available. Sheet navigation is `Hoja n de N`.

### PDF export

Use stable `PDFsharp 6.2.4` (MIT, `net10.0`) only in the export task. Each PDF page is created at the exact planned physical dimensions; PNGs are drawn into the planned rectangles in points converted from millimeters. Rotation is applied geometrically; images are not independently stretched to make an invalid layout fit.

Export is local and transactional: write a temporary PDF beside the requested destination, close/validate it, then replace/move into the destination only after success. Failure leaves the previous destination untouched.

### Validation

F2.4 automated validation covers planner dimensions, page counts, exact PDF MediaBox/page dimensions and successful reopen/render through the existing PDFium path. Barcode/QR automatic decode remains F2.6 to avoid pulling ZXing early.

## UX defaults

- output medium: Thermal while one-label exact-size workflow is active;
- sheet media selection exposes A4, Letter, Custom;
- layout default: 1;
- margins/gaps default: 0 mm;
- rotation default: 0°;
- custom dimensions are explicit and validated as finite positive millimeters;
- invalid layouts disable export and show why they do not fit.

## Dependency ruling

As of 2026-10-07, NuGet lists PDFsharp 6.2.4 as the latest stable release supporting `net10.0`; 7.0.0 remains preview. Pin 6.2.4 when Task 3 begins and record the MIT license in project dependency documentation/lock files.
