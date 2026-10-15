---
gsd_state_version: '1.1'
status: ui4-final-windows-qa-pending-f13-ab-approved
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: UI-4 Gate 0 final Windows verification before F13
---

# Project State — SG PDF Editor

**Updated:** 2026-10-15

## 1. Mandatory laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → TRACE/CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → CI → REMOVE REDUNDANT WORKAROUND`.

`main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`. No merge without explicit user approval.

## 2. Functional baseline F0–F12 — CLOSED

Functional anchor: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.
Historical CI: `1202/1202 PASS`.
Do not reopen F0–F12 without reproducible regression/new explicit requirement.

## 3. UI-4 Gate 0 — AUTOMATED FIXES GREEN / FINAL WINDOWS QA PENDING

Original tested UI base: `abd52d05279e3e656723ad4ff5a21d36de91508e`.

### Signature / Dark Mode

Root cause proven: AcroForm `ScrollViewer` remained visible over shared properties and intercepted signature hit-testing.

Remote reconstructed fix branch:
`feat/fix-ui4-signature-remote`

Accepted remote fix head:
`8c4a0f2619ca0a4835f6160ce4958d2d1b981435`

Automated result: `1215/1215 PASS`.
Windows reverify already confirmed V10–V12 dialogs reopen.

### V08 Labels

Root cause proven: ZPL action state depended on `_isBusy`, but `SetBusy()` refreshed only viewer controls; after ZPL load completed, Export/Print retained stale disabled state.

Branch:
`feat/fix-ui4-v08-label-actions`

Current head:
`b4f19fcd5f2854c9025e60385e3b7819bd798a29`

GREEN change: when ZPL workspace exists, `SetBusy()` also refreshes label properties/actions.

Cause test: `MainWindowZplBusyCompletionUiTests.EndingBusyState_RefreshesValidThermalLabelActions`.

CI run `38681417442`, job `116246600509`:
- locked restore PASS;
- Release build PASS, 0 errors;
- suite `1216/1216 PASS`;
- only known PKCS 8.0.1 warnings remain on this branch;
- PR workflow checked merge-ref `2bff61420498287b1025c3346c705fa36d01cbf4` built from current branch head + base.

Checkpoint:
`docs/history/2026-10-15-ui4-v08-green-checkpoint.md`.

### Remaining Gate 0 work — Windows only

Run on exact branch head `b4f19fcd5f2854c9025e60385e3b7819bd798a29`:
- V04 1366×768;
- V05 Windows scaling;
- V08 manual Thermal actions;
- V09 password dialog visual if practical;
- V10–V12 final confirmation;
- V13 name/library path;
- V14 signature overlay + Save As + reopen;
- representative Edit/Save + reopen classification.

Prompt prepared:
`SG_PDF_CODEX_UI4_FINAL_WINDOWS_GATE_2026-10-15.md` (conversation artifact).

Gate 0 becomes GREEN only with no remaining P0/P1.

## 4. PKCS hygiene — GREEN isolated

Branch `feat/fix-pkcs-hygiene`, HEAD `781cd6060121367f4d51efbe2724d820d1dfb842`.
CI `1214/1214 PASS`.
PKCS 8.0.2 is test-graph-only; app/runtime unchanged. Integrate conservatively after UI stabilization.

## 5. F13 — Product Shell / Dashboard — A+B APPROVED

A visual spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

B technical plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Mockup reference:
`docs/design/2026-10-15-f13-approved-mockup-reference.md`

F13 starts immediately after Gate 0 GREEN + release-truth consolidation.

Key outputs:
- approved Home dashboard;
- 96 px left rail;
- compact document workspace;
- 320 px contextual right panel;
- Light/Dark;
- native vector icons;
- safe removal of redundant legacy mode strip after equivalence.

### Official application icon

F13.4. Required on window/exe/taskbar/Alt+Tab and later shortcut/installer.
Still source-gated: must use actual user-approved logo/icon source, not mockup placeholder.

## 6. F14 — Productize existing F0–F12 capabilities

After F13 shell is stable.
Expose current owners through the new UI without duplicating logic:
- Organize/page operations;
- OCR/Redact/Protect/watermark/numbering;
- Comment/Sign/Forms;
- Compare/Convert/Batch/QR/ZPL;
- safe-transition guards preserved.

## 7. F15 — Edit V3

Approved design/spec ready.
Goal: direct object selection, contextual properties, honest editable/limited/read-only states, improved image/text editing UX, reliable `Guardar como...`, reopen verification.
Preserve current workspaces/writer; no second engine.

## 8. F16–F18 expansion

**F16 new local utilities:** PDF→JPG/export studio, Image→PDF, Crop, authorized Unlock, safe Optimize, image watermark after capability gate.

**F17 professional UX:** Compare V2, more AcroForm types/simple designer, signature UX completion.

**F18 optional integrations:** detected local LibreOffice→PDF, scanner, local HTML→PDF after separate gates.

## 9. Release sequence

1. Finish final UI-4 Windows gate.
2. Consolidate accepted UI fixes + isolated PKCS release truth.
3. F13 approved redesign + official icon.
4. F14 expose/productize all existing capabilities.
5. F15 Edit V3.
6. Reassess first public release after F15.
7. F16–F18 may continue after that release or be included only by explicit scope decision.
8. Publish/installer on the intended stable baseline.
9. `main` only with explicit user approval.

## 10. Immediate next action

Run the final Windows QA prompt on `b4f19fcd...`; audit result; if no P0/P1, close Gate 0 and start F13 implementation natively.
