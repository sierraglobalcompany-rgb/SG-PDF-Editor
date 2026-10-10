# F6 — KISS-A checkpoint — single EDITAR initialization path

**Date:** 2026-10-10  
**Branch:** `feat/f6-images`  
**Task 7 checkpoint base:** `372518fde4cb04fd25f65599c5ba9ba93fbf8123`  
**KISS-A RED:** `9c7556ccf1b61cb07b353f6fc11c34ced0f88d0a`  
**KISS-A functional GREEN:** `41dba2e532f46a278c5d2cf14070115995272d62`

## 1. Purpose

Behavior-preserving KISS cleanup before F6 Task 8. The audit found that EDITAR had begun accumulating the same patch-over-patch UI initialization pattern already present in older ORGANIZAR work: one base `Loaded` initializer plus a second `Loaded` initializer used only for hardening/dirty guards.

KISS-A removes that duplicate initialization route without changing image-edit product behavior.

## 2. RED witness

RED commit: `9c7556ccf1b61cb07b353f6fc11c34ced0f88d0a`  
Workflow run: `38054409016`

Added only:
- `tests/SGPdf.App.Tests/MainWindowEditImageKissTests.cs`

The RED contract requires EDITAR to have one initialization path and therefore forbids the secondary hardening initialization members:
- `_imageEditHardeningUiInitialized`;
- `ImageEditHardeningLoadedHookRegistered`;
- `RegisterImageEditHardeningLoadedHook`;
- `ImageEditHardeningHost_Loaded`;
- `InitializeImageEditHardeningUi`.

Exact RED evidence:
- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- test step: FAIL as expected;
- production code at that SHA still contained the five forbidden secondary-initialization members.

No production code changed in RED.

## 3. Minimal GREEN

Functional commit: `41dba2e532f46a278c5d2cf14070115995272d62`  
Workflow run: `38054582577`

Changes:
- `MainWindow.EditImages.Hardening.cs`
  - removed the secondary `Loaded` class handler;
  - removed the second initialization flag;
  - removed `InitializeImageEditHardeningUi()`;
  - kept the existing dirty guard, confirmation behavior and window-closing guard;
  - introduced only `WireImageEditGuards()` to attach those existing handlers.
- `MainWindow.EditImages.cs`
  - the existing idempotent `InitializeImageEditUi()` now calls `WireImageEditGuards()` once.

The main EDITAR `Loaded` hook remains the single entry point. No state-machine framework, DI, base UI class, event bus or new architecture was introduced.

## 4. GREEN evidence

Exact-head Windows run `38054582577` on `41dba2e532f46a278c5d2cf14070115995272d62`:
- checkout: PASS;
- repository hygiene: PASS;
- pinned Labelize staging: PASS;
- locked restore: PASS;
- Release build: PASS;
- full test step: PASS.

The suite had 641 tests at the Task-7 checkpoint and KISS-A adds exactly one test without deleting tests, so the expected discovered total is 642. The GitHub Actions job-level API used for this checkpoint exposes step success but not the console test-count line; therefore the durable claim is full-suite PASS, not a separately observed numeric console summary.

## 5. Scope audit

GREEN diff relative to RED:
- exactly 2 production files;
- `MainWindow.EditImages.Hardening.cs`: +1 / -27;
- `MainWindow.EditImages.cs`: +1 / -0.

KISS-A total relative to Task-7 checkpoint:
- 1 structural regression test;
- 2 production files;
- no dependencies;
- no PDF writer changes;
- no PDFium changes;
- no image-edit state changes;
- no ORGANIZAR cleanup;
- no preservation policy changes;
- no Task-8 implementation.

## 6. Behavior preserved

Existing hardening behavior remains owned by the same methods:
- dirty EDITAR can block leaving when discard is declined;
- clean EDITAR leaves without prompting;
- accepted discard clears only the in-memory edit workspace;
- window close remains guarded;
- open/switch flows continue to use `TryLeaveImageEditModeWithGuard()`.

This cleanup changes wiring topology, not product semantics.

## 7. Boundaries

- `main` remains `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No PR or merge was created.
- Manual Windows UX QA remains **NOT RUN**.
- ORGANIZAR Task6/7/8 wiring debt remains intentionally deferred to the later KISS-B cleanup.
- Common PDFium file-write helper remains deferred.
- `ImageReplacementAsset` memory/payload cleanup remains deferred to Task 9 after reference verification.
- Documentation consolidation remains deferred to final F6 closure/KISS-B as planned.

## 8. Next exact step

After exact-head CI for this checkpoint commit is GREEN, KISS-A is CLOSED.

The next user-approved `continua` should start **F6 Task 8 — independent preservation matrix + Save As warning policy**, using RED → GREEN. Do not start Task 8 from this checkpoint automatically.
