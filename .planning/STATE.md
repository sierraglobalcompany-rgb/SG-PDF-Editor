---
gsd_state_version: '1.0'
status: f12-2-task1-capability-closed-f12-professional-in-progress
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

F12 Professional remains IN PROGRESS on branch `feat/f12-professional`.

F12.1 Safe Redaction is **AUTOMATED CLOSED / AUTO PASS**.

F12.2 Cryptographic Signature is **IN PROGRESS**. Task 1 capability gate is **CLOSED / AUTO PASS**; Task 2 has not started.

Main remains untouched at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.

## F12.1 completed scope

- conservative whole-text-object eligibility via existing `RedactionPolicy`;
- fail-closed unsupported cases, including tagged / semantic-text risk;
- staged redaction draft via existing EDITAR state;
- no source mutation while staging;
- source path + fingerprint identity and pre-publish race recheck;
- true removal of approved text objects plus opaque black path;
- reopened-output validation for secret absence, preserved non-target text, renderability and black path;
- multi-target redaction;
- combined normal text edit + redaction;
- minimal existing-panel WPF route with `Redactar`;
- `Guardar como...` remains the only publication path;
- signed/source-drift/cancellation/failure paths remain fail closed;
- duplicate multi-target test coverage merged under the Ley de Ruido.

## F12.2 Task 1 completed scope

- pinned x64 PDFium capability proven for `FPDF_GetSignatureObject`, `FPDFSignatureObj_GetContents`, `FPDFSignatureObj_GetByteRange` and `FPDFSignatureObj_GetSubFilter`;
- PDFsharp 6.2.4 signing surface proven: `DigitalSignatureHandler` plus `IDigitalSigner` from `PdfSharp.System`;
- `System.Security.Cryptography.Pkcs.SignedCms` proven available from the .NET 10 runtime;
- direct PKCS package trial rejected after NuGet `NU1510` proved the explicit reference redundant;
- Ley del Ruido applied: no redundant PackageReference, lock entry, manifest entry or copied license retained;
- runtime dependency allowlist remains PDFium + PDFsharp only.

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

### F12.2 Task 1

Exact functional SHA: `e276ac79c833949476c31cd02b4385f7a8e8cde3`.
Windows CI run `38341476654`, job `115113211445`:

- repository hygiene PASS;
- locked restore PASS;
- Release build PASS;
- `0` warnings;
- `0` errors;
- `1112/1112` tests PASS;
- `0` skipped.

`LOCAL DIRECT RUN = NOT SEPARATELY EVIDENCED`.

## Guardrails

KISS / YAGNI and Ley del Ruido remain mandatory. F12.2 Task 1 added no runtime dependency, no second PDF engine, no signing framework and no UI. PDFium remains the primary inspector; PDFsharp remains the already-approved narrow PDF composition/signature structure dependency. Do not add a direct PKCS PackageReference while .NET 10 provides the capability automatically. No merge to `main` without explicit user approval.

## Manual/private/physical gates

Still open unless separately evidenced:

- real Windows redaction/signing UX;
- representative private PDFs and real PFX/P12 certificates;
- Adobe/Foxit/Edge interoperability;
- offline/representative-hardware smoke;
- smartcard/HSM/TSA/revocation/legal-regulatory validation remains future scope.

Automated PASS does not replace those gates.

## Next

```text
F12.2 Task 1 CLOSED / AUTO PASS
→ Task 2 only: PFX loader + CMS signer
→ RED certificate-loading tests first
→ no PDF writing yet
→ no UI yet
→ no new dependency unless fresh evidence proves required
→ do not merge to main
```

## Continuity

- F12.1 closure: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`.
- F12.2 design: `docs/superpowers/specs/2026-10-12-f12-2-cryptographic-signature-design.md`.
- F12.2 plan: `docs/superpowers/plans/2026-10-12-f12-2-cryptographic-signature.md`.
- Task 1 checkpoint: `.planning/checkpoints/F12_2_CHECKPOINT_2026-10-13_TASK1_RED_CORRECTO.md`.
