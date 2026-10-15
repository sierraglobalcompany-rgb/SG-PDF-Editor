---
gsd_state_version: '1.1'
status: f13-design-audit-correction-required-task5-open
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: F13 Product Shell
  current_task: Task 5 visual alignment correction before GREEN
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

Authoritative visual/spec sources:
- `docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`
- `docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`
- approved `sg_pdf_f13_approved_mockup.jpg`

Current mockup-alignment override:
`docs/history/2026-10-15-f13-mockup-alignment-audit-and-correction.md`

### Task 1 — shell visual primitives — STRUCTURALLY GREEN

- branch `feat/f13-shell-primitives`
- head `5a8e13f88175fe7469c18a1769fe86a96e001f10`
- draft PR `#34`
- CI `38686210087` GREEN
- tests `1218/1218 PASS`.

Audit note: primitives remain valid, but icon geometries/consumption were not completed to mockup fidelity.

### Task 2 — phase-honest ToolCatalog — GREEN

- branch `feat/f13-tool-catalog`
- head `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`
- draft PR `#35`
- GREEN CI `38686853745`
- tests `1221/1221 PASS`.

Boundary remains frozen: F13 document ActionIds only `read/edit/comment/sign/organize`; full utility routing belongs to F14.

### Task 3 — HomeDashboard — STRUCTURALLY GREEN / VISUAL DEBT OPEN

- branch `feat/f13-home-dashboard`
- functional head `b9905be21b3131ccf0c63e11dc4b170329c72017`
- draft PR `#36`
- CI `38687569030` GREEN
- tests `1224/1224 PASS`.

Home structure/ownership is valid, but design audit found a material fidelity gap: all tool cards render the same literal `PDF` instead of consuming their distinct `IconKey`; Home rail is text-only. Correct this without creating a second dashboard or navigation owner.

### Task 4 — MainWindow Home/PDF/ZPL shell state — GREEN

- branch `feat/f13-shell-workspace-state`
- functional GREEN head `21bf03e0d303bc888639e74ad523de793f986e66`
- tree-equivalent head `d267cbc0425c5e4243432145d9b60c522a9ae331`
- draft PR `#37`
- fresh verification CI `38753602191`
- build 0 warnings / 0 errors
- tests `1227/1227 PASS`.

State owner remains valid and reusable.

## 4. F13 Task 5 — OPEN / NOT GREEN

Branch:
- `feat/f13-document-tool-rail`
- current product head `eb0be24071a9b205a6386e57e54912b3600ab3b1`
- draft PR `#38`
- base `d267cbc0425c5e4243432145d9b60c522a9ae331`.

TDD evidence:
- valid RED: prior 1227 tests stayed GREEN and exactly 4 new rail/router tests failed as intended;
- first GREEN build passed;
- workflow run `38758303793` FAILED in tests;
- result `1229/1231 PASS`, exactly 2 failures still unresolved/classification pending.

Do not patch either test or product before classification.

### Design audit findings that now gate Task 5

1. Document rail is 96 px/order-correct but text-only; approved mockup requires icon + text.
2. `ToolCatalog.Document` already has `IconKey`; UI must consume it rather than invent a parallel mapping.
3. Home cards similarly ignore their existing `IconKey` and show generic `PDF`.
4. Home rail is text-only and must converge to icon + text.
5. Selected/disabled state must be visually explicit and accessible; selected cannot rely only on color.
6. No Forms/OCR/Redact/Compare/Convert/Protect routing may be activated before F14.

### Exact resume

1. Preserve current Task 5 branch/head; no destructive rewind.
2. Classify the 2 current failures.
3. Extend/correct focused visual contract for actual `IconKey` consumption and icon+text rails.
4. Make the minimum visual remediation across existing owners/resources only.
5. Focused PASS → full Release suite PASS → exact-head CI GREEN.
6. Only then advance to Task 6.

## 5. Remaining F13 visual convergence

### Task 6
Strengthened gate: contextual toolbar + right frame ~320 px + global command hierarchy coherent with mockup. Preserve owner semantics and do not pull in F15 business changes.

### Task 7
Prove equivalence, decouple legacy initialization and remove only proven redundant centered navigation. Final F13 must not leave visually competing old/new navigation.

### Task 8 — official application icon — SOURCE CONFIRMED

Approved source already exists in the project Library:
`Icono PDF con lápiz azul y logo SG(1).png`.

The previous wording “waiting for user to supply/approve source” is obsolete. If bytes cannot be materialized by the current tool surface, classify that as a technical transfer/tooling issue, not a missing design decision. Do not invent or reapprove another icon.

F13 must wire this approved identity to window/exe/taskbar/Alt+Tab before final visual QA. Installer/file-association work later reuses the same asset.

### Task 9
Windows QA must include side-by-side comparison against the approved Dashboard and Document mockup, plus large/1366/scaled/light/dark/keyboard/disabled/right-scroll checks. F13 closes only with no P0/P1.

## 6. Later sequence

Close corrected F13 → F14 productize existing capabilities → F15 Edit V3 → reassess first public release → F16 new utilities → F17 professional UX → F18 optional local integrations.

No product code was changed by this design audit itself.
