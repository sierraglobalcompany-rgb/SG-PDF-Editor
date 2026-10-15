---
gsd_state_version: '1.1'
status: ui4-fail-p1-expansion-planned
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: UI-4 stabilization before F13
---

# Project State — SG PDF Editor

**Updated:** 2026-10-14

## Authority

Operational order for the post-UI4 transition:
1. live GitHub exact refs + exact-head CI;
2. `docs/history/2026-10-14-post-ui4-expansion-checkpoint.md`;
3. this `STATE.md`;
4. `.planning/ROADMAP.md`;
5. `docs/product/2026-10-14-local-suite-expansion.md`;
6. focused slice plans/requirements;
7. older master/history documents only when not superseded.

## Mandatory laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Protected `main`

`main` remains read-only at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge to `main` without explicit user approval.

## Closed functional baseline F0–F12

Historical authoritative functional head:
`8e1845d6d8fe72374d7a80994d8500b6d36077cc`

Historical functional exact-head CI:
- run `38555829715`;
- job `115824598759`;
- Release build 0 warnings / 0 errors;
- tests `1202/1202 PASS`.

F0–F12 remains a closed evidence baseline. The newly approved F13–F18 roadmap extends product scope; it does not invalidate the F0–F12 closure.

## Release/UI modernization branch

Remote branch:
`feat/release-ui-modernization`

Exact UI-4 tested HEAD:
`abd52d05279e3e656723ad4ff5a21d36de91508e`

Automated suite at the pre-UI4 head had reached `1214/1214 PASS`.

### UI-4 real Windows QA — FAIL-P1

Environment reported:
- Windows 11 Home Single Language x64 build `10.0.26300`;
- 1920×1200 @100%, 96 DPI;
- .NET SDK 10.0.402.

Results:
- V01 light shell PASS;
- V02 dark shell PASS after a local uncommitted mode-strip contrast fix;
- V03 PDF render/navigation/zoom PASS;
- V04 exact 1366×768 NOT RUN;
- V05 scaled Windows profile NOT RUN;
- V06 keyboard traversal PASS;
- V07 disabled states PASS;
- V08 first run FAIL-P2 / classification pending because label export/print stayed disabled;
- V09 protected-PDF flow NOT RUN;
- V10 Draw Signature FAIL-P1;
- V11 Signature from Photo FAIL-P1;
- V12 Signature Library FAIL-P1;
- V13 NOT RUN because name-dialog path was unreachable;
- V14 FAIL-P1 because signature workflows did not respond.

**Do not close UI-4 until real rerun evidence shows no P0/P1.**

## Local Codex diff — not committed/pushed

Codex reported one local change in:
`src/SGPdf.App/MainWindow.Sign.cs`

Purpose: replace hard-coded light strip visuals with existing shared toolbar styles for dark-mode contrast.

This diff is not part of the remote branch. Audit/preserve it before deciding whether to commit.

## Current P1 diagnostic

Windows-only diagnostic scope already prepared:
- trace V10–V12 runtime path;
- reproduce Edit/Save and reopen output;
- classify V08 expected state vs product bug;
- execute V09 with existing qpdf fixture generation if quickly viable.

Codex may prepare only a small local fix after demonstrated root cause. No redesign/publish/installer/push/main.

## Edit/Save audit finding

Remote code already proves:
- signature buttons have handlers and real dialog calls, so duplicate Click wiring is not an acceptable blind fix;
- Edit creates conservative image/text workspaces;
- some text is intentionally read-only when unsupported;
- Edit `Guardar como...` should be visible/enabled when a valid workspace exists;
- user-reported inability to edit/save therefore requires focused runtime classification as product bug vs expected limitation vs UX/discoverability.

## PKCS hygiene — GREEN on isolated branch

Branch:
`feat/fix-pkcs-hygiene`

HEAD:
`781cd6060121367f4d51efbe2724d820d1dfb842`

Exact-head normal CI:
- run `38645380308`;
- job `116130807039`;
- locked restore PASS;
- Release build 0 warnings / 0 errors;
- tests `1214/1214 PASS`.

Change is limited to the **test dependency graph**, pinning `System.Security.Cryptography.Pkcs 8.0.2` in test csproj/lock. App/runtime project is unchanged.

**PKCS is not yet integrated into the UI/release branch.**

## New approved product direction

Product spec:
`docs/product/2026-10-14-local-suite-expansion.md`

Execution roadmap:
`docs/superpowers/plans/2026-10-14-f13-f18-local-suite-expansion-roadmap.md`

Planning/checkpoint branch:
`plan/post-ui4-f13-f18-expansion-20261014`

### F13 — Product Shell / Dashboard
- local Home/dashboard;
- tool cards;
- left document tool rail;
- right contextual properties;
- compact top bar;
- official app icon from approved source only.

### F14 — Productize existing F0–F12 capabilities
Expose existing features coherently before adding parallel functionality.

### F15 — Edit V3
Make conservative text/image editing discoverable, explicit, reliably saveable and honest about limitations.

### F16 — New local utilities
Independent capability-gated slices: PDF→JPG, Image→PDF, Crop, authorized unlock, safe Optimize, image watermark if proven safe.

### F17 — Professional UX
Compare V2, additional existing AcroForm types, possible simple form designer, signature UX completion.

### F18 — Optional local integrations
LibreOffice Office→PDF, scanner, local HTML only after separate gates. Never mandatory for core runtime.

## Official app icon — REQUIRED

Pending actual approved source image materialization.

Required final surfaces:
- WPF window;
- executable;
- Alt+Tab/taskbar;
- shortcut;
- installer.

Never synthesize the official icon from a textual description alone.

## Immediate resume order

1. Audit the Windows P1 diagnostic result when Codex returns it.
2. Resolve only demonstrated P1 root causes.
3. Full suite + exact-head CI after each accepted code fix.
4. Rerun V10–V14 and remaining V04/V05/V08/V09 evidence.
5. Close UI-4 only when no P0/P1 remain.
6. Consolidate release docs and integrate PKCS conservatively.
7. Start F13.1/F13.2 planning/implementation; do not jump directly to later utilities.
8. Re-evaluate publish/installer only after the newly approved shell/product baseline is stable.
9. `main` only with explicit user approval.

## Codex usage rule

ChatGPT should do repository research, architecture, plans, code changes, tests, GitHub audit/integration and documentation whenever possible.

Use Codex only for tasks that materially require the user's Windows PC, real GUI interaction, hardware/private fixtures or locally installed application behavior. Avoid duplicated work/token spend.
