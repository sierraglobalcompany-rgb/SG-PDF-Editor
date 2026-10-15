---
gsd_state_version: '1.1'
status: ui4-green-release-truth-consolidation-before-f13
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: release-truth consolidation before F13
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

## 3. UI-4 Gate 0 — GREEN

Accepted UI head:
`b4f19fcd5f2854c9025e60385e3b7819bd798a29`

Automated:
- locked restore PASS;
- Release build PASS;
- suite `1216/1216 PASS`;
- CI run `38681417442` SUCCESS.

Windows final QA:
- V04 1366×768 PASS;
- V05 125% scaling PASS;
- V08 Thermal actions + PDF export PASS;
- V10 Draw Signature PASS;
- V11 Signature from Photo PASS;
- V12 Signature Library PASS;
- V13 signature-name dialog PASS;
- V14 signature overlay + Save As + reopen PASS;
- representative Edit/Save + reopen PASS;
- no P0/P1/P2 reproduced.

V09 manual password entry was NOT RUN because the local computer-use workflow would not type authentication credentials. This is non-blocking because the accepted head contains automated password-path coverage for wrong+cancel preserving workspace, wrong→correct retry and password non-persistence.

UI-4 is therefore closed GREEN. Do not reopen without a reproducible regression.

## 4. PKCS hygiene — GREEN isolated / consolidation next

Branch `feat/fix-pkcs-hygiene`, HEAD `781cd6060121367f4d51efbe2724d820d1dfb842`.
CI `1214/1214 PASS`.
PKCS 8.0.2 is test-graph-only; app/runtime unchanged.

Immediate next task: consolidate accepted UI head + isolated PKCS hygiene into one release-truth branch, audit diff, run CI, then use that accepted head as F13 base.

## 5. F13 — Product Shell / Dashboard — A+B APPROVED

A visual spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

B technical plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Mockup reference:
`docs/design/2026-10-15-f13-approved-mockup-reference.md`

F13 begins immediately after release-truth consolidation.

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

1. UI-4 GREEN — CLOSED.
2. Consolidate accepted UI fixes + isolated PKCS release truth.
3. F13 approved redesign + official icon.
4. F14 expose/productize all existing capabilities.
5. F15 Edit V3.
6. Reassess first public release after F15.
7. F16–F18 may continue after that release or be included only by explicit scope decision.
8. Publish/installer on the intended stable baseline.
9. `main` only with explicit user approval.

## 10. Immediate next action

Create/audit release-truth branch from `b4f19fcd...`, apply only the previously proven PKCS 8.0.2 test-graph hygiene delta, run CI, then start F13 Task 1 from that accepted head.
