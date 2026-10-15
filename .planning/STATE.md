---
gsd_state_version: '1.1'
status: ui4-signature-root-cause-proven-local-fix-pending-remote-audit-ci-v08-open-f13-ab-approved
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: UI-4 Gate 0 stabilization before F13
---

# Project State — SG PDF Editor

**Updated:** 2026-10-15

## 1. Mandatory laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → TRACE/CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

`main` is read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`. No merge without explicit user approval.

## 2. Functional baseline F0–F12 — CLOSED

Functional anchor: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.  
Historical CI: run `38555829715`, job `115824598759`, tests `1202/1202 PASS`.

Do not reopen F0–F12 without a reproducible regression/new explicit requirement.

## 3. Release/UI branch — Gate 0 OPEN

UI branch: `feat/release-ui-modernization`  
UI-4 tested base HEAD: `abd52d05279e3e656723ad4ff5a21d36de91508e`  
Pre-manual suite: `1214/1214 PASS`.

### Signature P1 — ROOT CAUSE PROVEN LOCALLY

Authoritative checkpoint:
`docs/history/2026-10-15-ui4-signature-root-cause-proven-local-fix-pending-remote-ci.md`

Proven cause:
- the AcroForm `ScrollViewer` remained visible in the shared right-properties `Grid` after its inner panel was collapsed;
- it intercepted hit-testing above the visual-signature controls;
- before fix, `InputHitTest` returned that `ScrollViewer` at the control centers.

Reported minimal local fix:
- AcroForm `ScrollViewer` starts collapsed;
- becomes visible only when AcroForm editor is active;
- collapses again on AcroForm reset.

Reported local changed files:
- `src/SGPdf.App/MainWindow.Forms.cs`
- `src/SGPdf.App/MainWindow.Sign.cs` — preserves prior Dark Mode strip correction
- `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs` — causal test

Reported verification:
- causal test RED→GREEN;
- locked restore PASS;
- Release build PASS;
- full suite `1215/1215 PASS`;
- `git diff --check` PASS;
- PNG/photo/draw/library dialogs physically reopen in Windows;
- no push/main/PKCS changes yet.

**Important:** this fix is accepted as root-cause-correct locally, but is not yet release truth. Exact diff audit + exact-head CI on a remote isolated branch are mandatory first.

### V08 — separate PRODUCT BUG

Windows state after signature fix:
- `MediaKind = Thermal`;
- label layout plan exists;
- not busy;
- Export PDF disabled;
- Print disabled;
- validation text empty.

This contradicts the existing UI contract and must be investigated as a separate root cause. Do not mix the V08 fix into the signature commit.

### Remaining Windows evidence

- V04 1366×768: NOT RUN
- V05 scaled profile: NOT RUN
- V09 password dialog visual: NOT RUN; source functional tests already cover protected-PDF logic
- V13: reachable, not fully persisted
- V14: PARTIAL; stable overlay + save/reopen not fully verified

### Gate 0 exit

Gate 0 becomes GREEN only after:
1. signature three-file diff is committed/pushed to an isolated branch;
2. GitHub diff audit accepts it;
3. exact-head CI GREEN;
4. V08 root cause + minimal fix + cause test + CI;
5. V04/V05/V08/V09/V10–V14 completed on the accepted exact head;
6. no P0/P1 remains.

## 4. PKCS hygiene — GREEN isolated

Branch: `feat/fix-pkcs-hygiene`  
HEAD: `781cd6060121367f4d51efbe2724d820d1dfb842`  
CI run `38645380308`, job `116130807039`, tests `1214/1214 PASS`.

PKCS 8.0.2 is test-graph-only. App/runtime unchanged. Not integrated into UI/release branch.

## 5. F13 — Product Shell / Dashboard — A+B APPROVED

Planning branch: `plan/post-ui4-f13-f18-expansion-20261014`.

A — visual spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

B — technical plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Mockup reference:
`docs/design/2026-10-15-f13-approved-mockup-reference.md`

A+B are cumulative. No further design approval required.

Frozen constraints:
- 96 px rail;
- 24 px Home padding;
- left rail + center workspace + 320 px right context pane;
- local/offline; no login/ads/cloud/AI;
- Light/Dark baseline;
- no second PDF engine/writer/UI framework;
- F13 owns shell/core modes only; F14 owns full productization;
- legacy mode strip removed only after safe de-coupling/equivalence.

**Do not start F13 product code before Gate 0 GREEN.**

## 6. F14 — Productize existing capabilities

Plan:
`docs/superpowers/plans/2026-10-14-f14-productize-existing-capabilities.md`

Inventory:
`docs/product/2026-10-14-existing-tool-entrypoint-inventory.md`

Preflight finding:
- some dirty/safe-transition guards are attached to legacy UI rather than fully contained in owner entrypoints;
- new shell must invoke existing guards through one minimal explicit safe-transition path;
- do not duplicate business logic or create a command bus/state machine.

No F14 product code while Gate 0 is open.

## 7. F15 — Edit V3 — DESIGN APPROVED / SPEC READY

Spec:
`docs/superpowers/specs/2026-10-15-f15-edit-v3-design.md`

Preserve current workspaces/writer. Direct object selection + contextual right pane + honest limitations. F15 target save semantics: visible in Edit, no dirty→disabled, dirty→enabled. Image Undo/Redo reused; no fake universal history.

Implementation plan waits for F13 shell stability.

## 8. F16–F18

- **F16:** PDF→JPG, Image→PDF, Crop, authorized Unlock, safe Optimize, image watermark after capability gate.
- **F17:** Compare V2, more AcroForm types/simple designer, signature UX completion.
- **F18:** optional LibreOffice→PDF, scanner, local HTML→PDF after separate gates.

## 9. Official icon — REQUIRED / SOURCE-GATED

Do not synthesize from the mockup placeholder or text description. Required later on window, exe, Alt+Tab/taskbar, shortcut and installer.

## 10. Immediate resume order

1. Commit/push only the current three-file signature + Dark Mode + causal-test diff to the isolated branch; no V08 changes in that commit.
2. Audit that exact remote diff from GitHub.
3. Exact-head CI GREEN.
4. Investigate V08 separately by root cause; cause test + minimal fix + suite + CI.
5. Complete Windows V04/V05/V08/V09/V10–V14 on the accepted exact head.
6. Close UI-4 only with no P0/P1.
7. Consolidate release truth + isolated PKCS cleanly.
8. Execute approved F13 A+B natively.
9. Execute F14 through existing owners/guards.
10. F15 after F13 stability and implementation-plan approval.
11. Publish/installer only after intended product baseline is stable.
12. `main` only with explicit user approval.
