---
gsd_state_version: '1.1'
status: f13-task9-t9-1-r1-reader-core-green-r1b-state-ownership-next
progress:
  total_phases: 19
  completed_phases: 13
  current_phase: F13
  current_task: Task9
  current_subtask: T9.1
---

# Project State

## Authority

Operational order:
1. live GitHub exact HEAD + exact-head CI;
2. this file;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. approved slice/spec/plan;
6. for UI/UX: `docs/design/SGPDF_DESIGN_LAW.md` + `.agents/skills/sgpdf-design/SKILL.md`;
7. `docs/MASTER_CONTEXT.md` for durable architecture only;
8. historical/master snapshots as context only.

## Mandatory rules

- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.
- **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **Ley SG de Diseño:** `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.
- **Guardrail anti-parches** (separate, not a numbered law): `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Frozen history

- F0–F12: CLOSED / FROZEN for the automated baseline already recorded in requirements/history.
- Real/manual/private/physical QA remains separate wherever historical records say NOT RUN.
- F13 Tasks 1–8: CLOSED by their existing checkpoints.
- F14–F18: FROZEN; do not start opportunistically.

## F13 Task9 — ACTIVE

- T9.0: DONE.
- T9.1: ACTIVE.
- T9.2: NOT STARTED.
- No merge to `main` approved.

### Last verified GREEN product anchor before document-shell realignment

Commit:
`0abae7ceb5f08611180c883346e21589d1509664`

Exact-head CI:
- run `39000181654`;
- conclusion `SUCCESS`.

### DG-0 — SG Design Governance — GREEN / CLOSED

Branch:
`feat/f13-dg0-sg-design-governance-20261017`

GREEN implementation anchor:
`3011f700e9014d72951314291bab4c3a67b89c8c`

Closure checkpoint:
`9ea973d79b4cd49946022cbbd174678a1280821f`

Validated governance:
- `docs/design/SGPDF_DESIGN_LAW.md`;
- `.agents/skills/sgpdf-design/SKILL.md`;
- focused references for properties panels, navigation, copy/forms, WPF/accessibility and final review;
- `DesignGovernanceContractTests`;
- no runtime dependency or UI framework added.

### T9.1 — Document shell realignment

Canonical branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

Historical R0/R1 RED checkpoint retained for traceability and governance contracts:
`bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`

Current verified checkpoint:
`454dcadb6eca13ab98b5d219146dd10ec25af78d`

Exact-head CI:
- run `39004872760`;
- job `117315172824`;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — 30 s;
- Test PASS — 1266 passed / 0 failed / 4 skipped / 1270 total;
- overall SUCCESS.

The 4 skipped contracts are intentional future-phase contracts:
- R2 document tabs / `+`;
- R3 official document rail artwork;
- R4 collapsible Pages/Bookmarks drawer;
- R5 PDF workspace context menu.
They are preserved, not deleted.

### R1 reader multi-document core — GREEN

Implemented and verified at `454dcadb...`:
- Open PDF dialog allows selecting multiple PDFs (`Multiselect = true` + `FileNames`).
- Opening another PDF creates/tracks a `PdfDocumentWorkspace`; the previous session is not disposed merely because another PDF opens.
- `OpenDocuments` and `ActiveDocument` exist as the document registry/active owner.
- each workspace owns PDF session, page sizes, navigation, zoom, vertical offset and dirty flag;
- document activation rebinds the legacy reader state to the selected workspace;
- page/zoom/scroll state is saved before activation changes;
- tracked sessions are disposed on explicit untracking/window shutdown;
- password/open failure restores the previous active document where possible;
- legacy tests that assumed single-document disposal were realigned to the multi-document owner rather than restoring the old behavior.

R1 is not the whole user-facing multi-tab experience yet. No visible document tab strip exists until R2.

### Newly confirmed gap before R2 — R1B state ownership / safe tab switching

The user explicitly requires that switching tabs must preserve pending per-document work, not force saving merely to change tabs.

Current audit shows global MainWindow mode state still exists for edit/sign/organize/comments/forms. `ActivateDocumentAsync()` resets some mode UI/state, so visible tabs must NOT be enabled until per-document pending state and close/switch guards are intentionally owned.

Next exact bounded block:
`T9.1-R1B — PER_DOCUMENT_PENDING_STATE_AND_CLOSE_GUARD`

Goals:
1. characterize which pending states exist per mode;
2. establish a KISS per-document owner/snapshot without DI/event bus/mode manager;
3. preserve unsaved work when switching documents where technically supported;
4. add explicit dirty-close behavior;
5. RED → GREEN focused tests;
6. full suite + smoke + exact-head CI;
7. checkpoint before R2.

Do not create visible tabs before R1B is safe.

## Open findings / approved sequence

- DS-001 DOCUMENT_SHELL — P1 OPEN.
- DS-002 MULTI_DOCUMENT_WORKSPACE — P1 ACTIVE; reader core GREEN, pending-state ownership next.
- DS-003 SHELL_RAIL_PRESENTATION — P1 OPEN.
- DS-004 DOCUMENT_TOOL_RAIL — P1 OPEN.
- DS-005 READER_NAV_DRAWER — P1 OPEN.
- DS-006 BOOKMARKS — P2 AUDIT_REQUIRED.
- DS-007 IMAGE_SELECTION_RENDERING — P1 REPRODUCE_REQUIRED; code currently declares transparent selection fill, so visible full-image blue tint remains a bug to reproduce, not intended behavior.
- DS-008 PDF_CONTEXT_MENU — P1 OPEN.
- DS-009 TOP_SHELL — P1 OPEN.
- DS-010 EDIT_OBJECT_INTERACTION — P1 OPEN.

Execution order:
`R1B → R2 top shell/tabs → R3 document rail → R4 pages/bookmarks drawer → R5 context menus → R6 image selection root cause → R7 final visual/functional QA`.

## Main

`main` live historical/read-only anchor remains:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

Read-only until explicit user approval. No automatic merge/rebase/force-push.

## Runtime / architecture constraints

- Windows x64 + WPF + .NET 10.
- `SGPdf.App + SGPdf.App.Tests` while sufficient.
- PDFium primary PDF engine; `PdfiumRuntime.NativeGate` for native access.
- Labelize 1.7.0 only ZPL runtime renderer.
- PDFsharp narrow approved roles only; not general second engine.
- qpdf narrow AES sidecar.
- Tesseract local OCR.
- ZXing.Net test/QA only.
- No backend, DB, cloud, plugin framework, second viewer/engine, generalized ModeManager/conversion/Batch framework without explicit decision.
- No new UI framework/package simply for visual polish.

## Visual / design authority

The approved app icon, PDF document icon and 15 Home tool icons keep distinct roles. Do not reinterpret them during unrelated UX work.

For all visible/interactable changes, load the SG design skill:
`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.

The ZPL properties panel is the canonical progressive-disclosure problem: capabilities remain, but unrelated/advanced settings must not all compete in one long visible property sheet.

## QA semantics

Automated PASS never upgrades Windows-real, hardware, private-corpus, scanner, printer, stylus/touch, external-reader or other manual evidence to PASS. Use `NOT RUN`, `BLOCKED — ENVIRONMENT`, or the actual observed result.

## Recovery if the current chat ends

Start from:
- branch `feat/f13-task9-t9-1-document-shell-realignment-20261017`;
- verified checkpoint `454dcadb6eca13ab98b5d219146dd10ec25af78d`;
- CI run `39004872760` SUCCESS;
- current block `T9.1-R1B`;
- T9.2 frozen;
- no merge approved.

Read this STATE first, then the latest `docs/history/2026-10-17-f13-t9-1-r1-reader-core-green-checkpoint.md` if present.
