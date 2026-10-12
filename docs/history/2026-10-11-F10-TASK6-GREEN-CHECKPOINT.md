# F10 — Task 6 GREEN checkpoint

Date: 2026-10-11 / 2026-10-12 UTC rollover
Branch: `feat/f10-local-ocr`
Status: **Task 6 CLOSED / GREEN**

## Scope closed

Task 6 — OCR workflow orchestration and hardening.

Closed behavior:

- OCR preflight wraps the existing edit preflight and blocks password-opened and cryptographically signed PDFs.
- `OcrPreflightResult` exposes OCR-specific `CanProceed`, password/signature flags, reason, and findings without duplicating the underlying inspection policy.
- Happy path supports Spanish default, English, Spanish+English, hybrid documents, current/range scopes, rotated raster forwarding, and fully searchable no-op.
- Workflow is sequential and fail-closed: page-by-page raster/recognition/planning; `PdfOcrWriter` is invoked once at the end; no partial-success publication.
- Request workspace and per-page raster/TSV artifacts are cleaned on render/process/parser/page-N/writer/cancellation failure paths.
- Production constructor `(sourcePath, destinationPath)` is wired to the real PDFium rasterizer, pinned Tesseract runtime, strict TSV parser, text-layer planner, and transactional OCR writer.
- Production raster serialization is local BMP only; no second PDF/image engine was introduced.
- Source fingerprint is captured before OCR and forwarded to the writer.
- Progress is monotonic by eligible page: `Rendering -> Recognizing` per page, then `Writing -> Validating` after a successful writer result.

## TDD evidence

### Production wiring RED

Commit: `5e1eb95f29d0c1c7991765c8b71d1bec797e74c8`
CI run: `38191151699`
Result:

- Build: 0 warnings / 0 errors
- Tests: 1043 PASS / 1 FAIL / 0 SKIP
- The only failure was the intentionally missing `(string, string)` production `OcrWorkflow` constructor.

### Production wiring GREEN

Functional commit: `19b8b6c0a92bf721f12fa70d5d66f7f5b73586b2`
CI run: `38191240632`
Job: `114624255090`
Result:

- Build: 0 warnings / 0 errors
- Tests: 1044 PASS / 0 FAIL / 0 SKIP
- Real image-only Spanish scan became searchable through the production workflow while the source remained byte-for-byte unchanged.

### Progress RED

Commit: `8de59f7dd94b0b11278ba244efc03e00c0d29780`
CI run: `38191510290`
Result:

- Build: 0 warnings / 0 errors
- Tests: 1045 PASS / 1 FAIL / 0 SKIP
- The sole intended failure proved `Validating` was not yet emitted.

### Progress GREEN / Task 6 functional head

Functional commit: `c5e35830a7b95e39c01aab36ac787c2a773227f8`
CI run: `38191661069`
Job: `114625512865`
Result:

- Build: 0 warnings / 0 errors
- Tests: **1046 PASS / 0 FAIL / 0 SKIP**
- Progress order includes final `Validating` only after writer success.

## Closure constraints

- Task 7 / OCR WPF UI has not started.
- Task 8 / preservation and F10 final closure has not started.
- F11 has not started.
- `main` remains untouched and no merge is authorized.
- Manual Windows/Adobe/Foxit/Edge/private-corpus QA is still NOT RUN unless separately evidenced.

## Next permitted work

Begin **F10 Task 7** with RED tests for the OCR WPF command/dialog/view-model integration defined by the active F10 plan. Do not start Task 8 or F11 until Task 7 is closed and exact-head CI is green.
