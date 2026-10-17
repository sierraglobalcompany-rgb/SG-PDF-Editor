# SG PDF Editor — F13 Task9 / T9.1
## R1 reader multi-document core — GREEN checkpoint

**Fecha:** 2026-10-17

## Git authority

```yaml
branch: feat/f13-task9-t9-1-document-shell-realignment-20261017
verified_product_checkpoint: 454dcadb6eca13ab98b5d219146dd10ec25af78d
state_update_commit: 44d4c5057c62b857b9ad09858c34ac56db4aedea
merge: NO
main_changed: NO
t9_2_started: NO
```

## Exact-head evidence for `454dcadb...`

```yaml
run_id: 39004872760
job_id: 117315172824
conclusion: SUCCESS
build:
  result: PASS
  warnings: 0
  errors: 0
startup_smoke:
  result: PASS
  seconds: 30
tests:
  passed: 1266
  failed: 0
  skipped: 4
  total: 1270
```

The four skipped contracts are intentional future-phase contracts, not hidden failures:

- R2: real document tabs + add button;
- R3: official artwork in document tool rail;
- R4: collapsible Pages/Bookmarks drawer;
- R5: PDF workspace context menu.

## R1 reader core now implemented

- `OpenFileDialog.Multiselect = true`.
- `dialog.FileNames` opens multiple selected PDFs.
- each opened PDF gets a `PdfDocumentWorkspace`.
- `OpenDocuments` tracks open workspaces.
- `ActiveDocument` identifies the active workspace.
- opening another PDF no longer disposes the previous PDF merely to replace it.
- each workspace owns its `PdfDocumentSession`, page sizes, navigation, zoom, vertical offset and dirty marker.
- activation saves current reader page/zoom/scroll and restores the selected workspace state.
- explicit untracking/window shutdown disposes tracked sessions.
- failed/password-cancelled opens preserve or restore the previous document where possible.

## Historical test drift resolved

The move from single-document to multi-document ownership exposed tests that assumed the previous session must be disposed immediately when a second PDF opens. Those tests were realigned to the new owner instead of reintroducing the obsolete disposal behavior.

The first multi-document GREEN attempt also exposed static tests that expected `ResetAcroFormUi()` to remain textually inside `MainWindow.Reader.cs`. The owner had intentionally moved to `MainWindow.Documents.cs`; the tests were updated to follow the actual document-switch owner.

## Important limitation: do not start visible tabs yet

The reader/session core is green, but the complete user requirement is stronger:

> changing document tabs must preserve pending per-document work such as edit/sign/comment/organize state rather than requiring a save just to switch tabs.

Current audit shows several mode states still live as `MainWindow` globals. `ActivateDocumentAsync()` resets signature/form UI/state and other feature owners are not yet persisted per `PdfDocumentWorkspace`.

Therefore the next bounded block is:

```text
T9.1-R1B — PER_DOCUMENT_PENDING_STATE_AND_CLOSE_GUARD
```

R1B must be completed before R2 exposes tabs to users.

## R1B goals

1. inventory the real pending state owned by Edit, Sign, Comments, Organize and Forms;
2. decide the smallest per-document state container/snapshot that reuses existing owners;
3. preserve unsaved state across document activation where supported;
4. implement dirty-close confirmation/guard without silently discarding work;
5. focused RED tests before implementation;
6. full suite + startup smoke + exact-head CI;
7. checkpoint before R2.

## UI/UX authority

All upcoming visible work obeys:

```text
INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY
```

Read first:

- `docs/design/SGPDF_DESIGN_LAW.md`
- `.agents/skills/sgpdf-design/SKILL.md`

The approved app icon, document icon and 15 tool icons remain fixed. No redesign is authorized.

## Open document-shell findings

```yaml
DS-001_DOCUMENT_SHELL: P1_OPEN
DS-002_MULTI_DOCUMENT_WORKSPACE: P1_ACTIVE_R1B_NEXT
DS-003_SHELL_RAIL_PRESENTATION: P1_OPEN
DS-004_DOCUMENT_TOOL_RAIL: P1_OPEN
DS-005_READER_NAV_DRAWER: P1_OPEN
DS-006_BOOKMARKS: P2_AUDIT_REQUIRED
DS-007_IMAGE_SELECTION_RENDERING: P1_REPRODUCE_REQUIRED
DS-008_PDF_CONTEXT_MENU: P1_OPEN
DS-009_TOP_SHELL: P1_OPEN
DS-010_EDIT_OBJECT_INTERACTION: P1_OPEN
```

## Mandatory sequence

```text
R1B pending state + close guard
↓
R2 top shell + real tabs
↓
R3 document rail + approved icons
↓
R4 Pages/Bookmarks collapsible drawer
↓
R5 contextual menus
↓
R6 image-selection root cause
↓
R7 final visual/functional QA
↓
T9.1 CLOSED
↓
T9.2 READY
```

## Guardrails

```text
NO merge
NO main
NO force push
NO T9.2
NO Registry
NO installer
NO new UI framework
NO ModeManager/event bus/DI architecture
NO second PDF engine
NO code solely to fake a contract green
```

KISS:
`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`

Anti-noise:
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

Guardrail anti-patch:
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → MANUAL RE-RUN`

## Recovery

If a new chat takes over, start with `.planning/STATE.md`, verify the branch live HEAD/CI, then use this file as the R1 checkpoint. Do not resume from the old `bbae51f...` RED checkpoint except for historical comparison.
