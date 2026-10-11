# F9 Task 6 — Protection capability gate and engine decision

Date: 2026-10-11

## Status

Task 6 is closed at the automated capability/engine-decision level.

- Base checkpoint (Task 5): `35f904d2d947f61d5bfc7b142d25df5d0588c20f`
- Functional head before this checkpoint: `497f62422323fda9d0833cb7ff182c4fa4c9b2aa`
- Branch: `feat/f9-offline-utilities`
- `main` remained frozen at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` during the Task 6 scope audit.
- Task 7 was NOT started.
- Manual Windows/external-reader QA: NOT RUN.

## Canonical Task 6 contract

Task 6 owns only the protection capability gate and engine decision:

1. RED characterization for creating a newly protected copy and reopening with wrong/right password.
2. Determine whether the exact pinned PDFium can create the required encryption.
3. If PDFium is insufficient, evaluate exactly one minimal local fallback, preferring qpdf.
4. If adopted, pin version/digest/license, keep runtime offline, prove the process adapter, run exact-head Windows CI, and record the engine decision for open password and permission flags.

No protection product UI/workflow belongs to this task.

## RED evidence

RED commit:

`de2dfcfaba9e712dfa402f63f354343765b9def0`

Windows CI:

- run: `38114783292`
- job: `114397404198`
- build: 0 warnings / 0 errors
- tests: 920 PASS / 1 FAIL / 0 SKIP / 921 TOTAL

The only failure was the new Task 6 behavioral contract because `PdfProtectionCapabilityAdapter` did not yet exist. All inherited tests remained green.

The RED contract requires:

- source bytes remain unchanged;
- a separate protected copy is produced;
- opening with no password fails;
- opening with a wrong password fails;
- opening with the correct password succeeds.

## Exact pinned PDFium characterization

Pinned runtime:

`bblanchon.PDFium.Win32 156.0.8076`

Characterization commit:

`47ee3275fe79ba22f296068159e39e1e9f9d3448`

Windows CI:

- run: `38114928892`
- job: `114397829420`
- build: 0 warnings / 0 errors
- the new PDFium export characterization passed;
- the generic Task 6 protection-creation contract remained RED because the adapter had not yet been implemented.

The exact loaded `pdfium.dll` proves this public surface:

Present:

- `FPDF_LoadDocument`
- `FPDF_SaveAsCopy`
- `FPDF_GetDocPermissions`
- `FPDF_GetSecurityHandlerRevision`

Absent:

- `FPDF_SetPassword`
- `FPDF_SetDocPermissions`
- `FPDF_SetEncryption`
- `FPDF_SetEncrypt`

### Decision: PDFium NO-GO for creating protection

The pinned PDFium is sufficient to consume password-protected PDFs and inspect security information, but its public API does not expose the creation setters required to produce a newly password-protected copy.

No private/undocumented PDFium entry point was adopted.

## Single fallback evaluation and selection

Exactly one fallback engine was evaluated and adopted:

### qpdf 12.4.2

- upstream: `https://github.com/qpdf/qpdf`
- version: `12.4.2`
- official Windows distribution: `qpdf-12.4.2-msvc64.zip`
- SHA-256: `db87077e683630c1217e0e8f9a20a9749d952ab676e881c3689187763a5de25d`
- license: Apache-2.0
- required NOTICE tracked in repository
- no pdfcpu fallback is maintained.

Repository legal files:

- `third_party/licenses/QPDF-Apache-2.0.txt`
- `third_party/licenses/QPDF-NOTICE.md`

Manifest:

- `third_party/manifest.json`

Staging:

- `tools/setup-qpdf.ps1`
- development/CI downloads the official archive only for staging;
- archive SHA-256 is verified before extraction;
- staged runtime remains untracked under `third_party/runtime/qpdf`;
- product output receives the verified staged runtime;
- product runtime performs no network download.

CI was updated only to stage this pinned qpdf runtime before restore/build/test.

## Protection capability adapter

Production gate adapter:

`src/SGPdf.App/Features/Utilities/PdfProtectionCapabilityAdapter.cs`

This is a Task 6 capability adapter, not the Task 7 product writer.

Behavior proven by tests:

- requires different source/destination paths;
- source must exist;
- destination must not already exist;
- uses bundled `qpdf/qpdf.exe` only;
- creates AES-256 protection via QPDFJob JSON;
- user and owner passwords are put in an ephemeral JSON job file, not in qpdf process command-line arguments;
- uses existing `ChildProcessRunner` and its timeout/cancellation behavior;
- deletes partial output on failure;
- deletes the temporary password-bearing job JSON in `finally`;
- source PDF remains byte-for-byte unchanged.

The Task 7 product writer still owns transactional sibling-temp/reopen-validation/atomic-publication semantics.

## GREEN evidence

First functional GREEN:

`c6e8394b58cd7e86cc22cea6f8066bfe90b2e75d`

Windows CI:

- run: `38115144518`
- job: `114398446101`
- qpdf 12.4.2 staging: PASS
- build: 0 warnings / 0 errors
- tests: 922 PASS / 0 FAIL / 0 SKIP

Final hardening commit before checkpoint:

`497f62422323fda9d0833cb7ff182c4fa4c9b2aa`

Windows CI:

- run: `38115270125`
- job: `114398814571`
- qpdf staging: PASS
- build: 0 warnings / 0 errors
- tests: 922 PASS / 0 FAIL / 0 SKIP

The final capability contract additionally proves:

- correct user password opens the protected copy;
- correct owner password opens the protected copy;
- no-password open fails;
- wrong-password open fails;
- no `*sgpdf-qpdf-job-*.json` secret-bearing temp file remains after success.

## Product decision

### Open password: GO

Task 7 may build the authorized password-protection product slice on qpdf 12.4.2 using AES-256, while retaining PDFium for normal reader validation/reopen behavior.

Task 7 must still add its own authorization, signatures policy, Save As cancellation, sibling-temp validation, existing-destination preservation, atomic publication, and UI tests. This Task 6 adapter is not a shortcut around those product requirements.

### Permission flags: NOT ENABLED for product exposure

qpdf supports PDF permission flags, but those restrictions are enforced by cooperating readers and are not DRM. Task 6 did not prove a preservation/reader-enforcement matrix for print/copy/edit restrictions.

Therefore Task 7 must not expose print/copy/edit permission toggles based only on this gate. If those controls are desired later, they require a separate explicit round-trip/validation matrix and user-facing wording that does not present them as unbreakable access control.

This is an intentional KISS/fail-closed decision, not an unfinished fallback implementation.

## Scope audit from Task 5 checkpoint

Base:

`35f904d2d947f61d5bfc7b142d25df5d0588c20f`

Functional head:

`497f62422323fda9d0833cb7ff182c4fa4c9b2aa`

Exactly these Task 6 files changed:

1. `.github/workflows/build.yml`
2. `src/SGPdf.App/Features/Utilities/PdfProtectionCapabilityAdapter.cs`
3. `src/SGPdf.App/SGPdf.App.csproj`
4. `tests/SGPdf.App.Tests/PdfProtectionCapabilityTests.cs`
5. `tests/SGPdf.App.Tests/PdfiumProtectionApiAvailabilityTests.cs`
6. `third_party/licenses/QPDF-Apache-2.0.txt`
7. `third_party/licenses/QPDF-NOTICE.md`
8. `third_party/manifest.json`
9. `tools/setup-qpdf.ps1`

No Task 7 UI was added. No `MainWindow` file was changed. No production `PdfiumNative` interop was changed. No NuGet package was added. No second protection fallback was added.

## Next allowed action

When work resumes, start **Task 7 — Authorized protection product slice** from this Task 6 checkpoint.

Do not merge to `main` without explicit user approval.
