# F10 Task 5 — transactional searchable OCR writer CLOSED

Date: 2026-10-11
Branch: `feat/f10-local-ocr`
Previous checkpoint: `007178f0fa8edca3aa1e488de81d87bbab5f7f1c`

## Scope

This checkpoint closes **F10 / Task 5 — Transactional searchable-layer writer and semantic validator**, including Step 7 end-to-end synthetic writer proof and Step 8 full-suite/exact-head Windows verification.

It does **not** start Task 6, WPF OCR UI, F11, or any merge to `main`.

## Step 7 RED

End-to-end writer tests were added at:

`a61a1c6cd2a1bd52541f5a30df480c86da74ed41`

They exercise the real default `PdfOcrWriter` with:

- image-only Spanish source becoming searchable/selectable/copyable after Save As;
- English placements round-tripping exactly;
- bilingual placements round-tripping exactly;
- a 90-degree rotated image-only source round-tripping searchable text;
- source bytes remaining unchanged;
- visible render bytes remaining unchanged on the deterministic fixture;
- sibling transaction temp cleanup;
- a readable-but-semantically-misplaced OCR output that must be rejected before publication.

Exact-head RED Windows CI:

- workflow: `38184396730`
- job: `114603521094`
- repository hygiene: PASS
- pinned Labelize staging: PASS
- pinned qpdf staging: PASS
- pinned Tesseract 5.5.3 staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **1016 PASS / 1 FAIL / 0 SKIP / 1017 total**

The sole intended RED was:

`PdfOcrWriterEndToEndTests.SaveAsCopy_DefaultPublicationGate_RejectsReadableButSemanticallyMisplacedOutput`

Expected `Succeeded == false`; actual was `true`. This proved the real product constructor was still using the old readable-copy gate instead of the semantic OCR validator.

The other four new end-to-end round-trip tests already passed in RED, establishing that the existing native writer path itself could write the required invisible searchable Unicode content.

## Minimum GREEN

Functional GREEN commit:

`25115627c75bb61b66c466e83ef8177e41a6f0ff`

`PdfOcrWriter` now keeps the existing injected boolean validator seam for focused transactional tests, but its **default product constructor** uses `PdfOcrOutputValidator.Validate`.

Before native mutation, the default path captures a semantic expectation from the source session:

1. expected page count;
2. exact native text for pages skipped by the OCR eligibility policy;
3. deterministic 72-DPI source renders for the pages that will receive OCR placements;
4. the exact managed OCR placements supplied to the writer.

After the temporary PDF is saved, but before atomic publication, `PdfOcrOutputValidator` must prove:

- page count unchanged;
- exact Unicode OCR tokens are searchable/extractable through the existing F4 reader path;
- reopened OCR geometry intersects the expected placement region;
- skipped searchable-page text is unchanged;
- visible render pixels of affected pages are byte-identical at the captured DPI.

If semantic validation fails, the existing destination remains untouched and the sibling temp is cleaned. PDF readability alone is no longer sufficient for the product publication path.

## GREEN verification

Exact-head Windows CI for `25115627c75bb61b66c466e83ef8177e41a6f0ff`:

- workflow: `38184579358`
- job: `114604057245`
- repository hygiene: PASS
- pinned Labelize staging: PASS
- pinned qpdf staging: PASS
- pinned Tesseract 5.5.3 staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **1017 PASS / 0 FAIL / 0 SKIP / 1017 total**
- workflow conclusion: **success**

The complete suite contains and passed the targeted Task 5 writer/planner/validator tests. No separate targeted-only GitHub workflow was introduced merely to duplicate the stronger exact-head full-suite proof.

## Task 5 closure state

- Step 1 planner RED: CLOSED.
- Step 2 planner GREEN/geometry acceptance: CLOSED.
- Step 3 transactional writer RED: CLOSED.
- Step 4 writer GREEN: CLOSED.
- Step 5 semantic validator RED: CLOSED.
- Step 6 semantic validator GREEN/publication gate primitive: CLOSED.
- Step 7 end-to-end synthetic writer proof: CLOSED.
- Step 8 full suite + exact-head Windows CI: CLOSED.

**Task 5 automated status: CLOSED / GREEN.**

## Guardrails preserved

- PDFium remains the PDF owner.
- Tesseract does not generate the final PDF.
- True invisible render mode remains the Task 1-proven PDFium path.
- Save As only; source is never overwritten.
- Existing destination is replaced only after validation.
- Source fingerprint is rechecked before mutation and publication.
- Cancellation propagates and transaction temps are cleaned.
- Unsupported glyphs remain fail-closed.
- No cloud/backend/account/API key/telemetry dependency was added.
- No merge to `main` was performed.

## Manual/external evidence

Real Windows OCR UX, Adobe/Foxit/Edge reader checks, private PDF corpus, low-quality real scans, and representative user-hardware performance remain **NOT RUN**.

## Exact resume point

Next allowed work is **F10 / Task 6 / Step 1 — RED preflight tests** only:

- cryptographically signed PDF blocks before raster/Tesseract;
- password-opened PDF blocks before raster/Tesseract;
- ordinary source proceeds;
- user-facing failure text must not claim signature/password removal support.

Do not start Task 6 implementation before observing the Step 1 RED. Do not start Task 7, Task 8, F11, or merge `main`.
