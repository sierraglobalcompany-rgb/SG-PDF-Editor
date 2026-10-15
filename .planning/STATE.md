---
gsd_state_version: '1.1'
status: ui4-fail-p1-f13-mockup-plan-ready-f15-spec-ready
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: UI-4 stabilization before F13
---

# Project State — SG PDF Editor

**Updated:** 2026-10-15

## Authority

Operational order for the post-UI4 transition:
1. live GitHub exact refs + exact-head CI;
2. this `STATE.md`;
3. `docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`;
4. `docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`;
5. `docs/design/2026-10-15-f13-approved-mockup-reference.md`;
6. `.planning/ROADMAP.md`;
7. focused specs/plans in `docs/superpowers/`;
8. `docs/product/2026-10-14-local-suite-expansion.md`;
9. older master/history only when not superseded.

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

F0–F12 remains closed evidence baseline. F13–F18 extends scope without invalidating that closure.

## Release/UI branch

Remote branch:
`feat/release-ui-modernization`

Exact UI-4 tested HEAD:
`abd52d05279e3e656723ad4ff5a21d36de91508e`

Automated suite before manual UI-4: `1214/1214 PASS`.

### UI-4 Windows QA — FAIL-P1

Environment:
- Windows 11 Home Single Language x64 build `10.0.26300`;
- 1920×1200 @100%, 96 DPI;
- .NET SDK 10.0.402.

Results:
- V01 PASS;
- V02 PASS after local uncommitted dark-mode strip fix;
- V03 PASS;
- V04 NOT RUN;
- V05 NOT RUN;
- V06 PASS;
- V07 PASS;
- V08 FAIL-P2 / classification pending;
- V09 NOT RUN;
- V10 FAIL-P1;
- V11 FAIL-P1;
- V12 FAIL-P1;
- V13 NOT RUN;
- V14 FAIL-P1.

**Do not close UI-4 until real rerun evidence shows no P0/P1.**

## Local Codex diff — not committed/pushed

Reported local change:
`src/SGPdf.App/MainWindow.Sign.cs`

Purpose: replace hard-coded light strip visuals with shared toolbar styles for dark-mode contrast.

Preserve/audit before any local branch manipulation.

## Current Windows-only diagnostic

Codex scope already prepared:
- trace V10–V12 runtime path;
- reproduce Edit/Save and reopen output;
- classify V08 expected state vs product bug;
- execute V09 with existing qpdf if quickly viable.

Codex may prepare only a small local fix after demonstrated root cause. No redesign, publish, installer, push or main.

## PKCS hygiene — GREEN isolated

Branch:
`feat/fix-pkcs-hygiene`

HEAD:
`781cd6060121367f4d51efbe2724d820d1dfb842`

Exact-head CI:
- run `38645380308`;
- job `116130807039`;
- locked restore PASS;
- Release build 0 warnings / 0 errors;
- tests `1214/1214 PASS`.

PKCS 8.0.2 is pinned only in the test dependency graph. App/runtime unchanged.

**Not yet integrated into UI/release branch.**

## Expansion planning branch

`plan/post-ui4-f13-f18-expansion-20261014`

Contains planning/documentation only; no product implementation.

### F13 — Product Shell / Dashboard — MOCKUP + SPEC APPROVED / IMPLEMENTATION PLAN READY

Authoritative visual spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

Authoritative technical plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Approved mockup identity/reference:
`docs/design/2026-10-15-f13-approved-mockup-reference.md`

Old preliminary plan:
`docs/superpowers/plans/2026-10-14-f13-product-shell.md`

The old plan is now explicitly **SUPERSEDED / DO NOT EXECUTE**.

Frozen F13 decisions:
- 96 px Home/document rail;
- 24 px Home content padding;
- tool cards with icon + title + short description;
- local/offline identity; no login, ads, cloud or AI;
- document workspace with left rail, center canvas and 320 px contextual right panel;
- contextual toolbar per active owner;
- SG dynamic theme resources; Light/Dark baseline;
- `PDF a PNG` is the honest F13 conversion label; JPG remains F16;
- `Imagen a PDF` may be shown only disabled/`Próximamente` before F16;
- current feature owners remain single owners; shell implements no PDF business logic;
- centered legacy mode strip is deleted only after automated equivalence + real Windows proof;
- mockup red `SG` is placeholder only and does not authorize the official app icon.

**Implementation Gate 0:** do not execute F13 product code until UI-4 no longer has P0/P1.

**Process gate:** implementation plan is ready for user review. After approval, execution method remains ChatGPT/native; Codex is used only for Windows/manual gates.

### F14 — Productize existing capabilities

Plan:
`docs/superpowers/plans/2026-10-15-f14-productize-existing-tools.md`

Principle: expose existing owners through the new shell before building parallel features.

Owner/entrypoint inventory:
`docs/product/2026-10-14-existing-tool-entrypoint-inventory.md`

### F15 — Edit V3 — DESIGN APPROVED / SPEC READY

Approved approach: **A — unified UX over existing engine**.

Spec:
`docs/superpowers/specs/2026-10-15-f15-edit-v3-design.md`

Key frozen decisions:
- preserve `ImageEditWorkspace`, `TextEditWorkspace`, `RedactionWorkspace`, `PdfEditWriter`;
- direct object selection: text/image detected automatically;
- contextual right panel;
- `Guardar como...` always visible in Edit mode;
- no dirty → save disabled;
- any dirty → save enabled;
- save always creates copy; original stays active;
- no automatic reopen of saved copy;
- image Undo/Redo reused;
- text/redaction do not currently have history, so no fake/universal Undo stack;
- any future history extension must remain inside the owning workspace;
- no second engine/writer, plugin framework, command bus, DI rewrite, cloud or AI.

F15 implementation plan remains pending until its written spec is separately reviewed/approved and F13 shell is sufficiently stable.

### F16 — New local utilities
Independent capability-gated slices: PDF→JPG, Image→PDF, Crop, authorized unlock, safe Optimize, image watermark if proven safe.

### F17 — Professional UX
Compare V2, additional AcroForm types, possible simple form designer, signature UX completion.

### F18 — Optional local integrations
LibreOffice Office→PDF, scanner, local HTML only after separate gates; never mandatory core runtime.

## Official app icon — REQUIRED

Pending actual approved source image materialization.

Required surfaces:
- WPF window;
- executable;
- Alt+Tab/taskbar;
- shortcut;
- installer.

Never synthesize official icon from mockup placeholder or textual description.

## Immediate resume order

1. Audit Windows P1 diagnostic when Codex returns it.
2. Resolve only demonstrated root causes.
3. Full suite + exact-head CI after each accepted code fix.
4. Rerun V10–V14 plus V04/V05/V08/V09.
5. Close UI-4 only with no P0/P1.
6. Conservatively consolidate release truth + PKCS.
7. After user review of the F13 implementation plan, execute F13 Tasks 1–9 natively, respecting Gate 0.
8. Use Codex only for F13 Windows equivalence/final visual QA or Windows-only defects.
9. Execute F14 by routing existing owners, not rewrites.
10. F15 implementation only after its written spec + implementation plan approval.
11. Reassess publish/installer after the intended product baseline is stable.
12. `main` only with explicit user approval.

## Codex usage rule

ChatGPT handles repository research, architecture, plans, code changes, tests, GitHub audit/integration and docs whenever possible.

Codex only for work that materially requires the user's Windows PC, real GUI interaction, hardware/private fixtures or locally installed application behavior. Avoid duplicated work/token spend.
