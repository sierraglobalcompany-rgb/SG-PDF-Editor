# SG PDF — F13 Start Readiness Checkpoint

**Date:** 2026-10-15  
**Purpose:** allow F13 to start immediately after UI-4 Gate 0 becomes GREEN, without reopening approved design/planning decisions.

## Status now

**F13 PRODUCT CODE: BLOCKED ONLY BY FINAL WINDOWS GATE.**

Automated UI-4 line:
- signature/Dark Mode root-cause fix accepted in isolated line;
- V08 root-cause fix accepted in isolated line;
- current V08 head `b4f19fcd5f2854c9025e60385e3b7819bd798a29`;
- CI run `38681417442`, job `116246600509`;
- Release build PASS;
- full suite `1216/1216 PASS`.

Final Windows evidence still required: V04, V05, V08, V09 if practical, V10–V14, representative Edit/Save + reopen. No F13 code before no-P0/P1 result.

## Approved F13 authority

A — visual contract:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

B — implementation plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

A+B are cumulative and approved. Do not ask for another design approval before implementation.

## Frozen architecture

- Native WPF only.
- Existing PDFium/PDFsharp/Labelize/qpdf/Tesseract boundaries stay unchanged.
- No new UI framework, icon package, DI container, command bus, event aggregator, plugin system or generic navigation service.
- Existing owners/guards remain authoritative.
- Same `MainWindow`: Home state when no document/ZPL; document workspace when open.
- Keep `main` read-only until explicit user approval.

## First implementation sequence after Gate 0 GREEN

### F13.1 — visual primitives
Modify/extend `Themes/Modern.xaml` only as needed:
- shell surfaces;
- rail item style;
- tool card style;
- contextual toolbar/pane styles;
- focus/selected/disabled states;
- native vector `Geometry` icons.

Add `ShellThemeContractTests`.

### F13.2 — capability-honest catalog
Create small deterministic descriptors/catalog.
No reflection/discovery framework.
Current core rail only:
- Leer;
- Editar;
- Comentar;
- Firmar;
- Organizar.

Future/productized tools may be shown only with honest disabled/`Próximamente` state until F14 routes them.

### F13.3 — Home dashboard
Create Home shell/dashboard:
- Open PDF;
- Open ZPL;
- drag/drop if current open path supports it cleanly;
- recent local PDFs via existing `RecentPdfStore`;
- responsive capability-honest cards.

No cloud/login/ads/AI.

### F13.4 — shell state
Create minimal `MainWindow.Shell.cs` and Home/PDF/ZPL state transition.
Failed/cancelled open must preserve current valid state.

### F13.5 — document rail
Add new ~96 px primary rail while legacy strip still exists.
Route only to current owners first.
Preserve existing control names where useful to avoid test churn.
Do not route F14 utility set yet.

### F13.6 — contextual frame
Add contextual toolbar/right-panel framing around existing owners.
Do not create universal undo/history or command model.
Do not change F15 save/dirty semantics here.

### F13.7 — equivalence and cleanup
Prove new rail reaches existing owners and dirty guards.
De-couple `FindModePanel()` dependencies before removing legacy centered mode strip.
Delete redundant legacy UI only after equivalence tests/Windows smoke.

### F13.8 — official icon
Only after user supplies/approves actual source artwork.
Generate multiresolution `.ico` and apply to window/exe/taskbar/Alt+Tab; shortcut/installer later.
Mockup red `SG` tile is not an approved icon source.

### F13.9 — final visual QA
Windows evidence:
- large desktop;
- 1366×768;
- scaled profile;
- light/dark;
- keyboard/focus;
- no clipping;
- Home and open-document hierarchy match approved direction.

Then full suite + CI + checkpoint.

## Capability honesty reminders

- Current conversion is PDF→PNG, not JPG until F16.
- Image→PDF is F16.
- Watermark current capability is text watermark; image watermark is F16-gated.
- Current safe redaction is selected-text policy/workspace, not arbitrary rectangle.
- Merge/Split Home cards must not overclaim top-level semantics until F14 confirms owner behavior.
- Digital PFX/P12 signature stays distinct from visual signature.

## F13 exit / next phase

F13 GREEN requires:
- shell visual contract implemented;
- owners/guards preserved;
- full suite GREEN;
- CI GREEN;
- Windows visual/accessibility evidence;
- no P0/P1;
- no redundant legacy mode strip left after safe deletion;
- official icon either integrated from approved source or explicitly recorded as source-blocked.

Next phase is **F14 productization**, then **F15 Edit V3**.
