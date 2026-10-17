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

Last verified GREEN anchor before the current document-shell R1:
`0abae7ceb5f08611180c883346e21589d1509664`

Exact-head CI:
- run `39000181654` — SUCCESS.

Current R1 document-shell/multi-document branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

Frozen incomplete checkpoint:
`bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`

Its run `39001470186` is FAILURE, so it is not a GREEN baseline.

## Current bounded block — DG-0 SG Design Governance

Branch:
`feat/f13-dg0-sg-design-governance-20261017`

Base:
`0abae7ceb5f08611180c883346e21589d1509664`

Purpose:
- make the Ley SG de Diseño durable;
- create a project-owned design skill;
- align AGENTS/README/STATE/ROADMAP/current architecture context;
- prevent future agents from routing back to stale F12 UI instructions;
- establish progressive-disclosure rules before further panel/menu/document-shell work.

DG-0 changes governance only. It must not alter runtime/product behavior or “fix” R1 while the governance gate is being validated.

Exit:
- static governance contracts GREEN;
- full suite GREEN;
- exact-head CI GREEN;
- KISS/anti-noise audit PASS;
- no merge;
- main unchanged;
- R1 unchanged;
- checkpoint recorded.

## After DG-0

### 1. Resume T9.1 R1

Resume from the frozen R1 checkpoint using the validated design governance. Do not bypass the existing RED contracts and do not restart from a stale F12 branch.

For every UI surface changed in R1 or later T9.1 work:

`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

The mandatory skill is `.agents/skills/sgpdf-design/SKILL.md`.

### 2. Complete remaining T9.1 slices

Continue the already approved T9.1 document-shell/UI sequence (remaining R2–R7 as defined by its current approved spec/plan). Do not invent new scope from this roadmap.

Each slice exits through:
- focused RED→GREEN where behavior changes;
- full suite;
- exact-head CI;
- required Light/Dark/125%/keyboard/state visual QA where applicable;
- no open P0/P1 for its exit gate.

### 3. T9.2

Do not start automatically. It becomes eligible only after T9.1 is actually closed and its required evidence exists.

### 4. Release / installer / remaining real QA

Historical Packs A–D and installer/runtime/license gates remain useful evidence/requirements, but they no longer define the immediate execution order ahead of active F13 work. Resume them when the active F13 plan reaches its release gate.

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

Finish **DG-0** and produce a GREEN exact-head checkpoint. Then resume R1; do not merge to `main`, do not begin T9.2, and do not thaw F14–F18 without explicit routing/approval.
