# SG PDF — DG-0 Design Governance Implementation Plan

**Goal:** Convert the approved SG Design Law into durable project governance before continuing T9.1 document-shell UI work.

**Architecture:** Documentation-first, project-owned skill. No runtime dependency, package, UI framework, service, or upstream vendoring. Static contract tests only verify that governance remains discoverable and wired into authority files.

**Tech Stack:** Markdown governance + xUnit static repository contracts. Product stack remains C#/.NET 10/WPF/Windows x64.

**Spec:** `docs/design/SGPDF_DESIGN_LAW.md` (created in Task 2 from the user-approved law).

**Global constraints:**
- KISS and anti-noise remain mandatory.
- Anti-patch remains a guardrail, not a numbered law.
- No product/UI behavior changes in DG-0.
- Do not modify `main`.
- Do not modify the frozen/incomplete R1 branch while validating DG-0.
- Upstream UI/UX Pro Max is inspiration/reference only; do not vendor its datasets or mobile/web rules wholesale.

**Review focus:** governance drift, contradictory authority, accidental UI framework expansion, duplicated rules, and any statement that upgrades incomplete/manual QA to PASS.

---

## Task 1 — Governance RED contract

**Files:**
- Create: `tests/SGPdf.App.Tests/DesignGovernanceContractTests.cs`

**Steps:**
1. Add repository-level tests that require the project-owned design skill, exact SG Design Law sequence, AGENTS/README wiring, and current F13/DG-0 authority overlay.
2. Push the test-only commit on the isolated DG-0 branch.
3. Verify exact-head CI fails for the missing governance artifacts/references, not because build or smoke broke.

**Completion:** RED is confirmed on a branch whose base (`0abae7ceb5f08611180c883346e21589d1509664`) has green exact-head CI.

## Task 2 — Minimal GREEN governance

**Files:**
- Create: `.agents/skills/sgpdf-design/SKILL.md`
- Create: `.agents/skills/sgpdf-design/references/properties-panels.md`
- Create: `.agents/skills/sgpdf-design/references/navigation-menus.md`
- Create: `.agents/skills/sgpdf-design/references/forms-copy.md`
- Create: `.agents/skills/sgpdf-design/references/wpf-accessibility.md`
- Create: `.agents/skills/sgpdf-design/references/review-checklist.md`
- Create: `docs/design/SGPDF_DESIGN_LAW.md`
- Modify: `AGENTS.md`
- Modify: `README.md`
- Modify: `.planning/STATE.md`
- Modify: `docs/MASTER_CONTEXT.md`

**Steps:**
1. Materialize only the approved sequence: `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.
2. Make the skill mandatory for work that changes appearance, interaction, menus, bars, panels, dialogs, forms, copy, icon presentation, layout, states, accessibility, or visual hierarchy.
3. Encode SG-specific progressive disclosure, including the ZPL properties-panel rule: do not present unrelated sheet/layout controls when the selected workflow does not need them.
4. Keep WPF guidance desktop-specific; do not cargo-cult mobile touch/safe-area rules.
5. Update authority files to current F13 Task9 truth while preserving historical material as history.
6. Run exact-head CI. Build, startup smoke, and full test suite must all be green.

## Task 3 — Audit + checkpoint

**Files:**
- Create: `docs/history/2026-10-17-f13-dg0-design-governance-checkpoint.md`

**Steps:**
1. Audit diff for KISS, anti-noise, anti-patch taxonomy, contradictions, and stale authority.
2. Record exact branch/HEAD/CI and explicit `merge=NO`, `main_changed=NO`, `R1_resumed=NO`.
3. Re-run exact-head CI if the checkpoint itself changes tested governance surfaces.
4. Hand back the validated governance anchor; only then resume R1 on its own line of work.
