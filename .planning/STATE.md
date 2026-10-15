---
gsd_state_version: '1.1'
status: ui4-fail-p1-gate0-source-audited-f13-ab-approved-preflight-corrected-f15-spec-ready
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
3. `docs/history/2026-10-15-gate0-source-test-audit-before-f13.md`;
4. `docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`;
5. `docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`;
6. `docs/design/2026-10-15-f13-approved-mockup-reference.md`;
7. `.planning/ROADMAP.md`;
8. focused specs/plans in `docs/superpowers/`;
9. `docs/product/2026-10-14-local-suite-expansion.md`;
10. older master/history only when not superseded.

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
- V09 NOT RUN visually, source/functional path covered by protected-PDF tests;
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

## Gate 0 source/test audit — COMPLETE, no code change justified

Authoritative checkpoint:
`docs/history/2026-10-15-gate0-source-test-audit-before-f13.md`

Exact source/test conclusions on UI head `abd52d...`:
- V10–V12 signature entrypoints/delegates/default dialog constructors and `ShowDialog()` exist; existing tests cover buttons/seams/dialog construction but not the physical modal visibility chain. Windows trace remains mandatory before any signature fix.
- V08 Export is expected enabled only with valid `LabelLayoutPlan` and not-busy; Print additionally requires `Thermal`. Existing exact UI tests enforce this. Manual classification needs `MediaKind`, plan/null, busy state and validation text.
- Edit `Guardar como...` is already tested visible/enabled after successful Edit entry independent of selection; save/preflight/combined writer paths are covered. User dissatisfaction still needs classification as product bug vs safe limitation vs UX/discoverability.
- V09 source/functional path is already covered by `MainWindowReaderPasswordTests` using a genuinely protected PDF: wrong/cancel preserves the prior workspace, retry with the correct password succeeds, and the password is not persisted. Only Windows visual/dialog evidence remains.

**Anti-patch decision:** no product-code modification is accepted from source inspection alone. The next code change must be tied to a demonstrated Windows root cause.

## Current Windows-only diagnostic

Codex scope already prepared and remains the next external evidence source:
- trace V10–V12 runtime path down to actual modal visibility;
- reproduce Edit/Save and reopen output;
- classify V08 using real media/plan/busy state;
- visually verify V09 password-dialog behavior using an existing protected fixture where possible.

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

### F13 — Product Shell / Dashboard — A+B APPROVED / PREFLIGHT CORRECTED / READY AFTER GATE 0

The user explicitly approved **A+B** on 2026-10-15:
- **A:** visual/functional mockup specification;
- **B:** exact technical F13 implementation plan.

These are cumulative, not alternatives.

Authoritative visual spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

Authoritative technical plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Preflight correction commit:
`320f183d2f489ead40e1877cd545453f1f1a61f1`

The correction does not change the approved visual goal. It removes implementation noise found by source audit:
- F13 routes only core shell modes (`read/edit/comment/sign/organize`); F14 owns full capability productization/routing.
- invalid planned `EventHandler<string>` was replaced by a valid minimal local event contract (`Action` or tiny EventArgs only if needed).
- F13 preserves current Edit Save availability semantics; `dirty` gating remains F15.
- Task 7 must decouple `FindModePanel()` dependencies in Sign/Organize/Edit/Comments before deleting the centered legacy strip.
- existing automation/control names should be preserved on replacement controls where safe instead of generating broad test churn.

Approved mockup identity/reference:
`docs/design/2026-10-15-f13-approved-mockup-reference.md`

Old preliminary plan:
`docs/superpowers/plans/2026-10-14-f13-product-shell.md`

The old plan is explicitly **SUPERSEDED / DO NOT EXECUTE**.

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
- centered legacy mode strip is deleted only after automated equivalence + real Windows proof and after its init dependencies are removed;
- mockup red `SG` is placeholder only and does not authorize the official app icon.

**Implementation Gate 0:** do not execute F13 product code until UI-4 no longer has P0/P1.

**Process gate:** A+B is approved. No further design approval is required before F13 implementation once Gate 0 is GREEN.

### F14 — Productize existing capabilities

Plan:
`docs/superpowers/plans/2026-10-14-f14-productize-existing-capabilities.md`

Principle: F14 is the single owner for connecting the complete existing capability inventory to the F13 shell. Do not pre-duplicate that routing in F13.

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

F15 implementation plan remains pending until F13 shell is sufficiently stable and the written F15 spec is converted into its approved implementation plan.

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

1. Consume/audit the already-prepared Windows P1 diagnostic result when Codex returns it; do not send a second broad prompt in parallel.
2. Resolve only demonstrated root causes.
3. Full suite + exact-head CI after each accepted code fix.
4. Rerun V10–V14 plus V04/V05/V08/V09.
5. Close UI-4 only with no P0/P1.
6. Conservatively consolidate release truth + PKCS.
7. Execute corrected approved F13 A+B Tasks 1–9 natively, respecting Gate 0.
8. Use Codex only for F13 Windows equivalence/final visual QA or Windows-only defects.
9. Execute F14 as the single full-capability shell-routing/productization phase.
10. Convert F15 spec into implementation plan when F13 shell is sufficiently stable, then execute only after approval.
11. Reassess publish/installer after the intended product baseline is stable.
12. `main` only with explicit user approval.

## Codex usage rule

ChatGPT handles repository research, architecture, plans, code changes, tests, GitHub audit/integration and docs whenever possible.

Codex only for work that materially requires the user's Windows PC, real GUI interaction, hardware/private fixtures or locally installed application behavior. Avoid duplicated work/token spend.
