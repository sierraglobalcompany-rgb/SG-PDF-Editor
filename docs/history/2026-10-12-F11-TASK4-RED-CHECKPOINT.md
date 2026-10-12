# F11 — Task 4 RED Checkpoint

Date: 2026-10-12
Branch: `feat/f11-text-v2`

## Purpose

Freeze the exact continuity point requested by the user. Do not continue implementation from this checkpoint until explicitly instructed.

## Repository State at Capture

- Repository: `sierraglobalcompany-rgb/SG-PDF-Editor`
- F11 branch before this checkpoint doc commit: `3d20bcdf483e4b2b95dacebab0c4152d16516652`
- Parent GREEN integration head: `3340e7ede89b08984b57783999c19da25a68b3f3`
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`
- No merge to `main` was performed.
- F10 remains the stacked base of F11.

## F11 Status

### Task 1 — Model + deterministic one-column lines

CLOSED / GREEN.

Delivered:
- `TextLayoutModel`
- `TextLayoutAnalyzer`
- geometric reading order independent from PDF object order
- left-to-right run ordering
- invalid/degenerate input filtering
- cancellation

### Task 2 — Paragraphs + conservative one/two columns

CLOSED / GREEN.

Delivered:
- source text matrix retained in `TextRun`
- rotated text classified conservatively as unsupported
- title/body paragraph split on strong style change
- modest indentation remains inside one paragraph
- clear two-column detection
- unstable gutter classified ambiguous

Exact Task 2 GREEN head: `efc097473fefc8178f09d56dc3e3eaac0312d7b8`
Evidence: Windows Release build and full suite PASS, 0 warnings / 0 errors, 1065/1065 tests.

### Task 3 — PDFium data gate + F10 OCR integration

CLOSED / GREEN.

Delivered:
- `PdfDocumentSession.AnalyzeTextLayout(...)`
- direct reuse of existing `GetPageSize(...)` + `GetTextObjects(...)`
- no second text engine
- corpus proves geometry-based order, Spanish text, rotated text, and F10 invisible OCR text are consumable by F11
- PdfPig decision: **NO-NEED** at this point; no dependency added

Exact Task 3 GREEN head: `3340e7ede89b08984b57783999c19da25a68b3f3`
CI:
- run: `38209383197`
- job: `114679912957`
- Windows Release build: PASS
- build warnings: 0
- build errors: 0
- tests: 1068 passed / 0 failed / 0 skipped

### Task 4 — TextReflowPolicy

OPEN / RED ONLY.

RED commit:
- `3d20bcdf483e4b2b95dacebab0c4152d16516652`
- message: `test(f11): add red safe reflow policy matrix`

RED test matrix added in:
- `tests/SGPdf.App.Tests/TextReflowPolicyTests.cs`

Cases frozen by RED:
- safe simple single-column homogeneous paragraph -> allow
- two columns -> reject
- ambiguous/unsupported layout -> reject
- overlapping runs -> reject
- heterogeneous style -> reject
- invisible OCR run (`TextRenderMode != Fill`) -> reject as read-only
- blocked existing F7 edit preflight -> reject

At the instant this checkpoint was captured, the RED CI was still running:
- run: `38209600666`
- job: `114680584251`
- hygiene, pinned runtimes and locked restore had passed
- build was in progress
- no GREEN implementation of `TextReflowPolicy` had been written

Do not reinterpret this as Task 4 GREEN. Resume by first checking the final result/logs of run `38209600666`; the intended RED cause is absence of the new `TextReflowPolicy` implementation.

## Exact Resume Point

On explicit user instruction to continue:

1. Refresh `feat/f11-text-v2`, `main`, and latest CI state.
2. Inspect final logs for RED run `38209600666` and verify failure is only the intended missing-policy contract.
3. Implement the minimal pure `TextReflowPolicy` + decision result needed to satisfy the frozen tests.
4. Reuse existing `PdfEditPreflightResult.CanProceed`; do not introduce another preflight architecture.
5. Reuse Texto V1 read-only semantics (`TextRenderMode != Fill` remains non-editable).
6. Run exact-head Windows Release build + full tests.
7. Do not start Task 5 unless Task 4 is demonstrably GREEN.

## Guardrails

- KISS: no new PDF engine, no new dependency, no architecture expansion.
- PDFium remains the PDF owner.
- PdfPig remains out unless a future measured capability gap proves it necessary.
- F10 production code is not to be modified for F11 unless a real blocker is demonstrated.
- No merge to `main` without explicit user approval.
- Do not start F12.
- Manual/private/external-reader QA remains separate from automated closure claims.

## STOP

Checkpoint created by explicit user request. Stop here.
