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

Last verified GREEN product anchor before current document-shell work:
`0abae7ceb5f08611180c883346e21589d1509664`

## DG-0 SG Design Governance — CLOSED / GREEN

Validated authority:
- `docs/design/SGPDF_DESIGN_LAW.md`;
- `.agents/skills/sgpdf-design/SKILL.md`;
- focused design references;
- `DesignGovernanceContractTests`.

Implementation anchor:
`3011f700e9014d72951314291bab4c3a67b89c8c`

Closure checkpoint:
`9ea973d79b4cd49946022cbbd174678a1280821f`

DG-0 added no runtime feature, UI framework or production dependency.

## T9.1 document-shell realignment

Canonical branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

Historical R0/R1 RED checkpoint retained for traceability:
`bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`

### R1 reader multi-document core — GREEN

Verified checkpoint:
`454dcadb6eca13ab98b5d219146dd10ec25af78d`

Exact-head CI:
- run `39004872760`;
- job `117315172824`;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — 30 s;
- tests: `1266 passed / 0 failed / 4 skipped / 1270 total`;
- overall SUCCESS.

Implemented scope:
- multi-select Open PDF dialog;
- one `PdfDocumentWorkspace` per open PDF;
- `OpenDocuments` registry + `ActiveDocument`;
- no implicit disposal of previous PDF merely because another is opened;
- reader state page/zoom/vertical offset preserved per workspace;
- tracked sessions disposed on explicit removal/window shutdown;
- password/open failure preserves/restores previous active document where possible.

The four skipped tests deliberately reserve future T9.1 contracts:
- R2 tabs;
- R3 document rail official artwork;
- R4 collapsible Pages/Bookmarks drawer;
- R5 PDF context menu.

### R1B — per-document pending state + close guard — NEXT

Reason:
The user requires switching among multiple PDF tabs without silently discarding pending edits/signatures/comments/organize work. Current mode owners still include global MainWindow state and `ActivateDocumentAsync()` resets some feature state. Exposing tabs now would create a data-loss risk.

R1B must:
1. inventory pending state for Edit, Sign, Comments, Organize and Forms;
2. define the smallest per-document state owner/snapshot compatible with existing feature owners;
3. preserve pending state when switching documents where technically supported;
4. add explicit dirty-close behavior;
5. use focused RED→GREEN tests;
6. pass full suite + startup smoke + exact-head CI;
7. checkpoint before R2.

No DI container, event bus, ModeManager or document framework.

## Remaining T9.1 slices

### R2 — top shell + real document tabs

Only after R1B GREEN:
- tab strip with file name + close `×`;
- add/open `+`;
- active tab switches `ActiveDocument`;
- shell hierarchy aligned with approved mockup and Design Law.

### R3 — document tool rail

- 96 px rail aligned with approved mockup;
- reuse approved official artwork where mapped;
- no return to legacy monochrome glyphs for tools with approved art;
- selected/hover/disabled states accessible and coherent.

### R4 — Pages / Bookmarks drawer

- collapsed by default;
- explicit expand/collapse;
- preserve useful workspace width;
- reuse thumbnail renderer;
- audit whether Bookmarks has real outline binding before calling it functional.

### R5 — context menus

- right-click menu on PDF workspace/page;
- contextual image menu when applicable;
- reuse existing command owners; no duplicate engines/operations.

### R6 — image-selection rendering

- reproduce reported blue-tint behavior;
- code contract currently declares transparent selection fill, so full-image tint is treated as a defect until root cause is found;
- fix cause, not symptom.

### R7 — final visual / functional QA

Must cover:
- Home Light/Dark;
- PDF-open Light/Dark;
- two or more PDF tabs;
- switch preserving per-document state;
- drawer collapsed/expanded;
- context menus;
- image selection;
- real 125% Windows scaling / DPI evidence;
- P0=0, P1=0 for T9.1 exit.

### T9.2

Do not start automatically. It becomes eligible only after T9.1 is actually closed and its required evidence exists.

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

Execute **T9.1-R1B per-document pending state + close guard** on the canonical branch. Do not merge to `main`, do not begin R2 before R1B is safe, do not begin T9.2, and do not thaw F14–F18.
