# F10 Task 6 — Production OCR Wiring PAUSE Checkpoint

Date: 2026-10-11/12 (America/Bogota)
Branch: `feat/f10-local-ocr`
Status: Task 6 OPEN / Step 4 production wiring GREEN candidate under exact-head CI

## Last fully proven checkpoint before this block

- checkpoint: `2e3b5efc1ab83d4e640457b33493194a0da44dcc`
- message: `docs(f10): checkpoint OCR workflow hardening green`
- exact CI: `38189977378`
- result: SUCCESS
- full suite at that checkpoint: **1043 PASS / 0 FAIL / 0 SKIP**
- Step 3 hardening/failure atomicity: GREEN

## Step 4 RED — production constructor / real local pipeline

A dedicated production end-to-end test was added in:

- `tests/SGPdf.App.Tests/OcrProductionWorkflowTests.cs`

Corrected RED commit:

- `5e1eb95f29d0c1c7991765c8b71d1bec797e74c8`
- message: `test(f10): fix production OCR result assertion`
- exact workflow: `38191151699`
- job: `114623993687`
- Release build: **0 warnings / 0 errors**
- tests: **1043 PASS / 1 expected FAIL / 0 SKIP / 1044 total**
- only failure: production `OcrWorkflow(string sourcePath, string destinationPath)` constructor was absent (`Assert.NotNull` on constructor)

The RED proves that Step 4 must expose and execute a real production path rather than only injected orchestration seams.

## Step 4 GREEN candidate implemented

Production code added/changed:

1. `src/SGPdf.App/Features/Ocr/OcrRasterFileSerializer.cs`
   - commit `3b77c373c74ceeb4c49dae07e62330a3a9a42a24`
   - writes a minimal 24-bit top-down BMP directly from PDFium BGRA pixels
   - no second image/PDF engine or new package
   - cleans partial file on failure/cancellation

2. `src/SGPdf.App/Features/Ocr/OcrWorkflow.cs`
   - functional candidate commit `19b8b6c0a92bf721f12fa70d5d66f7f5b73586b2`
   - adds production constructor `(string sourcePath, string destinationPath)`
   - production wiring:
     - `OcrPreflight`
     - `OcrPageEligibilityPolicy`
     - `OcrRasterizer` at 300 DPI
     - request-scoped unique temp workspace
     - BMP raster serializer
     - bundled `TesseractProcessRecognizer`
     - strict `TesseractTsvParser`
     - `OcrTextLayerPlanner`
     - page-local image/TSV cleanup
     - single final transactional `PdfOcrWriter.SaveAsCopy`
   - production recognition timeout currently fixed at 2 minutes per page
   - sequential pages only; no partial publication

## Exact-head CI status at pause

For functional candidate `19b8b6c0a92bf721f12fa70d5d66f7f5b73586b2`:

- workflow: `38191240632`
- job: `114624255090`
- repository hygiene: PASS
- Labelize 1.7.0 staging: PASS
- qpdf 12.4.2 staging: PASS
- Tesseract 5.5.3 staging: PASS
- locked restore: PASS
- Release build: PASS
- test step: **IN PROGRESS at the moment this checkpoint was requested**

Therefore **Step 4 must NOT yet be called GREEN/CLOSED** from this checkpoint alone.

## Exact resume action

On resume:

1. Inspect workflow `38191240632` / job `114624255090` first.
2. If it finished SUCCESS with 1044/1044 tests, record Step 4 GREEN and create a proper GREEN checkpoint.
3. If it failed, read the exact failing test/log and fix only the root cause; do not weaken `OcrProductionWorkflowTests` unless the test itself is demonstrably wrong.
4. Only after Step 4 exact-head GREEN, open Task 6 Step 5 RED for progress ordering/monotonicity.
5. Then targeted Task 6 tests, full suite, exact-head CI, and Task 6 closure checkpoint.

## Guardrails still active

- KISS / YAGNI
- local/offline only
- PDFium owns PDF
- Tesseract recognition only
- no Tesseract-generated final PDF
- no parallel OCR
- source untouched
- destination publication only through transactional writer
- no partial-success publication
- Task 7 UI NOT STARTED
- Task 8 NOT STARTED
- F11 NOT STARTED
- `main` remains frozen; no merge without explicit user approval

## Stop instruction

This file is a deliberate PAUSE checkpoint requested by the user. Do not infer Step 4 GREEN from the implementation alone; resume from the exact CI evidence above.