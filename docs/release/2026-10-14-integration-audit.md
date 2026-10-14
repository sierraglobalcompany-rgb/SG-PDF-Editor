# SG PDF Editor — Release integration ancestry audit

Date: 2026-10-14

## Purpose

Freeze the release-integration facts before creating any integration candidate. This audit is read-only with respect to `main` and does not authorize merge, rebase, force-push, branch deletion or PR cleanup.

## Authority and exact heads

- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`
- automated functional baseline: `feat/f12-7-pdf-to-png` @ `6e9240fbb8bb16947d64514ded56d548c638c1c6`
- release-prep documentation baseline: `chore/release-prep-qa-docs` @ `36aa4c7cddd738e82abf9b863cde08e8279e1307`
- this audit branch: `plan/release-integration-audit`

GitHub exact ancestry and exact-head CI remain higher authority than historical PR descriptions or old handoff documents.

## Canonical lineage finding

The current functional baseline is a strict descendant of `main`:

- compare `31c059475...` → `6e9240f...`: **1217 commits ahead / 0 behind**;
- merge-base is exactly `31c059475...`.

The F12 lineage is also strictly stacked on the F11 closure:

- F11 closure `7a890b7c8ece75869730c95c275a1ee8202efe93` → final functional `6e9240f...`: **229 ahead / 0 behind**;
- merge-base is exactly the F11 closure.

Therefore the release source of truth is the final linear functional head, not a reconstruction by replaying old PRs.

## Representative branch ancestry audit

The following representative historical/checkpoint heads were compared against the final functional head and were all found to be ancestors (`final ahead`, `0 behind`, merge-base equal to the sampled head):

| Sampled branch/head | Final ahead | Behind | Finding |
|---|---:|---:|---|
| `feat/f12-professional` @ `b1c445ef...` | 162 | 0 | contained in canonical lineage |
| `audit/f12-4-state-plan-20261014` @ `4e69b477...` | 59 | 0 | contained |
| `feat/f12-6-qr-generator-ui` @ `803dcee3...` | 9 | 0 | contained |
| old F0 temp/review aliases @ `a9a6e0e...` | 1135 | 0 | contained |
| `design/f2-labelize-architecture` @ `417ca155...` | 1090 | 0 | contained |
| `design/f6-images` @ `3d350de...` | 682 | 0 | contained |

No divergent product line or missing unique production commit was found in the representative branches inspected. This is a branch-sampling conclusion, not a destructive-cleanup authorization.

## Pull request finding

The repository still has the historical stacked PR chain from A0 through F11 open/draft. The observed sequence includes PR #2, #5–#30, ending with F11 in PR #30.

**F12 has no equivalent PR chain.** Searches for F12.1–F12.7 / Professional found no F12 PRs even though the F12 branches and exact-head CI evidence exist.

Consequences:

1. Do **not** integrate by merging every historical PR one by one.
2. Do **not** infer that F12 is absent simply because there are no F12 PRs.
3. The canonical exact-head ancestry already contains the historical stacked work plus F12.
4. Old PRs should be treated as historical review/evidence surfaces until a later cleanup decision, not as the release integration mechanism.

## Branch classification

### Release-source lineage

Use as functional source:
- `feat/f12-7-pdf-to-png` @ `6e9240f...`.

Use as current release-prep documentation source:
- `chore/release-prep-qa-docs` @ `36aa4c...`.

### Historical evidence / checkpoints

Keep read-only for now:
- F0–F11 stacked feature branches;
- F12 task/checkpoint branches;
- `design/**`, `audit/**`, `spike/**` branches;
- old `temp`, `tmp`, `review`, `do-not-use` aliases.

Their existence is repository noise, but deletion is deferred until the release is integrated and evidence retention is decided explicitly.

## Recommended KISS integration strategy

Next release block should create **one dedicated integration candidate branch** from the latest audited release-prep head, not from `main` and not by replaying PRs.

Recommended sequence:

1. create `integration/release-candidate` (or equivalent) from the latest audited head;
2. run full exact-head Windows CI on that candidate;
3. execute the prioritized real QA packs and record PASS/FAIL/NOT RUN honestly;
4. close installer/runtime/license provenance gates;
5. stop for explicit user approval before any merge into `main`;
6. only after approval, choose the final merge/squash policy as a separate release action.

## Explicit prohibitions

During this audit and the next candidate-preparation block:

- no merge to `main`;
- no rebase of the canonical lineage;
- no force-push;
- no branch deletion;
- no mass PR closing/retargeting;
- no feature expansion to solve repository-history noise.

## QA preconditions

The automated functional baseline is closed, but release readiness remains below 100% because real/manual/private/physical QA is still open. The canonical QA checklist is `docs/release/2026-10-14-real-qa-packs.md`.

No release-candidate label should imply physical/manual PASS until those packs actually have evidence.

## Audit verdict

**PASS for integration planning.** The inspected history supports a single canonical linear release source. The main risk is repository/PR clutter, not evidence of a competing product lineage. Integration can proceed conservatively on a dedicated candidate branch while `main` remains read-only.