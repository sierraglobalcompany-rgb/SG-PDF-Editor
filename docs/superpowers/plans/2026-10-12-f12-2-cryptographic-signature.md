# F12.2 Cryptographic Signature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir firma criptográfica PDF local mediante PFX/P12, CMS/PKCS#7 SHA-256, `Guardar como...` y validación independiente del `ByteRange`, sin modificar el original ni introducir un segundo motor PDF general.

**Architecture:** PDFsharp 6.2.4, ya presente, será responsable únicamente de estructurar la firma PDF (`DigitalSignatureHandler`). Un adaptador pequeño `IDigitalSigner` usará `System.Security.Cryptography.Pkcs` 10.0.12 y `X509Certificate2` para producir CMS detached SHA-256. PDFium seguirá siendo el lector/inspector principal y expondrá únicamente las primitivas de firma necesarias para leer `Contents` y `ByteRange`; un validador independiente reconstruirá los bytes firmados y verificará CMS antes de publicar atómicamente la salida.

**Tech Stack:** .NET 10 / WPF / PDFium 156.0.8076 / PDFsharp 6.2.4 / System.Security.Cryptography.Pkcs 10.0.12 / xUnit.

**Spec:** `docs/superpowers/specs/2026-10-12-f12-2-cryptographic-signature-design.md`

## Global Constraints

- KISS/YAGNI; no arquitectura de proveedores, plugins ni segundo writer general.
- Offline: no TSA, OCSP, CRL online, SaaS, API keys ni backend.
- PFX/P12 primero; Windows Certificate Store queda fuera del primer GREEN.
- SHA-256 es el único digest del MVP.
- Primer MVP admite certificados RSA con clave privada; ECDSA queda para un gate posterior si aparece una necesidad real.
- Contraseña PFX solo en memoria; no logs, archivos, settings ni persistencia.
- `Guardar como...` únicamente; el PDF fuente nunca se sobrescribe.
- PDF con firma previa y PDF abierto con contraseña siguen fail-closed.
- No BouncyCastle, iText, Syncfusion, Aspose ni SDK comercial/copyleft fuerte.
- `System.Security.Cryptography.Pkcs` se fija en `10.0.12`, estable de la línea .NET 10 y licencia MIT; lock y manifiesto deben quedar reproducibles.
- No merge a `main` sin aprobación explícita.

## Review Focus

- PFX válido pero sin clave privada → rechazo antes de escribir salida.
- PFX con contraseña incorrecta → error controlado, sin residuos ni salida parcial.
- `Contents` con padding hexadecimal de PDFsharp → el validador debe extraer exactamente el CMS DER, no depender de trimming ingenuo de ceros.
- `ByteRange` inválido/solapado/fuera de archivo → rechazo fail-closed antes de `SignedCms.CheckSignature`.
- Fuente cambia entre staging y publicación → no publicar y conservar intacto cualquier destino previo.

---

### Task 1: Capability + dependency gate

**Files:**
- Create: `tests/SGPdf.App.Tests/PdfCryptographicSigningCapabilityTests.cs`
- Modify: `src/SGPdf.App/SGPdf.App.csproj`
- Modify: `src/SGPdf.App/packages.lock.json`
- Modify: `tests/SGPdf.App.Tests/packages.lock.json`
- Modify: `third_party/manifest.json`
- Create: `third_party/licenses/System.Security.Cryptography.Pkcs-MIT.txt`

**Interfaces:**
- Consumes: pinned PDFium runtime and existing PDFsharp 6.2.4 package.
- Produces: proven availability of `SignedCms` and pinned PDFium exports `FPDF_GetSignatureObject`, `FPDFSignatureObj_GetContents`, `FPDFSignatureObj_GetByteRange`, `FPDFSignatureObj_GetSubFilter`.

- [ ] **Step 1: Write the failing capability test**

Add `CapabilityGate_PinnedRuntimeSupportsPdfSharpPkcsAndPdfiumSignatureInspection` asserting:
- `Type.GetType("System.Security.Cryptography.Pkcs.SignedCms, System.Security.Cryptography.Pkcs")` is non-null;
- the loaded pinned `pdfium.dll` exports the four signature inspection symbols above;
- `PdfSharp.Pdf.Signatures.DigitalSignatureHandler` and `IDigitalSigner` are available from the already referenced PDFsharp package.

- [ ] **Step 2: Run the focal test and verify RED**

Run the focal xUnit test on Windows. Expected: FAIL because `System.Security.Cryptography.Pkcs` is not yet in the locked runtime dependency graph; if any pinned PDFium export is missing, STOP F12.2 with a documented NO-GO rather than adding a parser.

- [ ] **Step 3: Add the minimal dependency**

Add `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="10.0.12" />` only to `src/SGPdf.App/SGPdf.App.csproj`; restore locked files; record the component in `third_party/manifest.json` and add the upstream MIT license text. Do not add BouncyCastle or another signing package.

- [ ] **Step 4: Run focal + locked restore + Release build**

Expected: capability test PASS, `dotnet restore --locked-mode` PASS after committed lock update, Release build 0 warnings/0 errors.

- [ ] **Step 5: Commit**

Commit only gate/dependency/license files.

---

### Task 2: PFX loader + CMS signer

**Files:**
- Create: `src/SGPdf.App/Features/Signing/PfxCertificateLoader.cs`
- Create: `src/SGPdf.App/Features/Signing/DotNetCmsSigner.cs`
- Create: `tests/SGPdf.App.Tests/PfxCertificateLoaderTests.cs`
- Create: `tests/SGPdf.App.Tests/DotNetCmsSignerTests.cs`

**Interfaces:**
- Produces: `internal static X509Certificate2 PfxCertificateLoader.Load(string path, string password, DateTimeOffset now)`.
- Produces: `internal sealed class DotNetCmsSigner : PdfSharp.Pdf.Signatures.IDigitalSigner` with constructor `DotNetCmsSigner(X509Certificate2 certificate)`.
- `DotNetCmsSigner.CertificateName : string`.
- `DotNetCmsSigner.GetSignatureSizeAsync() : Task<int>`.
- `DotNetCmsSigner.GetSignatureAsync(Stream stream) : Task<byte[]>`.

- [ ] **Step 1: RED tests for certificate loading**

Generate test certificates in-memory; export temporary PFX fixtures. Assert valid RSA PFX loads with private key using ephemeral key storage; wrong password throws controlled `CryptographicException`; certificate without private key is rejected; expired/not-yet-valid certificate is rejected against injected `now`.

- [ ] **Step 2: GREEN `PfxCertificateLoader`**

Use `X509CertificateLoader.LoadPkcs12FromFile` with ephemeral key storage. Validate file existence, `HasPrivateKey`, RSA private key availability and `NotBefore/NotAfter`. Do not persist password or certificate bytes.

- [ ] **Step 3: RED tests for CMS signer**

Sign known bytes, decode returned CMS with `SignedCms`, verify detached signature against the exact bytes, assert SHA-256 digest and certificate identity, and assert altered bytes fail verification.

- [ ] **Step 4: GREEN `DotNetCmsSigner`**

Implement `IDigitalSigner` using `SignedCms(new ContentInfo(bytes), detached: true)` + `CmsSigner(certificate)` with SHA-256 OID. `GetSignatureSizeAsync` signs a one-byte stream once and reserves `actualLength + 4`, matching PDFsharp's conservative no-TSA padding approach. No timestamp or HTTP code.

- [ ] **Step 5: Run focused tests + full suite, then commit**

No PDF writing yet in this task.

---

### Task 3: Real signed PDF writer

**Files:**
- Create: `src/SGPdf.App/Features/Signing/PdfCryptographicSignatureWriter.cs`
- Create: `tests/SGPdf.App.Tests/PdfCryptographicSignatureWriterTests.cs`

**Interfaces:**
- Consumes: `DotNetCmsSigner` and PDFsharp `DigitalSignatureHandler`.
- Produces: `internal void PdfCryptographicSignatureWriter.Write(string sourcePath, string destinationPath, X509Certificate2 certificate, CancellationToken cancellationToken = default)`.

- [ ] **Step 1: RED real-PDF test**

Create a small synthetic PDF, snapshot source bytes, generate a valid test RSA certificate, call `Write`, then assert source bytes unchanged, destination exists, destination reopens with `PdfDocumentSession`, page count is stable, render succeeds, and `GetCryptographicSignatureCount()` returns exactly `1`.

- [ ] **Step 2: GREEN narrow writer**

Open source with PDFsharp in modify mode, attach `DigitalSignatureHandler.ForDocument(document, new DotNetCmsSigner(certificate), options)`, use SHA-256 signer, `PageIndex = 0`, invisible/default-zero rectangle for this technical gate, and save only to the provided destination path. Do not add visual appearance or UI.

- [ ] **Step 3: Add narrow input tests**

Reject same source/destination path, missing source and cancellation before save. Do **not** duplicate the signed/password policy here; `PdfEditPreflightInspector` in Task 5 remains the single owner of those decisions.

- [ ] **Step 4: Run focused + full suite, then commit**

---

### Task 4: PDFium signature inspection + independent validator

**Files:**
- Modify: `src/SGPdf.App/Pdf/PdfiumNative.cs`
- Create: `src/SGPdf.App/Pdf/PdfCryptographicSignatureInfo.cs`
- Create: `src/SGPdf.App/Pdf/PdfDocumentSession.Signatures.cs`
- Create: `src/SGPdf.App/Pdf/PdfCryptographicSignatureValidator.cs`
- Create: `tests/SGPdf.App.Tests/PdfCryptographicSignatureInspectorTests.cs`
- Create: `tests/SGPdf.App.Tests/PdfCryptographicSignatureValidatorTests.cs`

**Interfaces:**
- Adds PDFium declarations for `FPDF_GetSignatureObject`, `FPDFSignatureObj_GetContents`, `FPDFSignatureObj_GetByteRange`, `FPDFSignatureObj_GetSubFilter`.
- Produces: `internal sealed record PdfCryptographicSignatureInfo(byte[] Contents, int[] ByteRange, string SubFilter)`.
- Produces: `internal IReadOnlyList<PdfCryptographicSignatureInfo> PdfDocumentSession.GetCryptographicSignatures(CancellationToken cancellationToken = default)`; no native handles escape the call.
- Produces: `internal void PdfCryptographicSignatureValidator.Validate(string filePath, CancellationToken cancellationToken = default)`.

- [ ] **Step 1: RED inspector test against Task 3 output**

Assert one managed signature snapshot is returned; `ByteRange` has exactly four integers for MVP; `Contents` is non-empty; `SubFilter` is present; no `IntPtr` is retained in returned state.

- [ ] **Step 2: GREEN PDFium inspector**

Read sizes first, allocate managed arrays, copy `Contents`, `ByteRange` and ASCII subfilter under `PdfiumRuntime.NativeGate`; fail on zero/error lengths. Never store `FPDF_SIGNATURE` beyond the native call scope.

- [ ] **Step 3: RED validator tests**

For a valid signed PDF assert PASS. Add explicit failures for negative/out-of-range/overlapping `ByteRange`, wrong count, tampered covered byte, invalid CMS, and output with signature count other than one.

- [ ] **Step 4: GREEN validator**

Require one signature and a four-int whole-document PDF signature range. Reconstruct signed bytes from the two ranges. Extract the DER CMS length from `Contents` using ASN.1 encoded-value parsing rather than trimming trailing zero bytes. Decode via detached `SignedCms` and call `CheckSignature(verifySignatureOnly: true)` against the reconstructed bytes. Reopen/render through PDFium as part of validation.

- [ ] **Step 5: Corruption proof**

Flip one byte inside a covered range of a valid output and prove validator rejection.

- [ ] **Step 6: Run focused + full suite, then commit**

---

### Task 5: Transactional signing service / Save As publication

**Files:**
- Create: `src/SGPdf.App/Features/Signing/PdfCryptographicSigningService.cs`
- Create: `tests/SGPdf.App.Tests/PdfCryptographicSigningServiceTests.cs`

**Interfaces:**
- Consumes: existing `PdfEditSourceFingerprint`, `PdfEditPreflightInspector`, `PfxCertificateLoader`, `PdfCryptographicSignatureWriter`, `PdfCryptographicSignatureValidator`.
- Produces: `internal void PdfCryptographicSigningService.Sign(string sourcePath, string destinationPath, string pfxPath, string password, CancellationToken cancellationToken = default)`.

- [ ] **Step 1: RED success transaction test**

Assert source bytes unchanged; destination is signed and validator PASS; signing uses a temp file before publication; temp residue is absent after success.

- [ ] **Step 2: GREEN minimal transaction**

Order: open/preflight source → fingerprint → load PFX → write same-directory temp → close all handles → validate temp independently → fingerprint recheck → atomically replace/move destination → cleanup temp in `finally`.

- [ ] **Step 3: RED failure/cancellation tests**

Cover wrong PFX password, already-signed source, source fingerprint drift before publish, cancellation, validator failure, and a pre-existing valid destination. Assert no partial publish and pre-existing destination preserved on every failure.

- [ ] **Step 4: GREEN failure behavior**

Reuse existing preflight/fingerprint semantics; do not duplicate signature policy. Publish only after validator PASS and final source fingerprint match.

- [ ] **Step 5: Run focused + full suite, then commit**

---

### Task 6: Minimal WPF flow + closure hardening

**Files:**
- Create: `src/SGPdf.App/MainWindow.Signing.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Create: `tests/SGPdf.App.Tests/MainWindowCryptographicSigningTests.cs`
- Modify at closure only: `.planning/STATE.md`, `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md`
- Create at closure only: `docs/history/2026-10-12-F12-2-cryptographic-signature-closure.md`

**Interfaces:**
- Consumes: `PdfCryptographicSigningService`.
- UI command: `Firmar digitalmente` only when a PDF session exists and no incompatible busy/edit mode is active.

- [ ] **Step 1: RED WPF presentation test**

Assert existing window gains one focused command `Firmar digitalmente`; invoking it with no open PDF is disabled/no-op; no professional sidebar or second document editor is created.

- [ ] **Step 2: GREEN minimal UI shell**

Add one existing-toolbar/menu command and put logic in `MainWindow.Signing.cs`. Flow: choose `.pfx/.p12` → secure password prompt kept in memory → show subject/issuer/validity → choose destination via `Guardar como...` → call service. Do not add Windows Store, TSA or visual signature appearance here.

- [ ] **Step 3: RED UI failure-state tests**

Wrong password, cancellation and service failure must leave current PDF/session unchanged and show conservative Spanish status; success text is exactly `Firma criptográfica creada y validada localmente.` only after service validation passes.

- [ ] **Step 4: GREEN UI behavior**

No password persistence/logging. Reuse existing dialog/status conventions and busy-state handling.

- [ ] **Step 5: Preservation + scope audit**

Characterize at least: page count, representative text, representative image, bookmarks/links/forms/metadata where supported by the PDFsharp signing pass, source exact bytes, signed output render, and independent CMS validation. If PDFsharp signing demonstrably damages a preserved structure, fail closed or stop F12.2; do not patch with a second PDF engine.

- [ ] **Step 6: Exact-head CI**

Run repository hygiene, locked restore, Release build and complete Windows suite. Require 0 warnings/0 errors and all tests PASS on exact HEAD.

- [ ] **Step 7: Closure docs**

Mark only **F12.2 PFX MVP** AUTO PASS. Keep Windows Certificate Store, real PFX/private corpus, Adobe/Foxit/Edge interoperability, smartcard/HSM, TSA/revocation and legal/regulatory validation as NOT RUN / future scope.

- [ ] **Step 8: Commit and STOP**

Do not start F12.3 and do not merge to `main` without explicit approval.

---

## Execution order / microblock rule

Execute exactly one task or one meaningful RED→GREEN sub-block at a time. For each block:

`PRECHECK once → RED focal → confirm expected failure → GREEN minimum → focused tests → full suite/exact-head CI when the task closes → one durable checkpoint only when useful/requested → STOP before opening the next architectural concern.`

Avoid CI/checkpoint churn per assertion. Do not create speculative abstractions for Windows Store, TSA, HSM, ECDSA, batch or visual appearance during the PFX MVP.