---
gsd_state_version: '1.1'
status: f13-task9-t9-1-r3-green-r4-next
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
7. `docs/MASTER_CONTEXT.md` for durable architecture;
8. `docs/history/**` as checkpoints/evidence.

## Mandatory rules

- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.
- **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **Ley SG de Diseño:** `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.
- **Guardrail anti-parches:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Frozen history

- F0–F12: CLOSED / FROZEN for automated baseline.
- F13 Tasks 1–8: CLOSED by checkpoints.
- F14–F18: FROZEN.
- `main`: read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` until explicit approval.

## F13 Task9 — ACTIVE

- T9.0: DONE.
- T9.1: ACTIVE.
- T9.2: NOT STARTED.
- No merge to `main` approved.

Canonical branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

## DG-0 — SG Design Governance — GREEN / CLOSED

- implementation: `3011f700e9014d72951314291bab4c3a67b89c8c`;
- checkpoint: `9ea973d79b4cd49946022cbbd174678a1280821f`.

## T9.1 slices

### R1 reader multi-document core — GREEN / CLOSED

- checkpoint: `454dcadb6eca13ab98b5d219146dd10ec25af78d`;
- CI run `39004872760`, job `117315172824`;
- tests `1266 passed / 0 failed / 4 skipped / 1270 total`.

Established `OpenDocuments`, `ActiveDocument` and one `PdfDocumentWorkspace` per PDF with independent session/page/zoom/scroll ownership.

### R1B per-document pending state + close guard — GREEN / CLOSED

- technical anchor: `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`;
- CI run `39040326178`, job `117418290462`;
- tests `1274 passed / 0 failed / 4 skipped / 1278 total`.

Each `PdfDocumentWorkspace` owns model-only pending state for Edit, Sign, Comments, Organize and AcroForm. Switching PDFs preserves pending work. Dirty close uses an explicit guard. Opening/switching PDFs does not force discard.

### R2 top shell + real document tabs — GREEN / CLOSED

Technical GREEN anchor:
`cf816557adab80f0e6e2e4ecb2f6628dc92eba90`

Exact-head CI:
- run `39043770373`;
- overall SUCCESS.

R2 truth:
- real document tabs exist;
- `+` opens/adds PDFs;
- tab activation reuses `ActiveDocument` / `ActivateDocumentAsync()`;
- tab close reuses the R1B dirty-close owner;
- no second document/session registry was introduced;
- top-shell menu reparenting was hardened by removing assumptions that Menu is a direct DockPanel child.

### R3 document tool rail + official artwork — GREEN / CLOSED technically

Product implementation:
`394bd320f8df03c0b00b5a517c00869a6c3a74e2`

Test alignment / exact-head GREEN:
`4050ef37422fc2cdb98b79f4bd4c215534fc553e`

Exact-head CI:
- run `39046256870`;
- job `117435463253`;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — 30 s;
- tests `1279 passed / 0 failed / 2 skipped / 1281 total`;
- overall SUCCESS.

R3 truth:
- document rail remains 96 px and ordered;
- accessibility/selected/disabled semantics remain;
- official approved artwork is used for `edit`, `sign`, `organize`, `ocr`, `redact`, `compare`, `convert`, `protect`;
- `read`, `comment`, `forms` keep semantic glyph fallback because no approved matching art exists yet;
- labels use 10.5 pt, no wrapping and ellipsis trimming;
- an old test requiring Path for all 11 buttons was realigned to the approved Image/Path split;
- a one-off Tesseract process timeout from the previous run did not reproduce; no OCR timeout/code change was made.

Checkpoint:
`docs/history/2026-10-17-f13-t9-1-r3-green-r4-next-checkpoint.md`.

## Next bounded block — R4

`T9.1-R4 — PAGES_BOOKMARKS_COLLAPSIBLE_DRAWER`

Status: **NEXT / NOT STARTED**.

Bounded goals:
1. Pages/Bookmarks drawer collapsed by default;
2. explicit expand/collapse control;
3. preserve workspace width while closed;
4. reuse existing thumbnail renderer/scheduler;
5. audit real bookmark/outline population before calling Bookmarks functional;
6. preserve drawer state per document only if it fits existing workspace ownership without new framework;
7. focused RED → GREEN;
8. build + smoke + full suite + exact-head CI;
9. checkpoint before R5.

Do not start R5 before R4 is GREEN.

## Remaining findings / sequence

- DS-001 DOCUMENT_SHELL — P1 ACTIVE, improved by R2/R3.
- DS-002 MULTI_DOCUMENT_WORKSPACE — R1/R1B/R2 GREEN.
- DS-003 SHELL_RAIL_PRESENTATION — R3 technical GREEN; final visual QA remains R7.
- DS-004 DOCUMENT_TOOL_RAIL — R3 technical GREEN.
- DS-005 READER_NAV_DRAWER — P1 NEXT / R4.
- DS-006 BOOKMARKS — P2 AUDIT_REQUIRED in R4.
- DS-007 IMAGE_SELECTION_RENDERING — P1 REPRODUCE_REQUIRED in R6.
- DS-008 PDF_CONTEXT_MENU — P1 OPEN / R5.
- DS-009 TOP_SHELL — R2 technical GREEN; final visual QA remains R7.
- DS-010 EDIT_OBJECT_INTERACTION — P1 OPEN.

Execution order:
`R4 pages/bookmarks drawer → R5 context menus → R6 image selection root cause → R7 final visual/functional QA`.

## QA semantics

Automated PASS proves only automated evidence. Windows-real, Light/Dark visual fidelity, 125% scaling, hardware/private corpus, printing/scanning/stylus and external-reader checks remain separate until actually run.

## Recovery if chat ends

Start from:
- canonical branch above;
- live HEAD + exact-head CI first;
- latest technical GREEN SHA `4050ef37422fc2cdb98b79f4bd4c215534fc553e` / CI `39046256870`;
- read `docs/history/2026-10-17-f13-t9-1-r3-green-r4-next-checkpoint.md`;
- next block R4, NOT STARTED;
- T9.2 remains frozen/not started;
- no merge approved.

If GitHub live is newer, live exact HEAD/CI takes precedence.
