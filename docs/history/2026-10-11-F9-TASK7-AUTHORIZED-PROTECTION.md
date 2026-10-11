# F9 Task 7 — Authorized Protection Product Slice

Date: 2026-10-11
Branch: `feat/f9-offline-utilities`
Task 6 base checkpoint: `b616d664fcf9f0990d80b6de34b8f880f8c53b71`
Functional Task 7 head before this checkpoint: `067795235f41ded2018476500980693cf9fab558`
Main verified unchanged: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## Decision inherited from Task 6

Task 6 established that pinned PDFium `156.0.8076` can open/query protected PDFs but does not provide the required public encryption-creation path. SG PDF therefore uses the single pinned local candidate qpdf 12.4.2 for creation of password-protected copies. qpdf staging, digest and licensing were completed in Task 6; Task 7 adds no new dependency, package, runtime download, license or fallback engine.

Open-password protection is GO. Print/copy/edit permission flags remain intentionally absent because Task 6 did not prove a sufficient cross-reader enforcement matrix. The UI makes no DRM claim.

## Product behavior delivered

### Authorization and existing protected sources

- The user must explicitly confirm authorization to protect the PDF.
- A new open password is required and must match its confirmation exactly.
- Password strings are not trimmed or persisted.
- An unprotected source can be protected directly.
- If the source was opened with a password, the current source password must be supplied again for this operation.
- A missing or wrong current password fails closed; Task 7 does not remove or bypass protection.
- The existing Task 6 qpdf adapter was extended with a backward-compatible overload that places the source password only in the ephemeral QPDFJob JSON, never on the process command line.

### Signature policy

- Cryptographic signatures are checked before qpdf is invoked.
- Any detected signature blocks protection.
- An indeterminate/negative signature count also fails closed.
- The product does not create a protected derivative that could silently invalidate a cryptographic signature.

### Transactional Save As

`PdfProtectionWriter` follows the conservative SG PDF publication model:

1. Validate explicit authorization and password settings.
2. Verify source/destination separation and source fingerprint.
3. Reopen the source with the required current password when necessary.
4. Check cryptographic-signature policy.
5. Create a sibling `*.sgpdf-tmp.pdf` protected copy through qpdf.
6. Re-check source fingerprint.
7. Reopen/validate the temporary output with the new password.
8. Confirm it cannot be opened without a password.
9. Confirm page count and render smoke.
10. Atomically publish by replacing an existing destination or moving into a new destination.
11. Clean the temporary file on failure/cancellation.

The original PDF is never modified in place.

### WPF product command

`Herramientas` now contains `_Proteger con contraseña...`.

The focused dialog exposes only:
- new open password;
- confirmation;
- current password when the loaded source was password-opened;
- explicit authorization checkbox.

The dialog states that a new protected copy is created and the original is unchanged. It explicitly avoids claiming print/copy/edit DRM restrictions.

The command is thin:

`UI -> PdfProtectionRequest -> PdfProtectionSettings -> PdfProtectionWriter -> validated Save As result`

No second utility workspace or second process runner was introduced.

## TDD evidence

### Writer RED

Commit: `80cfe9b09c69479dc057fed85a38ae402c07adeb`
Workflow run: `38116226224`
Job: `114401539170`

Result:
- build: 0 warnings / 0 errors;
- inherited tests: 922 PASS;
- new Task 7 writer contracts: 6 FAIL;
- total: 922 PASS / 6 FAIL / 0 SKIP.

All six new failures were caused by the not-yet-created Task 7 product settings/writer surface, so the RED was behavioral rather than a broken build.

### Writer GREEN

Commit: `a3299b43bb924a101dfc1e15a68f946e5be2554f`
Workflow run: `38116353251`
Job: `114401897485`

Result:
- qpdf staging: PASS;
- build: 0 warnings / 0 errors;
- tests: 928 PASS / 0 FAIL / 0 SKIP.

### UI/request RED

Commit: `4920e253f386f4cb01e17e7107fa40cb3612bec1`
Workflow run: `38116497177`
Job: `114402309414`

Result:
- build: 0 warnings / 0 errors;
- inherited tests: 928 PASS;
- new UI/request contracts: 6 FAIL;
- total: 928 PASS / 6 FAIL / 0 SKIP.

Failures were caused only by the missing `PdfProtectionRequest` and `ProtectionMenuItem`/command wiring.

### UI GREEN

Commit: `0b1f69904d2d74d8508d69c7ead7fbe6f9b60969`
Workflow run: `38116671029`
Job: `114402799523`

Result:
- build: 0 warnings / 0 errors;
- tests: 934 PASS / 0 FAIL / 0 SKIP.

### Final transactional/signature hardening

Commit: `067795235f41ded2018476500980693cf9fab558`
Workflow run: `38116772870`
Job: `114403101407`

Result:
- build: 0 warnings / 0 errors;
- tests: 937 PASS / 0 FAIL / 0 SKIP.

Additional deterministic tests prove:
- signed source blocks before the qpdf delegate is invoked;
- validator failure preserves an existing destination and removes the sibling temp;
- cancellation after temporary-file creation preserves the existing destination and removes the temp.

The GitHub Actions Node.js 20/punycode deprecation message is runner/action maintenance noise, not a C# product warning.

## Scope audit

Compared:
`b616d664fcf9f0990d80b6de34b8f880f8c53b71..067795235f41ded2018476500980693cf9fab558`

Status:
- ahead by 11 commits;
- behind by 0;
- merge base is exactly the Task 6 checkpoint.

Task 7 changed exactly these 11 paths:

1. `src/SGPdf.App/Features/Utilities/PdfProtectionCapabilityAdapter.cs`
2. `src/SGPdf.App/Features/Utilities/PdfProtectionRequest.cs`
3. `src/SGPdf.App/Features/Utilities/PdfProtectionSettings.cs`
4. `src/SGPdf.App/Features/Utilities/ProtectionDialog.xaml`
5. `src/SGPdf.App/Features/Utilities/ProtectionDialog.xaml.cs`
6. `src/SGPdf.App/MainWindow.Utilities.cs`
7. `src/SGPdf.App/Pdf/PdfProtectionOutputValidator.cs`
8. `src/SGPdf.App/Pdf/PdfProtectionWriter.cs`
9. `tests/SGPdf.App.Tests/MainWindowProtectionUtilitiesTests.cs`
10. `tests/SGPdf.App.Tests/PdfProtectionWriterHardeningTests.cs`
11. `tests/SGPdf.App.Tests/PdfProtectionWriterTests.cs`

No Task 7 changes to:
- package references;
- `.csproj` dependency declarations;
- third-party manifest/licenses;
- workflow configuration;
- PDFium interop;
- cloud/network/account/API-key features;
- Task 8 optimize/repair code.

## Manual QA status

- Manual Windows UI QA: **NOT RUN**.
- Adobe/Foxit/Edge external-reader QA: **NOT RUN**.

Automated reopen/render validation is not represented as manual external-reader QA.

## Boundary / next task

Task 7 is bounded to authorized password protection only. Permission flags remain absent. Task 8 (optimize/repair capability gates and preservation matrix) has not been started by this checkpoint.

Do not merge to `main` without explicit approval.
