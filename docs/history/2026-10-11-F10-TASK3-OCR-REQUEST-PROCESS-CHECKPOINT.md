# F10 — Task 3 OCR request/process core checkpoint

Date: 2026-10-11
Branch: `feat/f10-local-ocr`
Functional head verified before this checkpoint: `42002f61f9dd75d287b99f9540a88129fdc778f9`
Status: **TASK 3 GREEN / CLOSED**

## Delivered

- Immutable OCR language/request/page-scope models.
- Deterministic page eligibility using extracted PDF text.
- V1 meaningful-text boundary: `<20` alphanumeric code points remains OCR-eligible; `>=20` is treated as already searchable.
- Automatic/current/range resolution with ordered candidate/skipped page indices and invalid-range rejection.
- PDFium page-text extraction support using `FPDFText_CountChars`.
- Private `TesseractProcessRecognizer` under `Features/Ocr`.
- Exact `spa`, `eng`, `spa+eng` language arguments and explicit private `--tessdata-dir`.
- TSV-only recognition, no shell/PATH fallback, stdout/stderr capture.
- Non-zero exit and missing/empty TSV fail closed; stale TSV is removed before execution and not accepted as success.
- Timeout and caller cancellation kill the entire child process tree.
- Real bundled-runtime Spanish OCR smoke fixture including `NIÑO`.

## TDD evidence

- RED process behavior head: `9fcf8031f4102d7781172e7671fa673bfa963742`.
- RED result: Release build `0 warnings / 0 errors`; `964 PASS / 5 FAIL`, with all five failures caused by the intentional `NotImplementedException` in the recognizer/runner.
- Implementation head: `2939971a9d8a181e2eaae0da4e3d0c7c2105b7e1`.
- Mechanical `System.IO` import correction: `a134ecfe786116834d03d1406b7c3333aedecd01`.
- Test-harness-only .NET 10 async task subtype correction: `42002f61f9dd75d287b99f9540a88129fdc778f9`.

## Exact-head Windows CI

Workflow: `38158670272`
Job: `114526817887`
Checkout: `42002f61f9dd75d287b99f9540a88129fdc778f9`

- repository hygiene: PASS
- Labelize staging: PASS
- qpdf staging: PASS
- Tesseract staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **969 PASS / 0 FAIL / 0 SKIP**

Only GitHub Actions Node/punycode deprecation noise was observed; it is not product C# compiler output.

## Next

Task 4 only: 300-DPI rasterization + bounded allocation guard, then strict TSV parser, then PDF coordinate mapping. Keep those as separate RED/GREEN slices so a failure has one cause.

Manual Windows OCR UX, Adobe Reader, Foxit, Edge and private real-scan corpus remain **NOT RUN**.