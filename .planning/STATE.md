---
gsd_state_version: '1.1'
status: f13-task2-green-implemented-ci-running
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: F13 Product Shell
  current_task: Task 2 phase-honest ToolCatalog — GREEN implemented, CI running
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

### Task 1 — shell visual primitives — GREEN

Branch: `feat/f13-shell-primitives`
Base: `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`
Head: `5a8e13f88175fe7469c18a1769fe86a96e001f10`
Draft PR: `#34`

TDD:
- RED head `97876802fa6326051a185858b18e5c4a0943e4b2` proved missing approved shell resource keys.
- GREEN adds only native WPF primitives in `Themes/Modern.xaml`.
- no Dashboard, routing, packages or feature-owner changes.

CI run `38686210087`: SUCCESS.
- locked restore PASS
- Release build PASS, 0 warnings / 0 errors
- tests `1218/1218 PASS`

### Task 2 — phase-honest ToolCatalog — GREEN IMPLEMENTED / CI RUNNING

Branch: `feat/f13-tool-catalog`
Base: Task 1 head `5a8e13f88175fe7469c18a1769fe86a96e001f10`
Draft PR: `#35`

RED head: `b92e29dc07b3f7d42952e0214e336237f25d661b`
RED CI run `38686564150`:
- build PASS, 0 warnings / 0 errors
- prior `1218` tests PASS
- exactly 3 new ToolCatalog contract tests FAIL because `SGPdf.App.Shell.ToolCatalog` does not exist
- RED therefore valid and isolated.

GREEN implementation head: `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`
Product delta only:
- `src/SGPdf.App/Shell/ToolDescriptor.cs`
- `src/SGPdf.App/Shell/ToolCatalog.cs`

Contract:
- document rail order matches approved spec;
- only `read/edit/comment/sign/organize` have F13 ActionIds;
- Forms/OCR/Redact/Compare/Convert/Protect remain disabled for F14 wiring;
- Home catalog is capability-honest;
- `PDF a PNG` is not mislabeled JPG;
- `Imagen a PDF` is explicitly `Próximamente`;
- only ZPL has a Home ActionId in F13;
- no reflection, MainWindow reference, delegate, command bus or routing in product.

GREEN CI run `38686853745` is currently RUNNING. Do not declare Task 2 closed until it passes full suite.

## 6. Exact resume point

1. Read CI run `38686853745` for head `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`.
2. If full suite GREEN, close Task 2.
3. Only then begin F13 Task 3 HomeDashboard in a new stacked branch with RED first.
4. Do not merge PR #33/#34/#35 or touch `main` without explicit user approval.

## 7. Later sequence

F13 shell → F14 productize existing capabilities → F15 Edit V3 → reassess public release → F16 new utilities → F17 professional UX → F18 optional local integrations.

Official application icon remains source-gated until the user supplies/approves the actual source asset; the mockup SG tile is not authoritative.
