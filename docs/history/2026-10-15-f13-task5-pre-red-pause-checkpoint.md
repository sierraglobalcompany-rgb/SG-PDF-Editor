# SG PDF — F13 Task 5 PRE-RED pause checkpoint

**Fecha:** 2026-10-15
**Estado:** PAUSED by user request. Resume from this file; do not repeat Tasks 1–4.

## Proven GREEN baseline

F13 Tasks 1–4 are GREEN.

Task 4 exact branch/state:
- branch `feat/f13-shell-workspace-state`
- functional GREEN head `21bf03e0d303bc888639e74ad523de793f986e66`
- current tree-equivalent head `d267cbc0425c5e4243432145d9b60c522a9ae331`
- fresh CI `38753602191`
- Release build: 0 warnings / 0 errors
- full suite: `1227/1227 PASS`

The temporary structural test that attempted to force explicit Reader/Labels notifications was removed as over-specified. No product patch was introduced; the resulting tree is equivalent to the previously proven GREEN Task 4 tree.

## Task 5 branch created — no Task 5 product changes yet

- branch `feat/f13-document-tool-rail`
- exact head `d267cbc0425c5e4243432145d9b60c522a9ae331`
- this is the same exact tree used to close Task 4
- no RED test for Task 5 has been committed yet
- no rail/routing implementation has been committed yet

Therefore Task 5 is still **PRE-RED** and can be resumed cleanly without rollback.

## Audit already started before pause

Read-only tracing began for the existing mode owners/guards so the new rail does not bypass pending-change protections.

Files inspected include:
- `MainWindow.Edit.cs`
- `MainWindow.Sign.cs`
- `MainWindow.Comments.cs`
- `MainWindow.Comments.Integration.cs`
- `MainWindow.Organize.cs`
- `MainWindow.Organize.Commands.cs`
- `MainWindow.Organize.Integration.cs`

Confirmed principle: Task 5 must route to the existing owners and preserve their current guards; it must not invent parallel mode logic.

The owner/guard audit was **not yet completed** when the pause was requested. Resume by finishing that trace before writing the RED contract.

## Exact Task 5 scope

Only:
- additive ~96 px document rail;
- approved order from `ToolCatalog.Document`;
- visible text plus accessible/focusable buttons;
- executable routes in F13 only: `read`, `edit`, `comment`, `sign`, `organize`;
- explicit local `HandleToolAction(string actionId)` switch;
- route to existing owners/guards;
- keep the legacy centered mode strip during Task 5 because current initialization still depends on it.

Out of scope:
- Forms/OCR/Redact/Compare/Convert/Protect routing — F14;
- deleting legacy navigation — only after equivalence proof in later F13 task;
- F15 Edit V3;
- official app icon;
- packages/engines/backend/cloud;
- merge to `main`.

## Exact resume sequence

1. Verify `feat/f13-document-tool-rail` still points to `d267cbc0425c5e4243432145d9b60c522a9ae331` or document any legitimate newer head before work.
2. Finish owner/guard trace for `read/edit/comment/sign/organize`.
3. Add one focused RED contract for rail structure + routing/equivalence.
4. Prove RED without breaking prior baseline unexpectedly.
5. Implement minimum additive rail/router.
6. Focused tests PASS.
7. Full Release suite PASS.
8. Exact-head CI GREEN.
9. Only then advance to F13 Task 6.

## Laws

- KISS: A→B, no generic navigation service/DI/command bus.
- Noise: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Anti-patch: reproduce/trace/root-cause/owner/minimal change/cause test/full suite/CI.
- `main` remains read-only; no merge without explicit user approval.
