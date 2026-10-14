# Pack A — Reader / Organize / Editing

Date: 2026-10-14

Purpose: canonical executable QA record for Pack A. This document maps existing automated evidence and defines the remaining real/manual Windows checks. It does **not** convert automated evidence into manual PASS.

## Authority / base

- repository: `sierraglobalcompany-rgb/SG-PDF-Editor`
- branch: `plan/release-pack-a-qa`
- base: `045273678f15f1a1a2c7e2d18197f264d6cc9ab0`
- functional implementation head: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7` — READ ONLY
- baseline exact-head CI before this QA record: run `38556696463`, job `115827512297`, SUCCESS
- F0–F12: CLOSED / AUTO PASS / FEATURE FROZEN

## Status semantics

- `AUTO PREFLIGHT PASS`: existing automated implementation/evidence is present and the current exact-head CI is green.
- `PASS`: the real/manual item was actually executed and evidence was recorded.
- `FAIL`: the real/manual item was executed and failed.
- `PARTIAL`: only a documented subset was actually executed.
- `NOT RUN — ENVIRONMENT`: the current execution environment cannot perform the required GUI/external-reader/offline/manual action.

Automated PASS never implies manual, physical, external-reader or offline PASS.

## Pack A automated evidence map

No new product tests are added for this pack because the approved behaviors already have automated evidence. Add a test only if a real uncovered behavior, defect or regression risk is demonstrated.

| Area | Existing automated authority | Preflight |
|---|---|---|
| PDF open / navigation / zoom / fit / search / selection | `.planning/REQUIREMENTS.md`: F0 PDF Base + F4 `READER-01..07` | AUTO PREFLIGHT PASS |
| Windows print path including Microsoft Print to PDF | `.planning/REQUIREMENTS.md`: `PDF-BASE-06` | AUTO PREFLIGHT PASS; real print NOT RUN |
| Organize: reorder / rotate / delete / duplicate / insert / merge / extract / split | `.planning/REQUIREMENTS.md`: F5 `ORG-01..04`; `docs/history/2026-10-09-F5-preservation-matrix.md` | AUTO PREFLIGHT PASS |
| Image editing / save / preservation | `.planning/REQUIREMENTS.md`: F6 `IMG-01..04`; `docs/history/2026-10-09-F6-preservation-matrix.md` | AUTO PREFLIGHT PASS |
| Text editing / save / reopen | `.planning/REQUIREMENTS.md`: F7 `TEXT-01..04`; `docs/history/2026-10-10-F7-preservation-matrix.md` | AUTO PREFLIGHT PASS |
| Comments / annotation save path | `.planning/REQUIREMENTS.md`: F8 COMMENTS; `docs/history/2026-10-10-F8-preservation-matrix.md` | AUTO PREFLIGHT PASS |
| Text V2 conservative reflow | `.planning/REQUIREMENTS.md`: F11 TEXT-V2; `docs/history/2026-10-12-F11-preservation-matrix.md` | AUTO PREFLIGHT PASS |
| Source preservation / Save As semantics | `.planning/REQUIREMENTS.md`: cross-cutting `ORIGINAL` plus F5/F6/F7/F8/F11 preservation evidence | AUTO PREFLIGHT PASS |
| Offline architecture guards | `.planning/REQUIREMENTS.md`: `PDF-BASE-07`, cross-cutting `OFFLINE` | AUTO PREFLIGHT PASS; physical offline smoke NOT RUN |

### Automated preflight verdict

`PASS`

Reason: Pack A behaviors are already represented in the closed automated baseline and the audited base exact-head CI is green. This Pack A slice is QA/evidence work, not feature development.

## Representative corpus

Use non-customer fixtures whenever possible:

1. text-heavy PDF;
2. image-heavy PDF;
3. mixed portrait + landscape PDF;
4. bookmarks/links PDF;
5. authorized protected-open PDF.

Private/customer material, if needed, stays local/ignored and is never committed.

## Executable manual checklist

Every row starts as `NOT RUN — ENVIRONMENT` until the action is actually executed on the stated environment.

| ID | Fixture / precondition | Action | Expected result | Current result | Evidence to record | Severity if failed |
|---|---|---|---|---|---|---|
| A01 | text-heavy PDF; Windows UI | Open PDF; next/previous/go-to; continuous navigation | Pages render and navigation stays usable | NOT RUN — ENVIRONMENT | Windows build, fixture id/hash, screenshot/note | P1 if primary reader unusable; otherwise P2 |
| A02 | text-heavy PDF; Windows UI | Zoom, Fit Page, Fit Width | Correct visible scaling without primary-flow failure | NOT RUN — ENVIRONMENT | screenshot/note | P1 if unusable; otherwise P2 |
| A03 | searchable text PDF | Search and select representative text | Search locates expected text; selection works | NOT RUN — ENVIRONMENT | query + observed result | P1 if core search/selection broken |
| A04 | representative PDF | Print to Microsoft Print to PDF; reopen output | Print completes; output reopens and is readable | NOT RUN — ENVIRONMENT | output path/hash + reopen note | P1 if primary print path fails |
| A05 | multipage PDF | Reorder pages; Save As; reopen | New order persists; source unchanged | NOT RUN — ENVIRONMENT | before/after page order + source hash | P0 if source corrupted; P1 if output wrong |
| A06 | multipage PDF | Rotate page(s); Save As; reopen | Rotation persists; source unchanged | NOT RUN — ENVIRONMENT | page ids + output note | P0/P1 according to corruption/usability |
| A07 | multipage PDF | Delete + duplicate; Save As; reopen | Intended page count/order persists; source unchanged | NOT RUN — ENVIRONMENT | before/after page count/order | P0 if destructive source change; P1 otherwise |
| A08 | two representative PDFs | Insert/merge; Save As; reopen | Expected pages are present in deterministic order | NOT RUN — ENVIRONMENT | input/output page map | P1 |
| A09 | multipage PDF | Extract/split; reopen generated output(s) | Expected pages exported; originals unchanged | NOT RUN — ENVIRONMENT | output list + source hashes | P0 if source modified; P1 otherwise |
| A10 | image-bearing PDF | Edit representative image; Save As; reopen | Intended image edit persists without unrelated destructive change | NOT RUN — ENVIRONMENT | screenshot + fixture/output ids | P1; P0 if corruption/data loss |
| A11 | supported text PDF | Edit representative text; Save As; reopen | Intended text edit persists within approved conservative scope | NOT RUN — ENVIRONMENT | before/after text + output id | P1; approved unsupported case is not a defect |
| A12 | annotation-capable PDF | Create/edit/delete representative comment; Save As; reopen | Annotation lifecycle persists correctly | NOT RUN — ENVIRONMENT | screenshots/note | P1 if feature unusable; otherwise P2 |
| A13 | saved outputs from A05–A12 | Open at least one output in Adobe/Foxit/Edge when available | External reader opens representative output without material corruption | NOT RUN — ENVIRONMENT | reader name/version + result | P1 for material compatibility failure |
| A14 | all Save As scenarios | Compare/hash original inputs before/after | Original source files remain unchanged | NOT RUN — ENVIRONMENT | source hashes/size/timestamp note | P0 if source changed unexpectedly |
| A15 | normal local workflow; network disabled | Repeat representative open/read/edit/save/reopen flow | Normal approved local workflow remains usable offline | NOT RUN — ENVIRONMENT | network-disabled method + result | P1 if normal local workflow requires network |

## Defect routing

If any item fails:

`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → RERUN FAILED QA ITEM`

Rules:

- P0/P1 blocks Pack A closure and release progression.
- Product defects go to a focused `feat/fix-*` branch from the latest applicable green head.
- Do not fix product code in a mega QA branch.
- Do not add abstraction, engine, service or package unless the root cause proves it is required.
- A manual test problem is not automatically a product bug.

## Exit gate

Pack A is `PASS` only when all required representative manual items are actually executed with evidence and there is no open P0/P1.

Until then:

- automated evidence preflight: `PASS`;
- manual Pack A: `NOT RUN — ENVIRONMENT`;
- final Pack A gate: `NOT RUN`;
- release remains open;
- F0–F12 automated baseline remains closed/frozen.

## KISS / noise audit

- new product code: NO
- new tests: NO
- new runtime dependency: NO
- second PDF engine: NO
- new framework/service/manager: NO
- historical QA branch merged: NO
- historical checklist content reused: YES, only the needed Pack A semantics
- `main` changed: NO

This record is intentionally one compact QA artifact rather than a new document per test or subsystem.
