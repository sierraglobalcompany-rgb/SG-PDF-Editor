---
gsd_state_version: '1.1'
status: functional-baseline-closed-release-preparation
progress:
  total_phases: 13
  completed_phases: 13
  functional_percent_estimate: 100
  release_readiness_percent_estimate: 90
  overall_percent_estimate: 95
---

# Project State

## Source of truth

Authority on conflict:
1. live GitHub + exact-head CI;
2. this file;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. approved slice spec/plan;
6. master architecture docs;
7. historical handoffs.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.

## Current position — 2026-10-14

The automated functional baseline **F0–F12 is CLOSED**. Work has moved to release preparation.

Current release-prep branch: `chore/release-prep-qa-docs`, cut from F12.7 closure HEAD `6e9240fbb8bb16947d64514ded56d548c638c1c6`.

| Area | Status |
|---|---|
| F0–F11 | CLOSED / AUTO PASS; manual/private/physical gates remain where documented |
| F12.1–F12.6 | CLOSED / AUTO PASS |
| F12.7 PDF→PNG MVP | CLOSED / AUTO PASS |
| Real QA packs | **DEFINED / NOT RUN** |
| Documentation consolidation | **IN PROGRESS — AGENTS operational drift corrected** |
| Release integration/stabilization | OPEN |
| Release candidate | OPEN |

## Functional baseline evidence

F12.7 functional implementation baseline before release-prep docs:
- branch `feat/f12-7-pdf-to-png`;
- final closure HEAD `6e9240fbb8bb16947d64514ded56d548c638c1c6`;
- functional GREEN head `521f0974b4b8a6b5ae7138a51cf11e61d572a998`;
- Release build: 0 warnings / 0 errors;
- full tests: 1201/1201 PASS;
- no merge to `main`.

Baseline remains local/offline and narrow: PDFium primary PDF owner; PDFsharp/qpdf/Tesseract only in approved scopes; Labelize 1.7.0 for ZPL/QR; no generic conversion/batch/plugin architecture introduced.

## Release QA

Canonical checklist: `docs/release/2026-10-14-real-qa-packs.md`.

Four bounded packs are frozen:
- A — Reader / Organize / Editing;
- B — ZPL / QR / physical print-scan;
- C — visual + cryptographic signatures;
- D — OCR / Forms / Compare / Batch / PDF→PNG.

All four remain **NOT RUN** until real evidence exists. Defining the checklist does not increase release readiness by itself.

## Documentation consolidation

`AGENTS.md` was stale at F3.3/F3.4 despite F12 closure. It is now consolidated around current authority, KISS, anti-noise, owners and release-prep cadence. Fine-grained historical phase contracts remain in `.planning/phases/`, `docs/superpowers/` and `docs/history/` instead of being duplicated in an evergreen agent file.

Do not create additional handoffs/docs unless they change source-of-truth, record a material release decision/evidence, or are genuinely needed for recovery.

## Engineering estimates

Planning estimates, not telemetry:
- automated functional implementation: **~100%**;
- release readiness: **~90%**;
- headline weighted state: **~95%**.

No increase is claimed from checklist/document cleanup because real QA and integration are still open.

## KISS / anti-noise audit

Release preparation follows:
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

No feature expansion is allowed opportunistically. New conversions, symbologies, batch operations, form types, signing modes or PDF capabilities require a separate value/capability gate.

## Integration drift

**OPEN / HIGH PRIORITY.** `main` remains intentionally untouched. No merge/rebase/force-push/destructive branch cleanup is authorized.

## Next

```text
Release-prep docs + QA packs frozen
→ audit branch/PR ancestry and unique commits
→ choose conservative integration strategy
→ create dedicated integration branch only after audit
→ exact-head CI + prioritized real QA
→ installer/license/offline smoke
→ release candidate
```

No merge to `main` without explicit user approval.
