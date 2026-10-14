# Pack D — OCR / Forms / Compare / Batch / PDF→PNG

Date: 2026-10-14

Purpose: canonical executable QA record for Pack D. This document maps the already-closed automated evidence and defines the remaining representative real/manual/private Windows checks. It does **not** convert automated evidence into manual PASS.

## Authority / base

- repository: `sierraglobalcompany-rgb/SG-PDF-Editor`
- branch: `plan/release-pack-d-qa`
- base: `22f27d325bf364d407c2c6db72572716b80f7ce8`
- functional implementation head: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7` — READ ONLY
- F0–F12: CLOSED / AUTO PASS / FEATURE FROZEN

## Status semantics

- `AUTO PREFLIGHT PASS`: approved automated evidence exists and the current exact-head CI is green.
- `PASS`: the representative real/manual item was actually executed and evidence was recorded.
- `FAIL`: the real/manual item was executed and failed.
- `PARTIAL`: only a documented subset was actually executed.
- `NOT RUN — ENVIRONMENT`: the current execution environment cannot perform the required Windows/private-document/external-reader/manual action.

Automated PASS never implies manual/private/external-reader/visual PASS.

## Pack D automated evidence map

No new product tests are added for this pack because the approved behaviors already have automated evidence. Add a test only if a real uncovered behavior, defect or regression risk is demonstrated.

| Area | Existing automated authority | Preflight |
|---|---|---|
| OCR local/offline searchable PDF | `.planning/REQUIREMENTS.md`: F10 `OCR` + preservation matrix | AUTO PREFLIGHT PASS; real scans/Windows/external-reader NOT RUN |
| Existing classic AcroForm `/Tx` | `.planning/REQUIREMENTS.md`: F12.3 `PRO-FORMS-TX` | AUTO PREFLIGHT PASS; real forms/external-reader NOT RUN |
| Compare core + minimal UI | `.planning/REQUIREMENTS.md`: F12.4 `PRO-COMPARE-*` | AUTO PREFLIGHT PASS; real/private pairs NOT RUN |
| Batch TextWatermark | `.planning/REQUIREMENTS.md`: F12.5 `PRO-BATCH-*` | AUTO PREFLIGHT PASS; real multi-file Windows QA NOT RUN |
| PDF→PNG | `.planning/REQUIREMENTS.md`: F12.7 `PRO-CONVERT-*` | AUTO PREFLIGHT PASS; visual Windows export review NOT RUN |
| Source preservation / cancellation / errors | F10/F12 preservation + cross-cutting `ORIGINAL` | AUTO PREFLIGHT PASS |
| Offline operation | F10/F12 local paths + cross-cutting `OFFLINE` | AUTO PREFLIGHT PASS; physical offline smoke NOT RUN |

### Automated preflight verdict

`PASS`

Reason: all Pack D product behaviors are already represented in the closed F10/F12 automated baseline. This slice is QA/evidence preparation, not feature development.

## Representative corpus

Use non-customer fixtures whenever possible. Private/customer material stays local/ignored and is never committed.

1. low-quality scan;
2. medium-quality scan;
3. Spanish, English and bilingual scan samples;
4. classic AcroForm `/Tx` form, including at least one representative hierarchical/inherited field when available;
5. Compare pairs: identical, text-only change, visual-only change with equivalent text, page-count change;
6. multi-file batch corpus with at least one normal file and, when safe, one deliberately invalid/unprocessable item;
7. portrait + landscape multi-page PDF for PDF→PNG.

## Executable manual checklist

Every row starts as `NOT RUN — ENVIRONMENT` until the action is actually executed in the stated environment.

| ID | Fixture / precondition | Action | Expected result | Current result | Evidence to record | Severity if failed |
|---|---|---|---|---|---|---|
| D01 | real low/medium-quality Spanish scan; Windows UI | Run OCR; Save As; reopen; search representative recognized words | Searchable output is produced within approved OCR scope; source unchanged | NOT RUN — ENVIRONMENT | build, fixture id/hash, search terms/results | P1; P0 if source/data corruption |
| D02 | English + bilingual representative scans | Run English and bilingual OCR paths; reopen/search | Language selection behaves as approved and output remains readable/searchable | NOT RUN — ENVIRONMENT | language mode + search notes | P1 |
| D03 | OCR output from D01/D02 | Open in Adobe/Foxit/Edge when available and search recognized text | Representative external reader can open/search output without material corruption | NOT RUN — ENVIRONMENT | reader/version + result | P1 for material compatibility failure |
| D04 | OCR operation in progress or controlled failure fixture | Cancel or trigger controlled failure; inspect source/destination | Cancellation/error is controlled; original remains intact; no invalid published output is silently accepted | NOT RUN — ENVIRONMENT | source hash + output state | P0 if source modified/data loss; otherwise P1 |
| D05 | representative classic flat `/Tx` form | Inspect field, edit value, Save As, reopen | Edited value/appearance persists within approved `/Tx` scope; source unchanged | NOT RUN — ENVIRONMENT | field name/value + before/after ids | P1; P0 if source corruption |
| D06 | hierarchical/inherited `/Tx` form when available | Edit representative child/inherited field; Save As; reopen | Supported hierarchical/inherited `/Tx` value persists correctly | NOT RUN — ENVIRONMENT | field path + observed value | P1 |
| D07 | signed or password-protected form path covered by baseline guard | Attempt unsupported/guarded save path | App fails closed/controlled; source and existing destination remain safe | NOT RUN — ENVIRONMENT | guard message + file hashes | P0 if unsafe write; otherwise P1 |
| D08 | Compare pairs: identical/text/visual/page-count | Compare each pair from PDF A against B | Signals match approved semantics: identical, text-only, visual-only independent of text, structural page-count | NOT RUN — ENVIRONMENT | pair ids + reported signals | P1 |
| D09 | A open; invalid/unreadable/protected B | Attempt Compare and cancel/reset representative cases | Failure/cancel/reset is controlled and A remains intact/open | NOT RUN — ENVIRONMENT | A id/state + result | P1; P0 if A/source damaged |
| D10 | 3+ valid PDFs; explicit output folder | Run Batch TextWatermark | All valid items are attempted; deterministic output copies created; originals unchanged | NOT RUN — ENVIRONMENT | input/output list + hashes | P1; P0 if source modified |
| D11 | batch corpus including one bad item and/or existing destination | Run batch | One bad item does not abort remaining items; per-item result + final summary are correct; existing destination not silently overwritten | NOT RUN — ENVIRONMENT | per-item summary + destination evidence | P1; P0 if overwrite/data loss |
| D12 | portrait+landscape multipage PDF | Export opened PDF to PNG | Exactly one PNG per page; deterministic names; portrait/landscape orientation and dimensions visually sensible; source untouched | NOT RUN — ENVIRONMENT | page/output map + screenshots/dimensions | P1; P0 if source modified |
| D13 | PDF→PNG target folder with deliberate filename collision | Attempt export | Collision is rejected before write; no existing file silently overwritten and no partial invalid export remains | NOT RUN — ENVIRONMENT | folder listing before/after | P0 for overwrite; otherwise P1 |
| D14 | representative Pack D local workflow; network disabled | Repeat one OCR/form/compare/batch/export representative path that does not inherently require external reader | Approved local workflow remains usable without Internet | NOT RUN — ENVIRONMENT | network-disabled method + result | P1 if normal local path requires network |
| D15 | all modifying scenarios D01–D07, D10–D13 | Compare/hash original inputs before/after | Original source files remain unchanged unless an explicitly approved copy/output path says otherwise | NOT RUN — ENVIRONMENT | hashes/size/timestamps | P0 if source changed unexpectedly |

## Scope limits that are not defects

- OCR: no universal recognition/fidelity claim, especially for poor scans or complex layouts.
- Forms: only existing classic `/Tx`; no `/Btn`, `/Ch`, XFA, JavaScript/calculations/actions, signature-form fields, flattening, form designer or universal structure preservation claim.
- Compare: no side-by-side viewer, overlay/heatmap, semantic diff, OCR compare, page matching/reordering or universal visual fidelity claim.
- Batch: `TextWatermark` only; no generic BatchEngine and no implicit Bates/OCR/protection/organize batch operations.
- Conversion: PDF→PNG only; no JPEG/WebP/TIFF, configurable DPI studio, image→PDF, Office↔PDF or generic converter framework.

## Defect routing

If any item fails:

`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → RERUN FAILED QA ITEM`

Rules:

- P0/P1 blocks Pack D closure and release progression.
- Product defects go to a focused `feat/fix-*` branch from the latest applicable green head.
- Do not fix product code inside the release-QA planning branch.
- Do not add abstraction, engine, service or package unless root cause proves it is required.
- A difficult private document or unsupported case is not automatically a product defect; classify against approved scope first.

## Exit gate

Pack D is `PASS` only when required representative manual/private items are actually executed with evidence and there is no open P0/P1.

Until then:

- automated evidence preflight: `PASS`;
- manual/private Pack D: `NOT RUN — ENVIRONMENT`;
- final Pack D gate: `NOT RUN`;
- release remains open;
- F0–F12 automated baseline remains closed/frozen.

## KISS / noise audit

- new product code: NO
- new tests: NO
- new runtime dependency: NO
- second PDF engine: NO
- generic BatchEngine/conversion framework: NO
- historical QA branch merged: NO
- historical checklist content reused: YES, only Pack D semantics
- `main` changed: NO

This record is intentionally one compact QA artifact rather than a new document per subsystem/test.