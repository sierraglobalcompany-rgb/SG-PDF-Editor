# F10 — Task 7 RED pause checkpoint

Date: 2026-10-12 UTC
Branch: `feat/f10-local-ocr`
Purpose: freeze exact state at the user's requested stop point after creating and witnessing the first Task 7 RED tests, before any Task 7 production implementation.

## Authority / continuity

Use these sources in this order when resuming:

1. live Git state and exact-head CI evidence;
2. this checkpoint for the stop point;
3. `docs/history/2026-10-12-F10-CONTINUITY-CHECKPOINT-BEFORE-TASK7.md`;
4. `docs/superpowers/plans/2026-10-11-f10-local-ocr.md`;
5. `docs/superpowers/specs/2026-10-11-f10-local-ocr-design.md`.

Do not infer completion from chat text alone.

## Repository state before this docs checkpoint

Branch head before writing this file:

- `beb50325480708a189aa809bd5683f4ced86ae14`
- message: `test(f10): add task7 OCR UI RED coverage`
- parent: `540b100240a901c127565f02131a1861de005edd`

`main` was not modified during this work. No merge or PR action was performed.

## Task 7 status at pause

**Task 7 is IN RED. It is NOT GREEN and NOT CLOSED.**

No Task 7 production implementation was added before this checkpoint.
Task 8 and F11 remain NOT STARTED.

The only Task 7 implementation-cycle commit before this checkpoint is the RED test commit:

- `beb50325480708a189aa809bd5683f4ced86ae14`

Files added by that RED commit:

- `tests/SGPdf.App.Tests/OcrDialogTests.cs`
- `tests/SGPdf.App.Tests/MainWindowOcrTests.cs`

No production files were changed by the RED commit.

## RED contract encoded by the tests

The tests intentionally describe the minimum approved Task 7 behavior:

### Dialog contract

- `OcrDialog` exists as a lightweight WPF dialog.
- Language choices are only:
  - `Español` — default;
  - `Inglés`;
  - `Español + Inglés`.
- Scope choices are only:
  - `Automático` — default;
  - `Página actual`;
  - `Rango`.
- Range validation applies only while `Rango` is selected.
- User-facing page numbers map from one-based UI values to zero-based `OcrPageScope` indices.
- Progress UI exists and shows current/total page information.
- Cancel UI exists.
- No technical OCR controls such as DPI / PSM / OEM are exposed.

### MainWindow / Herramientas contract

- Exactly one command named `OcrMenuItem` appears under the existing single `Herramientas` menu.
- Exact user-facing label: `OCR / Hacer PDF buscable…`.
- Save As cancellation invokes no OCR workflow.
- No-op result surfaces a clear already-searchable/buscable message.
- Success surfaces processed/skipped counts.
- While OCR runs, the OCR command is disabled.
- Cancel cancels the request token.
- UI returns to usable state after success, failure, or cancellation.
- Tests provide a narrow workflow-runner seam only for deterministic MainWindow adapter tests; production must still delegate to the real `OcrWorkflow` by default.

## RED CI evidence

Exact RED commit:

- SHA: `beb50325480708a189aa809bd5683f4ced86ae14`
- workflow run: `38196331193`
- Windows job: `114639688066`
- overall conclusion: `failure`

Step evidence from the completed Windows job:

- repository hygiene: PASS;
- pinned Labelize staging: PASS;
- pinned qpdf staging: PASS;
- pinned Tesseract staging: PASS;
- locked restore: PASS;
- Build: **PASS**;
- Test: **FAIL**.

This is sufficient evidence that the RED test commit compiles and that failure occurs in the test stage rather than restore/build/hygiene.

Important verification note: the connector evidence recorded at this stop point exposes the step-level failure but not the exact failing assertion/log lines. On resume, inspect the RED test output or rerun the focused tests before writing GREEN production code, and verify that failures are only the intended missing Task 7 production contract. Do not skip this check.

## Production state still authoritative from Task 6

Task 6 remains CLOSED / GREEN from the prior checkpoint.
The production OCR engine already exists and Task 7 must reuse it rather than creating another OCR path:

- `OcrWorkflow(string sourcePath, string destinationPath)` production constructor;
- `RunAsync(OcrRequest, IProgress<OcrProgress>?, CancellationToken)`;
- `OcrRequest` carries only scope + language;
- PDFium remains PDF owner/rasterizer;
- pinned local Tesseract remains recognition only;
- writer publishes Save As transactionally;
- no cloud/backend/database.

Existing UI owner inspected before RED:

- `src/SGPdf.App/MainWindow.Utilities.cs`
- current single dynamic `Herramientas` menu is created by `InitializeUtilitiesUi()`;
- OCR must integrate there, not create a second top-level Tools/Herramientas menu.

Existing reader reopen path remains authoritative:

- reuse the current MainWindow PDF-opening/loading path after successful OCR output;
- do not invent a second PDF reader path.

## Exact next work when resuming

Resume from **Task 7 RED verification**, not from Task 7 planning and not from Task 8.

1. Confirm branch HEAD is this checkpoint commit or a docs-only descendant of it.
2. Confirm `main` is still untouched relative to the pre-existing authority.
3. Inspect/re-run the RED tests and capture the exact failing assertions.
4. Verify failures are exclusively due to missing Task 7 production UI/wiring.
5. Implement the minimum GREEN only:
   - `src/SGPdf.App/Features/Ocr/OcrDialog.xaml`;
   - `src/SGPdf.App/Features/Ocr/OcrDialog.xaml.cs`;
   - `src/SGPdf.App/MainWindow.Ocr.cs`;
   - the smallest necessary integration into the existing `MainWindow.Utilities.cs` host (prefer this over creating a second menu);
   - avoid modifying `MainWindow.xaml` unless the actual live structure proves it necessary.
6. Default production path must instantiate/run existing `OcrWorkflow`; the test override seam must not replace the production default.
7. Run focused Task 7 tests to GREEN.
8. Run full Release suite.
9. Run exact-head Windows CI.
10. Create a Task 7 GREEN checkpoint only after fresh evidence.
11. Stop before Task 8 unless separately authorized by the project workflow/user.

## Guardrails frozen at pause

- KISS / YAGNI.
- Local/offline/privacy only.
- No cloud OCR.
- No backend/database.
- No second PDF/image stack.
- PDFium remains PDF owner.
- Tesseract remains recognition only.
- No technical OCR tuning exposed in normal UI.
- Save As only; source unchanged.
- Strict RED -> verify intended failure -> minimum GREEN -> focused tests -> full tests -> exact-head Windows CI -> checkpoint.
- No merge to `main` without explicit user approval.
- Manual Windows / Adobe / Foxit / Edge / private-corpus QA remains NOT RUN unless separately evidenced.

## Stop condition

This checkpoint intentionally stops the work with Task 7 in RED.

Do **not** interpret this file as Task 7 completion.
Do **not** begin Task 8 from this state.
The next coding action is minimum Task 7 GREEN only after verifying the RED failure details.