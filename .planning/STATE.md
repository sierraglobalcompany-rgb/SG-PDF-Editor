---
gsd_state_version: '1.1'
status: f13-task1-red-checkpoint
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: F13 Product Shell
  current_task: Task 1 shell visual primitives — RED proven
---

# Project State — SG PDF Editor

**Updated:** 2026-10-15

## 1. Mandatory laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → TRACE/CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → CI → REMOVE REDUNDANT WORKAROUND`.

`main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`. No merge without explicit user approval.

## 2. F0–F12 — CLOSED

Functional anchor: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.
Do not reopen without reproducible regression/new explicit requirement.

## 3. UI-4 / Gate 0 — GREEN / CLOSED

Accepted UI head: `b4f19fcd5f2854c9025e60385e3b7819bd798a29`.
CI run `38681417442`: SUCCESS, suite `1216/1216 PASS`.
Windows final evidence: V04/V05/V08/V10–V14 and representative Edit/Save PASS; no P0/P1/P2 reproduced.
V09 manual credential entry was not run, but password-path behavior is covered automatically on the accepted head.

Durable closure:
`docs/history/2026-10-15-ui4-green-gate-closed.md`

## 4. Release-truth consolidation — GREEN

Branch: `integration/ui4-pkcs-release-truth-20261015`
Head: `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`
Draft PR: `#33`

Delta from accepted UI head is only:
- `tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj`
- `tests/SGPdf.App.Tests/packages.lock.json`

Purpose: test-only direct pin `System.Security.Cryptography.Pkcs 8.0.2`; app/runtime unchanged.
CI run `38685414773`: SUCCESS.
- locked restore PASS
- Release build PASS, 0 warnings / 0 errors
- tests `1216/1216 PASS`

This is the accepted base for F13 development.

## 5. F13 — Product Shell / Dashboard — IN PROGRESS

A+B approved.

Spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

Implementation plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Mockup reference:
`docs/design/2026-10-15-f13-approved-mockup-reference.md`

### Current Task 1 — SG shell visual primitives

Branch: `feat/f13-shell-primitives`
Base: `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`
Current head: `97876802fa6326051a185858b18e5c4a0943e4b2`
Draft PR: `#34`

RED contract added:
`tests/SGPdf.App.Tests/ShellThemeContractTests.cs`

Expected RED was demonstrated in CI run `38685764535`:
- restore PASS
- build PASS, 0 warnings / 0 errors
- tests: `1217 PASS / 1 FAIL / 1218 total`
- failing test: `ShellThemeContractTests.ModernTheme_DeclaresApprovedShellPrimitives`
- first missing key: `SgHomeCardStyle`

This failure is intentional and proves Task 1 RED before product implementation.
No Dashboard, routing, icon, Home state, F14 utility wiring or F15 Edit V3 code has been started in this branch.

## 6. Exact resume point

Resume F13 Task 1 from head `97876802fa6326051a185858b18e5c4a0943e4b2`.

Next action only:
1. Extend `src/SGPdf.App/Themes/Modern.xaml` minimally with the required approved shell primitive keys.
2. Reuse existing dynamic SG/Fluent resources; no hard-coded light-only hex colors, no package/icon framework.
3. Run focused contract tests.
4. Run full suite.
5. Require exact-head CI GREEN.
6. Only then close Task 1 and advance to Task 2 static phase-honest ToolCatalog.

Do not skip RED evidence, do not touch `main`, do not merge PR #33/#34 without explicit user approval.

## 7. Later sequence

F13 shell → F14 productize existing capabilities → F15 Edit V3 → reassess public release → F16 new utilities → F17 professional UX → F18 optional local integrations.

Official application icon remains source-gated until the user supplies/approves the actual source asset; the mockup SG tile is not authoritative.
