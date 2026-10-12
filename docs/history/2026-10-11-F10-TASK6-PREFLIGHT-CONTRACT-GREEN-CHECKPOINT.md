# F10 Task 6 — OCR Preflight Contract GREEN Checkpoint

Date: 2026-10-11 (America/Bogota)
Branch: `feat/f10-local-ocr`
Status: Task 6 OPEN / preflight contract GREEN

## RED evidence

- RED commit: `37b28f0077a759c0a92678c71071561d45662a29`
- Commit: `test(f10): require OCR-specific preflight result`
- Workflow: `38188118748`
- Job: `114614799240`
- Build: PASS, 0 warnings / 0 errors
- Tests: 1022 PASS / 1 expected FAIL / 0 SKIP / 1023 total
- Sole expected failure: `OcrWorkflowTests.Preflight_ContractReturnsOcrSpecificResult`
- Expected type: `SGPdf.App.Features.Ocr.OcrPreflightResult`
- Actual pre-GREEN type: `SGPdf.App.Features.Edit.PdfEditPreflightResult`

## GREEN evidence

- Functional commit: `8bd24fdce64f7b75112beb0efab11f8d88ed991e`
- Commit: `fix(f10): return OCR-specific preflight result`
- Workflow: `38188403616`
- Job: `114615650179`
- Repository hygiene: PASS
- Pinned Labelize 1.7.0 staging: PASS
- Pinned qpdf 12.4.2 staging: PASS
- Pinned Tesseract 5.5.3 staging: PASS
- Locked restore: PASS
- Release build: PASS, 0 warnings / 0 errors
- Full suite: 1023 PASS / 0 FAIL / 0 SKIP

## Contract now frozen

`OcrPreflight.Inspect(PdfDocumentSession, CancellationToken)` returns OCR-owned `OcrPreflightResult`.

The OCR result adapts the centralized `PdfEditPreflightInspector` findings rather than duplicating security inspection. It exposes:

- `Findings`
- `CanProceed`
- `IsPasswordBlocked`
- `IsSignedBlocked`
- first blocking `Reason`

Password and cryptographic-signature policy remains owned by the existing edit preflight. Cancellation is checked before and after delegation.

## Remaining Task 6 work

Next exact block is Step 2 RED/GREEN for `OcrWorkflow` happy paths:

- Spanish default
- English
- bilingual
- hybrid document (skip searchable page, OCR scan page)
- explicit current page / range
- rotated scan
- fully searchable document => `NoOp` and no destination publication

Step 3 hardening/failure atomicity remains after the happy-path workflow contract. Task 7 UI and F11 remain out of scope.

## Guardrails

- KISS/YAGNI
- sequential OCR only
- PDFium owns PDF; Tesseract recognition only
- Save As only; source unchanged
- one final transactional writer call
- no partial-success publication
- no merge to `main` without explicit approval
