---
gsd_state_version: '1.1'
status: integration-candidate-auto-green-pack-a-preflight-pass
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

The automated functional baseline **F0–F12 is CLOSED**. Release ancestry/PR audit is closed, the dedicated integration candidate is automated-green, and Pack A has completed an automated readiness preflight only.

| Area | Status |
|---|---|
| F0–F12 functional baseline | CLOSED / AUTO PASS |
| Documentation consolidation | BASELINE PASS |
| Release ancestry / PR audit | CLOSED / PASS FOR INTEGRATION PLANNING |
| Dedicated integration candidate | **AUTO GREEN** |
| Pack A automated readiness preflight | **PASS** |
| Pack A real/manual QA | **NOT RUN — ENVIRONMENT** |
| Packs B–D real QA | **NOT RUN** |
| Release candidate approval | OPEN |

## Integration candidate

Canonical branch:
- `integration/release-candidate`;
- cut directly from audited head `74c27e14567aaaf635e4059971e2fb223704ae02`;
- no PR replay, merge, rebase or force-push was used;
- `main` remains read-only.

During candidate validation one release-infrastructure gap was found: the build workflow listened to `main`, `feat/**` and `plan/**` but not `integration/**`. Root cause was the branch filter itself. The minimal fix added `integration/**`; no workaround, second workflow or product architecture was introduced.

Candidate exact-head automated evidence before the Pack A preflight note:
- SHA `2a0269635cfaa2ca87983f0c579d689ae8fa05ad`;
- workflow run `38516169305`, Windows job `115695345151`;
- exact checkout SHA verified;
- repository hygiene PASS;
- pinned Labelize 1.7.0, qpdf 12.4.2 and Tesseract 5.5.3 staging PASS;
- locked restore PASS;
- Release build: 0 warnings / 0 errors;
- tests: 1201/1201 PASS, 0 failed, 0 skipped.

The current documentation HEAD must also pass exact-head CI before this checkpoint is final.

## Canonical release lineage

Functional release source:
- `feat/f12-7-pdf-to-png` @ `6e9240fbb8bb16947d64514ded56d548c638c1c6`;
- F12.7 closure build: 0 warnings / 0 errors;
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

Pack A preflight evidence: `docs/release/2026-10-14-pack-a-preflight.md`.

The automated Pack A preflight mapped existing coverage for reader layout, organize operations, image-edit preservation and conservative text reflow. It found no automated regression and is **PASS for readiness only**.

The canonical Pack A itself remains **NOT RUN — ENVIRONMENT** because this execution environment cannot prove:
- interactive Windows UI behavior;
- Microsoft Print to PDF;
- representative real-document UI flows;
- Adobe/Foxit/Edge external-reader checks;
- a network-disabled desktop session.

Automated PASS never upgrades those manual/private/physical items to PASS.

Current real-QA status:
- Pack A — **NOT RUN — ENVIRONMENT**;
- Pack B — **NOT RUN**;
- Pack C — **NOT RUN**;
- Pack D — **NOT RUN**.

## Engineering estimates

Planning estimates, not telemetry:
- automated functional implementation: **~100%**;
- release readiness: **~90%**;
- headline weighted state: **~95%**.

The percentages remain unchanged because integration CI and automated preflight reduce uncertainty but do not substitute for real/manual/private/physical QA.

## KISS / anti-noise audit

Release preparation follows:
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

The repository has historical branch/PR clutter, but cleanup remains deferred until after integration because history is still useful recovery evidence. Do not solve history noise with feature changes, rebases or destructive cleanup.

## Next

```text
integration candidate automated-green
→ real Pack A on interactive Windows when available
→ Packs B/C/D according to hardware/private-fixture availability
→ installer/license/offline gates
→ stop for explicit approval before any merge to main
```

No merge to `main` without explicit user approval.