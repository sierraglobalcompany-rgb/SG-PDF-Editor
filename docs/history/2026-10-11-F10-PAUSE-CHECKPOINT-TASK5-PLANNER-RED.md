# F10 Pause Checkpoint — Task 5 Planner RED

Date: 2026-10-11
Branch: `feat/f10-local-ocr`
Checkpoint base HEAD before this document: `277d6e0c5c826ea5c4e29d63b8bd6c3d9be629b9`
Phase: `F10 — OCR local`
Task: `Task 5 — Transactional searchable-layer writer and semantic validator`
Status: **PAUSED after planner RED; production planner NOT implemented**

## 1. Resume point

Resume from **Task 5 / Step 2**, implementing only the minimum line-oriented `OcrTextLayerPlanner` needed to make the new planner tests green.

Do **not** start `PdfOcrWriter`, `PdfOcrOutputValidator`, workflow, UI, F11, merge, or `main` work before the planner subblock is green and verified.

## 2. Work completed in this session

Task 5 Step 1 RED was created in:

- `tests/SGPdf.App.Tests/OcrTextLayerPlannerTests.cs`

The tests define the initial executable planner contract:

1. TSV words sharing `(block, paragraph, line)` become one placement.
2. Joined line text uses single spaces.
3. First-appearance line ordering remains stable.
4. Unicode survives (`NIÑO`, `Bogotá`, `número`).
5. A non-empty OCR word with confidence `0` remains present; F10 V1 has no confidence cutoff.
6. Per-word mapped PDF geometry is retained.
7. Rotated-page mapped bounds remain inside valid PDF geometry.
8. Placement carries `PageIndex`, `Text`, `Bounds`, `Words`, and deterministic `SourceOrder` metadata expected by later validation.

No production `OcrTextLayerPlanner.cs` exists yet at this checkpoint.

## 3. RED evidence — first run

RED commit:

`c215944bfec8ef6a30730684b0fdc07871174dcd`

Message:

`test(f10): define OCR text layer planner contract`

GitHub Actions run:

`38164607872`

Windows job:

`114544451948`

Evidence:

- locked restore: PASS
- build: PASS
- build warnings: `0`
- build errors: `0`
- tests passed: `992`
- tests failed: `4`
- tests skipped: `0`
- total: `996`

All four failures were the new `OcrTextLayerPlannerTests`, failing because `SGPdf.App.Features.Ocr.OcrTextLayerPlanner` did not yet exist (`Assert.NotNull` in `InvokeBuild`). This is the intended RED cause.

Failing new tests:

- `Build_GroupsSameTsvLineWithSingleSpaces_AndKeepsPerWordGeometry`
- `Build_RotatedMappedBoundsStayInsidePdfPageGeometry`
- `Build_PreservesFirstAppearanceOrderAcrossDistinctLines`
- `Build_PreservesUnicodeAndConfidenceZeroWords`

## 4. Test harness correction

A small reflection-helper defect was noticed before GREEN: `Get<object>` used exact-type assertion instead of assignable-type assertion.

It was corrected in:

`277d6e0c5c826ea5c4e29d63b8bd6c3d9be629b9`

Message:

`test(f10): fix planner reflection helper`

This change modifies only the test helper behavior and does not implement production OCR planner logic.

## 5. RED evidence — second run

GitHub Actions run:

`38164782009`

Windows job:

`114544962489`

Evidence:

- locked restore: PASS
- build: PASS
- build warnings: `0`
- build errors: `0`
- OCR planner tests: still RED for the intended reason — planner type absent
- unrelated existing test also failed once:
  - `MainWindowOrganizeThumbnailTests.LargeDocument_OnlyRealizedPlusNeighborThumbnailsHoldBitmaps`
  - expected `[49, 50, 51, 52, 53]`
  - actual `[50]`

Second-run totals:

- passed: `991`
- failed: `5`
- skipped: `0`
- total: `996`

Interpretation: the four OCR planner tests remain correctly RED. The organize-thumbnail failure is outside the files touched by this Task 5 RED and must not be misclassified as an OCR regression without separate reproduction. Before declaring Task 5 GREEN, rerun the complete suite and report this test explicitly if it reproduces.

## 6. Existing production primitives already verified for reuse

Do not duplicate these:

- `OcrCoordinateMapper.Map(PdfRenderedPage, OcrPixelRect)` owns OCR bitmap-to-PDF affine mapping and validates out-of-device rectangles.
- `PdfRenderedPage` already carries `PageIndex` and internal `DeviceTransform`.
- `OcrWord` already carries TSV source metadata, confidence, Unicode text, and pixel bounds.
- `TesseractTsvParser` already preserves non-empty confidence-0 words and has no confidence threshold.

The planner should consume these primitives rather than invent rotation/orientation logic.

## 7. Minimum GREEN design boundary

The next implementation should remain KISS:

- Create only `src/SGPdf.App/Features/Ocr/OcrTextLayerPlanner.cs` plus the minimal placement models needed by that file.
- `Build(IReadOnlyList<OcrWord> words, PdfRenderedPage renderedPage)`.
- Group by TSV `(Block, Paragraph, Line)` in first-appearance/source order.
- Map each word through `OcrCoordinateMapper.Map`.
- Preserve each word's text, confidence, source metadata, and mapped PDF rectangle.
- Join line text with exactly one space between OCR words.
- Compute the line bounding rectangle as the union of mapped word rectangles.
- Emit deterministic `SourceOrder` starting at zero by first line appearance.
- Use `renderedPage.PageIndex` for the PDF page index.
- Do not add paragraph/column/reflow logic.
- Do not apply a confidence cutoff.
- Do not touch PDFium native writing yet.

After implementation:

1. Run planner-targeted tests.
2. Run the complete suite.
3. If the organize-thumbnail test fails again, diagnose it separately; do not weaken OCR tests to hide it.
4. Require exact-head Windows CI before considering the planner subblock green.

## 8. Explicit STOP boundary

At this checkpoint the following are intentionally **NOT STARTED** in Task 5:

- `PdfOcrWriter.cs`
- `PdfOcrWriterTests.cs`
- `PdfOcrOutputValidator.cs`
- `PdfOcrOutputValidatorTests.cs`
- line-selection geometry acceptance proof against a saved searchable OCR PDF
- word-oriented fallback decision
- end-to-end searchable-layer persistence
- Task 5 closure commit

## 9. Guardrails still binding

- `main` remains frozen; no merge without explicit user approval.
- Keep work on `feat/f10-local-ocr`.
- Offline-only OCR.
- Tesseract recognizes only; it never owns the final PDF.
- PDFium remains PDF owner.
- Save As / source untouched / transaction semantics remain mandatory for the later writer.
- Manual Windows/Adobe/Foxit/Edge/private-corpus QA remains NOT RUN unless actually executed.
- Do not mark Task 5 closed until targeted tests, full suite, exact-head Windows CI, writer/validator evidence, and checkpoint evidence are all complete.

## 10. Exact next action

**Implement the minimum `OcrTextLayerPlanner` GREEN against the four existing RED tests, and stop before `PdfOcrWriter` until that planner subblock is independently green.**
