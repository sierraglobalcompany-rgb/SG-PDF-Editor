# F10 Task 6 — Workflow Happy Path GREEN Checkpoint

Date: 2026-10-11 (America/Bogota)
Branch: `feat/f10-local-ocr`
Status: Task 6 OPEN / Step 2 happy path GREEN

## Preflight prerequisite

Task 6 preflight contract was already frozen GREEN before this block:

- contract GREEN functional commit: `8bd24fdce64f7b75112beb0efab11f8d88ed991e`
- checkpoint commit: `7bca91ffc50834965a2918fe36f676caa3ca249b`
- full suite at checkpoint: 1023 PASS / 0 FAIL / 0 SKIP
- Release build: 0 warnings / 0 errors

## RED evidence

The first Step 2 test commit was not accepted as valid RED because the test harness itself did not compile:

- commit: `58df615292062a4c1a53d6e4021c888c0156e53e`
- workflow: `38188904716`
- job: `114617166901`
- failure: CS0051 caused by exposing internal `OcrLanguage` as a parameter of a public xUnit theory method

The harness was corrected without production changes:

- valid RED commit: `115e44606c5bac66f0e5ddaca3cb02a43a263582`
- commit: `fix(f10): keep OCR workflow RED tests buildable`
- workflow: `38189029899`
- job: `114617556184`
- Release build: PASS, 0 warnings / 0 errors
- tests: 1023 PASS / 8 expected FAIL / 0 SKIP / 1031 total
- all 8 expected failures were caused by `SGPdf.App.Features.Ocr.OcrWorkflow` being absent

The RED contract covers:

- Spanish as default language
- English
- Spanish + English
- one writer publication call after an eligible page
- hybrid document: searchable page skipped, scan page processed
- explicit current-page scope
- explicit range scope
- rotated raster forwarded intact to recognition/planning
- fully searchable document => `NoOp`, no raster, no writer, existing destination unchanged

## GREEN implementation

New production models:

- `src/SGPdf.App/Features/Ocr/OcrResult.cs`
- `src/SGPdf.App/Features/Ocr/OcrProgress.cs`
- `src/SGPdf.App/Features/Ocr/OcrWorkflow.cs`

Intermediate commits:

- `3632db45c7bd1f3b55e7b3f613390715df4617b3` — result model
- `7162523b3514c0fe7b2c14eb0832a962bf9f4345` — progress model
- `b138439c05bbe07501229e59cba21bf1b3419dea` — initial workflow implementation; CI correctly exposed a missing namespace import before tests

Functional GREEN head:

- `caf7d20d8b2e3db6d268108daf8f4833e9673e62`
- commit: `fix(f10): import edit fingerprint for OCR workflow`
- workflow: `38189270520`
- job: `114618302371`
- repository hygiene: PASS
- pinned Labelize 1.7.0: PASS
- pinned qpdf 12.4.2: PASS
- pinned Tesseract 5.5.3: PASS
- locked restore: PASS
- Release build: PASS, 0 warnings / 0 errors
- full suite: **1031 PASS / 0 FAIL / 0 SKIP**

## Behavior now proven

The Step 2 orchestration seam:

1. captures the source fingerprint once;
2. opens one PDFium session;
3. runs OCR preflight;
4. resolves eligibility using the requested scope;
5. returns `NoOp` when there are no OCR candidates and does not rasterize or publish;
6. processes eligible pages sequentially;
7. forwards the exact requested language;
8. preserves the rendered-page/device-transform object through the recognition/planning boundary;
9. accumulates managed placements;
10. invokes the transactional writer exactly once at the end;
11. returns processed and skipped searchable page indices.

`OcrResult` has only completed outcomes `Succeeded` and `NoOp`; cancellation remains exception-based.

## Deliberate scope boundary

This is NOT the final production OCR workflow yet.

The current happy-path constructor is an injected orchestration seam. Task 6 Step 3/4 must still prove and wire:

- request-scoped raster/TSV temporary artifacts;
- real `TesseractProcessRecognizer` execution;
- strict `TesseractTsvParser` use;
- `OcrTextLayerPlanner` integration;
- page-by-page temp cleanup;
- Tesseract non-zero exit and timeout;
- invalid UTF-8 / malformed TSV / parser failure;
- failure on a later page after earlier OCR work;
- writer semantic-validation failure;
- stale source fingerprint;
- cancellation during render / process / write;
- no partial-success publication;
- destination byte preservation on every failure path;
- progress ordering including validation semantics.

No raster image encoder has been added yet. If the production sidecar path requires one, keep it minimal and local; do not introduce a second image/PDF engine.

## Next exact block

Task 6 Step 3 RED: `OcrHardeningTests` for failure atomicity and cleanup. Only after a valid RED should Step 4 wire the real sequential local sidecar path.

## Guardrails

- KISS / YAGNI
- local/offline only
- PDFium owns PDF; Tesseract recognition only
- source unchanged
- one final writer publication attempt
- no partial success
- no parallel Tesseract
- Task 7 UI remains unopened
- F11 remains unopened
- no merge to `main` without explicit approval
