# SG PDF — F13 Task 4 GREEN checkpoint

**Fecha:** 2026-10-15
**Estado:** F13 Tasks 1–4 GREEN; Task 5 NEXT.

## Baseline

- `main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- accepted F13 base: `integration/ui4-pkcs-release-truth-20261015` @ `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`.
- UI-4 is CLOSED/GREEN.

## F13 completed blocks

### Task 1 — visual primitives
- branch `feat/f13-shell-primitives`
- head `5a8e13f88175fe7469c18a1769fe86a96e001f10`
- PR #34
- CI `38686210087`: `1218/1218 PASS`, build 0 warnings/0 errors.

### Task 2 — ToolCatalog
- branch `feat/f13-tool-catalog`
- head `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`
- PR #35
- CI `38686853745`: `1221/1221 PASS`, build 0 warnings/0 errors.

### Task 3 — HomeDashboard
- branch `feat/f13-home-dashboard`
- functional GREEN head `b9905be21b3131ccf0c63e11dc4b170329c72017`
- PR #36
- CI `38687569030`: `1224/1224 PASS`, build 0 warnings/0 errors.
- Home is intent-only and reuses the existing recent-file model.

### Task 4 — Home/PDF/ZPL shell state
- branch `feat/f13-shell-workspace-state`
- functional GREEN head `21bf03e0d303bc888639e74ad523de793f986e66`
- current tree-equivalent head `d267cbc0425c5e4243432145d9b60c522a9ae331`
- PR #37
- CI `38689293686`: `1227/1227 PASS`.
- fresh CI `38753602191`: `1227/1227 PASS`, build 0 warnings/0 errors.
- compare `21bf03e...` → `d267cbc...`: zero file delta.

A structural test added during audit to force explicit Reader/Labels state notifications was intentionally removed after proving it over-specified an implementation detail not required by the approved F13 design. No product patch was added for that preference.

## Exact next step

Start **F13 Task 5 — additive document rail + core routing** from:

`d267cbc0425c5e4243432145d9b60c522a9ae331`

Rules:
- RED first;
- 96 px rail, approved catalog order;
- route only `read/edit/comment/sign/organize`;
- explicit local switch, no command bus/navigation service;
- reuse existing owners/guards;
- keep legacy mode strip during Task 5 because current module initialization still depends on it;
- no F14 utility routing, no F15 Edit V3, no icon, no packages, no engine changes, no `main` merge.

After Task 5 GREEN: Task 6 contextual shell frame; Task 7 prove equivalence then remove redundant legacy navigation; Task 8 official icon only if approved source exists; Task 9 final Windows visual QA.
