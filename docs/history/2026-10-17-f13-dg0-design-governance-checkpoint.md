# F13 / DG-0 — SG Design Governance Checkpoint

**Date:** 2026-10-17  
**Status:** GREEN / CLOSED pending only final checkpoint exact-head CI on the commit that contains this record  
**Scope:** project governance and design authority only; no runtime/product behavior changes.

## Branch and ancestry

Branch:
`feat/f13-dg0-sg-design-governance-20261017`

Validated base:
`0abae7ceb5f08611180c883346e21589d1509664`

That base is the last verified GREEN anchor immediately before the current R1 document-shell/multi-document RED work.

`main` remains read-only at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## RED evidence

RED contract commit:
`57b1eeb22302b33619e58246a3f3dcb1b8970363`

Exact-head CI:
- run `39002512451`;
- job `117308235572`;
- Build: PASS;
- Smoke app startup: PASS;
- Test: FAIL;
- overall: FAILURE.

The failure was expected because the contract required governance artifacts and authority wiring that did not yet exist.

## GREEN implementation evidence

GREEN implementation commit:
`3011f700e9014d72951314291bab4c3a67b89c8c`

Exact-head CI:
- run `39002956513`;
- job `117309529279`;
- Build: PASS;
- Smoke app startup: PASS;
- Test: PASS;
- overall: SUCCESS.

## Implemented governance

Created:
- `docs/design/SGPDF_DESIGN_LAW.md`;
- `.agents/skills/sgpdf-design/SKILL.md`;
- `.agents/skills/sgpdf-design/references/properties-panels.md`;
- `.agents/skills/sgpdf-design/references/navigation-menus.md`;
- `.agents/skills/sgpdf-design/references/forms-copy.md`;
- `.agents/skills/sgpdf-design/references/wpf-accessibility.md`;
- `.agents/skills/sgpdf-design/references/review-checklist.md`;
- `tests/SGPdf.App.Tests/DesignGovernanceContractTests.cs`;
- `docs/superpowers/plans/2026-10-17-sg-design-governance.md`.

Aligned current authority:
- `AGENTS.md`;
- `README.md`;
- `.planning/STATE.md`;
- `.planning/ROADMAP.md`;
- `docs/MASTER_CONTEXT.md`.

## Frozen law

`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

This is now the project-owned design law for visible/interactable work.

The anti-patch process remains a separate guardrail and is not renamed as a numbered law.

## Upstream absorption boundary

UI/UX Pro Max was used as an external design reference. SG PDF absorbs useful principles such as progressive disclosure, visible labels, near-field validation, navigation hierarchy, accessibility and WPF implementation guidance.

Not imported:
- upstream repository wholesale;
- style/product/palette datasets;
- web/mobile-specific rules as defaults;
- animation libraries/presets;
- new runtime or UI framework dependencies.

## ZPL design decision

The right properties panel remains functionally capable but is now explicitly governed by progressive disclosure. Future implementation must avoid exposing all quantity, size, resolution, sheet, grid, rotation, margin and gap controls at once when the selected workflow does not need them.

This checkpoint **does not claim the ZPL panel itself has already been redesigned**. It establishes the mandatory design authority under which that future work must be implemented and verified.

## Audit — KISS / anti-noise / anti-patch / design

- KISS: PASS — one local project skill, one design-law authority, focused references; no framework/service/runtime addition.
- Anti-noise: PASS — upstream content was distilled instead of vendored; references are loaded by concern instead of one giant design document for every task.
- Anti-patch taxonomy: PASS — guardrail remains separate and root-cause oriented.
- Design Law: PASS — exact sequence is contract-tested and wired through project authorities.
- Product code behavior: unchanged by DG-0.
- New production dependency: none.
- New UI framework: none.

## Explicit state

```text
DG0_GREEN_IMPLEMENTATION=3011f700e9014d72951314291bab4c3a67b89c8c
DG0_GREEN_CI_RUN=39002956513
DG0_GREEN_CI_JOB=117309529279
merge=NO
main_changed=NO
r1_modified=NO
r1_resumed=NO
t9_2_started=NO
f14_f18_frozen=YES
manual_visual_qa_for_DG0=NOT_APPLICABLE_DOCS_GOVERNANCE_ONLY
```

Current R1 remains frozen/incomplete at:
`bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`

## Next bounded action

After this checkpoint commit passes its own exact-head CI, DG-0 is fully closed and the next bounded action is to resume R1 from its frozen checkpoint while carrying this validated governance. Do not skip existing R1 RED contracts, do not auto-start T9.2, and do not merge to `main`.
