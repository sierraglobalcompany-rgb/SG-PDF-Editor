# F8 Task 9 — Transaction hardening + preflight checkpoint

Date: 2026-10-11
Branch: `feat/f8-comments-v1`
Base checkpoint (Task 8): `28df475388b50e9a00eab19bd42d67ea187bd5c4`

## Scope

Task 9 hardens COMENTAR Save As without changing annotation geometry/materialization or opening the UI slice.

Implemented:
- F8-specific preflight policy in `Features/Comments/PdfCommentPreflight.cs`.
- Reuse of the existing F7 detector only for structure detection; F7 preservation conclusions are deliberately discarded.
- Cryptographic signatures: Block / Unknown.
- Password-opened sources: Block / Unknown.
- Metadata and page rotation: Info / Unknown.
- Other detected preservation-sensitive structures: Warning / Unknown and explicit confirmation required.
- `PdfCommentWriter.SaveAsCopy(..., warningsConfirmed, ...)` overload while preserving the legacy overload.
- Preflight before temporary output creation.
- Injectable preflight/validator seams for deterministic transaction tests.
- Existing transactional ordering retained: source checks -> plan/scope -> preflight -> identity resolution -> sibling temp -> save -> reopen/validate -> cancellation check -> atomic publish -> finally cleanup.
- Writer never calls `MarkSaved()`; post-success baseline/reload remains a Task 10 UI responsibility.

Not implemented here:
- COMENTAR UI / interaction ownership.
- Task 11 preservation matrix proof.
- Any new PDF engine, dependency, package or network service.
- Any merge to `main`.

## TDD evidence

### Harness diagnostics before valid RED

Two early runs were rejected as RED evidence because the new tests did not compile:

1. `85d2709c022e7a6751cb1909c5379a67c0f28d6c`
   - workflow `38100815039`
   - job `114356197562`
   - harness error: named arguments used property names instead of the real `CommentState` constructor parameter contract.

2. `d68dd691a3f5f0ebad6e001c030a7ce7dc5724fe`
   - workflow `38100896455`
   - job `114356436950`
   - harness error: used a nonexistent direct `CommentWorkspace.Create(state)` mutation instead of `PrepareCreate` -> `CommitCandidate`.

Only the test harness was corrected in both cases; production remained untouched.

### Valid RED

SHA: `11b9a78dd8f5f17ceae68fd47e30db4748b0a7ce`
Workflow: `38101008965`
Job: `114356758428`

- Build: 0 warnings / 0 errors.
- Tests: 822 PASS / 15 FAIL / 0 skipped / 837 total.
- All 15 failures were the new Task 9 expectations:
  - missing `PdfCommentPreflightInspector`;
  - missing injected writer constructor / warning-confirmation overload.
- Existing stale-source and password-opened writer regressions passed.

### GREEN

Functional SHA: `8d8edeb0c02e239e02e08fb3d5b7b65c89427758`
Workflow: `38101168385`
Job: `114357219989`

- Exact-head checkout confirmed.
- Repository hygiene: PASS.
- Locked restore: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 837 PASS / 0 FAIL / 0 skipped / 837 total.

Transaction proofs include:
- stale source blocks without publishing;
- password-opened source blocks without publishing;
- crypto-signature preflight block occurs before temp creation;
- warnings without explicit confirmation block before temp creation;
- validator failure preserves an existing destination byte-for-byte, cleans sibling temp, and keeps workspace dirty;
- cancellation after validator execution preserves an existing destination byte-for-byte, cleans sibling temp, and keeps workspace dirty.

## Scope audit

Compare `28df475388b50e9a00eab19bd42d67ea187bd5c4` -> `8d8edeb0c02e239e02e08fb3d5b7b65c89427758`:

- ahead by 6, behind by 0;
- merge base equals Task 8 checkpoint;
- changed files only:
  1. `src/SGPdf.App/Features/Comments/PdfCommentPreflight.cs`
  2. `src/SGPdf.App/Pdf/PdfCommentWriter.cs`
  3. `tests/SGPdf.App.Tests/PdfCommentPreflightTests.cs`
  4. `tests/SGPdf.App.Tests/PdfCommentWriterTransactionTests.cs`

No UI, interop, project/package/lockfile, dependency, Task 10 or Task 11 changes.

## State

Task 9 functional implementation is automated-GREEN at `8d8edeb0c02e239e02e08fb3d5b7b65c89427758`.
This document is the durable Task 9 checkpoint; its own exact-head Windows CI must remain green before Task 9 is declared closed.

Manual Windows QA: NOT RUN.
Merge: NOT PERFORMED.
