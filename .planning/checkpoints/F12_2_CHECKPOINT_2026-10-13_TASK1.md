# F12.2 checkpoint — Task 1 capability/dependency gate

Date: 2026-10-13
Branch: `feat/f12-professional`

## Current scope

F12.2 cryptographic signing MVP is approved and planned. Work is intentionally stopped inside **Task 1: Capability + dependency gate**. Do not start Task 2 from this checkpoint until Task 1 reaches a valid GREEN.

Authoritative documents:

- `docs/superpowers/specs/2026-10-12-f12-2-cryptographic-signature-design.md`
- `docs/superpowers/plans/2026-10-12-f12-2-cryptographic-signature.md`

## Branch state before this checkpoint

Last functional/planning HEAD before Task 1 code: `3830bd2553f2766510bac93245b261fb5cde7ff6`.

Task 1 RED commits created so far:

1. `436346bca9eed0c61defcf336c4508429243c3e2` — `test(f12.2): define cryptographic signing capability gate`
2. `108cbeb202349b24fecd5390e34ab9ed17926f38` — `test(f12.2): locate pinned PDFium runtime in test output`

No runtime signing implementation has been added.
No PKCS package reference has been added yet.
No lock file, manifest or license file has been changed yet.
No Task 2 work has started.
No merge to `main` has been performed.

## Task 1 test added

File:

`tests/SGPdf.App.Tests/PdfCryptographicSigningCapabilityTests.cs`

Test:

`CapabilityGate_PinnedRuntimeSupportsPdfSharpPkcsAndPdfiumSignatureInspection`

It checks:

- pinned PDFium runtime is present in test output;
- exports exist:
  - `FPDF_GetSignatureObject`
  - `FPDFSignatureObj_GetContents`
  - `FPDFSignatureObj_GetByteRange`
  - `FPDFSignatureObj_GetSubFilter`
- PDFsharp types exist:
  - `PdfSharp.Pdf.Signatures.DigitalSignatureHandler`
  - `PdfSharp.Pdf.Signatures.IDigitalSigner`
- `System.Security.Cryptography.Pkcs.SignedCms` is loadable.

## RED evidence

GitHub Actions run for `436346bca9eed0c61defcf336c4508429243c3e2`:

- Run: `38338954426`
- Job: `115105387121`
- Build: PASS, 0 warnings, 0 errors
- Tests: 1111 PASS + 1 FAIL
- Total: 1112

The failure was **not accepted as the intended RED** because it failed before testing capability:

`Pinned PDFium runtime not found at '<test output>/pdfium.dll'`

Root cause: the native PDFium DLL is not copied to the root of the test output. The test was corrected to search recursively under `AppContext.BaseDirectory` rather than assuming a root-level path.

That correction is commit `108cbeb202349b24fecd5390e34ab9ed17926f38`.

## CI currently in flight at stop

GitHub Actions run for corrected RED HEAD `108cbeb202349b24fecd5390e34ab9ed17926f38`:

- Run: `38339138480`
- Job: `115105967275`
- At last observed state:
  - repository hygiene: PASS
  - Labelize staging: PASS
  - qpdf staging: PASS
  - Tesseract staging: PASS
  - locked restore: PASS
  - Release build: PASS
  - test step: IN PROGRESS

Do not infer the final result from this checkpoint. On resume, fetch this exact run/job result first.

## Important dependency finding

Current `src/SGPdf.App/packages.lock.json` does **not** contain `System.Security.Cryptography.Pkcs` as a direct runtime dependency.

Current `tests/SGPdf.App.Tests/packages.lock.json` already contains `System.Security.Cryptography.Pkcs 8.0.1` transitively through PDFsharp. Therefore a test that only proves that some `SignedCms` type loads in the test host can give a false sense of success.

Task 1 must prove the intended runtime dependency state for **10.0.12**, not merely the historical transitive test dependency.

Verified package data prepared for GREEN:

- Package: `System.Security.Cryptography.Pkcs`
- Version: `10.0.12`
- License: MIT
- NuGet content hash observed in a public reproducible lock:
  `W6BsawU2tB2qy6u4+3cBOSOmiTUgYWW365b5WMVA1bDxQwZZeNWD+qufKJkDQNKzAKq4b6Q04DDKhmr2SNJ1eg==`
- Upstream license source: `.NET runtime` MIT license.

Do not manually edit lock files from this value if restore can generate them. Use normal restore/update, then verify the generated lock matches the package actually resolved.

## Required resume sequence

1. Fetch final result/logs of run `38339138480`, job `115105967275`.
2. Decide whether the corrected RED failed for the intended capability reason.
3. If any required PDFium export is missing: **STOP F12.2 as NO-GO** per spec; do not add a parser or second PDF engine.
4. If all PDFium/PDFsharp capability checks pass and the remaining failure is the intended PKCS runtime dependency gap, perform Task 1 GREEN only:
   - add direct `System.Security.Cryptography.Pkcs` `10.0.12` to `src/SGPdf.App/SGPdf.App.csproj`;
   - regenerate both lock files reproducibly;
   - update `third_party/manifest.json`;
   - add `third_party/licenses/System.Security.Cryptography.Pkcs-MIT.txt`;
   - strengthen capability assertion if needed so it proves version `10.0.12`, not merely type availability;
   - run focal test + locked restore + Release build + full suite;
   - require exact-head CI GREEN before closing Task 1.
5. Do not start Task 2 in the same recovery step unless Task 1 is formally closed.

## Guardrails

- KISS / YAGNI.
- Ley del Ruido: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- TDD: RED must fail for the intended reason before GREEN.
- Offline runtime.
- No BouncyCastle, iText, Syncfusion, Aspose or second PDF engine.
- No TSA/OCSP/CRL online/HSM/smartcard/Windows Store in first GREEN.
- No merge to `main` without explicit user approval.

## STOP

Checkpoint requested by the user. Stop here. Do not continue implementation from this message.
