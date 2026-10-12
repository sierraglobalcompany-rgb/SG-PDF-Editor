# F10 Task 6 — PAUSE CHECKPOINT

Date: 2026-10-11 (America/Bogota)
Branch: `feat/f10-local-ocr`
Status: PAUSED BY USER / TASK 6 OPEN

## Repository state at pause

- Branch: `feat/f10-local-ocr`
- HEAD before this checkpoint doc: `37b28f0077a759c0a92678c71071561d45662a29`
- Commit message: `test(f10): require OCR-specific preflight result`
- `main`: untouched; no merge performed.
- F11: not started.

## Last fully GREEN functional state

### OCR preflight wrapper

- RED commit: `64b584a4d79f7fe1942dbe113f6e35351cb65657`
- RED workflow: `38187677009`
- RED job: `114613448727`
- RED build: PASS, 0 warnings / 0 errors
- RED tests: 1017 PASS / 5 expected FAIL / 0 SKIP / 1022 total
- Expected cause: `OcrPreflight` type did not yet exist.

- GREEN functional commit: `c75601461dc1fa206ea123640326ee995c2e390b`
- GREEN workflow: `38187807120`
- GREEN job: `114613848098`
- GREEN build: PASS, 0 warnings / 0 errors
- GREEN tests: 1022 PASS / 0 FAIL / 0 SKIP

### Preflight checkpoint doc

- Checkpoint commit: `d650afd404841c29ecb0ce81f3d1ede25af3acef`
- Workflow: `38187980668`
- Job: `114614377973`
- Exact-head CI: PASS

## Active RED at pause

A contract audit against the approved F10 Task 6 plan found a real mismatch:

- Approved interface requires:
  `OcrPreflight.Inspect(...) -> OcrPreflightResult`
- Current implementation returns:
  `PdfEditPreflightResult`

A focused RED was added to prevent carrying this mismatch into workflow/UI work.

- RED commit: `37b28f0077a759c0a92678c71071561d45662a29`
- Commit message: `test(f10): require OCR-specific preflight result`
- Workflow: `38188118748`
- Job: `114614799240`
- Build: PASS, 0 warnings / 0 errors
- Tests: 1022 PASS / 1 expected FAIL / 0 SKIP / 1023 total
- Sole failure: `OcrWorkflowTests.Preflight_ContractReturnsOcrSpecificResult`
- Expected mismatch:
  - expected return type: `SGPdf.App.Features.Ocr.OcrPreflightResult`
  - actual return type: `SGPdf.App.Features.Edit.PdfEditPreflightResult`

This is a valid RED, not a regression in previously closed functionality.

## Exact next action when resuming

1. Add the minimum OCR-specific result model, e.g. `OcrPreflightResult`, preserving the existing centralized `PdfEditPreflightInspector` policy rather than duplicating security logic.
2. Change `OcrPreflight.Inspect(...)` to return `OcrPreflightResult` while retaining the same password/signature/block findings and cancellation behavior.
3. Run exact-head Windows CI and require:
   - repository hygiene PASS;
   - pinned Labelize/qpdf/Tesseract staging PASS;
   - locked restore PASS;
   - Release build 0 warnings / 0 errors;
   - 1023 PASS / 0 FAIL / 0 SKIP.
4. Only after that GREEN, continue Task 6 with RED/GREEN for `OcrWorkflow` orchestration:
   - eligibility + hybrid/no-op behavior;
   - current/range scope;
   - sequential 300-DPI render;
   - Tesseract TSV recognition;
   - strict parse/map/plan;
   - one final transactional writer call;
   - stale fingerprint / cancellation / failure atomicity;
   - temp cleanup and progress ordering.

## Guardrails to preserve on resume

- KISS/YAGNI.
- Strict RED -> expected failure -> minimum GREEN -> exact-head CI -> checkpoint.
- PDFium remains the PDF owner; Tesseract recognition only.
- No cloud/backend/account/API-key/runtime download.
- Source PDF remains untouched; Save As only.
- No partial-success publication.
- No merge to `main` without explicit user approval.
- Do not start Task 7 UI until Task 6 is fully GREEN.
