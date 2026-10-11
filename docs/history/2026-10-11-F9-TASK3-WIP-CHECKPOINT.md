# F9 Task 3 — WIP Checkpoint: transactional text-overlay writer

Date: 2026-10-11
Status: **WIP / NOT GREEN / DO NOT ADVANCE TO TASK 4**
Branch: `feat/f9-offline-utilities`
Functional HEAD before this checkpoint commit: `19e5d47c6496e11ad10de65954130579995002b0`
Previous closed checkpoint: F9 Task 2 at `1737a9515a1eb39f23064947246cf68b63ae88d5`
Plan owner: `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`

## 1. Purpose of this checkpoint

Freeze the exact in-progress state of **F9 Task 3 — transactional text-overlay Save As writer** after the user requested an immediate checkpoint and stop.

This is deliberately a **WIP checkpoint**, not a completion checkpoint. Task 3 has valid RED evidence and a first GREEN implementation, but the latest functional HEAD does **not compile yet**, therefore no Task 3 GREEN claim is allowed.

## 2. Scope that was opened

Only Task 3 scope was opened:

- transactional Save As text-overlay writer;
- shared edit preflight reuse;
- source fingerprint guard;
- background / foreground z-order materialization;
- placement matrix preservation, including rotated-page fixture coverage;
- sibling temp output;
- validate-before-publish;
- destination preservation on cancellation / validation failure;
- unsupported fallback glyph fail-closed behavior.

Explicitly **not opened**:

- F9 Task 4 anchor / placement engine;
- F9 UI;
- image overlay production writer;
- new packages;
- `PdfiumNative` expansion;
- `main` integration.

## 3. Commits created during Task 3 work

1. `1f35cdac3315794264a15bcde792c770beb44822`
   - `test(f9): add red transactional overlay writer contracts`
   - Added the initial RED writer/preflight tests.

2. `899c14bf624a89ec32de81fa3e2e7395b3aa57ca`
   - `test(f9): fix transactional writer red harness`
   - Corrected the test harness to use the repository's real `PdfEditPreflightResult` contract and removed xUnit analyzer warnings.

3. `9bcefee306e39130f9dec4a70e1375d14ca98e42`
   - `feat(f9): add transactional text overlay writer`
   - Added the minimal production implementation for Task 3.

4. `19e5d47c6496e11ad10de65954130579995002b0`
   - `fix(f9): import IO for overlay writer`
   - Added the missing `System.IO` import after the first GREEN build failure.

## 4. Valid RED evidence

Canonical RED SHA: `899c14bf624a89ec32de81fa3e2e7395b3aa57ca`

GitHub Actions run: `38111920631`
Job: `114389078515`

Observed evidence:

- repository hygiene: PASS;
- locked restore: PASS;
- build: PASS;
- build warnings: **0**;
- build errors: **0**;
- tests total: **885**;
- pre-existing tests passed: **874**;
- new Task 3 tests failed: **11**;
- skipped: **0**.

The 11 failures were expected RED failures because the new Task 3 production types did not yet exist (`PdfTextOverlayWriter`, `TextOverlayPlacement`, `PdfUtilityPreflight`). This RED is valid and should be preserved as the Task 3 TDD baseline.

## 5. Production code currently present

Task 3 production changes currently exist in:

- `src/SGPdf.App/Features/Utilities/TextOverlaySettings.cs`
  - adds `TextOverlayPlacement` as a materialized placement DTO;
- `src/SGPdf.App/Features/Utilities/PdfUtilityPreflight.cs`
  - thin adapter over existing `PdfEditPreflightInspector`;
- `src/SGPdf.App/Pdf/PdfTextOverlayOutputValidator.cs`
  - reopens output and checks expected text overlays;
- `src/SGPdf.App/Pdf/PdfTextOverlayWriter.cs`
  - transactional sibling-temp writer using PDFium;
  - CID Type2 fallback font path reused from F7/F9 Task 1 characterization;
  - background insertion at index 0;
  - foreground append;
  - one `FPDFPage_GenerateContent` per touched page;
  - validation before publish;
  - `File.Replace` / `File.Move` only after validation;
  - cleanup in `finally`.

Task 3 tests currently exist in:

- `tests/SGPdf.App.Tests/PdfTextOverlayWriterTests.cs`
- `tests/SGPdf.App.Tests/PdfUtilityPreflightTests.cs`

No new package dependency was added.

## 6. GREEN attempts and current blocking evidence

### GREEN attempt 1 — `9bcefee306e39130f9dec4a70e1375d14ca98e42`

GitHub Actions run: `38112073388`
Job: `114389525981`

Build failed before tests:

- warnings: **0**;
- errors: **2**;
- both `CS0246` in `PdfTextOverlayWriter.cs` because `FileStream` was missing its `System.IO` import.

This was corrected by commit `19e5d47c6496e11ad10de65954130579995002b0`.

### GREEN attempt 2 — `19e5d47c6496e11ad10de65954130579995002b0`

GitHub Actions run: `38112141977`
Job: `114389727977`

Repository hygiene and restore passed, but build still failed before tests.

Exact current blockers:

1. **Error CS0117**
   - file: `src/SGPdf.App/Pdf/PdfTextOverlayWriter.cs`
   - line reported by CI: 52
   - message: `PdfEditSourceFingerprint` does not contain a definition for `PathEquals`.

2. **Warning CA2264**
   - file: `src/SGPdf.App/Pdf/PdfTextOverlayWriter.cs`
   - line reported by CI: 44
   - message: calling `ArgumentNullException.ThrowIfNull` with a non-nullable value is a no-op.

Latest build result:

- warnings: **1**;
- errors: **1**;
- tests: **SKIPPED because build failed**.

Therefore Task 3 is **not GREEN and not complete**.

## 7. Exact resume point

When work resumes, do **not** open Task 4.

Resume only F9 Task 3 from functional HEAD:

`19e5d47c6496e11ad10de65954130579995002b0`

First actions, in order:

1. Inspect the real `PdfEditSourceFingerprint` API and replace the invalid `PathEquals` call by reusing the repository's existing path-equality convention; do not add a second fingerprint abstraction.
2. Remove/fix the `CA2264` no-op null guard without changing behavior.
3. Run/observe build until **0 warnings / 0 errors**.
4. Run the Task 3 tests and full suite.
5. If tests fail, debug only Task 3 causes; preserve the RED contracts.
6. Only after Task 3 targeted tests + full suite are GREEN, run exact-head CI verification and scope audit.
7. Then create the real **F9 Task 3 completion checkpoint**.
8. Only after that may Task 4 be considered.

## 8. Guardrails to preserve on resume

- KISS: reuse existing fingerprint, preflight, PDFium and font infrastructure.
- Do not modify `main`.
- Do not merge without explicit user approval.
- Do not add packages unless a new blocker is proven and separately approved.
- Do not broaden Task 3 into anchor layout or UI logic.
- Do not claim preservation or transactional safety until validator/cancellation/stale-source tests pass.
- Source and destination must remain distinct.
- Password/signature risk remains fail-closed.
- Unsupported fallback glyph remains fail-closed.
- Destination must remain unchanged on cancellation or validation failure.
- Temporary sibling files must be cleaned.

## 9. Current branch / integration status

- Working branch: `feat/f9-offline-utilities`.
- Functional HEAD at freeze: `19e5d47c6496e11ad10de65954130579995002b0`.
- `main` was not modified during this Task 3 work.
- No merge was performed.
- No Task 4 work was started.

## 10. Stop condition

The user explicitly requested: **create checkpoint and stop**.

After committing this checkpoint document, perform no further functional code changes, no additional GREEN fixes, no Task 4 work, and no merge.
