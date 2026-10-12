# F10 Task 5 — Planner GREEN Checkpoint

Date: 2026-10-11
Branch: `feat/f10-local-ocr`
Phase: `F10 — OCR local`
Task: `Task 5 — Transactional searchable-layer writer and semantic validator`
Subblock status: **OcrTextLayerPlanner GREEN; writer NOT started**

## 1. Resume point

Resume from **Task 5 / Step 3**: write RED transactional tests for `PdfOcrWriter`.

Do not skip directly to implementation. Preserve strict RED → GREEN. Do not start `PdfOcrOutputValidator`, workflow, UI, F11, merge, or `main` work before the writer RED is demonstrated and the minimum writer GREEN is subsequently proven.

## 2. Planner implementation closed

Production file added:

- `src/SGPdf.App/Features/Ocr/OcrTextLayerPlanner.cs`

Implementation commit:

- `2206dd50f72620f0ac0f752f1935d47a056fe25f` — `feat(f10): add OCR text layer planner`

The planner now:

1. Groups OCR words by TSV `(block, paragraph, line)` in first-appearance order.
2. Joins words on one line with a single space.
3. Preserves Unicode text and non-empty confidence-0 words.
4. Reuses `OcrCoordinateMapper` for every word; it does not duplicate rotation logic.
5. Retains mapped per-word geometry for later semantic validation.
6. Produces line placements with `PageIndex`, `Text`, union `Bounds`, `Words`, deterministic `SourceOrder`, and TSV line metadata.
7. Keeps rotated-page geometry inside the proven PDF mapping path.
8. Adds no paragraph/column/reflow/F11 heuristics.

No `PdfOcrWriter.cs` or `PdfOcrOutputValidator.cs` was created in this subblock.

## 3. RED evidence retained

Planner RED tests were introduced before production implementation in:

- `tests/SGPdf.App.Tests/OcrTextLayerPlannerTests.cs`

Initial RED commit:

- `c215944bfec8ef6a30730684b0fdc07871174dcd` — `test(f10): define OCR text layer planner contract`

First RED Windows CI:

- Workflow `38164607872`
- Build: PASS, 0 warnings / 0 errors
- Tests: expected RED
- Result: 4 planner tests failed because `OcrTextLayerPlanner` did not exist; 992 existing tests passed; 0 skipped.

A test-reflection helper was then corrected in:

- `277d6e0c5c826ea5c4e29d63b8bd6c3d9be629b9` — `test(f10): fix planner reflection helper`

That run still retained the intended 4 planner RED failures. One unrelated pre-existing thumbnail test also failed transiently in that run; it did not reproduce on the GREEN exact-head run below.

## 4. GREEN exact-head evidence

Exact implementation HEAD verified:

- `2206dd50f72620f0ac0f752f1935d47a056fe25f`
- Workflow: `38165116837`
- Windows job: `114545963826`
- Repository hygiene: PASS
- Labelize staging: PASS
- qpdf 12.4.2 staging: PASS
- Tesseract 5.5.3 staging: PASS
- Locked restore: PASS
- Release build: PASS — 0 warnings / 0 errors
- Full suite: **996 PASS / 0 FAIL / 0 SKIP**
- Test duration reported by CI: 30 s

The previous transient `MainWindowOrganizeThumbnailTests.LargeDocument_OnlyRealizedPlusNeighborThumbnailsHoldBitmaps` failure did not reproduce on this exact-head run.

## 5. Guardrails carried forward

- `main` remains untouched; no merge without explicit user approval.
- Continue only on `feat/f10-local-ocr`.
- PDFium remains PDF owner.
- Tesseract remains recognition/TSV only; never final PDF writer.
- Reuse the proven F7 CID Type2 / fallback font primitives and F9 transaction/save patterns; do not create a generic mega-writer.
- Save As must remain transactional: source untouched, stale fingerprint rejection, sibling temp artifacts, semantic validation before publication, source-race recheck, atomic replacement, cleanup on every path.
- True invisible text must use only the Task 1-proven PDFium API.
- Manual Windows UX / Adobe / Foxit / Edge / private corpus remains NOT RUN unless actually executed.

## 6. Exact next action on `continua`

1. Verify branch/head and this checkpoint.
2. Re-read Task 5 Step 3 writer contract in `docs/superpowers/plans/2026-10-11-f10-local-ocr.md`.
3. Inspect existing F7 text writer/font primitives and F9 transactional writer patterns only as needed.
4. Add `tests/SGPdf.App.Tests/PdfOcrWriterTests.cs` first.
5. Cover at minimum: source==destination rejection, stale fingerprint, invalid page index, unsupported glyph fail-closed, source unchanged, sibling temp cleanup, cancellation before/during native mutation, existing destination preserved on validator failure, and publication only after validation.
6. Run exact evidence and confirm RED because `PdfOcrWriter` is absent/incomplete.
7. Only then implement the minimum writer GREEN.

STOP here. Writer work is intentionally not part of this checkpoint.
