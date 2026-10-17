# Roadmap — SG PDF Editor

> Operational authority: exact GitHub HEAD/CI → `.planning/STATE.md` → this roadmap → `.planning/REQUIREMENTS.md` → approved slice specs/plans. Historical/master documents never override newer operational state.

## Current routing — 2026-10-17

F0–F12 remain **CLOSED / FROZEN** for the automated baseline already documented. F13 is active. F14–F18 remain frozen.

`main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` until explicit user approval.

## Phase status

| Phase | Status | Notes |
|---|---|---|
| F0–F12 | CLOSED / FROZEN | automated baseline; real/manual/private/physical gates remain separate where documented |
| F13 | ACTIVE | current product/UI hardening and document-shell work |
| F14–F18 | FROZEN | do not start opportunistically |
| main | LOCKED | no merge without explicit approval |

## F13

Tasks 1–8 are closed by their existing checkpoints.

### Task9

- T9.0 — DONE.
- T9.1 — ACTIVE.
- T9.2 — NOT STARTED.

Last verified GREEN product anchor before current document-shell R1:
`0abae7ceb5f08611180c883346e21589d1509664`

Exact-head CI:
- run `39000181654` — SUCCESS.

## DG-0 SG Design Governance — CLOSED / GREEN

Branch:
`feat/f13-dg0-sg-design-governance-20261017`

Base:
`0abae7ceb5f08611180c883346e21589d1509664`

RED contract:
`57b1eeb22302b33619e58246a3f3dcb1b8970363` → CI run `39002512451` with Build/Smoke PASS and expected Test FAIL.

GREEN implementation:
`3011f700e9014d72951314291bab4c3a67b89c8c` → CI run `39002956513` SUCCESS, including Build/Smoke/Test PASS.

Closure record:
`docs/history/2026-10-17-f13-dg0-design-governance-checkpoint.md`

DG-0 established:
- `docs/design/SGPDF_DESIGN_LAW.md`;
- `.agents/skills/sgpdf-design/SKILL.md`;
- focused references for properties, navigation, copy/forms, WPF/accessibility and final review;
- static governance contracts;
- current F13 routing in AGENTS/README/STATE/ROADMAP/MASTER_CONTEXT.

It did not add a runtime feature, UI framework or production dependency.

## Current bounded block — Resume R1 document shell

Branch already holding the intentional R1 RED:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

Frozen RED checkpoint:
`bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`

Its run `39001470186` is FAILURE, so it is not a GREEN baseline.

Next work is to resume those existing RED contracts while deliberately carrying the validated design governance. Do not restart from stale F12 routing and do not silently discard the R1 test intent.

Before editing any R1 XAML/copy/navigation/interaction, execute:

`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

Mandatory skill:
`.agents/skills/sgpdf-design/SKILL.md`

## After R1

### Remaining T9.1 slices

Continue the approved T9.1 document-shell/UI sequence (remaining R2–R7 as defined by its current approved spec/plan). Do not invent new scope from this roadmap.

Each applicable UI slice exits through:
- focused RED→GREEN where behavior changes;
- full suite;
- exact-head CI;
- required Light/Dark/125%/keyboard/state visual QA;
- no open P0/P1 for its exit gate.

### T9.2

Do not start automatically. It becomes eligible only after T9.1 is actually closed and its required evidence exists.

### Release / installer / remaining real QA

Historical Packs A–D and installer/runtime/license gates remain evidence/requirements, but they do not override active F13 routing. Resume them when the active F13 plan reaches its release gate.

## Engineering governance

### KISS
`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.

### Anti-noise
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

### Ley SG de Diseño
`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.

### Guardrail anti-parches
Separate operational guardrail:
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Design-specific routing

Any task involving menus, toolbars, Home, side rails, properties panels, dialogs, forms, copy, icon presentation, visual states, accessibility, alignment or layout must consult:
- `docs/design/SGPDF_DESIGN_LAW.md`;
- `.agents/skills/sgpdf-design/SKILL.md`;
- only the relevant reference under `.agents/skills/sgpdf-design/references/`.

The ZPL right panel is the canonical anti-pattern to avoid: supported settings are preserved, but irrelevant/advanced controls must be disclosed contextually rather than exposed as one endless property list.

## Next bounded task

Resume **R1 document-shell/multi-document** from its frozen RED checkpoint under the now-GREEN DG-0 governance. Do not merge to `main`, do not begin T9.2, and do not thaw F14–F18.
