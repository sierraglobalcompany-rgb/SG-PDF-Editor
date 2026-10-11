# F9 Task 3 — Transactional Text-Overlay Writer — Completion Checkpoint

Date: 2026-10-11
Status: **AUTOMATED PASS / TASK 3 CLOSED**
Branch: `feat/f9-offline-utilities`
Task 2 checkpoint base: `1737a9515a1eb39f23064947246cf68b63ae88d5`
Functional GREEN SHA: `269b22835869c1dce48301bb7b561f9d9f1d42dd`
Plan owner: `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`

## 1. Scope closed

F9 Task 3 implemented the transactional Save As writer for text overlays while preserving the already-approved KISS boundaries.

Closed behavior:

- typed `TextOverlayPlacement` materialization input;
- shared `PdfEditPreflightInspector` policy reuse through a thin utility adapter;
- stale-source protection through the existing `PdfEditSourceFingerprint` owner;
- source/destination path separation using the repository's existing normalized case-insensitive path comparison convention;
- PDFium CID Type2 fallback font path reused from F7/F9 Task 1 characterization;
- foreground append and background insertion-at-index-zero behavior;
- one `FPDFPage_GenerateContent` per touched page;
- placement matrix materialization including rotated-page coverage;
- sibling temporary output;
- output validation before publish;
- destination preservation on cancellation and validation failure;
- temp cleanup through `finally`;
- password/signature risk fail-closed behavior;
- unsupported fallback glyph fail-closed behavior;
- existing destination replacement only after successful validation.

Explicitly not opened:

- F9 Task 4 anchor / placement engine;
- F9 UI;
- image overlay production writer;
- new packages;
- `PdfiumNative` expansion;
- workflow changes;
- `main` integration.

## 2. TDD evidence

### Valid RED

Canonical RED SHA: `899c14bf624a89ec32de81fa3e2e7395b3aa57ca`

GitHub Actions run: `38111920631`
Job: `114389078515`

Observed RED evidence:

- repository hygiene: PASS;
- locked restore: PASS;
- build: PASS;
- build warnings: 0;
- build errors: 0;
- total tests: 885;
- pre-existing tests passed: 874;
- new Task 3 tests failed: 11;
- skipped: 0.

The 11 failures were expected because the Task 3 production types did not yet exist. This is the valid behavioral RED baseline.

### GREEN attempt history

1. `9bcefee306e39130f9dec4a70e1375d14ca98e42`
   - first production implementation;
   - build failed before tests with two `CS0246` errors because `FileStream` lacked `System.IO`.

2. `19e5d47c6496e11ad10de65954130579995002b0`
   - added `System.IO`;
   - build still failed before tests with:
     - `CS0117`: invalid `PdfEditSourceFingerprint.PathEquals` call;
     - `CA2264`: no-op `ThrowIfNull` against non-nullable `PdfEditSourceFingerprint` record struct.

3. WIP checkpoint: `00d569e014c6c98b575558e11f31c539e661ea6c`
   - froze the exact not-green state at user request;
   - made no functional fix.

4. Final functional GREEN: `269b22835869c1dce48301bb7b561f9d9f1d42dd`
   - reused the existing repository convention from `PdfCommentWriter`:
     `string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)`;
   - removed the impossible null guard on the fingerprint value type;
   - no other writer behavior changed.

## 3. Final automated verification

Functional GREEN SHA: `269b22835869c1dce48301bb7b561f9d9f1d42dd`

GitHub Actions run: `38112276246`
Job: `114390135770`

Exact-head evidence:

- checkout exact SHA: PASS;
- repository hygiene: PASS;
- Labelize 1.7.0 staging: PASS;
- locked restore: PASS;
- build: PASS;
- build warnings: **0**;
- build errors: **0**;
- tests: **885 PASS / 0 FAIL / 0 SKIP**;
- test duration reported by CI: 13 s.

The only emitted warning is the external GitHub Actions maintenance warning about Node.js 20 deprecation for `actions/checkout@v4` and `actions/setup-dotnet@v4`. It is not a product build/test warning and no workflow change belongs in Task 3.

## 4. Task 3 behavioral contracts proven

Automated tests cover:

1. source and destination must differ;
2. stale fingerprint fails without publishing;
3. password risk fails closed;
4. cryptographic-signature risk fails closed;
5. background/foreground overlays preserve expected page-object order;
6. rotated-page placement preserves the requested object matrix;
7. cancellation during validation preserves an existing destination and cleans sibling temp files;
8. validator failure preserves an existing destination and cleans sibling temp files;
9. unsupported fallback glyph fails closed without publishing;
10. plain-PDF utility preflight reuses the supported edit policy;
11. preflight cancellation propagates before inspection.

## 5. Files in Task 3 scope

Compared from Task 2 checkpoint `1737a9515a1eb39f23064947246cf68b63ae88d5` to functional GREEN `269b22835869c1dce48301bb7b561f9d9f1d42dd`:

- `docs/history/2026-10-11-F9-TASK3-WIP-CHECKPOINT.md` — WIP recovery checkpoint only;
- `src/SGPdf.App/Features/Utilities/PdfUtilityPreflight.cs` — added;
- `src/SGPdf.App/Features/Utilities/TextOverlaySettings.cs` — minimal extension for materialized placement DTO;
- `src/SGPdf.App/Pdf/PdfTextOverlayOutputValidator.cs` — added;
- `src/SGPdf.App/Pdf/PdfTextOverlayWriter.cs` — added;
- `tests/SGPdf.App.Tests/PdfTextOverlayWriterTests.cs` — added;
- `tests/SGPdf.App.Tests/PdfUtilityPreflightTests.cs` — added.

Compare status:

- ahead: 6 commits;
- behind: 0;
- no package files;
- no `.csproj` / lockfile changes;
- no XAML;
- no workflow changes;
- no `PdfiumNative` changes;
- no Task 4 implementation.

This completion checkpoint itself is documentation-only and follows the functional GREEN commit.

## 6. Architecture / KISS decisions preserved

- Fingerprint remains owned by existing `PdfEditSourceFingerprint`; no parallel fingerprint abstraction was added.
- Path equality reuses the normalized case-insensitive convention already used by `PdfCommentWriter`.
- Preflight delegates to the existing edit preflight owner rather than creating a separate security policy.
- The placement DTO contains already-materialized geometry; anchor/layout policy remains outside the writer and belongs to Task 4.
- The writer reuses existing PDFium/font infrastructure and does not add a package.
- Transactionality follows the existing sibling-temp -> validate -> publish pattern.

## 7. Integration state

- Working branch: `feat/f9-offline-utilities`.
- `main` remains unchanged at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No merge was performed.
- Manual QA: **NOT RUN**; no manual evidence is claimed.

## 8. Next permitted work

Task 3 is now automated-green and may be treated as closed after the CI for this documentation checkpoint also passes on its exact HEAD.

Next planned work: **F9 Task 4 — anchor / placement engine**.

Before touching Task 4:

1. verify this checkpoint commit is the branch HEAD;
2. verify exact-head CI for the checkpoint is green;
3. reread the canonical Task 4 plan section;
4. start Task 4 with a new behavioral RED;
5. do not reopen Task 3 unless a regression is proven.

No merge to `main` without explicit user approval.
