# F10 Task 6 — Workflow Hardening GREEN Checkpoint

Date: 2026-10-11 (America/Bogota)
Branch: `feat/f10-local-ocr`
Status: Task 6 OPEN / Step 3 hardening seam GREEN

## Step 2 prerequisite

- happy-path checkpoint: `3cd6655a2daeb52590e70f88f57a22ff0c6558b9`
- exact CI: `38189376925`
- full suite: 1031 PASS / 0 FAIL / 0 SKIP

## RED evidence

- RED commit: `28fe17b930c4a339f771659bf8d14c87e97700de`
- commit: `test(f10): define OCR workflow failure atomicity`
- workflow: `38189591423`
- job: `114619268270`
- Release build: PASS, 0 warnings / 0 errors
- tests: **1031 PASS / 12 expected FAIL / 0 SKIP / 1043 total**
- every new failure was caused by the workspace-aware `OcrWorkflow` constructor being absent

The RED covers:

- render failure
- Tesseract non-zero-exit-shaped failure
- timeout
- invalid UTF-8-shaped parser failure
- malformed TSV-shaped parser failure
- failure on a later OCR page after earlier placements were accumulated
- transactional writer semantic-validation failure
- stale source fingerprint forwarded to the writer and failed closed
- cancellation during render
- cancellation during recognition/process boundary
- cancellation during writer boundary
- blocked preflight before workspace creation/rasterization
- existing destination bytes preserved on every represented failure path
- request workspace recursively removed on represented failure/cancellation paths

## GREEN implementation

First GREEN attempt:

- commit: `c6763d229296c6fb59780ffd1242d1cf1c42040a`
- workflow: `38189749838`
- build exposed one constructor-signature collision (`CS0111`) because nullable annotations do not differentiate CLR signatures
- no tests ran; this was not accepted as GREEN

Corrected functional GREEN:

- commit: `68193bd8f771e053379729b59bfe89d43f4e484d`
- commit: `fix(f10): disambiguate OCR workflow constructors`
- workflow: `38189837702`
- job: `114620014896`
- repository hygiene: PASS
- Labelize/qpdf/Tesseract pinned staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- full suite: **1043 PASS / 0 FAIL / 0 SKIP**

## Behavior now proven

The existing Step 2 constructor remains intact.

A workspace-aware overload now:

1. performs fingerprint capture, open, preflight and eligibility before requesting a workspace;
2. does not create a workspace for blocked preflight or a fully searchable no-op;
3. creates one request-scoped workspace only when OCR candidates exist;
4. passes the exact workspace path into the page recognition/planning boundary;
5. processes candidates sequentially;
6. never calls the final writer when render/recognition/parser-shaped work fails on any page;
7. invokes the writer only after all candidate placements are accumulated;
8. preserves cancellation as `OperationCanceledException`;
9. recursively removes the request workspace from `finally` on success, failure and cancellation;
10. leaves destination publication exclusively to the already-validated transactional `PdfOcrWriter`.

## Important limitation / next block

Step 3 proves orchestration hardening through injected boundaries. **Production OCR wiring is still not complete.**

Task 6 Step 4 must connect the real local pipeline:

- unique request workspace factory
- PDFium `OcrRasterizer` at 300 DPI
- minimal local raster-file serializer accepted by the pinned Tesseract sidecar
- `TesseractProcessRecognizer`
- strict `TesseractTsvParser`
- `OcrTextLayerPlanner`
- page-local raster/TSV cleanup while retaining only managed placements
- real `PdfOcrWriter` single final call

A dedicated failing test must prove the production constructor/wiring before implementation. No image/PDF engine should be added merely to serialize the OCR raster.

## Guardrails

- KISS / YAGNI
- sequential pages only
- no partial-success publication
- local/offline only
- PDFium owns PDF
- Tesseract recognition only
- Task 7 UI unopened
- F11 unopened
- `main` untouched; no merge without explicit approval
