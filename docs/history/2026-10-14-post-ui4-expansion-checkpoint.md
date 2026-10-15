# SG PDF Editor — Checkpoint post UI-4 FAIL-P1 + F13–F18 expansion

**Date:** 2026-10-14
**Purpose:** durable restart point if the active chat/context is lost.

## Authority order

1. live GitHub exact refs/CI;
2. this checkpoint for the post-UI4 transition facts below;
3. `.planning/STATE.md` on this planning branch;
4. `.planning/ROADMAP.md` on this planning branch;
5. `docs/product/2026-10-14-local-suite-expansion.md`;
6. focused implementation plans;
7. older master/history docs only when not superseded.

## Three mandatory rules

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Protected refs

### `main`
Read-only until explicit user approval:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

### Remote UI branch tested by UI-4
`feat/release-ui-modernization`

Exact tested HEAD:
`abd52d05279e3e656723ad4ff5a21d36de91508e`

Do not rewrite/merge this history merely to absorb planning docs.

## UI-4 real Windows result

Codex executed real Windows QA against exact HEAD `abd52d05279e3e656723ad4ff5a21d36de91508e`.

Environment reported:
- Windows 11 Home Single Language x64, build `10.0.26300`;
- desktop `1920×1200 @100%` / 96 DPI;
- .NET SDK `10.0.402`;
- worktree started CLEAN.

Result:
- V01 PASS — light shell;
- V02 PASS — dark shell after a local uncommitted contrast fix;
- V03 PASS — synthetic PDF render/navigation/zoom;
- V04 NOT RUN — exact 1366×768 still required;
- V05 NOT RUN — scaled profile still required;
- V06 PASS — keyboard traversal/focus;
- V07 PASS — disabled-state visibility/UIA;
- V08 FAIL-P2 in the first run — label preview/properties visible but Save PDF/Print disabled; classification still requires focused state investigation;
- V09 NOT RUN in first run — protected-PDF fixture not generated yet;
- V10 FAIL-P1 — Draw Signature did not open canvas/dialog;
- V11 FAIL-P1 — Create from Photo did not open file picker;
- V12 FAIL-P1 — Signature Library did not open;
- V13 NOT RUN because name-dialog path was unreachable;
- V14 FAIL-P1 because signature-dialog functional smoke failed.

**UI-4 status: `FAIL-P1`. Do not mark PASS from CI.**

## Local Codex diff — not in GitHub

Codex reported one intentional local diff in:
`src/SGPdf.App/MainWindow.Sign.cs`

Purpose:
- remove hard-coded light mode-strip colors;
- reuse shared toolbar styles;
- improve dark-theme contrast.

That diff was **not committed or pushed** at the checkpoint. Preserve/audit it before deciding whether to keep it.

Important audit result:
- `MainWindow.Sign.cs` does build the dynamic mode strip, so a dark-mode strip fix in that file is structurally plausible.

## User manual product findings after UI-4

The user explicitly reported:
- visual design is improved but still needs another major polish pass;
- Read/Edit-style navigation should move to the left instead of a centered mode strip;
- primary actions should use icons for faster visual recognition;
- Edit mode did not allow a satisfactory edit/save experience;
- Edit/Save must be treated as a possible product bug + UX/discoverability issue until reproduced;
- official application icon is still missing and is release-required.

These findings are requirements, not optional cosmetics.

## Code audit already completed after the UI-4 report

### Signature
Remote code confirms the three UI actions are wired:
- Draw → `TryDrawSignature()` → `SignatureDrawDialog.Draw(...)`;
- Photo → `OpenFileDialog.ShowDialog(...)`;
- Library → `SignatureLibraryDialog.Open(...)`.

Therefore V10–V12 must be debugged at runtime/root cause; do **not** add duplicate Click handlers blindly.

### Edit / Save
Remote code confirms:
- Edit builds image/text workspaces;
- some text is intentionally read-only based on supported rendering conditions;
- `Guardar como...` should become visible/enabled when Edit mode has a valid workspace;
- real inability to save after entering a valid Edit workspace is therefore a focused P1 candidate;
- inability to edit an unsupported object may instead be an expected limitation, but the UI must explain it.

### Labels V08
The label Save PDF / Print controls start disabled by design. Need determine the true enabling precondition before calling this a product bug.

## Codex scope currently required

A compact Windows-only prompt was prepared for:
- runtime tracing of V10–V12;
- real Edit/Save reproduction + reopen verification;
- V08 expected-state vs product-bug classification;
- V09 protected fixture using existing qpdf if quick/viable.

Codex is allowed to prepare only a small local fix after a demonstrated root cause. No redesign, icon work, publish, installer, PKCS, `main` or push.

## PKCS hygiene — GREEN but isolated

Separate branch:
`feat/fix-pkcs-hygiene`

Clean final HEAD:
`781cd6060121367f4d51efbe2724d820d1dfb842`

Change:
- test graph pins `System.Security.Cryptography.Pkcs 8.0.2`;
- app/runtime project remains unchanged;
- final diff is limited to test csproj + test lock file.

Exact-head normal CI:
- run `38645380308`;
- job `116130807039`;
- locked restore PASS;
- Release build `0 warnings / 0 errors`;
- tests `1214/1214 PASS`, failed 0, skipped 0.

**Do not claim PKCS is integrated into the UI/release branch.** Consolidate later after UI stabilization using normal non-destructive ancestry checks.

## New product direction — approved for planning

Spec:
`docs/product/2026-10-14-local-suite-expansion.md`

Roadmap:
`docs/superpowers/plans/2026-10-14-f13-f18-local-suite-expansion-roadmap.md`

Direction:
- local suite dashboard inspired by iLovePDF discoverability;
- document workspace inspired by Acrobat's left-tool/right-properties pattern but with SG PDF's own visual identity;
- no ads, login, cloud dependency or AI baseline;
- productize existing F0–F12 capabilities before adding new motors;
- Edit V3 is a dedicated phase;
- new tools are capability-gated and implemented one by one.

Planned phases:
- F13 — Product Shell / Dashboard;
- F14 — Productize existing capabilities;
- F15 — Edit V3;
- F16 — local utilities (PDF→JPG, Image→PDF, Crop, authorized unlock, safe optimize, image watermark if gated);
- F17 — Compare V2 / Forms V2 / signature UX;
- F18 — optional local integrations such as LibreOffice/scanner/HTML only after independent gates.

## App icon rule

Required before release, ideally materialized in F13.4:
- use only the actual user-approved source image;
- generate multi-resolution `.ico`;
- apply to WPF window, executable, taskbar/Alt+Tab, shortcut and installer;
- never regenerate from textual description alone.

## Immediate resume order

1. Receive/audit the Windows P1 diagnostic result.
2. Resolve only demonstrated P1 root causes.
3. Run full tests + exact-head CI for each accepted code fix.
4. Rerun V10–V14 as applicable plus missing V04/V05/V09/V08 verification.
5. Close UI-4 only when no P0/P1 remain.
6. Consolidate/update release docs and integrate PKCS conservatively.
7. Start F13 with a focused design/implementation plan; do **not** jump directly to F16/F17.
8. Publish/installer ordering must be consciously re-evaluated against the newly approved expansion scope; do not ship an installer for a UI/product baseline the user has explicitly decided to redesign first.

## Branch for this checkpoint/planning work

`plan/post-ui4-f13-f18-expansion-20261014`

This branch exists to preserve current truth and future planning without moving the exact UI-4 tested branch or `main`.
