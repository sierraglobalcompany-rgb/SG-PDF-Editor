# F13–F18 Local Suite Expansion Implementation Roadmap

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans for each approved phase plan. This file is a roadmap across independent subsystems; create a focused implementation plan per phase/slice before touching product code.

**Goal:** evolve SG PDF Editor from the closed F0–F12 functional baseline into a coherent local desktop PDF suite with a discoverable home, Acrobat-like document workspace, improved editing, and carefully capability-gated local utilities.

**Architecture:** reuse the current WPF/.NET 10 shell and existing PDFium/qpdf/Tesseract/Labelize/current-writer capabilities. F13–F18 are incremental product slices, not a rewrite; each slice must independently pass cause-focused tests, full suite and exact-head CI before the next slice becomes authoritative.

**Tech Stack:** Windows x64, C#, .NET 10, WPF, PDFium, PDFsharp only in its existing narrow approved roles, qpdf 12.4.2, Tesseract local OCR, Labelize 1.7.0.

**Spec:** `docs/product/2026-10-14-local-suite-expansion.md`

## Global Constraints

- KISS: simple, efficient, optimized, effective; A→B.
- Anti-noise: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Anti-patch: `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.
- Local/offline normal operation; no mandatory backend, cloud, account, API or AI.
- Preserve source documents; use explicit Save As/copy semantics for publication.
- No second general PDF engine without explicit capability evidence and approval.
- No new UI framework for the shell unless a future capability gate proves native WPF insufficient.
- No merge to `main` without explicit user approval.
- Current UI-4 P1 defects must close before product-code implementation of F13.

## Review Focus

1. **Mode/tool state leakage:** switching Read/Edit/Sign/Organize must not leave hidden overlays, dirty-state guards or contextual panels active.
2. **Small/scaled screens:** shell must remain usable at 1366×768/100% and one real scaled Windows profile without clipped primary actions.
3. **Unsupported PDF objects:** Edit must explain limitations instead of silently failing or pretending success.
4. **Source protection:** every modifying tool must preserve the original and reject silent destination overwrite/races.
5. **Offline/dependency absence:** optional integrations must fail clearly when an optional local dependency is absent and must not disable core PDF functionality.

---

## Gate 0 — Close current UI-4 P1 before F13 product code

**Owner:** current release/UI stabilization, not F13.

- [ ] Reproduce and root-cause V10–V12 signature dialogs on real Windows.
- [ ] Reproduce user-reported Edit/Save flow with synthetic representative PDF.
- [ ] Classify V08 label disabled controls as expected state or product bug.
- [ ] Execute V09 protected-PDF flow if qpdf fixture generation is available locally.
- [ ] Complete still-missing V04 1366×768 and V05 scaled-profile evidence.
- [ ] Accept only minimal root-cause fixes; full suite + exact-head CI after any product-code change.
- [ ] UI-4 becomes PASS/PASS-WITH-P2 only with real evidence and no P0/P1.

**Checkpoint:** update `docs/release/2026-10-14-ui-visual-qa.md` + `.planning/STATE.md`. Do not begin F13 implementation before this gate is GREEN.

---

# F13 — Product Shell / Dashboard

**Purpose:** reorganize existing capability discovery without changing PDF engines.

## F13.1 — Tool catalog contract

**Likely files:**
- Create: `src/SGPdf.App/Shell/ToolDescriptor.cs`
- Create: `src/SGPdf.App/Shell/ToolCatalog.cs`
- Test: `tests/SGPdf.App.Tests/Shell/ToolCatalogTests.cs`

**Interface target:** a deliberately small immutable descriptor containing tool id, title, category, icon key, short description and local action/availability identity. No plugin architecture, reflection registry, DI framework or dynamic loading.

- [ ] Write RED tests proving unique ids, deterministic order, no duplicate actions and required core tools.
- [ ] Implement the smallest static catalog.
- [ ] Run focused tests.
- [ ] Run full suite.
- [ ] Exact-head CI.
- [ ] Checkpoint commit.

## F13.2 — Home dashboard

**Likely files:**
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Create only if needed to keep responsibilities bounded: `src/SGPdf.App/Shell/HomeDashboard.xaml` + `.xaml.cs`
- Reuse existing recent-files/open-file logic.
- Test: shell/UI contract tests under `tests/SGPdf.App.Tests/`.

Required behavior:
- primary Open PDF;
- drag/drop local files;
- recent local documents;
- category cards sourced from the static catalog;
- icon + title + concise description;
- no network/login/advertising placeholders.

- [ ] RED shell contract.
- [ ] Minimal native-WPF implementation.
- [ ] Focused tests.
- [ ] Full suite.
- [ ] Exact-head CI.
- [ ] Real Windows visual smoke.
- [ ] Checkpoint.

## F13.3 — Document workspace navigation

**Likely files:** existing MainWindow shell partials/XAML; introduce one focused shell partial/control only if it reduces existing file responsibility.

Required behavior:
- left primary tool rail: Read/Edit/Comment/Sign/Organize/Forms/OCR/Redact/Compare/Convert/Protect/More;
- center document canvas preserved;
- right contextual properties panel preserved/reused;
- compact top document actions;
- no duplicated global mode strip.

- [ ] RED mode-navigation contract.
- [ ] Reuse existing mode handlers; do not duplicate business flows.
- [ ] Prove existing handlers still fire.
- [ ] Full suite + exact-head CI.
- [ ] Windows visual QA light/dark + keyboard + small/scaled profiles.
- [ ] Checkpoint.

## F13.4 — Official app icon

- [ ] Confirm actual user-approved source image is available.
- [ ] Materialize multi-resolution `.ico` from that source only.
- [ ] Apply to project/window/executable surfaces supported by WPF/MSBuild.
- [ ] Verify executable, window, Alt+Tab and taskbar on Windows.
- [ ] Carry the same asset into later installer work; no alternate logo.
- [ ] Checkpoint.

**F13 exit:** coherent home + document workspace, no detached handlers, no P0/P1 visual/functional regressions, exact-head CI GREEN and real Windows shell evidence.

---

# F14 — Productize Existing Capabilities

**Purpose:** expose existing F0–F12 features through the new shell before adding new engines.

Create a dedicated F14 implementation plan before coding. Divide work into reviewer-sized slices:

- [ ] F14.1 Organize / page operations routing.
- [ ] F14.2 OCR / Redact / Protect / watermark / numbering routing.
- [ ] F14.3 Comment / Sign / Forms routing.
- [ ] F14.4 Compare / Convert / Batch / QR / ZPL routing.
- [ ] F14.5 Remove redundant legacy entry points only after proving the new route covers them and deletion is safe.

**F14 exit:** every already-supported product capability is discoverable from Home/sidebar with no duplicate implementation and no removal of a still-needed fallback path.

---

# F15 — Edit V3

**Purpose:** make existing conservative text/image editing understandable and reliable before expanding raw editing capability.

Create a separate `F15 Edit V3` spec/plan. Required slices:

- [ ] F15.1 explicit Edit state + object hover/selection affordance.
- [ ] F15.2 contextual text properties with `Editable / Limited / Read-only` status and reason.
- [ ] F15.3 contextual image properties reusing existing image workspace.
- [ ] F15.4 visible Undo/Redo only where backed by actual workspace history.
- [ ] F15.5 persistent `Guardar como...` command + dirty/saving/saved/error state.
- [ ] F15.6 scanned-document guidance to OCR, never hidden automatic OCR.
- [ ] F15.7 representative real-PDF Windows QA + reopen verification in external readers where appropriate.

**F15 exit:** user can tell what is editable, make a supported edit, save a copy, reopen it and see the materialized change; unsupported objects fail clearly rather than silently.

---

# F16 — New Local Utilities

Each utility is an independent capability-gated slice; do not batch them into a generic conversion framework.

## F16.1 PDF → JPG / export studio
Reuse existing PDFium render path and PDF→PNG service. Add format/range/DPI/quality only where the existing abstraction can be safely extended.

## F16.2 Image → PDF
Local JPG/PNG input, deterministic ordering, page/media options, explicit output.

## F16.3 Crop PDF
Visual crop selection, page/range targeting, transactional copy, reopened-output validation.

## F16.4 Authorized unlock
Known-password only. Reuse qpdf where capability is demonstrated. No password cracking.

## F16.5 Safe Optimize
Expose only the already-characterized optimization behavior that can be validated; show before/after size and avoid unsupported marketing claims.

## F16.6 Image watermark
Only after writer preservation/capability gate proves image overlay can be safely materialized.

For **every** F16 slice:
- [ ] capability gate;
- [ ] RED test;
- [ ] minimal implementation;
- [ ] output validation/reopen;
- [ ] source-preservation/race tests;
- [ ] full suite;
- [ ] exact-head CI;
- [ ] real Windows smoke;
- [ ] checkpoint before next utility.

---

# F17 — Professional UX Expansion

Independent plans required.

## F17.1 Compare V2
- side-by-side A/B;
- optional synchronized page/zoom/scroll;
- overlay/transparency only if current compare/render signals support it cleanly;
- change navigation panel;
- reuse existing compare core; no AI/semantic service.

## F17.2 Forms V2
- existing AcroForm `/Btn` and `/Ch` capability gate;
- implement supported existing fields before any designer;
- optional flatten only after preservation proof.

## F17.3 Simple form designer
Only after Forms V2 is stable. Text/check/radio/combo/signature fields; no XFA/JavaScript/calculation engine baseline.

## F17.4 Signature UX completion
Typed signature/initials/date may reuse the existing visual placement pipeline if it remains simple. PFX/P12 stays a separate `Digital signature` concept.

---

# F18 — Optional Local Integrations

Each is optional and must not become a mandatory runtime dependency.

- [ ] F18.1 detect local LibreOffice and capability-gate Office→PDF.
- [ ] F18.2 scanner integration after real hardware/API evaluation.
- [ ] F18.3 local HTML→PDF only after renderer/licensing/offline capability decision.

Core SG PDF must remain functional when none of these integrations is installed.

---

# Cross-phase checkpoint protocol

After every accepted slice record:
- branch + exact HEAD;
- files changed;
- root/value decision;
- focused tests;
- full suite totals;
- exact-head CI run/job;
- real/manual status explicitly PASS/FAIL/NOT RUN;
- remaining P0/P1/P2;
- next bounded task;
- `main` untouched unless explicitly approved.

Do not create Markdown-per-commit noise. Update the phase plan/state and create a historical checkpoint only at a real decision/phase boundary.

# Execution recommendation

Use **native/ChatGPT-led implementation** for design, repository changes, tests, GitHub audit and integration. Use Codex only for work that materially requires the user's Windows machine, local GUI interaction, hardware, private fixtures or installed-app behavior. This minimizes token cost and prevents duplicated work.
