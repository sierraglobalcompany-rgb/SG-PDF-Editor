---
gsd_state_version: '1.1'
status: release-integration-audit-closed-candidate-next
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

The automated functional baseline **F0–F12 is CLOSED**. Release ancestry/PR audit is now recorded on `plan/release-integration-audit`.

| Area | Status |
|---|---|
| F0–F12 functional baseline | CLOSED / AUTO PASS |
| Real QA packs | DEFINED / NOT RUN |
| Documentation consolidation | BASELINE PASS |
| Release ancestry / PR audit | **CLOSED / PASS FOR INTEGRATION PLANNING** |
| Dedicated integration candidate | NEXT / NOT CREATED |
| Release candidate | OPEN |

## Canonical release lineage

Functional release source:
- `feat/f12-7-pdf-to-png` @ `6e9240fbb8bb16947d64514ded56d548c638c1c6`;
- exact-head Release build: 0 warnings / 0 errors;
- tests: 1201/1201 PASS;
- no merge to `main`.

Release-prep docs baseline:
- `chore/release-prep-qa-docs` @ `36aa4c7cddd738e82abf9b863cde08e8279e1307`.

Ancestry audit evidence:
- final functional head is **1217 commits ahead / 0 behind `main`**; merge-base is exactly `main`;
- final functional head is **229 ahead / 0 behind F11 closure**; merge-base is exactly F11 closure;
- representative F12 audit/task, design and old temp/review branches inspected are ancestors of the final functional head;
- no competing product lineage or missing unique production commit was found in the representative branches inspected.

Canonical audit artifact: `docs/release/2026-10-14-integration-audit.md`.

## Pull request finding

Historical stacked draft PRs remain open from A0 through F11. The observed chain includes PR #2 and #5–#30 and terminates at F11.

F12 has no equivalent PR chain even though F12 branches and CI evidence exist. Therefore release integration must use the canonical exact-head lineage, **not** replay/merge every historical PR into `main`.

No PR closing, retargeting or branch deletion is authorized at this stage.

## Release QA

Canonical checklist: `docs/release/2026-10-14-real-qa-packs.md`.

Four packs remain **NOT RUN**:
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

The ancestry audit reduces uncertainty but does not itself count as real QA or release integration, so percentages are unchanged.

## KISS / anti-noise audit

Release preparation follows:
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

The repository has substantial historical branch/PR clutter, but cleanup is deferred until after integration because history is still useful recovery evidence. Do not solve repository noise by feature changes, rebases or destructive cleanup.

## Next

```text
Ancestry/PR audit CLOSED
→ create one dedicated integration candidate from the latest audited release-prep head
→ run full exact-head Windows CI
→ execute prioritized real QA packs
→ installer/license/offline gates
→ stop for explicit approval before any merge to main
```

No merge to `main` without explicit user approval.