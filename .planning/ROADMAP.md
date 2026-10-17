# Roadmap — SG PDF Editor

> Operational authority: exact GitHub HEAD/CI → `.planning/STATE.md` → this roadmap → `.planning/REQUIREMENTS.md` → approved slice specs/plans. Historical documents never override newer operational state.

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

Implementation anchor: `3011f700e9014d72951314291bab4c3a67b89c8c`.
Closure checkpoint: `9ea973d79b4cd49946022cbbd174678a1280821f`.

## T9.1 document-shell realignment

Canonical branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

### R1 reader multi-document core — GREEN / CLOSED

Verified checkpoint:
`454dcadb6eca13ab98b5d219146dd10ec25af78d`

Exact-head CI:
- run `39004872760`;
- job `117315172824`;
- Build 0 warnings / 0 errors;
- startup smoke PASS 30 s;
- tests `1266 passed / 0 failed / 4 skipped / 1270 total`;
- overall SUCCESS.

Implemented owner: one `PdfDocumentWorkspace` per PDF, `OpenDocuments`, `ActiveDocument`, multi-select open, independent reader page/zoom/scroll state and explicit tracked-session lifetime.

### R1B per-document pending state + close guard — GREEN / CLOSED

Technical GREEN anchor:
`26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`

Exact-head CI:
- run `39040326178`;
- job `117418290462`;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — 30 s;
- tests `1274 passed / 0 failed / 4 skipped / 1278 total`;
- overall SUCCESS.

Checkpoint:
`docs/history/2026-10-17-f13-t9-1-r1b-pending-state-green-checkpoint.md`.

R1B preserves model state per PDF for Edit, Sign, Comments, Organize and AcroForm while switching documents. UI controls are not stored in the per-document model. Dirty-close is explicit before disposal.

Closure audit also removed legacy single-document Open-PDF discard guards from Ctrl+O, Edit and Comments, while preserving guards for ZPL, mode changes and window close. Obsolete `MainWindow.Comments.Integration.cs` was deleted rather than patched around.

Focused RED for that route drift:
- commit `db94045d9710fb67d827b79b260090c4eb3a5aa5`;
- run `39039423862`;
- 3 expected route failures before the minimal correction.

No DI container, event bus, document framework or generalized ModeManager was introduced.

The four skipped tests deliberately reserve future T9.1 contracts:
- R2 tabs;
- R3 document rail official artwork;
- R4 collapsible Pages/Bookmarks drawer;
- R5 PDF context menu.

## Remaining T9.1 slices

### R2 — top shell + real document tabs — NEXT

Now eligible because R1B is GREEN, but **not started by this checkpoint**.

Bounded scope:
- visible tab strip with file name + close `×`;
- add/open `+`;
- active tab switches existing `ActiveDocument`;
- close routes through the R1B dirty-close owner;
- no duplicate document/session owner;
- shell hierarchy aligned with approved mockup and Design Law;
- RED → GREEN → full suite → exact-head CI → checkpoint.

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
- audit real bookmark/outline binding before calling Bookmarks functional.

### R5 — context menus

- right-click menu on PDF workspace/page;
- contextual image menu when applicable;
- reuse existing command owners; no duplicate engines/operations.

### R6 — image-selection rendering

- reproduce reported blue-tint behavior;
- current contract declares transparent selection fill, so full-image tint is a defect until root cause is found;
- fix cause, not symptom.

### R7 — final visual / functional QA

Must cover Home/PDF Light+Dark, two or more PDF tabs, switching with pending per-document state, drawer, context menus, image selection, real 125% Windows scaling/DPI and P0=0/P1=0 for T9.1 exit.

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
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Next bounded task

**T9.1-R2 top shell + real document tabs** is the next eligible slice. Do not merge to `main`, do not begin T9.2, and do not thaw F14–F18.
