# Roadmap — SG PDF Editor

**Updated:** 2026-10-14

> Operational authority: live GitHub exact refs/CI → `docs/history/2026-10-14-post-ui4-expansion-checkpoint.md` → `.planning/STATE.md` → this roadmap → product spec + focused slice plans. Older master/history docs are context only when not superseded.

## Product laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Protected refs

`main` is read-only at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge to `main` without explicit user approval.

Current exact UI-4 tested branch/head:
- branch `feat/release-ui-modernization`;
- HEAD `abd52d05279e3e656723ad4ff5a21d36de91508e`.

Planning/checkpoint branch:
`plan/post-ui4-f13-f18-expansion-20261014`

## Baseline

F0–F12 automated functional baseline remains closed evidence. It is not reopened wholesale by the new roadmap.

Historical functional anchor:
`8e1845d6d8fe72374d7a80994d8500b6d36077cc`

Historical functional CI:
- run `38555829715`;
- job `115824598759`;
- 1202/1202 PASS;
- Release build 0 warnings / 0 errors.

The UI modernization line subsequently reached 1214/1214 automated tests before real UI-4.

---

# Current blocking gate — UI-4 stabilization

## Status: FAIL-P1

Real Windows evidence already exists; do not describe UI-4 as NOT RUN.

PASS:
- light shell;
- dark shell after local uncommitted strip-style fix;
- PDF render/navigation/zoom;
- keyboard traversal/focus;
- disabled-state visuals.

Open/failed:
- V04 exact 1366×768 — NOT RUN;
- V05 scaled Windows profile — NOT RUN;
- V08 label export/print disabled — classification pending;
- V09 protected PDF — NOT RUN;
- V10 Draw Signature — FAIL-P1;
- V11 Signature from Photo — FAIL-P1;
- V12 Signature Library — FAIL-P1;
- V13 name-dialog path — NOT RUN;
- V14 functional smoke — FAIL-P1 due signature flows.

User manual findings also elevate Edit/Save to a focused runtime investigation because editing/saving was not satisfactory in real use.

## Current bounded work

1. Windows runtime trace for V10–V12.
2. Edit/Save real reproduction + saved-copy reopen verification.
3. V08 expected-state vs product-bug classification.
4. V09 with existing qpdf if viable.
5. Minimal root-cause fix only if demonstrated.
6. Full suite + exact-head CI.
7. Rerun affected real UI cases plus missing V04/V05.
8. Close UI-4 only with no P0/P1.

Do not begin F13 product-code implementation until this gate is GREEN.

---

# Dependency hygiene

PKCS task is GREEN on isolated branch but not integrated:
- branch `feat/fix-pkcs-hygiene`;
- HEAD `781cd6060121367f4d51efbe2724d820d1dfb842`;
- run `38645380308` / job `116130807039`;
- locked restore PASS;
- Release build 0 warnings / 0 errors;
- 1214/1214 PASS.

Integrate conservatively after UI stabilization. Do not claim it is already in the release/UI branch.

---

# Approved product expansion

Spec:
`docs/product/2026-10-14-local-suite-expansion.md`

Cross-phase roadmap:
`docs/superpowers/plans/2026-10-14-f13-f18-local-suite-expansion-roadmap.md`

The product direction is a **local-first desktop PDF suite** with iLovePDF-style discoverability and an Acrobat-inspired left-tool/right-properties document workspace, adapted to SG PDF's own identity and constraints.

No AI, advertising, mandatory login, backend or cloud baseline.

---

# F13 — Product Shell / Dashboard

**Goal:** reorganize existing functionality before adding more engines.

### F13.1 Static tool catalog
Small deterministic descriptor list only; no plugin framework/reflection/DI architecture.

### F13.2 Home dashboard
- Open PDF;
- drag/drop;
- recent local files;
- categorized tool cards;
- icon + title + short description;
- no remote content.

### F13.3 Open-document workspace
- left main tool rail;
- compact top document actions;
- PDF canvas center;
- right contextual properties;
- reuse existing handlers/workspaces;
- retire centered legacy mode strip only after replacement coverage is proven.

### F13.4 Official application icon
Only from actual user-approved source asset. Multi-resolution `.ico`; window/exe/taskbar/Alt+Tab and later shortcut/installer.

**F13 exit:** exact-head CI GREEN + real Windows visual/keyboard/small-screen/scaled-screen evidence + no detached handlers/P0/P1.

---

# F14 — Productize Existing F0–F12 Capabilities

Expose existing engines through the new shell before adding parallel functionality.

Slices:
1. Organize/page operations.
2. OCR/Redact/Protect/watermark/numbering.
3. Comment/Sign/Forms.
4. Compare/Convert/Batch/QR/ZPL.
5. Remove redundant old entry points only after proving safe deletion.

**F14 exit:** current capabilities are discoverable and coherent without duplicated business logic.

---

# F15 — Edit V3

Dedicated product phase because current conservative editing is functional in automation but confusing in real use.

Required slices:
1. explicit Edit state + clear object hover/selection;
2. contextual text properties;
3. `Editable / Limited / Read-only` status + Spanish reason;
4. contextual image properties;
5. visible Undo/Redo only where truly supported;
6. persistent `Guardar como...` + dirty/saving/saved/error state;
7. scanned-document guidance to OCR;
8. representative real-PDF Windows QA + reopen verification.

**F15 exit:** a user can identify a supported object, edit it, save a copy, reopen it and verify the materialized change; unsupported content fails clearly rather than silently.

---

# F16 — New Local Utilities

Independent capability-gated slices; no generic conversion framework.

### F16.1 PDF → JPG / export studio
Extend existing PDFium/PDF→PNG path where safe.

### F16.2 Image → PDF
Local JPG/PNG, deterministic ordering, page/media options, explicit output.

### F16.3 Crop PDF
Visual crop, page/range targeting, transactional output validation.

### F16.4 Authorized unlock
Known password only; use existing qpdf capability where demonstrated. No cracking.

### F16.5 Safe Optimize
Expose only validated optimization behavior; report before/after size without unsupported claims.

### F16.6 Image watermark
Only after writer/preservation gate proves safe overlay support.

Every F16 slice requires its own capability gate, focused RED→GREEN, source/output validation, full suite, exact-head CI, Windows smoke and checkpoint.

---

# F17 — Professional UX Expansion

### F17.1 Compare V2
Side-by-side, synchronized navigation/zoom/scroll where clean, overlay/transparency if supported, change navigation. Reuse current compare core; no AI service.

### F17.2 Forms V2
Capability-gate existing `/Btn` and `/Ch` support before implementing them.

### F17.3 Simple form designer
Only after existing-field Forms V2 is stable. No XFA/JavaScript/calculation engine baseline.

### F17.4 Signature UX completion
Typed signature/initials/date only if they cleanly reuse the visual placement pipeline. PFX/P12 remains a distinct Digital Signature flow.

---

# F18 — Optional Local Integrations

Optional only; never mandatory for core SG PDF.

Potential slices:
- detected local LibreOffice → Office→PDF;
- scanner integration after real hardware/API evaluation;
- local HTML→PDF after renderer/licensing/offline capability decision.

If the optional dependency is absent, core SG PDF must continue working normally.

---

# Explicit deferrals

Do not promise or implement without new evidence/approval:
- high-fidelity PDF→Word/Excel/PowerPoint;
- universal PDF repair;
- PDF/A conformance without standards-compliant validation;
- XFA/complex form JavaScript/calculations;
- cloud signature-request service;
- AI features;
- generic plugin/conversion architecture.

---

# Release / installer order after scope expansion

The prior release sequence must not be followed mechanically after the user explicitly approved a larger shell/product redesign.

New decision:
1. close UI-4 P1;
2. integrate PKCS/document current release truth;
3. stabilize F13 shell and official icon;
4. productize existing features (F14);
5. complete Edit V3 (F15);
6. reassess release target before later F16–F18 scope;
7. only create deterministic publish/installer for the product baseline actually intended to ship.

A later decision may freeze a release after F15 and defer F16–F18; do not assume every future phase must block the first public release.

---

# Checkpoint protocol

At every real slice boundary record:
- branch + exact HEAD;
- files changed;
- value/root-cause decision;
- focused tests;
- full-suite totals;
- exact-head CI run/job;
- real/manual status PASS/FAIL/NOT RUN;
- P0/P1/P2;
- next bounded task;
- whether `main` remained untouched.

Avoid Markdown-per-commit noise. Create durable checkpoints only at real decision/phase boundaries.

# Codex rule

ChatGPT leads repository research, architecture, planning, implementation, tests, GitHub work and documentation. Use Codex only when the task materially requires the user's Windows GUI, hardware/private fixtures or local installed-app behavior.
