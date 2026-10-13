---
gsd_state_version: '1.0'
status: f12-2-pfx-mvp-closed-f12-professional-in-progress
progress:
  total_phases: 13
  completed_phases: 12
  total_plans: 13
  completed_plans: 12
  percent: 92
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.

## Current state

F12 Professional remains **IN PROGRESS** on the feature lineage rooted at `feat/f12-professional`.

- F12.1 Safe Redaction: **AUTOMATED CLOSED / AUTO PASS**.
- F12.2 Cryptographic Signature — PFX/P12 MVP: **AUTOMATED CLOSED / AUTO PASS**.
- Later F12.x professional slices are not started by this closure.

`main` remains untouched at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` (reverified 2026-10-13).

## F12.1 completed scope

- conservative whole-text-object redaction eligibility;
- fail-closed unsupported/tagged/semantic-risk cases;
- true removal + opaque black path;
- transactional `Guardar como...` using the existing EDITAR transaction;
- source fingerprint race protection and reopened-output validation;
- no new PDF engine or runtime dependency.

## F12.2 PFX MVP completed scope

- pinned PDFium signature-inspection capability verified;
- existing PDFsharp 6.2.4 `DigitalSignatureHandler` reused;
- `.pfx/.p12` certificate loading validates file, private key, RSA support and certificate validity;
- detached SHA-256 CMS signing uses .NET `SignedCms` without a redundant direct PKCS package;
- signed PDF writer creates only a destination copy and preserves source bytes;
- PDFium snapshots signature `Contents`, `ByteRange` and `SubFilter` into managed data;
- validator accepts only the demonstrated `adbe.pkcs7.detached` contract, requires one signature and a valid four-integer whole-document `ByteRange`, extracts the exact DER CMS with ASN.1 and verifies it with `SignedCms.CheckSignature(true)`;
- malformed ranges, unknown subfilters, tampering, invalid CMS and unexpected signature counts fail closed;
- transactional signing service reuses existing edit preflight + source fingerprint, writes same-directory temp, validates independently, rechecks source fingerprint and only then publishes;
- existing destination is preserved on failure and temp output is cleaned;
- minimal WPF command `Firmar digitalmente` supports PFX/P12 selection, memory-only password entry, subject/issuer/validity confirmation and `Guardar como...`;
- current session remains unchanged on success, cancellation and failure;
- exact success status is `Firma criptográfica creada y validada localmente.`;
- representative synthetic preservation gate covers source exact bytes, two-page structure, text, native PDF image XObject, bookmark/destination, URI link, pre-existing AcroForm field name/value, selected Info metadata, signed render and independent CMS validation;
- no second PDF engine, signing framework, TSA, HSM, cloud/backend or new runtime dependency was added.

## Fresh automated evidence

### F12.1

Exact functional SHA: `f40b117cba235e221df4c4c5776b41bc8e89dac4`.
Windows CI run `38288995654`, job `114944198278`:

- repository hygiene PASS;
- locked restore PASS;
- Release build PASS;
- `0` warnings;
- `0` errors;
- `1111/1111` tests PASS;
- `0` skipped.

### F12.2 milestones

- Task 1 capability gate: SHA `e276ac79c833949476c31cd02b4385f7a8e8cde3` — `1112/1112` PASS.
- Task 2 PFX loader + CMS signer: SHA `786e981a99fce3a7d051f394aca791a940e081a7` — `1122/1122` PASS.
- Task 3 signed PDF writer: final Task 3 exact-head suite — `1126/1126` PASS.
- Task 4 PDFium inspector + independent validator: SHA `864aed82bdac32e418bf38dde8e6f57c3e799a15` — `1136/1136` PASS.
- Task 5 transactional service: SHA `647c109f8bd42b5b1d83235071d76668c68c8984` — rerun of the same exact SHA `1143/1143` PASS after an unrelated transient WPF thumbnail failure; no unrelated patch was added.
- Task 6 UI + representative preservation gate: SHA `8d7c155972bd62740fbec48bc0a7a52b1b37b92a`, Windows CI run `38354220346`, job `115153250913` — repository hygiene PASS, locked restore PASS, Release build PASS, `0` warnings, `0` errors, `1150/1150` tests PASS, `0` skipped.

`LOCAL DIRECT RUN = NOT SEPARATELY EVIDENCED`.

## KISS / Ley del Ruido audit

- Existing PDFium remains the primary inspector.
- Existing PDFsharp remains the narrow PDF signing writer.
- Existing `PdfEditPreflightInspector` and `PdfEditSourceFingerprint` are reused; no signing-specific duplicate preflight was created.
- No direct `System.Security.Cryptography.Pkcs` PackageReference was retained because .NET 10 already provides the required capability.
- No second PDF engine, generic signing framework, backend, account system or enterprise DI layer was introduced.
- Task 6 reused the application's existing dynamic partial-UI pattern; `MainWindow.xaml` did not require a parallel command architecture.
- Password is kept only in UI/local memory and is not persisted or logged.
- No merge to `main` without explicit user approval.

## Manual/private/physical gates

Still **NOT RUN** unless separately evidenced:

- real Windows end-to-end signing UX with user-selected certificates;
- representative private PDFs and real/private PFX/P12 corpus;
- Adobe / Foxit / Edge interoperability;
- physical/offline/representative-hardware smoke;
- Windows Certificate Store integration;
- smartcard/HSM;
- TSA/timestamping;
- revocation/OCSP/CRL policy;
- legal/regulatory signature qualification or compliance validation.

The automated synthetic/self-signed-certificate PASS does not replace those gates.

## Next / stop rule

```text
F12.2 PFX MVP CLOSED / AUTO PASS
→ STOP
→ do not start F12.3 implicitly
→ do not merge to main
→ any later professional slice requires its own narrow spec/plan and explicit continuation
```

## Continuity

- F12.1 closure: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`.
- F12.2 design: `docs/superpowers/specs/2026-10-12-f12-2-cryptographic-signature-design.md`.
- F12.2 plan: `docs/superpowers/plans/2026-10-12-f12-2-cryptographic-signature.md`.
- F12.2 closure: `docs/history/2026-10-12-F12-2-cryptographic-signature-closure.md`.
