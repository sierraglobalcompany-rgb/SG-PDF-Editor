---
gsd_state_version: '1.1'
status: f13-task2-green-task3-next
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: F13 Product Shell
  current_task: Task 3 HomeDashboard — ready for RED
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
Head: `5a8e13f88175fe7469c18a1769fe86a96e001f10`
Draft PR: `#34`
CI run `38686210087`: SUCCESS.
- Release build PASS, 0 warnings / 0 errors
- tests `1218/1218 PASS`

### Task 2 — phase-honest ToolCatalog — GREEN

Branch: `feat/f13-tool-catalog`
Base: Task 1 head `5a8e13f88175fe7469c18a1769fe86a96e001f10`
Head: `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`
Draft PR: `#35`

RED head: `b92e29dc07b3f7d42952e0214e336237f25d661b`
RED CI run `38686564150`:
- build PASS;
- prior `1218` tests PASS;
- exactly 3 new ToolCatalog tests FAIL because the production catalog is absent.

GREEN product delta only:
- `src/SGPdf.App/Shell/ToolDescriptor.cs`
- `src/SGPdf.App/Shell/ToolCatalog.cs`

Contract:
- approved document rail order;
- F13 executable document actions limited to Read/Edit/Comment/Sign/Organize;
- Forms/OCR/Redact/Compare/Convert/Protect held for F14;
- Home catalog capability-honest;
- `PDF a PNG` label honest;
- `Imagen a PDF` explicitly `Próximamente`;
- only ZPL has a Home ActionId in F13;
- no product reflection, MainWindow references, delegates, command bus or routing.

GREEN CI run `38686853745`: SUCCESS.
- locked restore PASS
- Release build PASS, 0 warnings / 0 errors
- tests `1221/1221 PASS`

### Task 3 — HomeDashboard — NEXT

Start from Task 2 head `8c40cc98e102e75a3bd550f8ef3e9e58c599df32` in a new stacked branch.
RED first.

Scope only:
- `Shell/HomeDashboard.xaml`
- `Shell/HomeDashboard.xaml.cs`
- tests for structure, catalog-driven cards, honest disabled states, open-PDF/open-ZPL events and lightweight recent-file presentation.

Do not route business logic into the dashboard. No PDF engine, RecentPdfStore duplication, F14 wiring, F15 Edit V3, icon asset, packages or framework changes.

## 6. Exact resume point

1. Create Task 3 stacked branch from `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`.
2. Read the approved Task 3 plan/spec before writing tests.
3. Add the smallest RED contract for HomeDashboard.
4. Prove RED in CI.
5. Implement only enough HomeDashboard to turn it GREEN.
6. Full suite + CI before Task 4.
7. No merge of PR #33/#34/#35 or `main` without explicit user approval.

## 7. Later sequence

F13 shell → F14 productize existing capabilities → F15 Edit V3 → reassess public release → F16 new utilities → F17 professional UX → F18 optional local integrations.

Official application icon remains source-gated until the user supplies/approves the actual source asset; the mockup SG tile is not authoritative.
