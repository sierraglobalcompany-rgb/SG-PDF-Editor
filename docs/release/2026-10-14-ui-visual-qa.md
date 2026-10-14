# SG PDF Editor — Windows Visual QA

**Status:** READY TO RUN / MANUAL EVIDENCE NOT RUN

**Branch:** `feat/release-ui-modernization`

**Automated visual implementation anchor:** `60bb0f73090cd4d3e24d5eab5c69ee707432f823`

**Exact-head CI:** run `38632179198` — SUCCESS; full suite `1214/1214 PASS`.

## Purpose

This gate validates the real rendered WPF UI after UI-1 Foundation, UI-2 Main Shell and UI-3 Dialog Consistency. It does not re-test PDF algorithms and it must not expand F0–F12 scope.

## Rules

- A CI/synthetic PASS never upgrades a visual/manual case to PASS.
- Do not change C# behavior to fix cosmetic issues unless a reproduced functional defect proves it is required.
- P0/P1 usability defects may be fixed with the smallest XAML/resource change and must return through full suite + exact-head CI.
- Do not add a UI framework, icon library, animation framework, MVVM rewrite or new navigation architecture.
- Keep `main` read-only.
- Screenshots are QA evidence; do not commit binary screenshots to the repository unless explicitly approved.

## Test environment

Minimum:
- Windows 11 x64;
- system light and dark theme;
- 1366×768 at 100% scaling;
- one higher-resolution/scaled profile, preferably 1920×1080 at 125% or the closest available real setup;
- branch exact HEAD checked before execution.

Before starting, record:
- Windows build: `NOT RUN`;
- display resolution/scaling: `NOT RUN`;
- application commit SHA: `NOT RUN`;
- light/dark theme tested: `NOT RUN`.

## Visual matrix

| ID | Scene | Required checks | Status |
|---|---|---|---|
| V01 | Empty main window — light | Shell hierarchy, menu/toolbar/panels/status, no clipping, readable empty state | NOT RUN |
| V02 | Empty main window — dark | No unreadable custom surfaces; borders/text remain legible | NOT RUN |
| V03 | PDF loaded | Page canvas, left/right regions, navigation and zoom remain usable | NOT RUN |
| V04 | 1366×768 | Critical controls visible; toolbar/right panel do not clip required actions | NOT RUN |
| V05 | Scaled Windows profile | No overlap/truncation that blocks actions | NOT RUN |
| V06 | Keyboard traversal | Visible focus cue on primary interactive controls | NOT RUN |
| V07 | Disabled controls | Disabled actions remain visually and semantically distinguishable | NOT RUN |
| V08 | Label workflow | Label navigation/properties/export/print hierarchy remains coherent | NOT RUN |
| V09 | PDF password dialog | Shared surface/button hierarchy, readable error/message, default action clear | NOT RUN |
| V10 | Signature draw dialog | White ink canvas preserved, surrounding Fluent shell readable, actions unclipped | NOT RUN |
| V11 | Signature photo dialog | Checkerboard/preview preserved, sliders/combos readable, primary action clear | NOT RUN |
| V12 | Signature library dialog | List/preview/status/buttons readable in both themes | NOT RUN |
| V13 | Signature name dialog | Input/status/cancel/accept hierarchy coherent | NOT RUN |
| V14 | Functional smoke during UI QA | Open PDF, navigate, zoom, open/close tested dialogs without crash or detached handlers | NOT RUN |

## Required screenshot set

Capture locally, at minimum:
1. empty main shell — light;
2. empty main shell — dark;
3. PDF loaded;
4. label mode/properties;
5. password dialog;
6. signature draw;
7. signature photo;
8. signature library.

Recommended filename pattern: `UIQA_<ID>_<scene>_<theme>_<resolution>_<scale>.png`.

Do not mark PASS from a screenshot alone when the case requires interaction, focus, disabled-state or functional smoke.

## Classification

- **PASS:** rendered and interacted with as required; no release-blocking visual/usability defect.
- **FAIL-P0:** crash, unusable window/dialog, critical action inaccessible.
- **FAIL-P1:** serious clipping/contrast/focus problem that materially blocks normal use.
- **FAIL-P2:** cosmetic inconsistency that does not block release flow; record but do not create architecture for it.
- **NOT RUN:** no real Windows evidence yet.

## Open release-adjacent items that are not part of this visual matrix

- official app/PDF `.ico` binary is still pending materialization from the user-approved visual master;
- `System.Security.Cryptography.Pkcs 8.0.1` NU1902/NU1903 dependency hygiene remains a separate root-cause task;
- deterministic self-contained `win-x64` publish and installer remain after visual QA;
- Packs A–D retain their real/manual/private/physical gates.

## Completion condition

UI-4 may be called GREEN only when:
1. exact-head automated CI is SUCCESS;
2. V01–V14 have real Windows evidence;
3. all P0/P1 findings are resolved and re-verified;
4. the UI modernization record and `.planning/STATE.md` are updated with the actual evidence;
5. no manual/physical result is inferred from CI.
