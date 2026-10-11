# F9 Task 5 — Focused WPF utility commands — Completion checkpoint

Date: 2026-10-11
Branch: `feat/f9-offline-utilities`
Previous checkpoint: `49ad2d4359d736ef5105b2da38a3d727c8c85281`
Functional GREEN before this checkpoint: `6273fd69f7d0ddde9c2b75088bfafdb2970df5af`
Main at closure audit: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## 1. Scope

Task 5 wires the already-implemented F9 text overlay stack into focused WPF product commands:

- text watermark;
- page numbering / Bates numbering;
- small validation dialogs;
- Save As output only;
- safe cancellation and error reporting;
- persistence delegated to the Task 3 transactional `PdfTextOverlayWriter`;
- geometry/numbering delegated to Task 4 planners.

No alternate PDF writer, parser, planner, package, native interop, or workflow was added.

## 2. Image watermark ruling

Task 1 characterized the required PDFium image primitives as GO. However, the canonical Task 5 path is required to delegate persistence to the Task 3 utility writer, and Task 3 currently implements text overlays only. There is no product-level image overlay planner/writer with transactional validation in the current F9 stack.

Therefore Task 5 intentionally exposes only:

1. text watermark; and
2. page numbering / Bates.

This is fail-closed and KISS: a measured native capability is not presented as a product feature until its product persistence contract exists. No fake or partially persisted image-watermark command was added.

## 3. Valid RED baseline

Commit:

`fe87670bfbf595d67ae7d661f732528b1a1a12dd`

Message:

`test(f9): add red focused utility command contracts`

CI:

- workflow: `38113694202`
- job: `114394264094`
- build: 0 warnings / 0 errors
- pre-existing tests: 912 PASS
- new Task 5 tests: exactly 6 FAIL
- total: 918

The six expected RED contracts covered:

- `Herramientas` commands present and disabled without a PDF;
- commands enabled after opening a PDF;
- text-watermark Save As cancellation is a no-op;
- invalid watermark request is rejected before writing;
- page-numbering Save As cancellation is a no-op;
- writer/source error preserves the source and returns a controlled status.

The RED tests use reflection so missing Task 5 product types fail behaviorally without breaking compilation.

## 4. Production implementation

### Request/adaptation models

Added:

- `src/SGPdf.App/Features/Utilities/WatermarkUtilityRequest.cs`
- `src/SGPdf.App/Features/Utilities/PageNumberingUtilityRequest.cs`

Responsibilities:

- validate user-facing values;
- convert 1-based UI pages to zero-based `UtilityPageRange`;
- support All / Current / Explicit range behavior;
- map background/foreground layer;
- map RGB into `PdfTextFillColor`;
- build the existing Task 2/4 model objects;
- keep planning and persistence out of the request classes.

### Focused dialogs

Added:

- `WatermarkDialog.xaml`
- `WatermarkDialog.xaml.cs`
- `PageNumberingDialog.xaml`
- `PageNumberingDialog.xaml.cs`

Text watermark exposes:

- text;
- All / Current / Range scope;
- 9 anchors;
- font size;
- opacity;
- rotation;
- background/foreground;
- RGB.

Page numbering / Bates exposes:

- initial number;
- prefix and suffix;
- minimum padding width;
- All / Current / Range scope;
- 6 header/footer anchors;
- font size;
- opacity;
- background/foreground;
- RGB.

Offsets remain `0` in the focused dialogs. The underlying planners retain their offset support; Task 5 deliberately does not overload the UI with technical controls.

### MainWindow adapter

Added:

`src/SGPdf.App/MainWindow.Utilities.cs`

Responsibilities:

- expose `Herramientas`;
- expose `Marca de agua de texto...`;
- expose `Numerar páginas / Bates...`;
- disable utility commands until a PDF is available;
- block execution while other edit/sign/organize/ZPL modes or busy execution are active;
- open the focused dialogs;
- request a PDF Save As destination;
- turn requests into Task 4 placements;
- capture `PdfEditSourceFingerprint`;
- delegate persistence to `PdfTextOverlayWriter`;
- surface success/error/cancel state through `StatusText`;
- reset `_utilityExecuting` in all paths;
- never overwrite or mutate the source PDF.

## 5. MainWindow.xaml operational deviation

The canonical plan listed a thin `MainWindow.xaml` edit. The GitHub connector available in this session returned that large XAML file truncated, while its update action replaces the complete file. Replacing an incomplete ~24 KB XAML file would be unsafe.

To avoid destructive blind editing, Task 5 reuses the repository's established dynamic MainWindow pattern: a class-level `Loaded` hook in `MainWindow.Utilities.cs` creates and registers the `Herramientas` menu items at runtime.

Result:

- same visible command surface;
- no risky full-file XAML replacement;
- `MainWindow.xaml` remains untouched;
- no architectural duplication.

## 6. GREEN history

### GREEN attempt 1 — WPF generated accessibility mismatch

Functional SHA:

`be7d803696475dbf85be767f0d50f7d45fb49ba6`

CI:

- workflow: `38114012364`
- job: `114395177051`

Result:

- restore PASS;
- build FAIL;
- 0 warnings;
- 2 errors `CS0262` because generated XAML partial classes were public while code-behind classes were internal;
- tests skipped.

Fix: align only the two code-behind class accessibility modifiers to `public`.

### GREEN attempt 2 — request test-harness shape mismatch

SHA:

`df4293dfe555d85703d46550a85aba885b77c59e`

CI:

- workflow: `38114089570`
- job: `114395403683`
- build: 0 warnings / 0 errors
- tests: 914 PASS / 4 FAIL / 0 SKIP / 918 total.

The remaining four failures were test-harness shape failures: C# `record` types generate copy constructors, while the reflection harness expected exactly one request constructor.

Fix: change the two immutable request carriers from `record` to `sealed class`; API and product behavior otherwise unchanged.

### First full GREEN

SHA:

`cff87e50625e00333862186ab274731d19a1cfbe`

CI:

- workflow: `38114226201`
- job: `114395791704`
- build: 0 warnings / 0 errors
- tests: 918 PASS / 0 FAIL / 0 SKIP.

This is the first full GREEN for all Task 5 RED contracts.

## 7. End-to-end command verification

Additional verification-only tests were then added in:

`tests/SGPdf.App.Tests/MainWindowUtilitiesTests.cs`

Commit:

`6273fd69f7d0ddde9c2b75088bfafdb2970df5af`

Message:

`test(f9): verify utility command materialization`

The two added tests call the actual private MainWindow utility adapters and verify:

### Text watermark

- Save As succeeds to a different file;
- original source bytes remain unchanged;
- output reopens through `PdfDocumentSession`;
- watermark text exists on both selected pages;
- success status is surfaced;
- `_utilityExecuting` resets.

### Bates numbering

- Save As succeeds to a different file;
- original source bytes remain unchanged;
- output reopens;
- the three selected pages contain, in order:
  - `SG-001001`
  - `SG-001002`
  - `SG-001003`
- success status is surfaced;
- `_utilityExecuting` resets.

Exact-head CI for this functional GREEN:

- workflow: `38114352282`
- job: `114396166026`
- exact SHA: `6273fd69f7d0ddde9c2b75088bfafdb2970df5af`
- build: 0 warnings / 0 errors
- tests: **920 PASS / 0 FAIL / 0 SKIP**

## 8. Scope audit

Diff from Task 4 checkpoint `49ad2d4359d736ef5105b2da38a3d727c8c85281` to functional GREEN `6273fd69f7d0ddde9c2b75088bfafdb2970df5af` contains exactly eight paths:

1. `src/SGPdf.App/Features/Utilities/PageNumberingDialog.xaml`
2. `src/SGPdf.App/Features/Utilities/PageNumberingDialog.xaml.cs`
3. `src/SGPdf.App/Features/Utilities/PageNumberingUtilityRequest.cs`
4. `src/SGPdf.App/Features/Utilities/WatermarkDialog.xaml`
5. `src/SGPdf.App/Features/Utilities/WatermarkDialog.xaml.cs`
6. `src/SGPdf.App/Features/Utilities/WatermarkUtilityRequest.cs`
7. `src/SGPdf.App/MainWindow.Utilities.cs`
8. `tests/SGPdf.App.Tests/MainWindowUtilitiesTests.cs`

Confirmed absent from the Task 5 diff:

- `MainWindow.xaml` modification;
- package changes;
- lock-file changes;
- PDFium/native changes;
- Task 3 writer changes;
- Task 4 planner changes;
- workflow changes;
- image-watermark product implementation;
- Task 6 work.

## 9. Integration state

- branch: `feat/f9-offline-utilities`
- `main` remains untouched at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`
- no merge performed;
- no PR merge performed;
- no package additions;
- no Task 6 work started.

## 10. Manual QA

Manual interactive WPF QA: **NOT RUN**.

Automated command/UI contracts and Save As/reopen integration are GREEN, but this checkpoint does not claim manual visual/dialog QA.

## 11. Stop condition

Task 5 is functionally complete at `6273fd69f7d0ddde9c2b75088bfafdb2970df5af`, pending only exact-head CI for this checkpoint documentation commit.

Do not begin Task 6 until this checkpoint commit itself is verified GREEN and the user explicitly continues.
