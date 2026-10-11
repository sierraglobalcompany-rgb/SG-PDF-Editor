# F10 Task 5 — PdfOcrOutputValidator RED checkpoint

Date: 2026-10-11
Branch: `feat/f10-local-ocr`

## State

F10 remains ACTIVE. Task 5 planner and `PdfOcrWriter` are GREEN. `PdfOcrOutputValidator` is now at a verified RED checkpoint only; no production validator implementation exists yet.

Parent writer checkpoint: `03865c76a10201224a7843c1348e2060deea2cd8`
RED test commit: `af63575f35399a40993cf868d7de60d0a694aa6c`

## Contract under test

Plan/spec authoritative entry point:

`PdfOcrOutputValidator.Validate(string outputPath, OcrValidationExpectation expectation, CancellationToken cancellationToken) -> PdfOcrValidationResult`

The RED tests encode the minimum KISS expectation payload needed by the semantic validator:

- expected page count;
- OCR text placements and their expected PDF geometry;
- baseline text for skipped searchable pages;
- deterministic source renders for visual-preservation checks.

Production types `OcrValidationExpectation`, `PdfOcrOutputValidator`, and `PdfOcrValidationResult` have not been created yet.

## RED tests

File: `tests/SGPdf.App.Tests/PdfOcrOutputValidatorTests.cs`

Seven semantic cases were added:

1. accepts a valid true-invisible searchable-text fixture;
2. fails closed on page-count mismatch;
3. fails closed when an exact Spanish Unicode token is missing;
4. fails closed when baseline text on a skipped searchable page changes;
5. fails closed when deterministic render pixels differ from source;
6. fails closed when inserted-text geometry misses the expected OCR region;
7. fails closed on malformed output instead of returning publication confidence.

The tests reuse existing PDFium paths (`PdfDocumentSession`, F4 search/render behavior, and `OcrInvisibleTextNativeHarness`) rather than creating alternate PDF/search/render implementations.

## Exact-head Windows CI evidence

Workflow: `38183231635`
Exact head: `af63575f35399a40993cf868d7de60d0a694aa6c`

### Attempt 1

Job: `114600042332`

- repository hygiene: PASS;
- Labelize 1.7.0 staging: PASS;
- qpdf 12.4.2 staging: PASS;
- Tesseract 5.5.3 staging: PASS;
- locked restore: PASS;
- Release build: PASS, 0 warnings / 0 errors;
- tests: 1004 PASS / 8 FAIL / 0 SKIP / 1012 total.

Seven failures were the intended new validator RED cases. Each stopped at the reflection guard because `SGPdf.App.Pdf.OcrValidationExpectation` does not exist yet.

One unrelated existing test also failed in this first attempt:
`MainWindowOrganizeThumbnailTests.OneTileRenderFailure_IsIsolatedFromOtherTilesAndReaderSession`.
This was not treated as a validator failure and was not labeled flaky without evidence.

### Attempt 2 — same SHA, diagnostic rerun

Job: `114600389315`

- repository hygiene: PASS;
- Labelize 1.7.0 staging: PASS;
- qpdf 12.4.2 staging: PASS;
- Tesseract 5.5.3 staging: PASS;
- locked restore: PASS;
- Release build: PASS, 0 warnings / 0 errors;
- tests: **1005 PASS / 7 FAIL / 0 SKIP / 1012 total**.

All seven failures are exactly the new `PdfOcrOutputValidatorTests` and all stop because the expected production expectation/validator types are absent. The unrelated thumbnail failure from attempt 1 did not reproduce on the exact same SHA.

This is the required RED evidence: the previous 1005-test baseline is GREEN while only the seven new semantic validator tests remain RED.

## Guardrails preserved

- no production validator implementation added in this subblock;
- `PdfOcrWriter` functional implementation remains the previously verified GREEN writer;
- no workflow/UI work started;
- no F11 work started;
- no merge and no changes to `main`;
- PDFium remains the PDF owner;
- no alternate search/render stack introduced.

## Exact next action

Resume Task 5 / semantic validator GREEN:

1. implement the minimum `OcrValidationExpectation`, `PdfOcrValidationResult`, and `PdfOcrOutputValidator.Validate(...)` needed to satisfy these RED semantics;
2. reuse existing `PdfDocumentSession` page count, text extraction/search, geometry, and render APIs;
3. keep cancellation fail-closed and rethrow `OperationCanceledException`;
4. verify the seven validator tests GREEN, then full Windows CI;
5. only after validator GREEN, wire the semantic validator into the writer publication gate as required by the Task 5 plan;
6. do not open Task 6 workflow or Task 7 UI yet.
