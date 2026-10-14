---
gsd_state_version: '1.1'
status: integration-candidate-created-ci-verification
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

The automated functional baseline **F0–F12 is CLOSED**. Release ancestry/PR audit is closed and one dedicated integration candidate now exists.

| Area | Status |
|---|---|
| F0–F12 functional baseline | CLOSED / AUTO PASS |
| Real QA packs | DEFINED / NOT RUN |
| Documentation consolidation | BASELINE PASS |
| Release ancestry / PR audit | CLOSED / PASS FOR INTEGRATION PLANNING |
| Dedicated integration candidate | **CREATED / CI VERIFICATION** |
| Release candidate approval | OPEN |

## Integration candidate

Canonical branch:
- `integration/release-candidate`;
- cut directly from audited head `74c27e14567aaaf635e4059971e2fb223704ae02`;
- no PR replay, merge, rebase or force-push was used;
- `main` remains read-only.

The branch carries the complete audited functional lineage plus release-prep documentation. Live exact-head CI is required before the candidate is considered automated-green.

## Canonical release lineage

Functional release source:
- `feat/f12-7-pdf-to-png` @ `6e9240fbb8bb16947d64514ded56d548c638c1c6`;
- exact-head Release build at F12.7 closure: 0 warnings / 0 errors;
- tests: 1201/1201 PASS.

Ancestry audit evidence:
- final functional head is **1217 commits ahead / 0 behind `main`**; merge-base is exactly `main`;
- final functional head is **229 ahead / 0 behind F11 closure**; merge-base is exactly F11 closure;
- representative F12 audit/task, design and old temp/review branches inspected are ancestors of the final functional head;
- no competing product lineage or missing unique production commit was found in the representative branches inspected.

Canonical audit artifact: `docs/release/2026-10-14-integration-audit.md`.

## Pull request finding

Historical stacked draft PRs remain open from A0 through F11. The observed chain includes PR #2 and #5–#30 and terminates at F11.

F12 has no equivalent PR chain even though F12 branches and CI evidence exist. Release integration therefore uses the canonical exact-head lineage, **not** replay/merge of historical PRs.

No PR closing, retargeting or branch deletion is authorized at this stage.

## Release QA

Canonical checklist: `docs/release/2026-10-14-real-qa-packs.md`.

Four packs remain **NOT RUN** until real evidence exists:
- A — Reader / Organize / Editing;
- B — ZPL / QR / physical print-scan;
- C — visual + cryptographic signatures;
- D — OCR / Forms / Compare / Batch / PDF→PNG.

Automated PASS does not upgrade manual/private/physical gates.

## Engineering estimates

Planning estimates, not telemetry:
- automated functional implementation: **~100%**;
- release readiness: **~90%**;
- headline weighted state: **~95%**.

Creating the candidate does not raise the percentages by itself.

## KISS / anti-noise audit

Release preparation follows:
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

The repository has historical branch/PR clutter, but cleanup remains deferred until after integration because history is still useful recovery evidence. Do not solve history noise with feature changes, rebases or destructive cleanup.

## Next

```text
integration/release-candidate created
→ exact-head Windows CI
→ prioritized real-QA execution/preflight with honest PASS/FAIL/NOT RUN
→ installer/license/offline gates
→ stop for explicit approval before any merge to main
```

No merge to `main` without explicit user approval.