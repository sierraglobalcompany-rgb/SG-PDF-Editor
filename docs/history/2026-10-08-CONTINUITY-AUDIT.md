# SG PDF Editor — Continuity Audit through F3.4 Spec Gate

**Date:** 2026-10-08  
**Scope:** A0 → A1 → F0 → F1 → F2 → F3.1–F3.4 spec gate.  
**Purpose:** reconcile the portable handoff with live GitHub before continuing F3.4.

## Executive result

**Product/code lineage is coherent and may continue.** No missing implemented product slice or broken stacked-base transition was found from A0 through F3.3.

The audit found documentation/tracing debt, not a product-code blocker. This commit reconciles the high-priority GSD operational docs without changing product code, dependencies, the F3.4 design spec or `main`.

## Git / branch findings

- `main` remains at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` and does not contain the stacked feature chain, by design.
- Productive chain remains stacked and draft/unmerged:
  A0 → A1 → F0.1…F0.6 → F2.1…F2.6 → F3.1 → F3.2 → F3.3.
- F1 is a decision spike and F2 architecture branch bridges F0/F1 into F2 product slices.
- PR #5 / `feat/pdf-reader` is historical/superseded and is not the productive line.
- Temporary/historical branches remain in GitHub (`feat/f0-1-...-temp/review/do-not-use`, `tmp/f2-4-lock-probe`, `tmp-ignore`, etc.). They are cleanup debt only and were not deleted during this audit.

## Exact-head CI audit

Fresh GitHub inspection confirmed successful CI on final heads across the executed chain, including:

- A0 `4333674327dfc3e4c47e586c0648303475fc2713` — success.
- A1 `32a3f04ced3ff5695653b221af621c0e152577b8` — success.
- F0.1 `a4edc2e6bc101654a4d99a06c2df5aeb2c45739e` — success.
- F0.2 `119a7af1482e5e2e92b3d97fc709b596fca58f88` — success.
- F0.3 `cb3841163b8f79ef5fb8ad58f0fd920ecbf84e30` — success.
- F0.4 `1b948dd2452acdf830cf054adb085889cdb51a9d` — success.
- F0.5 `1b2355d7611db9101d74d7e42babcf9d11c9ca94` — success.
- F0.6 `6ddf9891705bda905b49d9a1a0bd9fd765ec916c` — success.
- F1 synthetic Gate `2df2f374ae6124729389640425dc8334d0647f9f` / run `37577735748` — success.
- F2.1 `d6d69db0c8998eca569d5e4434e6c5c18234fabc` — success.
- F2.2 `9af600ce19812adb67a11718a47734c346b51bfc` — success.
- F2.3 `1f33f48390681c6e4000c17329f1763eb1551047` — success.
- F2.4 `1355ddf1ea92c7b3b25addf05f7722dec6fe57b9` — success.
- F2.5 `c003d6512a5df5be2f53aab2262d09c6dcbf92cf` — success.
- F2.6 `6c7d60a5bad22db20860685e955aa5ef03fbfa19` — success.
- F3.1 `d559169280f9d9ee2c19f1c245f6657d1b598c6d` — success.
- F3.2 `2aa1f58a6e01397e84d8f8cfcf7eb6e0e168cf62` — success.
- F3.3 `8b5bfd35b59fbc75826f8d7616aaaca3e2f31233` — PR CI `37802865627` success; 246 tests in closure evidence.
- F3.4 spec commit `ea1a8f6e6395d60729d8c8a1196b8ae94b8a2103` — push run `37824381432` success through hygiene, pinned Labelize staging, locked restore, build and tests.

Push and PR workflow IDs may differ for the same exact SHA; that is expected and not a contradiction.

## Architecture/dependency audit

Current runtime project references remain:

- `bblanchon.PDFium.Win32` 156.0.8076;
- `PDFsharp` 6.2.4;
- pinned local Labelize 1.7.0 sidecar when staged.

ZXing.Net remains test/QA-only. GSD Core/Graphify remain dev-only. No F3.4 dependency was added.

Third-party manifest still records:

- Labelize as runtime-current F2;
- BinaryKits as historical Gate evidence, not runtime;
- PDFsharp as label PDF composition/export only;
- residual Labelize `ZplGSCustom.ttf` provenance review before a public installer.

## Documentation gaps found and reconciled

1. `.planning/STATE.md` still described F3.3 exact-head closure as pending.
2. `.planning/ROADMAP.md` had the same stale F3.3/F3.4 wording.
3. `.planning/PROJECT.md` still described BinaryKits as preferred candidate and Labelize as fallback.
4. `.planning/REQUIREMENTS.md` traced only F3.1 core and omitted explicit F3.2/F3.3/F3.4 requirements.
5. `.planning/phases/` had no Phase 3 directory even though F2.1–F2.6 exist. A continuity index is added at `.planning/phases/03-f2-zpl-workspace/PLAN.md`; historical files are not moved.
6. No dedicated F1 synthetic Gate history was present in `docs/history/`; `2026-10-07-F1-GATE-ZPL-A.md` now records the decision without falsely claiming private-corpus closure.

Large master documents (`docs/MASTER_CONTEXT.md`, `docs/MASTER_PLAN.md`) contain some historical status wording from F2.1, but their architecture is still consistent with the current decisions and both explicitly defer executed state to GitHub/STATE. They are therefore not rewritten wholesale during this KISS reconciliation; the corrected operational state above supersedes their dated status passages.

`AGENTS.md` likewise contains a dated F3.3 current-position summary but its mandatory architecture/process rules remain correct. Current execution position is taken from GitHub + `.planning/STATE.md`.

## Manual/private gates intentionally still open

- F0 physical Windows UI/print/offline smoke.
- F1 private Mercado Libre corpus.
- F2 private real-label corpus.
- F2 thermal printer/ruler/scanner QA.
- F3.1 real transparent-signature UX/save/open.
- F3.2 real photo/scan quality.
- F3.3 real mouse/touch/stylus quality.

These are not forgotten work and are not blockers for continuing automated slices; they are blockers for claiming their corresponding physical/private acceptance or public-release readiness.

## F3.4 position after audit

- Design intent was approved conversationally before the handoff.
- Written F3.4 spec exists and has passed a self-audit.
- The user still must review/approve the **written spec** before the TDD implementation plan is written.
- No product code has been started.
- No F3.4 PR has been opened.
- No merge has occurred.

## Next permitted step

```text
written F3.4 spec approval
→ write + self-audit TDD plan
→ plan approval
→ RED → GREEN
→ closure + exact-head CI
```
