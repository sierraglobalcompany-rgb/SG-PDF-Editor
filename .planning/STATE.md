---
gsd_state_version: '1.1'
status: f13-task5-paused-pre-red
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: F13 Product Shell
  current_task: Task 5 additive document rail — paused before RED
---

# Project State — SG PDF Editor

**Updated:** 2026-10-15

## 1. Mandatory laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → TRACE/CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → CI → REMOVE REDUNDANT WORKAROUND`.

`main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`. No merge without explicit user approval.

## 2. Closed foundation

F0–F12 remain CLOSED. UI-4/Gate 0 is GREEN/CLOSED at accepted UI head `b4f19fcd5f2854c9025e60385e3b7819bd798a29`.

Accepted F13 base after test-only PKCS 8.0.2 consolidation:
- branch `integration/ui4-pkcs-release-truth-20261015`
- head `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`
- CI `38685414773` GREEN
- build 0 warnings / 0 errors
- tests `1216/1216 PASS`.

## 3. F13 A+B — IN PROGRESS

Authoritative spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

Authoritative implementation plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

### Task 1 — shell visual primitives — GREEN

- branch `feat/f13-shell-primitives`
- head `5a8e13f88175fe7469c18a1769fe86a96e001f10`
- draft PR `#34`
- CI `38686210087` GREEN
- build 0/0
- tests `1218/1218 PASS`.

### Task 2 — phase-honest ToolCatalog — GREEN

- branch `feat/f13-tool-catalog`
- head `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`
- draft PR `#35`
- valid RED CI `38686564150`
- GREEN CI `38686853745`
- build 0/0
- tests `1221/1221 PASS`.

Boundary remains frozen: F13 document ActionIds only `read/edit/comment/sign/organize`; full utility routing belongs to F14.

### Task 3 — HomeDashboard — GREEN

- branch `feat/f13-home-dashboard`
- functional GREEN head `b9905be21b3131ccf0c63e11dc4b170329c72017`
- draft PR `#36`
- valid RED head `434eacfd9ec09bdb44cdd32aea17b36e9c3c4aab`
- GREEN CI `38687569030`
- build 0/0
- tests `1224/1224 PASS`.

Dashboard is intent-only: Open PDF, Open ZPL, recent PDF, and ToolRequested. It reuses `RecentPdfEntry`; no second recent store or business logic.

A later accidental `_probe.tmp` add/delete produced no final tree delta. Current branch tree remains equivalent to the proven GREEN tree; do not rewrite branch history destructively.

### Task 4 — MainWindow Home/PDF/ZPL shell state — GREEN

- branch `feat/f13-shell-workspace-state`
- functional GREEN head `21bf03e0d303bc888639e74ad523de793f986e66`
- current tree-equivalent head `d267cbc0425c5e4243432145d9b60c522a9ae331`
- draft PR `#37`
- valid RED CI `38688722593`: 3 new shell-state tests failed only because shell state did not exist; prior 1224 stayed GREEN.
- first GREEN CI `38689293686`: build 0/0, tests `1227/1227 PASS`.
- fresh tree-equivalent verification CI `38753602191`: build 0 warnings / 0 errors, tests `1227/1227 PASS`.

Product delta remains only:
- `src/SGPdf.App/MainWindow.Shell.cs`
- minimal recent-open reuse in `MainWindow.ReaderShortcuts.cs`
- shell-state tests.

A temporary structural test attempting to forbid the existing Title-property trigger was proven RED, then classified as over-specified because it mandated an implementation detail not required by the approved plan. It was deleted before advancing; compare `21bf03e...` → `d267cbc...` has zero file delta.

## 4. F13 Task 5 — PAUSED PRE-RED

Branch already created:
- `feat/f13-document-tool-rail`
- exact head `d267cbc0425c5e4243432145d9b60c522a9ae331`
- no Task 5 product commit
- no Task 5 RED test committed yet.

Read-only owner/guard tracing has started across Edit, Sign, Comments and Organize so the rail cannot bypass existing pending-change guards. That trace is not complete yet.

Authoritative pause checkpoint:
`docs/history/2026-10-15-f13-task5-pre-red-pause-checkpoint.md`

### Exact resume

1. Verify branch head/state.
2. Finish owner/guard trace for `read/edit/comment/sign/organize`.
3. Add focused RED rail structure + routing/equivalence contract.
4. Prove RED.
5. Implement minimum additive ~96 px document rail and local `HandleToolAction(string actionId)` switch.
6. Preserve legacy centered strip during Task 5.
7. Focused PASS → full Release suite PASS → exact-head CI GREEN.
8. Only then Task 6.

Do not route Forms/OCR/Redact/Compare/Convert/Protect until F14. Do not touch F15 Edit V3, official icon, engines, packages, `main`, or merge policy.

## 5. Later sequence

F13 Tasks 5–9 → F14 productize existing capabilities → F15 Edit V3 → reassess first public release → F16 new utilities → F17 professional UX → F18 optional local integrations.

Official application icon remains source-gated until the user supplies/approves the real source asset; the mockup SG tile is not authoritative.
