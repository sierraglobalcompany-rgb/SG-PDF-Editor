# Pack B — ZPL / QR / Physical Print-Scan

Date: 2026-10-14

Purpose: canonical executable QA record for Pack B. This document maps existing automated evidence and defines the remaining private/physical ZPL + QR checks. It does **not** convert automated decode/render evidence into physical printer/scanner PASS.

## Authority / base

- repository: `sierraglobalcompany-rgb/SG-PDF-Editor`
- branch: `plan/release-pack-b-qa`
- base: `a9d9efee51bff1ab97e329f59c14861911790099`
- functional implementation head: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7` — READ ONLY
- Pack A preflight checkpoint: `a9d9efee51bff1ab97e329f59c14861911790099`
- F0–F12: CLOSED / AUTO PASS / FEATURE FROZEN

## Status semantics

- `AUTO PREFLIGHT PASS`: existing automated implementation/evidence is present and current exact-head CI is green.
- `PASS`: the real/private/physical item was actually executed and evidence was recorded.
- `FAIL`: the real/private/physical item was executed and failed.
- `PARTIAL`: only a documented subset was actually executed.
- `NOT RUN — ENVIRONMENT`: the current environment cannot perform the required printer/ruler/scanner/phone/private-corpus action.

Automated PASS never implies private corpus, physical size, printer, scanner, phone or offline-device PASS.

## Pack B automated evidence map

No new product tests are added because approved ZPL/QR baseline behavior already has automated evidence. Add a test only for a demonstrated uncovered behavior, defect or regression risk.

| Area | Existing automated authority | Preflight |
|---|---|---|
| Synthetic ZPL gate / required commands / benchmark | `.planning/REQUIREMENTS.md`: F1 `ZPL-GATE-02..04` | AUTO PREFLIGHT PASS |
| Open `.zpl/.txt/.prn` | `.planning/REQUIREMENTS.md`: `LABEL-01` | AUTO PREFLIGHT PASS |
| Multi-design separation + `^PQ` metadata | `.planning/REQUIREMENTS.md`: `LABEL-02` | AUTO PREFLIGHT PASS |
| Local Labelize preview | `.planning/REQUIREMENTS.md`: `LABEL-03` | AUTO PREFLIGHT PASS |
| File / one-each / custom quantity | `.planning/REQUIREMENTS.md`: `LABEL-04` | AUTO PREFLIGHT PASS |
| Layouts + thermal/A4/Letter/custom media | `.planning/REQUIREMENTS.md`: `LABEL-05` | AUTO PREFLIGHT PASS |
| PDF export without intentional barcode deformation | `.planning/REQUIREMENTS.md`: `LABEL-06` | AUTO PREFLIGHT PASS |
| Barcode/QR automated decode | `.planning/REQUIREMENTS.md`: `LABEL-08` automated decode evidence | AUTO PREFLIGHT PASS; physical scan NOT RUN |
| QR PNG generation | `.planning/REQUIREMENTS.md`: F12.6 `PRO-CODEGEN-01..05` | AUTO PREFLIGHT PASS |
| QR exact payload decode incl. UTF-8 / ZPL-looking text | `.planning/REQUIREMENTS.md`: `PRO-CODEGEN-04` | AUTO PREFLIGHT PASS |
| Local/offline QR generation | `.planning/REQUIREMENTS.md`: `PRO-CODEGEN-01` + cross-cutting OFFLINE | AUTO PREFLIGHT PASS; physical offline smoke NOT RUN |
| Private fixture hygiene | `.planning/REQUIREMENTS.md`: cross-cutting PRIVACY + CI hygiene | AUTO PREFLIGHT PASS |

### Automated preflight verdict

`PASS`

Reason: Pack B approved behaviors are represented in the closed automated baseline. The remaining evidence is specifically private/physical interoperability and dimensional accuracy.

## Representative corpus / hardware

Use:

1. synthetic representative ZPL labels already allowed in repo;
2. ignored local Mercado Libre representative fixtures when available;
3. at least one label with barcode;
4. at least one label with QR;
5. at least one `^PQ` quantity case;
6. generated QR PNG with plain text/URL;
7. generated QR PNG with UTF-8 payload;
8. thermal printer + correct stock when available;
9. physical ruler/caliper suitable for the label size;
10. at least one real barcode/QR scanner or phone camera.

Private/customer fixtures stay local/ignored and are never committed.

## Executable manual / physical checklist

Every row starts as `NOT RUN — ENVIRONMENT` until the action is actually executed.

| ID | Fixture / precondition | Action | Expected result | Current result | Evidence to record | Severity if failed |
|---|---|---|---|---|---|---|
| B01 | representative `.zpl`, `.txt`, `.prn` | Open each format in Windows UI and preview | File opens locally; representative design renders without primary-flow failure | NOT RUN — ENVIRONMENT | fixture id/hash + screenshot/note | P1 if primary workflow unusable |
| B02 | multi-design / `^PQ` fixture | Verify designs and quantity metadata; use file/one-each/custom quantity | UI preserves expected design separation and quantity behavior | NOT RUN — ENVIRONMENT | input commands + observed quantities | P1 if output count wrong |
| B03 | representative labels | Exercise thermal/A4/Letter/custom layout paths and export PDF where applicable | Selected layout/media is respected within approved baseline | NOT RUN — ENVIRONMENT | layout/media + output note | P1 for materially wrong output; P2 for noncritical UX |
| B04 | thermal printer + correct stock | Print representative label at intended dimensions | Print completes without unintended scaling/cropping | NOT RUN — ENVIRONMENT | printer model/driver/stock/settings | P1 |
| B05 | printed B04 output + ruler/caliper | Measure width/height against intended physical size | Measured size matches approved tolerance for the tested workflow | NOT RUN — ENVIRONMENT | intended vs measured dimensions + method | P1 if materially wrong physical size |
| B06 | printed barcode sample | Scan with real scanner/phone | Scanner returns expected exact payload | NOT RUN — ENVIRONMENT | scanner/phone + payload comparison | P1 |
| B07 | printed QR sample | Scan with real scanner/phone | Scanner returns expected exact payload | NOT RUN — ENVIRONMENT | scanner/phone + payload comparison | P1 |
| B08 | SG PDF generated QR PNG; plain + UTF-8 payloads | Generate PNG and scan with at least one real phone/scanner | Exact payload round-trips for tested representative cases | NOT RUN — ENVIRONMENT | payload + device/app + observed value | P1 |
| B09 | ignored private Mercado Libre representative corpus | Open/preview representative private labels and compare against expected visual/content behavior | No material regression versus approved workflow; private data remains local | NOT RUN — ENVIRONMENT | local fixture aliases/hashes only; no customer data committed | P1 if primary real corpus fails |
| B10 | repo/worktree after B09 | Verify private/customer fixtures are not tracked | `tests/PrivateFixtures/**` and private corpus remain untracked/ignored | NOT RUN — ENVIRONMENT | local `git status`/tracked-files note | P0 if customer/private data is committed |
| B11 | normal local ZPL/QR workflow; network disabled | Open/preview/generate representative local inputs | Approved normal local workflow remains usable offline | NOT RUN — ENVIRONMENT | network-disabled method + result | P1 if normal workflow requires Internet |

## Defect routing

If any item fails:

`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → RERUN FAILED QA ITEM`

Rules:

- P0/P1 blocks Pack B closure and release progression.
- Product defects go to a focused `feat/fix-*` branch from the latest applicable green head.
- Do not fix product code in the QA planning branch.
- Do not add another ZPL renderer, barcode package, cloud service or generic barcode studio unless a separately approved capability gap proves it necessary.
- A printer driver/setup problem is not automatically a product bug.
- Physical tolerance must be recorded with the tested printer/stock/settings; do not make universal hardware claims from one device.

## Separate installer gate — not Pack B completion

`ZplGSCustom.ttf` provenance/license remains a release/installer gate. Pack B physical PASS does not silently close font provenance.

## Exit gate

Pack B is `PASS` only when the required representative private/physical items are actually executed with evidence and there is no open P0/P1.

Until then:

- automated evidence preflight: `PASS`;
- private/physical Pack B: `NOT RUN — ENVIRONMENT`;
- final Pack B gate: `NOT RUN`;
- release remains open;
- F0–F12 automated baseline remains closed/frozen.

## KISS / noise audit

- new product code: NO
- new tests: NO
- new runtime dependency: NO
- second ZPL renderer: NO
- new barcode/QR package: NO
- barcode studio/framework: NO
- historical QA branch merged: NO
- existing evidence reused: YES
- `main` changed: NO

This is one compact canonical QA artifact; no document-per-device or document-per-test is required.
