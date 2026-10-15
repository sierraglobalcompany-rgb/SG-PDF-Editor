# Roadmap — SG PDF Editor

**Updated:** 2026-10-15

> Operational authority: live GitHub refs/CI → `.planning/STATE.md` → durable checkpoints → this roadmap → focused specs/plans.

## Product laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → CI → REMOVE REDUNDANT WORKAROUND`.

`main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`. No merge to `main` without explicit user approval.

---

# Baseline F0–F12 — CLOSED

Functional anchor: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.
Historical CI: `1202/1202 PASS`.

Do not reopen F0–F12 without a reproducible regression or explicit new requirement.

---

# Current blocking gate — UI-4 final Windows verification

## Status: AUTOMATED FIXES GREEN / WINDOWS FINAL QA PENDING

Original UI base:
`abd52d05279e3e656723ad4ff5a21d36de91508e`.

### Signature + Dark Mode

Accepted isolated fix line culminates in base:
`8c4a0f2619ca0a4835f6160ce4958d2d1b981435`.

Root cause:
- AcroForm `ScrollViewer` remained visible over the shared properties area;
- intercepted hit-testing for visual-signature controls.

Verification:
- causal RED→GREEN;
- Windows V10–V12 dialogs reopened;
- automated suite `1215/1215 PASS`.

### V08 Labels

Current branch:
`feat/fix-ui4-v08-label-actions`

Current HEAD:
`b4f19fcd5f2854c9025e60385e3b7819bd798a29`

Root cause:
- ZPL actions depend on `_isBusy`;
- valid Thermal layout was committed while busy;
- `SetBusy(false)` refreshed only viewer controls, leaving Export/Print stale-disabled.

Minimal GREEN:
- `SetBusy()` refreshes label properties/actions only when a ZPL workspace exists;
- no forced `IsEnabled`;
- no new architecture.

CI run `38681417442`, job `116246600509`:
- Release build PASS, 0 errors;
- suite `1216/1216 PASS`.

PR #32 is draft and unmerged.

### Final Windows gate

Run on exact HEAD `b4f19fcd5f2854c9025e60385e3b7819bd798a29`:
- V04 — 1366×768;
- V05 — Windows scaling 125/150%;
- V08 — Thermal Export/Print enabled after load;
- V09 — password dialog visual if practical;
- V10–V12 — signature final confirmation;
- V13 — name/library path;
- V14 — signature overlay + Save As + reopen;
- Edit/Save representative smoke + reopen.

Gate 0 becomes GREEN only with no P0/P1.

**Do not start F13 product code before Gate 0 GREEN.**

---

# Dependency hygiene — PKCS

Isolated GREEN branch:
- `feat/fix-pkcs-hygiene`;
- HEAD `781cd6060121367f4d51efbe2724d820d1dfb842`;
- suite `1214/1214 PASS`.

PKCS 8.0.2 is test-graph-only; app/runtime unchanged.
Integrate conservatively after UI stabilization; do not claim it is already in release truth.

---

# F13 — Product Shell / Dashboard — A+B APPROVED

Authoritative visual spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

Authoritative implementation plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Goal: implement the approved mockup using existing owners and native WPF, not a new framework.

### F13.1 Shell visual primitives
- extend `Themes/Modern.xaml` minimally;
- native vector `Geometry` icons;
- no icon package/UI framework;
- theme-safe DynamicResource usage.

### F13.2 Static capability-honest tool catalog
- deterministic descriptors only;
- no DI/plugin/reflection architecture;
- do not advertise future capability as current.

### F13.3 Home dashboard
- Open PDF;
- Open ZPL;
- drag/drop where supported;
- local recents using existing `RecentPdfStore`;
- responsive tool cards;
- no login/ads/cloud/AI.

### F13.4 Open-document shell
- ~96 px primary rail;
- current 240 px pages/context area only where relevant;
- center PDF workspace;
- ~320 px right contextual pane;
- top actions/search/zoom/print;
- initial rail owners: Leer, Editar, Comentar, Firmar, Organizar;
- preserve existing mode control names where useful for test stability.

### F13.5 Safe legacy-strip retirement
- first prove new rail routes to existing owners/guards;
- de-couple `FindModePanel()` dependencies;
- only then delete redundant centered navigation.

### F13.6 Official app icon
Source-gated. Use only actual user-approved source artwork.
Apply to window, exe, taskbar, Alt+Tab; shortcut/installer later.
Do not synthesize from the mockup placeholder.

### F13 exit
- full suite GREEN;
- CI GREEN;
- real Windows large + 1366×768 + scaling + light/dark + keyboard;
- visual hierarchy matches approved mockup direction;
- no P0/P1;
- no detached owners/guards.

---

# F14 — Productize Existing Capabilities

Expose current F0–F12 capability through the new shell before adding more engines.

Order:
1. Organize/page operations.
2. OCR / selected-text redaction / Protect / text watermark / numbering.
3. Comments / visual Sign / digital Sign / Forms.
4. Compare / PDF→PNG / Batch watermark / QR / ZPL.
5. Remove redundant old entry points only after safe equivalence proof.

Important:
- current owners remain authoritative;
- use one small explicit safe-transition path for dirty guards;
- no command bus/state machine;
- no duplicated writer/business logic;
- no fake free-rectangle redaction claim;
- no image watermark claim until F16 gate.

F14 exit: implemented capabilities are discoverable and coherent in the new product shell.

---

# F15 — Edit V3

Approved design/spec:
`docs/superpowers/specs/2026-10-15-f15-edit-v3-design.md`

Goal: make current conservative edit capability understandable and reliable without replacing its engine.

Slices:
1. direct object selection;
2. clear Text/Image contextual state;
3. `Editable / Editable con limitaciones / Solo lectura` + Spanish reason;
4. text properties only when genuinely supported;
5. image replace/extract/opacity/rotate/delete/z-order + existing move/resize;
6. Undo/Redo only where the current owner supports it;
7. visible `Guardar como...` with F15 dirty-state semantics;
8. scanned-document guidance to existing OCR;
9. representative real-PDF save-copy + reopen verification.

F15 exit: supported edits are discoverable, save reliably, and unsupported content fails clearly.

---

# Release decision point after F15

Target first serious public baseline after:
- UI-4 closed;
- PKCS/release truth consolidated;
- F13 redesign + official icon;
- F14 productization;
- F15 Edit V3.

At that point reassess publish/installer instead of forcing F16–F18 to block release.

---

# F16 — New Local Utilities

Independent KISS slices:
1. PDF→JPG / export studio using existing PDFium path;
2. Image→PDF;
3. Crop PDF;
4. authorized Unlock with known password via demonstrated qpdf path;
5. safe Optimize only after characterized output-preservation gate;
6. image watermark only after writer/preservation gate.

Each slice: capability gate → RED→GREEN → output validation → full suite → CI → Windows smoke.

---

# F17 — Professional UX Expansion

- Compare V2: side-by-side/sync/overlay where clean.
- Forms V2: capability-gate `/Btn` and `/Ch` before exposing.
- Simple form designer only after Forms V2 stability.
- Signature UX completion: typed/initials/date/color only if reusing current visual-sign pipeline cleanly.
- PFX/P12 remains a separate Digital Signature flow.

---

# F18 — Optional Local Integrations

Optional only:
- detected local LibreOffice → Office→PDF;
- scanner integration after hardware/API evaluation;
- local HTML→PDF after renderer/licensing/offline gate.

Absence of optional dependencies must never break core SG PDF.

---

# Explicit deferrals

No baseline promise for:
- high-fidelity PDF→Word/Excel/PPT;
- universal repair;
- real PDF/A without compliant validation;
- XFA/complex JavaScript/calculation engine;
- cloud signature requests;
- AI/cloud backend;
- generic plugin/conversion framework.

---

# Checkpoint protocol

At real slice boundaries record:
- branch + exact HEAD;
- files changed;
- root cause/value decision;
- focused tests;
- full suite totals;
- CI run/job;
- Windows status;
- P0/P1/P2;
- next bounded task;
- confirmation that `main` stayed untouched.

Avoid Markdown-per-commit noise.

# Codex rule

ChatGPT leads repository research, architecture, planning, implementation, tests, GitHub and documentation.
Use Codex only when the work materially requires the user's Windows GUI, hardware/private fixtures or installed-app behavior.
