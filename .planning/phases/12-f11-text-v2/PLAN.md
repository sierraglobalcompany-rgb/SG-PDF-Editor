# Phase F11 — Texto V2

**Status:** AUTOMATED CLOSURE DECLARED; exact-head integration CI required  
**Branch:** `feat/f11-text-v2`  
**Base:** F10 closure `42935a865c898dea9e3548fbf94e1b400c8a5732`

## Goal

Extend the existing conservative text editor with deterministic reading order, lines, paragraphs, simple one/two-column structure and limited reflow only when the paragraph is proven safe.

## Delivered

- Managed layout model: run, line, paragraph, column, page and `Simple/Ambiguous/Unsupported` classification.
- Geometric reading order independent from raw PDF object ordering.
- Deterministic line/paragraph grouping and conservative one/two-column detection.
- Explicit rejection for rotated, overlapping, heterogeneous, OCR-invisible and otherwise unsafe layouts.
- PDFium data characterization proving the approved F11 scope without PdfPig.
- `TextReflowPolicy` and conservative `TextReflowPlanner` over existing text objects only.
- Atomic `TextEditWorkspace.ApplyReflowPlan` integration through the existing Texto V1 writer.
- Save As + reopen/validate + rendered-output automated evidence.
- Minimal WPF `Reflow seguro (Texto V2)` controls inside the existing EDITAR text properties flow.
- Spanish safe/unsafe status and rejection reasons.

## Invariants

- KISS/YAGNI; no Word-like page reflow.
- PDFium remains the PDF owner.
- PdfPig gate result: NO-NEED; no new PDF dependency.
- One/two simple columns may be analyzed; reflow is one-column safe paragraph only.
- Ambiguous/unsupported means fail closed.
- Reflow never creates/moves PDF text objects in F11; it redistributes text only across existing safe objects.
- Existing Texto V1 workspace/writer owns mutation and Save As transaction.
- Source untouched; reopen/validation required by the existing writer route.
- `main` frozen; no merge without explicit approval.
- AUTO PASS never implies manual/private/external-reader PASS.

## Functional evidence before closure-doc integration gate

- Task 1 model/reading order: automated GREEN.
- Task 2 paragraphs/columns: `efc097473fefc8178f09d56dc3e3eaac0312d7b8`; 1065/1065 at its exact-head evidence.
- Task 3 PDFium integration gate: `3340e7ede89b08984b57783999c19da25a68b3f3`; CI `38209383197` / `114679912957`; 1068/1068.
- Task 4 safe reflow policy: `8de3a746568a4662438269048e0d60093786158f`; 1075/1075 at its exact-head evidence.
- Task 5 planner/workspace/writer + rendered validation: functional head `024e0f043b6b46bee6b8901f8301c727c28aacbf`; 1082/1082 at final exact-head evidence.
- Task 6 RED: `0e2d930e487716a7417304b595d17b86978aa327`; CI `38261990458` / `114854559091`; Build PASS, 0 warnings/errors; 3 expected new presentation failures and 1082 previous tests passing.
- Task 6 GREEN: `554406390cf87a2e9a61471b60105c7fba2ba4fb`; CI `38262423101` / `114856039976`; Build PASS, 0 warnings/errors; 1085/1085 tests PASS.

## Manual status

Real Windows reflow UX, representative private PDFs, complex editorial layouts, Adobe/Foxit/Edge interoperability and physical-offline QA: **NOT RUN**.

F11 is not a Word processor and does not certify arbitrary PDF reflow.

## Integration gate

The commit carrying this closure declaration must pass exact-head Windows CI. Then a draft/open/unmerged PR must exist from `feat/f10-local-ocr` to `feat/f11-text-v2`, and `main` must remain unchanged. Once those conditions are verified: **STOP F11 and await explicit continuation**.

Canonical executable plan: `docs/superpowers/plans/2026-10-12-f11-text-v2.md`.
