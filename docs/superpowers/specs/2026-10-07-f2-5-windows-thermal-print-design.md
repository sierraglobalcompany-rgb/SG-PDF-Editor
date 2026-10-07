# F2.5 — Windows Thermal Print Design

**Date:** 2026-10-07  
**Status:** written spec awaiting user review  
**Base:** F2.4 final head `1355ddf1ea92c7b3b25addf05f7722dec6fe57b9`  
**Branch:** `feat/f2-5-windows-thermal-print`

## Intent

Print ZPL labels from SG PDF Editor through normal installed Windows printer drivers while preserving the physical dimensions already selected and validated in F2.3/F2.4.

The user must be able to choose a thermal printer in the normal Windows print dialog and know, before anything is spooled, which printer, physical size, resolution and total label count will be used.

Success means that SG PDF Editor never silently shrinks or stretches a label to satisfy a driver, never multiplies quantities through a second copy counter, and blocks the job when the driver substitutes an incompatible media size.

## Scope

F2.5 adds only the Windows thermal-print path for ZPL labels:

- installed printer selection through WPF `PrintDialog`;
- `PrintQueue` / `PrintTicket` / `PrintCapabilities` preflight;
- exact requested label page size from the current F2.4 Thermal plan;
- driver validation through `MergeAndValidatePrintTicket`;
- imageable-area clipping warning, never auto-scaling;
- request a matching printer resolution when the driver exposes one;
- one physical label per `DocumentPaginator` page;
- quantities come only from `LabelOutputSequence`;
- current Labelize PNGs are reused; layout changes do not rerender Labelize;
- confirmation summary before spool submission;
- Windows spooler submission through the selected driver.

## Non-goals

- no RAW ZPL transport;
- no direct USB/serial/network printer protocol;
- no Zebra/TSC/Xprinter-specific SDK;
- no printer darkness/speed/calibration controls;
- no A4/Letter multi-up printing in this slice;
- no second layout engine;
- no PDF intermediary for thermal printing;
- no barcode/QR decode or scanner validation (F2.6);
- no automatic printer installation or driver download;
- no runtime Internet access.

## Existing foundations reused

F2.5 must reuse rather than duplicate:

- `ZplDocument` and `ZplQuantitySelection` from F2.1/F2.3;
- `ZplRenderedLabel` PNGs produced by Labelize;
- `LabelOutputSequence` for lazy copy ordering;
- `LabelLayoutPlan` / `LabelLayoutPlanner` from F2.4;
- current applied `ZplRenderOptions` width, height and dpmm;
- existing WPF busy/status/error patterns.

The existing `PdfDocumentPaginator` is **not** reused for labels because it intentionally uses `FitInsidePage`, which scales a PDF page to printable area. Thermal labels require exact physical geometry instead.

## Chosen architecture

```text
Current ZPL workspace
  ├─ ZplDocument
  ├─ ZplQuantitySelection
  ├─ ZplRenderedLabel[]
  └─ Thermal LabelLayoutPlan (1 label/page)
             ↓
Windows PrintDialog
             ↓
selected PrintQueue + dialog PrintTicket
             ↓
WindowsThermalPrintPreflight
  ├─ request exact PageMediaSize
  ├─ force CopyCount = 1
  ├─ request matching resolution when available
  ├─ MergeAndValidatePrintTicket
  └─ inspect validated size + PageImageableArea
             ↓
ThermalPrintPreflightResult
  ├─ valid / blocked
  ├─ warnings
  └─ summary
             ↓ user confirms
LabelPrintPaginator
  └─ exact physical page + existing PNG
             ↓
PrintDialog.PrintDocument(...)
             ↓
Windows driver / spooler
```

No generic printer abstraction/plugin framework is introduced. Keep the Windows-specific adapter small and keep validation math independently testable where practical.

## Media-size authority

The authoritative requested dimensions are the **currently applied F2.3/F2.4 physical label dimensions**, not the printer driver's defaults.

For Thermal media:

- one output label = one physical page;
- requested page width/height come from the current thermal `LabelLayoutPlan`;
- WPF device-independent units use `96 / 25.4` units per millimeter;
- label content is drawn at that requested physical rectangle;
- no `FitInsidePage`, stretch-to-printable-area, auto-orientation or shrink-to-fit is allowed.

### Driver size validation

The app may request a custom `PageMediaSize(width, height)` even when the exact size is not listed in `PageMediaSizeCapability`; `MergeAndValidatePrintTicket` is the final authority on what the driver accepted.

After validation:

- compare the validated media width/height against the requested dimensions;
- allow a transport/driver quantization tolerance of **0.5 mm per dimension**;
- this tolerance is only for deciding whether the driver accepted the requested media; it does **not** authorize scaling the label content;
- if either dimension differs by more than 0.5 mm, block the job before spooling;
- orientation swaps are accepted only when they correspond exactly to the requested orientation, never silently.

Reason for tolerance: Windows print units and driver-reported media commonly quantize physical dimensions. F2.6 physical QA may tighten or revise this threshold from real hardware evidence.

## Imageable-area policy

`PrintCapabilities.PageImageableArea` is advisory for F2.5.

- if the reported imageable area does not cover the full requested label rectangle, show a clipping warning;
- do **not** reduce or translate the label merely to fit the imageable area;
- warning includes the reported non-printable offsets/extent when available;
- user may cancel or explicitly continue;
- actual clipping behavior is measured with physical printers in F2.6.

This follows the master-plan rule that imageable area warns about clipping but never causes silent shrink-to-fit.

## Resolution policy

Printer resolution comes from `PrintCapabilities.PageResolutionCapability`.

F2.5 behavior:

1. inspect the current applied label dpmm;
2. translate known dpmm presets to their approximate printer DPI class:
   - 6 dpmm ≈ 152 dpi;
   - 8 dpmm ≈ 203 dpi;
   - 12 dpmm ≈ 300 dpi;
   - 24 dpmm ≈ 600 dpi;
3. if the printer exposes a matching resolution, request it in the candidate `PrintTicket`;
4. if no matching resolution is exposed, keep the driver's validated resolution and show a warning;
5. F2.5 does not automatically mutate the workspace or silently rerender labels solely because the selected printer has another DPI.

A later physical test may prove that print-only rerendering at native printer DPI is necessary; that change belongs in F2.6 unless evidence during F2.5 makes it mandatory for correctness.

## Quantity policy

`LabelOutputSequence` is the only quantity authority.

- `Del archivo`, `Una de cada`, or `Personalizada por diseño` determine the print sequence;
- `PrintTicket.CopyCount` is normalized to **1**;
- if the user selected more than one copy in the Windows dialog, the confirmation warns that SG PDF Editor controls quantity and the validated ticket will use one spooler copy;
- do not multiply `^PQ`/custom quantity by Windows copies;
- sequence remains lazy and must not allocate one object per requested copy;
- because WPF `DocumentPaginator.PageCount` is `int`, a total label count above `Int32.MaxValue` is blocked with a clear message rather than overflowing.

No arbitrary lower hard cap is added in F2.5. The confirmation always shows the total label count so accidental large jobs are visible before submission.

## LabelPrintPaginator

A dedicated `LabelPrintPaginator` is required because PDF printing has different scaling semantics.

Responsibilities:

- consume `LabelOutputSequence`, thermal `LabelLayoutPlan`, and the existing per-design `ZplRenderedLabel` collection;
- `PageCount` = total output labels, after checked `int` validation;
- each page resolves its design lazily from the output sequence;
- `PageSize` = exact requested thermal media size in WPF units;
- draw white background plus the selected PNG at the exact full requested label rectangle;
- no centering/scaling calculation based on printable area;
- no PDF generation;
- no Labelize call during normal print pagination.

The driver/XPS pipeline may rasterize/resample internally, but SG PDF Editor does not alter the requested physical geometry.

## UI flow

The label properties panel gains `Imprimir etiquetas...` below `Guardar como PDF...`.

Enable only when:

- a ZPL document is loaded;
- rendered labels exist;
- current label layout is valid;
- current output medium is **Thermal**;
- app is not busy.

Flow:

1. user clicks `Imprimir etiquetas...`;
2. standard Windows `PrintDialog` opens;
3. user selects printer/preferences and presses Print;
4. **nothing is spooled yet**;
5. app obtains selected `PrintQueue` + `PrintTicket` and performs preflight;
6. if blocked, show the specific reason and return to the workspace;
7. if valid, show confirmation summary;
8. on confirmation, assign the validated ticket and call `PrintDocument` with `LabelPrintPaginator`;
9. show success/failure status without changing the current ZPL workspace.

### Confirmation summary

Minimum information:

```text
Impresora: Zebra ZD421
Tamaño: 102 × 152 mm
Resolución: 203 dpi
Etiquetas: 25
Copias de Windows: 1
Escalado SG PDF Editor: ninguno
```

Warnings appear directly below, for example:

- `El área imprimible reportada no cubre toda la etiqueta; puede haber recorte.`
- `La impresora no reporta una resolución equivalente a 203 dpi; se usará la resolución validada por el driver.`
- `La cantidad se controla en SG PDF Editor; las copias del diálogo se ajustaron a 1.`

Buttons: `Imprimir` / `Cancelar`.

## Failure semantics

Before spool submission, any of these block the job:

- no selected `PrintQueue`;
- printer capabilities cannot be read;
- validated media size incompatible by more than tolerance;
- total quantity cannot fit `DocumentPaginator.PageCount`;
- thermal plan is missing/invalid;
- rendered design count does not match printable design count;
- validated `PrintTicket` cannot be produced.

Warnings do not mutate the workspace.

If `PrintDocument` throws after confirmation, report failure; do not alter quantities, label dimensions, selected design or preview.

## Offline/privacy

- all printer capability queries are local Windows APIs;
- no HTTP/API/service call;
- no temporary PDF;
- no new persistent temp label files;
- existing managed PNG bytes feed the paginator;
- remote/network printers may of course be handled by Windows if the user selected one, but SG PDF Editor itself does not create network connections or discover printers over the Internet.

## Testing strategy

CI cannot depend on a physical printer, so F2.5 separates deterministic validation from real-driver smoke.

### Automated tests

Cover at minimum:

- mm ↔ WPF-unit conversions;
- validated exact-size acceptance;
- <=0.5 mm media quantization acceptance without content scaling;
- >0.5 mm media substitution blocking;
- swapped/incompatible dimensions blocking;
- imageable-area shortfall produces warning, not scaling;
- matching resolution selection;
- unsupported/missing resolution warning;
- Windows copies normalized to one;
- output quantities are not multiplied twice;
- lazy design sequence preserved;
- `Int32.MaxValue` paginator boundary/overflow block;
- paginator page size equals requested physical label size;
- paginator never calls the PDF `FitInsidePage` path;
- paginator chooses correct design for repeated quantities;
- UI print button only enabled for valid Thermal layout;
- cancel/preflight failure does not invoke spool submission.

Use small local delegates/test seams where needed for `PrintDialog`/preflight submission; do not create a general printer framework.

### Manual / physical QA deferred to F2.6 or available hardware

- Zebra/TSC/Xprinter or available Windows thermal driver;
- 102×152, 100×150, 100×100 and one custom size;
- 203/300/600 dpi where hardware exists;
- measure printed physical dimensions;
- edge/clipping behavior;
- barcode/QR scanner validation;
- Windows copy-count interaction;
- offline/privacy/temp-residue smoke.

## Expected files when implemented

Likely small surface, subject to the implementation plan:

- `src/SGPdf.App/Printing/LabelPrintPaginator.cs`;
- `src/SGPdf.App/Printing/WindowsThermalPrintPreflight.cs` or equivalent small Windows adapter;
- one small pure validation/result model if needed;
- `src/SGPdf.App/MainWindow.LabelLayout.cs` or a focused `MainWindow.LabelPrinting.cs` partial;
- `src/SGPdf.App/MainWindow.xaml`;
- focused tests under `tests/SGPdf.App.Tests/`.

No new NuGet package is expected. `System.Printing`, WPF `PrintDialog` and the existing Windows Desktop target should be sufficient.

## Rejected approaches

### Print the F2.4 PDF through the existing PDF print path

Rejected because `PdfDocumentPaginator` intentionally fits the page to printable area. Thermal output needs exact physical size and must not inherit that scale-to-fit behavior.

### Send RAW ZPL directly

Rejected for F2.5 because it would couple the product to specific printer languages/drivers and would not serve generic Windows thermal printers. It can be reconsidered only as an explicit later feature with device/language detection and physical evidence.

### Vendor SDKs

Rejected: unnecessary dependency, reduced printer portability, and contrary to the current KISS/free/offline goal.

## Acceptance criteria

F2.5 automated closure requires:

1. current F2.4 layout remains the geometry authority;
2. selected Windows printer is preflighted through validated `PrintTicket`;
3. incompatible driver media substitution blocks before spool;
4. imageable-area issues warn but never scale;
5. Windows copy count cannot multiply SG PDF Editor quantity;
6. `LabelPrintPaginator` draws exact requested physical page geometry;
7. no PDF intermediary, RAW ZPL path or new runtime package;
8. build/tests/CI green with fresh evidence;
9. state/roadmap/history updated;
10. PR remains draft/unmerged until explicit user approval.

Physical printer/scanner acceptance remains an explicit later/manual gate and must not be falsely claimed from CI alone.
