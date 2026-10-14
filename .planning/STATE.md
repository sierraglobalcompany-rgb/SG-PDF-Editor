---
gsd_state_version: '1.1'
status: f12-functional-baseline-closed-release-open
progress:
  total_phases: 13
  completed_phases: 13
  functional_percent_estimate: 100
  release_readiness_percent_estimate: 90
  overall_percent_estimate: 95
---

# Project State

## Authority

Operational order:
1. live GitHub exact HEAD + exact-head CI;
2. this file;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. approved slice/release record;
6. `docs/MASTER_CONTEXT.md` for durable architecture;
7. `docs/MASTER_PLAN.md` + `docs/history/**` as historical/architectural context only.

**Core value:** resolver PDF + ZPL diario de forma rápida, privada, estable y offline.

## Current baseline — 2026-10-14

F0–F12 automated functional baseline: **CLOSED / FEATURE-FROZEN**.

Authoritative functional branch:
`feat/f12-7-pdf-to-png-mvp-green3`

Functional HEAD:
`8e1845d6d8fe72374d7a80994d8500b6d36077cc`

Functional exact-head CI:
- run `38555829715`;
- job `115824598759`;
- repository hygiene PASS;
- Labelize/qpdf/Tesseract staging PASS;
- locked restore PASS;
- Release build **0 warnings / 0 errors**;
- tests **1202/1202 PASS**, 0 failed, 0 skipped.

`main` remains read-only at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge/rebase/force-push to `main` without explicit user approval.

## Conservative integration candidate — SELECTED

Canonical candidate:
`integration/release-candidate-20261014`

Candidate HEAD:
`01d4adc3972ed939c5a2723a75d1833e61825d63`

Why this candidate:
- direct descendant of functional HEAD;
- functional HEAD → candidate: documentation/planning lineage only;
- no replay of stacked historical PRs;
- no product-code/dependency/architecture delta from the closed functional baseline;
- old `integration/release-candidate` is divergent/stale and must not be reused.

The candidate SHA is the same exact SHA verified on `plan/release-routing-audit`:
- run `38593351241`;
- job `115953668459`;
- hygiene, staging, locked restore, Release build and full test step PASS.

`integration/**` does not currently trigger push CI; the exact SHA evidence above is therefore the candidate verification anchor.

## Branch / PR ancestry audit — RESULT

The old open PRs are stacked historical slice PRs. They are evidence, **not a merge queue**.

Classification:
- prior F0–F11/F12 slice heads that are ancestors of `green3`: **CONTAINED**;
- `feat/f12-7-pdf-to-png-mvp`, `...-green`, `...-green2`: **SUPERSEDED / no unique release value**;
- divergent `feat/f12-7-pdf-to-png`: **HISTORICAL ALTERNATIVE / DO NOT BASE**;
- old `integration/release-candidate`: **STALE / DIVERGENT / DO NOT REUSE**;
- old `plan/release-integration-audit*`: **HISTORICAL EVIDENCE ONLY**;
- `chore/release-prep-qa-docs`: **SEMANTIC CHECKLIST SOURCE ONLY / NO WHOLESALE MERGE**.

No destructive branch cleanup is required for release correctness.

## Release QA Packs

Automated preflights exist and PASS; real/manual/private/physical execution remains open.

| Pack | Canonical record | Automated preflight | Real gate |
|---|---|---|---|
| A — Reader / Organize / Editing | `docs/release/2026-10-14-pack-a-reader-organize-editing.md` | PASS | NOT RUN — ENVIRONMENT |
| B — ZPL / QR / print-scan | `docs/release/2026-10-14-pack-b-zpl-qr-physical.md` | PASS | NOT RUN — ENVIRONMENT |
| C — visual + PFX/P12 signatures | `docs/release/2026-10-14-pack-c-signatures.md` | PASS | NOT RUN — ENVIRONMENT |
| D — OCR / Forms / Compare / Batch / PDF→PNG | `docs/release/2026-10-14-pack-d-ocr-forms-compare-batch-convert.md` | PASS | NOT RUN — ENVIRONMENT |

Synthetic/CI PASS never upgrades a manual/physical/private gate to PASS.

## Routing consolidation — CLOSED

Current routing documents (`AGENTS`, ROADMAP, MASTER_PLAN header and README) are aligned with the closed F0–F12 baseline and release sequence.

Routing branch:
`plan/release-routing-audit`

Latest routing SHA used for candidate cut:
`01d4adc3972ed939c5a2723a75d1833e61825d63`

No stale master/history snapshot may reopen a closed phase.

## Runtime / installer / license audit

Canonical record:
`docs/release/2026-10-14-runtime-installer-license-audit.md`

Audit branch:
`plan/release-runtime-installer-audit`

Audit commit:
`2a5d0b25368bdec56fff54deb9041a0bb804fce7`

Exact-head CI:
- run `38595250869`;
- job `115960181338`;
- hygiene PASS;
- Labelize/qpdf/Tesseract staging PASS;
- locked restore PASS;
- Release build PASS;
- full test step PASS.

### Installer infrastructure

Repository audit found no existing WiX/Inno/MSIX/publish-profile framework.

This is **not a product defect**.

KISS release order is frozen as:

`deterministic publish → runtime inventory → clean/offline smoke → minimal installer → installer smoke → RC`

Do not add installer architecture before the published folder is proven.

### Labelize font provenance

`ZplGSCustom.ttf` re-audit: **COMPLETE**.

Verified:
- SG PDF stages the exact pinned official Labelize v1.7.0 Windows release ZIP; it does not separately download the font;
- Labelize embeds the GS font internally and documents it in upstream `THIRD_PARTY_LICENSES.md`;
- upstream states it was received from MIT-licensed `ingridhq/zebrash`;
- zebrash is MIT and contains the same-named GS font asset;
- upstream also states that the original provenance before zebrash is not fully documented.

Upstream notice retained at:
`third_party/licenses/Labelize-THIRD-PARTY-NOTICES.md`

Status:
- provenance review: **COMPLETE**;
- engineering redistribution evidence: **SUPPORTIVE**;
- original-author provenance certainty: **RESIDUAL RISK OPEN**;
- legal certainty is **not claimed**.

Before a public installer, resolve explicitly:
- **A:** accept the documented residual risk and ship pinned Labelize + required notices; or
- **B:** replace/redraw the tiny GS glyph asset through a controlled path and rerun affected ZPL automated + real/private/physical QA.

Do not silently modify Labelize or add a second ZPL renderer.

## Runtime components to prove in publish

Approved runtime baseline:
- .NET 10 / WPF / Windows x64;
- PDFium `156.0.8076`;
- PDFsharp `6.2.4` narrow approved uses;
- Labelize `1.7.0`;
- qpdf `12.4.2`;
- Tesseract `5.5.3.20260724` + pinned `spa`/`eng` data;
- DejaVu Sans `2.37` fallback asset.

No backend, DB, cloud service, second viewer, second PDF engine, second ZPL renderer, generic BatchEngine or generic conversion framework.

## Next authoritative work

Functional F12 remains frozen.

### Immediate next gate — Windows publish validation

On the prepared Windows x64 development PC:
1. verify Codex environment audit result;
2. stage pinned Labelize/qpdf/Tesseract with existing scripts;
3. locked restore;
4. Release build + full tests;
5. create one deterministic **self-contained `win-x64` publish** output;
6. inventory the output and prove required runtimes/assets/notices are present;
7. launch directly from the published folder;
8. run offline smoke with network disabled;
9. prove no runtime download, PATH, registry or global-install dependency;
10. record failures by root cause — no installer work until publish is GREEN.

### After publish GREEN

1. choose one minimal Windows installer technology;
2. package the already-proven publish payload rather than creating a second build pipeline;
3. include required third-party notices;
4. install/launch/uninstall smoke;
5. execute remaining representative Packs A–D where environment/hardware allows;
6. RC clean-clone / locked restore / build / tests / packaged runtime / offline / no P0-P1;
7. only then consider `main`, with explicit user approval.

## Engineering laws

### KISS
Simple, efficient, optimized, effective. Prefer A→B.

### Anti-noise
Before ADD: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

### Anti-patch guardrail
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Planning estimates

Planning estimates, not telemetry:
- automated functional implementation: **~100%**;
- release readiness: **~90%**;
- weighted overall: **~95%**.

They remain unchanged because publish, installer, real QA and RC are not yet complete.

## Recommended next bounded task

`Windows self-contained publish gate on integration/release-candidate-20261014 after Codex environment audit is GREEN; prove runtime payload and offline launch before adding installer technology.`
