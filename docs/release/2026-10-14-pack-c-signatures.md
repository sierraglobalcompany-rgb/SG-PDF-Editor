# Pack C — Visual + Cryptographic Signatures

Date: 2026-10-14

Purpose: canonical executable QA record for visual-signature and PFX/P12 signing release checks. This file maps existing automated evidence and defines the remaining real/manual Windows checks. Automated PASS does **not** imply manual, hardware, certificate, external-reader or legal/regulatory PASS.

## Authority / base

- repository: `sierraglobalcompany-rgb/SG-PDF-Editor`
- branch: `plan/release-pack-c-qa`
- base: `dede350ac11c7698e30d456b6232bf44b3a3bc07`
- functional implementation head: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7` — READ ONLY
- F0–F12 functional baseline: CLOSED / AUTO PASS / FEATURE FROZEN

## Status semantics

- `AUTO PREFLIGHT PASS`: existing automated implementation/evidence is present and exact-head CI is green.
- `PASS`: the stated real/manual item was actually executed with evidence.
- `FAIL`: executed and failed.
- `PARTIAL`: only a documented subset was executed.
- `NOT RUN — ENVIRONMENT`: current execution environment cannot perform the required Windows/photo/stylus/certificate/external-reader/manual action.

## Automated evidence map

No new product tests are added for this pack because the approved baseline already has automated evidence. Add a test only if real QA demonstrates an uncovered behavior, defect or regression risk.

| Area | Existing automated authority | Preflight |
|---|---|---|
| Transparent PNG signature import | `.planning/REQUIREMENTS.md`: `SIGN-01` | AUTO PREFLIGHT PASS |
| Move / proportional resize / duplicate / delete | `SIGN-02` | AUTO PREFLIGHT PASS |
| Device↔PDF coordinate mapping | `SIGN-03` | AUTO PREFLIGHT PASS |
| Save copy + reopen/render | `SIGN-04` | AUTO PREFLIGHT PASS |
| Photo/scan cleanup → `SignatureAsset` | `SIGN-05` | AUTO PREFLIGHT PASS; real photo quality NOT RUN |
| WPF InkCanvas drawing | `SIGN-06` | AUTO PREFLIGHT PASS; mouse/stylus/touch UX NOT RUN |
| Local signature library JSON + GUID PNG | `SIGN-07` | AUTO PREFLIGHT PASS; real library UX NOT RUN |
| PFX/P12 RSA signing | `.planning/REQUIREMENTS.md`: `PRO-SIGN-PFX` | AUTO PREFLIGHT PASS |
| Detached SHA-256 CMS + PDFsharp writer + PDFium signature snapshot | `PRO-SIGN-PFX` | AUTO PREFLIGHT PASS |
| Strict one-signature / ByteRange / `adbe.pkcs7.detached` validation | `PRO-SIGN-PFX` | AUTO PREFLIGHT PASS |
| Independent CMS verification | `SignedCms.CheckSignature(true)` in approved F12.2 path | AUTO PREFLIGHT PASS |
| Transactional temp→validate→fingerprint recheck→publish | `PRO-SIGN-PFX` | AUTO PREFLIGHT PASS |
| Password memory-only / destination preservation on failure | `PRO-SIGN-PFX` | AUTO PREFLIGHT PASS; real cancel/failure UX NOT RUN |

### Automated preflight verdict

`PASS`

Reason: visual-signature and PFX/P12 baseline behaviors are already represented in the closed automated baseline. Pack C is release QA/evidence work, not feature development.

## Explicit scope limits

Pack C does **not** certify or add:

- Windows Certificate Store integration;
- smartcard/HSM;
- TSA/timestamping;
- revocation policy / OCSP / CRL workflow;
- legal/regulatory qualification or jurisdiction-specific signature status;
- universal compatibility with every PDF reader;
- a second signing engine;
- cloud signing.

These remain outside the approved baseline unless separately approved.

## Representative corpus / environment

Use non-customer material whenever possible:

1. normal unsigned PDF;
2. representative transparent PNG signature;
3. real photo/scan of a signature on clean white paper;
4. mouse drawing and, when available, stylus/touch input;
5. representative local test PFX/P12 containing an RSA private key;
6. wrong-password/cancel/failure case;
7. destination-existing case for preservation checks;
8. at least one external reader when available (Adobe/Foxit/Edge).

Private keys, PFX/P12 files, passwords and customer PDFs stay local/ignored and are never committed.

## Executable manual checklist

Every row starts as `NOT RUN — ENVIRONMENT` until actually executed.

| ID | Fixture / precondition | Action | Expected result | Current result | Evidence to record | Severity if failed |
|---|---|---|---|---|---|---|
| C01 | transparent PNG + normal PDF | Import signature; place/move/resize | Signature appears with expected proportions and remains usable | NOT RUN — ENVIRONMENT | Windows build + screenshots | P1 if primary visual-sign flow unusable |
| C02 | C01 placed signature | Save As; reopen in SG PDF Editor | Placement persists and source remains unchanged | NOT RUN — ENVIRONMENT | source/output hashes + screenshot | P0 if source corrupted; P1 if output wrong |
| C03 | real photo/scan signature | Run approved cleanup/preparation flow | Background cleanup is visually usable and signature remains recognizable | NOT RUN — ENVIRONMENT | before/after local screenshots | P1 if workflow unusable; subjective cosmetic issue P2 |
| C04 | mouse input | Draw signature with InkCanvas; place/save/reopen | Drawing is usable and persists through approved asset/placement path | NOT RUN — ENVIRONMENT | screenshot + output id | P1 |
| C05 | stylus/touch available | Draw with stylus/touch; place/save/reopen | Input remains usable without material coordinate/placement defect | NOT RUN — ENVIRONMENT | device model + screenshot | P1; `NOT RUN` if hardware unavailable |
| C06 | local signature library | Add, reuse, then remove representative signature asset | Library lifecycle behaves as designed; no cloud/database dependency appears | NOT RUN — ENVIRONMENT | local library note/screenshots | P1 if lifecycle unusable; P2 minor UX |
| C07 | valid RSA PFX/P12 + normal PDF | Sign with correct password | Signed output is created; source remains unchanged | NOT RUN — ENVIRONMENT | cert alias/fingerprint if safe, output hash, result | P0 if source/destination corruption; P1 if signing fails |
| C08 | C07 signed output | Reopen/inspect in SG PDF Editor | Signed PDF opens and signature structure is accepted by approved local validation | NOT RUN — ENVIRONMENT | screenshot/note | P1 |
| C09 | C07 signed output | Open in at least one external reader | External reader opens document and recognizes signature structure without material corruption | NOT RUN — ENVIRONMENT | reader/version + screenshot/note | P1 for material compatibility failure |
| C10 | valid PFX/P12 | Enter wrong password | Controlled failure; no source change; no invalid destination publication | NOT RUN — ENVIRONMENT | error text + source/destination hashes | P0 if destructive; P1 if unsafe/uncontrolled |
| C11 | signing dialog | Cancel before publication | No signed output is published unexpectedly; source remains unchanged | NOT RUN — ENVIRONMENT | file listing/hashes | P0 if unintended publication/source change |
| C12 | destination already exists | Attempt signing to existing destination under approved flow | Existing destination remains preserved on failure/cancel according to baseline | NOT RUN — ENVIRONMENT | destination hash before/after | P0 if existing file overwritten/corrupted |
| C13 | PFX/P12 signing session | Observe password handling during normal UI flow | Password is not persisted by product workflow and is treated memory-only | NOT RUN — ENVIRONMENT | manual observation; never record password | P0/P1 depending on persistence/security defect |
| C14 | signed/unsigned representative PDFs | Repeat normal signature workflows with network disabled | Approved local signature flows remain usable offline | NOT RUN — ENVIRONMENT | method used to disable network + result | P1 if normal local flow unexpectedly requires network |

## Security / evidence rules

- Never commit PFX/P12, private keys, passwords or customer signatures.
- Never paste certificate passwords into Markdown, issues, screenshots or logs.
- Certificate identity evidence should be limited to non-secret metadata/fingerprint when safe.
- A reader showing a trust-chain warning is not automatically a product defect; distinguish document cryptographic structure from certificate trust/revocation/legal qualification.
- Do not claim TSA, long-term validation, qualified/electronic-signature legal status or regulatory compliance from this MVP.

## Defect routing

If any item fails:

`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → RERUN FAILED QA ITEM`

Rules:

- P0/P1 blocks Pack C closure and release progression.
- Product defects go to a focused `feat/fix-*` branch from the latest applicable green head.
- Do not fix product code in the QA branch.
- Do not add certificate stores, TSA, HSM, cloud services, abstractions or packages unless a separately approved capability decision proves they are required.
- External-reader trust-chain behavior must be classified before treating it as a product defect.

## Exit gate

Pack C is `PASS` only when all required representative manual items are actually executed with evidence and there is no open P0/P1.

Until then:

- automated evidence preflight: `PASS`;
- real/manual Pack C: `NOT RUN — ENVIRONMENT`;
- final Pack C gate: `NOT RUN`;
- release remains open;
- F0–F12 automated functional baseline remains closed/frozen.

## KISS / noise audit

- new product code: NO
- new tests: NO
- new runtime dependency: NO
- second signing/PDF engine: NO
- certificate-store/TSA/HSM/cloud architecture: NO
- manual PASS inferred from automated evidence: NO
- `main` changed: NO

This is intentionally one compact Pack C QA artifact rather than one document per signature subsystem.