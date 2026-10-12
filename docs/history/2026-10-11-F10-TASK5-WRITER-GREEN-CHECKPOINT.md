# F10 Task 5 — PdfOcrWriter GREEN checkpoint

Date: 2026-10-11
Branch: `feat/f10-local-ocr`
Phase: F10 — OCR local/offline
Task: 5 — transactional searchable-layer writer + semantic validator

## Status at this checkpoint

- `OcrTextLayerPlanner`: GREEN / closed.
- `PdfOcrWriter`: GREEN / closed for its transactional writer contract.
- `PdfOcrOutputValidator`: NOT STARTED.
- OCR workflow: NOT STARTED.
- WPF OCR UI: NOT STARTED.
- F10 PR: none.
- `main`: untouched/frozen; no merge performed.

## Writer RED contract

Tests were added first in:

- `tests/SGPdf.App.Tests/PdfOcrWriterTests.cs`
- RED commit: `be671716d5165dea6a6baf830fd8198d5bab25dd`
- commit message: `test(f10): define OCR writer contract`

Exact-head Windows evidence:

- workflow: `38167515223`
- job: `114553044400`
- Release build: PASS, 0 warnings / 0 errors
- tests: 996 PASS / 9 FAIL / 0 SKIP / 1005 total
- all 9 new failures were expected because `SGPdf.App.Pdf.PdfOcrWriter` did not yet exist.

The RED contract covers at minimum:

1. source and destination paths must differ;
2. stale source fingerprint fails without publication;
3. out-of-range page target fails without publication;
4. unsupported fallback-font glyph fails closed;
5. successful Save As never changes source bytes;
6. validator failure preserves an existing destination and cleans the sibling temp;
7. validation occurs before publication;
8. pre-cancelled execution does not mutate or publish;
9. cancellation during the write/validation path preserves destination and cleans temp.

## GREEN implementation history

Initial writer implementation:

- commit: `c1860b162b35c8311779dd26e881554b2d73d789`
- message: `feat(f10): add transactional OCR writer`
- CI workflow `38167704760`, job `114553611733`
- expected implementation iteration exposed a compile-only defect: missing `System.IO`; tests did not run.

Mechanical import correction:

- commit: `0f863e9ba08b344f87f32e1d0c86d9bd945ce091`
- message: `fix(f10): import IO for OCR writer`
- CI workflow `38167826627`, job `114553968019`
- build then exposed two incorrect invented API references plus one redundant value-type null check; tests did not run.

Final minimum writer correction:

- commit: `c7c9db664602d48471d398ce010f420ffce25453`
- message: `fix(f10): use OCR PDFium render mode binding`

The final implementation uses the already-proven F10 Task 1 PDFium binding directly:

- `PdfiumNative.FPDFTextObj_SetTextRenderMode(...)`
- `PdfiumNative.FPDF_TEXTRENDERMODE_INVISIBLE` (`3`)

No new compatibility wrapper or invented render-mode gate was introduced.

## Final exact-head GREEN evidence

Exact SHA verified by checkout: `c7c9db664602d48471d398ce010f420ffce25453`

Windows CI:

- workflow: `38168132617`
- job: `114554876280`
- repository hygiene: PASS
- Labelize 1.7.0 staging: PASS
- qpdf 12.4.2 staging: PASS
- Tesseract 5.5.3 staging: PASS
- locked restore: PASS
- Release build: PASS
- warnings: 0
- errors: 0
- full test suite: **1005 PASS / 0 FAIL / 0 SKIP**

## Writer behavior now fixed

`PdfOcrWriter` now follows the established transactional writer pattern already used by the project:

- Save As only; source is never the publication target.
- source fingerprint checked before mutation and again before publication.
- preflight is applied before native mutation.
- page index is validated against the opened source.
- OCR text is written into a sibling temporary PDF.
- PDFium remains the PDF owner.
- fallback CID Type2 font mapping is reused; unsupported glyphs fail closed with `NotSupportedException`.
- native mutation is serialized with `PdfiumRuntime.NativeGate.Wait/Release`.
- OCR words are inserted with true invisible text render mode via the pinned PDFium API.
- cancellation is honored before and during mutation and before publication.
- the temporary output is validated before publication.
- an existing destination is preserved on validation failure/cancellation.
- sibling temporary output is cleaned on failure/cancellation.
- publication uses replace/move only after successful validation.

The default writer validator is intentionally only a readability guard at this checkpoint. It is not the Task 5 semantic OCR validator and must not be treated as such.

## Guardrails that remain in force

- Do not merge to `main` without explicit user approval.
- Do not create or merge an F10 PR unless explicitly appropriate/approved.
- Do not begin F11.
- Do not duplicate rotation logic already owned by `OcrCoordinateMapper` / `PdfRenderedPage.DeviceTransform`.
- Do not use Tesseract-generated PDF as final output.
- Do not claim manual Adobe/Foxit/Edge/private-corpus QA unless actually performed.
- Evidence before PASS; automated evidence is not manual evidence.
- KISS/YAGNI: one authoritative writer route, no abstraction added without a demonstrated need.

## Exact next action

Resume from **F10 / Task 5 — semantic output validator**.

1. Verify branch HEAD and this checkpoint before editing.
2. Re-read the Task 5 validator contract in `docs/superpowers/plans/2026-10-11-f10-local-ocr.md` and the F10 design spec.
3. Inspect only the existing validator/session primitives needed for semantic verification.
4. Add `PdfOcrOutputValidatorTests` first (RED).
5. The semantic validator must verify the saved temporary PDF, not merely that it opens.
6. Demonstrate exact RED evidence before implementing `PdfOcrOutputValidator`.
7. Implement the minimum validator GREEN and wire it into the default `PdfOcrWriter` path only after RED.
8. Run full exact-head Windows CI and checkpoint the validator separately.

STOP here. The semantic validator, OCR workflow and UI are intentionally outside this writer checkpoint.
