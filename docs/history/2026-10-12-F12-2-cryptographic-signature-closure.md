# F12.2 Cryptographic Signature — PFX MVP automated closure

Date: 2026-10-13  
Branch: `feat/f12-2-task6-ui-closure`  
Task 5 base / merge-base: `647c109f8bd42b5b1d83235071d76668c68c8984`  
Functional + preservation exact HEAD: `8d7c155972bd62740fbec48bc0a7a52b1b37b92a`  
Main remains untouched: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## Closure status

F12.2 Cryptographic Signature — **PFX/P12 MVP is AUTOMATED CLOSED / AUTO PASS**.

This closes only the narrow local PFX/P12 MVP. It does **not** close all of F12 Professional and does not claim Windows Certificate Store, real/private certificate corpus, external-reader interoperability, HSM/smartcard, TSA/revocation or legal/regulatory validation.

## Final functional exact-head evidence

Windows CI run: `38354220346`  
Job: `115153250913`  
Exact tested SHA: `8d7c155972bd62740fbec48bc0a7a52b1b37b92a`

- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- build warnings: `0`;
- build errors: `0`;
- tests: `1150/1150 PASS`;
- skipped: `0`.

The exact-head suite includes the minimal WPF flow and the representative preservation gate.

## Milestone evidence

- Task 1 capability gate: `e276ac79c833949476c31cd02b4385f7a8e8cde3` — `1112/1112` PASS.
- Task 2 PFX loader + CMS signer: `786e981a99fce3a7d051f394aca791a940e081a7` — `1122/1122` PASS.
- Task 3 signed PDF writer: exact-head suite — `1126/1126` PASS.
- Task 4 PDFium inspector + independent validator: `864aed82bdac32e418bf38dde8e6f57c3e799a15` — `1136/1136` PASS.
- Task 5 transactional signing service: `647c109f8bd42b5b1d83235071d76668c68c8984` — `1143/1143` PASS on rerun of the same SHA. A prior unrelated WPF thumbnail flake was not patched.
- Task 6 WPF flow before preservation expansion: `ab71ba46f45f84f18abed4f12a73e05b32c036d5` — `1149/1149` PASS.
- Task 6 preservation closure: `8d7c155972bd62740fbec48bc0a7a52b1b37b92a` — `1150/1150` PASS.

## Implemented guarantees

### Certificate and CMS

- User selects a local `.pfx/.p12`.
- Loader rejects missing/invalid files, incorrect password, missing private key, unsupported non-RSA key and certificates outside their validity window.
- RSA detached CMS uses SHA-256 and .NET `SignedCms`.
- No redundant direct `System.Security.Cryptography.Pkcs` PackageReference is retained; .NET 10 provides the capability used by the application.

### PDF writer

- Existing PDFsharp 6.2.4 `DigitalSignatureHandler` is reused.
- Signature is cryptographic/invisible in this MVP; no visual signature appearance is added.
- Source and destination must differ.
- Source is never intentionally modified.
- The PDFsharp `RangedStream` compatibility issue was fixed narrowly in the signer: no `CanSeek` probe and no generic `Stream.CopyTo()` dependency on that stream contract.

### Independent inspection and validation

- Existing pinned PDFium inspects signature objects.
- Native signature data is copied into managed snapshots; native handles do not escape the session/native gate.
- MVP requires exactly one signature.
- `ByteRange` must contain exactly four valid integers and represent the expected whole-document detached-signature layout.
- Only the demonstrated `adbe.pkcs7.detached` subfilter is accepted.
- CMS DER length is derived using ASN.1 encoded-value parsing; zero-padding is not removed by blind trimming.
- `SignedCms.CheckSignature(true)` validates the detached CMS cryptographically.
- Tampering, malformed/out-of-range/overlapping ranges, invalid CMS, unknown subfilter and unexpected signature count fail closed.
- Signed output is reopened/rendered as part of validation.

### Transaction / Save As

The service follows the narrow A→B sequence:

`preflight → source fingerprint → PFX → same-directory temp → independent validate → source fingerprint recheck → replace/move destination → cleanup`

- Existing `PdfEditPreflightInspector` is reused; no duplicate signing policy service exists.
- Existing `PdfEditSourceFingerprint` is reused.
- A source already carrying a cryptographic signature is blocked by the reused preflight policy.
- Password-opened sources remain blocked by the same preservation policy.
- Wrong PFX password, cancellation, source drift and validator failure publish nothing.
- A pre-existing valid destination remains intact on pre-publication failure.
- Temporary output is removed after success/failure where owned by the transaction.

### Minimal WPF flow

One focused command is exposed: `Firmar digitalmente`.

It is available only when a PDF session exists and no incompatible busy/edit/organize/visual-sign mode is active.

Flow:

`PFX/P12 → password in memory → subject/issuer/validity preview → Guardar como... → transactional signing service`

- Password is not persisted or logged.
- Cancel/failure leaves the current PDF session unchanged.
- Conservative Spanish status is shown on failure/cancel.
- Success is reported only after service validation returns successfully, with the exact text: `Firma criptográfica creada y validada localmente.`

## Representative preservation gate

A synthetic two-page PDF is characterized before and after signing. The exact-head PASS proves preservation for that representative fixture of:

- exact original source bytes;
- page count / two-page structure;
- representative searchable text;
- representative native PDF image XObject;
- bookmark title + destination page;
- URI link;
- a pre-existing AcroForm field name/value while allowing the signer to add its own signature field;
- selected Info metadata (`Title`, `Author`);
- successful signed-output render with stable dimensions;
- exactly one cryptographic signature;
- independent CMS validation through the F12.2 validator.

This is a **representative synthetic preservation gate**, not universal proof for arbitrary PDFs or every PDF feature.

## KISS / Ley del Ruido audit

- No second PDF engine.
- No new runtime PDF/crypto package.
- PDFium remains the primary inspector.
- PDFsharp remains the already-approved narrow writer/signature-structure dependency.
- Existing edit preflight and fingerprint are reused.
- No backend, account system, cloud signing service, generic signing framework, enterprise DI layer or certificate abstraction hierarchy was added.
- Task 6 reused the application's established dynamic partial-UI pattern. `MainWindow.xaml` was intentionally left unchanged rather than introducing a second command architecture inconsistent with the existing dynamic FIRMAR/EDITAR/ORGANIZAR controls.
- Preservation fixture uses native PDF objects instead of adding an image-decoder dependency solely for tests.

## Scope audit for Task 6

Relative to Task 5 base `647c109f8bd42b5b1d83235071d76668c68c8984`, functional/test changes before closure docs are limited to:

1. `src/SGPdf.App/MainWindow.Signing.cs` — added;
2. `tests/SGPdf.App.Tests/MainWindowCryptographicSigningTests.cs` — added;
3. `tests/SGPdf.App.Tests/PdfCryptographicSignatureWriterTests.cs` — preservation coverage extended.

No `.csproj`, lockfile, runtime manifest, second PDF engine or unrelated production module was changed by Task 6.

## Explicitly NOT RUN / future scope

Automated AUTO PASS must not be read as manual/product/legal certification. Still open:

- real Windows end-to-end UX on representative user machines;
- representative private PDF corpus;
- real/private PFX/P12 certificates and CA chains;
- Adobe Acrobat / Foxit / Edge interoperability;
- Windows Certificate Store selection;
- ECDSA or other key families beyond the proven RSA MVP;
- smartcard / HSM;
- TSA / trusted timestamping;
- revocation policy, OCSP and CRL;
- certificate trust-chain policy beyond cryptographic signature correctness;
- legal/regulatory signature qualification or jurisdiction-specific compliance;
- visible cryptographic signature appearance.

## Stop rule

**STOP after F12.2.** Do not start F12.3 or another professional slice implicitly. Do not merge to `main` without explicit user approval. Any later slice requires its own narrow spec/plan and must preserve KISS, Ley del Ruido, fail-closed semantics and exact-head evidence.
