# F10 Task 5 — OCR output validator GREEN checkpoint

Date: 2026-10-11
Branch: `feat/f10-local-ocr`
RED checkpoint: `1a76f020765b00861cffa79af5562ba292563158`
Functional GREEN head: `e580ac5109723d9d3c2e44edcf3873e8cf00e529`

## Scope closed in this checkpoint

This checkpoint closes only **Task 5 / Step 6 — semantic OCR output validator GREEN**.

It does **not** start Task 5 / Step 7, Task 6, OCR workflow/UI, F11, or any merge to `main`.

## Production implementation

`src/SGPdf.App/Pdf/PdfOcrOutputValidator.cs` now provides:

- `OcrValidationExpectation` with expected page count, OCR placements, skipped-page native text, and deterministic source renders.
- `PdfOcrValidationResult` with fail-closed `Succeeded` / `Error` semantics.
- `PdfOcrOutputValidator.Validate(...)` that reopens the temporary output through the existing `PdfDocumentSession` / PDFium path before publication confidence is granted.

The validator proves:

1. page count is unchanged;
2. every expected OCR token is found by the existing F4 `FindTextOnPage` path and round-trips through `GetTextRange` with exact ordinal Unicode text;
3. the reopened OCR text geometry intersects each expected OCR line/placement region;
4. skipped searchable pages retain exact full-page native text through `GetPageText`;
5. deterministic source renders retain identical dimensions, stride, and pixel bytes at the same DPI;
6. malformed/unreadable output and semantic mismatches fail closed;
7. caller cancellation continues to propagate as `OperationCanceledException`.

No second search, render, OCR, or PDF engine was added.

## RED evidence

RED tests were introduced at:

`af63575f35399a40993cf868d7de60d0a694aa6c`

RED checkpoint:

`1a76f020765b00861cffa79af5562ba292563158`

Exact-head RED CI:

- workflow: `38183509976`
- job: `114600862807`
- Release build: 0 warnings / 0 errors
- tests: **1005 PASS / 7 FAIL / 0 SKIP / 1012 total**
- all seven intended validator tests failed because the production expectation/validator types were absent.

## First GREEN candidate and root-cause correction

Initial product implementation:

`25a57eef22f375c971cddb69250d4673feaa25d9`

Exact-head CI:

- workflow: `38183800374`
- job: `114601733888`
- Release build: 0 warnings / 0 errors
- tests: **1011 PASS / 1 FAIL / 0 SKIP / 1012 total**

The sole failure was the happy-path semantic fixture: `Bogotá` was searchable and exact, but the first validator version incorrectly required **every individual token rect** to intersect the same synthetic common OCR region.

Root-cause review against the Task 1 capability harness showed that the proven contract is:

- representative Unicode tokens are independently searchable/extractable; and
- the inserted text **placement** has geometry intersecting the intended OCR source region.

Task 1 did not prove that every independently searched token must intersect one shared artificial rectangle.

The focused correction at:

`e580ac5109723d9d3c2e44edcf3873e8cf00e529`

therefore keeps exact Unicode validation per token while validating geometry once at the line/placement level. Unicode and render requirements were not relaxed.

## Functional GREEN verification

Exact-head Windows CI for `e580ac5109723d9d3c2e44edcf3873e8cf00e529`:

- workflow: `38183947210`
- job: `114602172511`
- repository hygiene: PASS
- pinned Labelize staging: PASS
- pinned qpdf staging: PASS
- pinned Tesseract 5.5.3 staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **1012 PASS / 0 FAIL / 0 SKIP / 1012 total**
- workflow conclusion: **success**

## Task 5 state

- Planner: GREEN.
- Transactional writer: GREEN at its existing writer checkpoint/evidence.
- Semantic output validator: GREEN at `e580ac5109723d9d3c2e44edcf3873e8cf00e529`.
- Task 5 / Step 7 end-to-end synthetic writer proof: **NOT STARTED**.
- Task 5 is therefore **not yet closed**.

Important remaining integration point: the production writer currently has its existing transactional validator hook/default readable-copy path. Step 7 must prove the actual searchable OCR publication path end to end and wire the semantic validator into the publication gate wherever the product path still lacks that semantic expectation.

## Manual/external evidence

Real Windows OCR UX, Adobe/Foxit/Edge reader checks, private PDF corpus, low-quality real scans, and representative user-hardware performance remain **NOT RUN**.

## Exact resume point

Resume **Task 5 / Step 7 — end-to-end synthetic writer proof** only.

Required proof before moving to Task 6:

- image-only Spanish synthetic source becomes searchable/selectable/copyable;
- English and bilingual placements round-trip;
- rotated fixture passes;
- source bytes remain untouched;
- actual publication is gated by the semantic validator, not merely PDF readability;
- destination atomicity and existing writer guarantees remain intact;
- full exact-head Windows CI stays green.

Do not open Task 6, WPF OCR UI, F11, or merge `main` until this step is closed with evidence.
