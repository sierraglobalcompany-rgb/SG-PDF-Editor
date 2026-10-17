---
gsd_state_version: '1.1'
status: f13-task9-t9-1-r1b-green-r2-next
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

Commit: `0abae7ceb5f08611180c883346e21589d1509664`.

### DG-0 — SG Design Governance — GREEN / CLOSED

Implementation anchor: `3011f700e9014d72951314291bab4c3a67b89c8c`.
Closure checkpoint: `9ea973d79b4cd49946022cbbd174678a1280821f`.

### T9.1 — Document shell realignment

Canonical branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

### R1 reader multi-document core — GREEN / CLOSED

Verified checkpoint:
`454dcadb6eca13ab98b5d219146dd10ec25af78d`

Exact-head CI:
- run `39004872760`;
- job `117315172824`;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — 30 s;
- tests `1266 passed / 0 failed / 4 skipped / 1270 total`;
- overall SUCCESS.

R1 established `OpenDocuments`, `ActiveDocument` and one `PdfDocumentWorkspace` per PDF with independent session/page/zoom/scroll ownership.

### R1B per-document pending state + close guard — GREEN / CLOSED technically

Technical GREEN anchor:
`26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`

Exact-head CI:
- run `39040326178`;
- job `117418290462`;
- exact checkout `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`;
- repository hygiene PASS;
- restore locked dependencies PASS;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — 30 s;
- tests `1274 passed / 0 failed / 4 skipped / 1278 total`;
- overall SUCCESS.

Checkpoint:
`docs/history/2026-10-17-f13-t9-1-r1b-pending-state-green-checkpoint.md`.

R1B truth:
- each `PdfDocumentWorkspace` owns one `PdfDocumentPendingState`;
- pending state contains model/workspace state, never WPF controls;
- Edit preserves image/text/redaction workspaces and relevant selections;
- Sign preserves `SignatureEditState`;
- Comments preserves `CommentWorkspace` plus required source/loaded metadata;
- Organize preserves `OrganizePlan`, source sizes and dirty state;
- AcroForm preserves original/current string values and rebuilds controls on activation;
- `ActivateDocumentAsync()` captures current model state, detaches current presentation, binds target and restores target model/UI;
- dirty document close is guarded explicitly before disposal;
- switching/opening another PDF does not itself require save/discard.

Closure audit found and fixed `LEGACY_SINGLE_DOCUMENT_GUARD_DRIFT`:
- Ctrl+O no longer runs edit/sign discard guards before opening another PDF;
- Open PDF no longer triggers Edit/Comment discard class handlers;
- Recent PDFs use the multi-document open path without legacy discard;
- obsolete `MainWindow.Comments.Integration.cs` was deleted;
- Open ZPL, mode-switch and window-close guards remain intact;
- stale old single-document shortcut tests were realigned instead of restoring obsolete behavior.

Focused route RED evidence:
- commit `db94045d9710fb67d827b79b260090c4eb3a5aa5`;
- run `39039423862`, job `117415694668`;
- Build/smoke PASS with exactly 3 expected route-contract failures before correction.

No DI container, event bus, generalized ModeManager or document framework was introduced.

The 4 skipped contracts remain intentional future-phase contracts:
- R2 document tabs / `+`;
- R3 official document rail artwork;
- R4 collapsible Pages/Bookmarks drawer;
- R5 PDF workspace context menu.
They are preserved, not deleted.

## Next bounded block — R2

`T9.1-R2 — TOP_SHELL_AND_REAL_DOCUMENT_TABS`

Status: **NEXT / NOT STARTED**.

R2 may now build visible tabs because R1/R1B own document/session/pending-state lifecycle safely.

Bounded goals:
1. visible tab strip with filename + close `×`;
2. add/open `+`;
3. switch active tab through existing `ActivateDocumentAsync()` / `ActiveDocument` owner;
4. close through existing R1B dirty-close guard;
5. no duplicate document registry/session owner;
6. apply SG Design Law before visible UI implementation;
7. focused RED → GREEN tests;
8. full suite + smoke + exact-head CI;
9. checkpoint before R3.

Do not start R2 in a recovery session without first verifying live HEAD/CI against this STATE.

## Open findings / approved sequence

- DS-001 DOCUMENT_SHELL — P1 OPEN.
- DS-002 MULTI_DOCUMENT_WORKSPACE — P1 ACTIVE; R1 + R1B GREEN, R2 NEXT.
- DS-003 SHELL_RAIL_PRESENTATION — P1 OPEN.
- DS-004 DOCUMENT_TOOL_RAIL — P1 OPEN.
- DS-005 READER_NAV_DRAWER — P1 OPEN.
- DS-006 BOOKMARKS — P2 AUDIT_REQUIRED.
- DS-007 IMAGE_SELECTION_RENDERING — P1 REPRODUCE_REQUIRED; visible full-image blue tint remains a bug to reproduce, not intended behavior.
- DS-008 PDF_CONTEXT_MENU — P1 OPEN.
- DS-009 TOP_SHELL — P1 OPEN.
- DS-010 EDIT_OBJECT_INTERACTION — P1 OPEN.

Execution order:
`R2 top shell/tabs → R3 document rail → R4 pages/bookmarks drawer → R5 context menus → R6 image selection root cause → R7 final visual/functional QA`.

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

## QA semantics

Automated PASS never upgrades Windows-real, hardware, private-corpus, scanner, printer, stylus/touch, external-reader or other manual evidence to PASS. Use `NOT RUN`, `BLOCKED — ENVIRONMENT`, or the actual observed result.

## Recovery if the current chat ends

Start from:
- branch `feat/f13-task9-t9-1-document-shell-realignment-20261017`;
- live HEAD + exact-head CI first;
- technical R1B GREEN anchor `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34` / CI run `39040326178`;
- checkpoint `docs/history/2026-10-17-f13-t9-1-r1b-pending-state-green-checkpoint.md`;
- current next block `T9.1-R2`, NOT STARTED;
- T9.2 frozen/not started;
- no merge approved.

Read this STATE first, then the R1B checkpoint. If live GitHub is newer, live exact HEAD/CI takes precedence.
