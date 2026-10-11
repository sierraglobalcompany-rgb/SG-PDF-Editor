# F9 Task 4 — RED Checkpoint

Date: 2026-10-11
Status: **PAUSED BY USER / VALID RED CAPTURED**
Branch: `feat/f9-offline-utilities`

## 1. Last fully closed task

F9 Task 3 — Transactional text-overlay writer — is closed and automated-green.

Completion checkpoint:
- `docs/history/2026-10-11-F9-TASK3-TRANSACTIONAL-TEXT-OVERLAY-WRITER.md`

Task 3 functional GREEN SHA:
- `269b22835869c1dce48301bb7b561f9d9f1d42dd`

Task 3 documentation checkpoint SHA:
- `250f91875b495f0cfae9620983cd23f3cfc31dc5`

Task 3 exact-head CI:
- run `38112379084`
- job `114390438502`
- conclusion: SUCCESS
- build: PASS
- tests: PASS
- previous functional GREEN evidence: 885 PASS / 0 FAIL / 0 SKIP, build 0 warnings / 0 errors.

`main` remained unchanged at:
- `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge was performed.

## 2. Current task

F9 Task 4 — Watermark and numbering product services.

Canonical plan:
- `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`

Task 4 planned production files:
- `src/SGPdf.App/Features/Utilities/WatermarkPlanner.cs`
- `src/SGPdf.App/Features/Utilities/PageNumberingPlanner.cs`

Task 4 planned tests:
- `tests/SGPdf.App.Tests/WatermarkPlannerTests.cs`
- `tests/SGPdf.App.Tests/PageNumberingPlannerTests.cs`

No Task 4 production implementation exists yet at this checkpoint.

## 3. RED contracts added

Two test files were added only:

1. `tests/SGPdf.App.Tests/WatermarkPlannerTests.cs`
2. `tests/SGPdf.App.Tests/PageNumberingPlannerTests.cs`

RED commits:
- watermark tests: `1b5e6272a24fc2ea56d854927e1379f38148fcbb`
- completed Task 4 RED test head: `769082301815003320e544ee357c1b06b88201e2`

### Watermark RED contract

The tests currently define:

- selected page ranges resolve through existing `UtilityPageRange`;
- placements preserve `TextOverlaySettings` text/font-size/opacity/layer;
- nine standard watermark anchor origins using a 36 pt margin:
  - BottomLeft
  - BottomCenter
  - BottomRight
  - MiddleLeft
  - Center
  - MiddleRight
  - TopLeft
  - TopCenter
  - TopRight
- explicit X/Y offsets are added to the anchor origin;
- rotation is represented through the placement matrix using cosine/sine;
- non-finite page geometry or rotation fails closed.

### Numbering RED contract

The tests currently define:

- selected range numbering is deterministic in resolved page order;
- numbering reuses existing `PageNumberingSettings` and `PageNumberFormatter` semantics;
- six header/footer anchor origins using a 36 pt margin:
  - HeaderLeft
  - HeaderCenter
  - HeaderRight
  - FooterLeft
  - FooterCenter
  - FooterRight
- style is preserved through font size, opacity and overlay layer;
- explicit X/Y offsets are supported;
- unsupported formatted glyphs fail closed;
- number overflow fails closed;
- invalid font size, opacity or page geometry fails closed.

## 4. RED CI evidence

Exact RED SHA:
- `769082301815003320e544ee357c1b06b88201e2`

GitHub Actions:
- run `38112594597`
- job `114391066672`

Result:
- repository hygiene: PASS
- Labelize 1.7.0 staging: PASS
- locked restore: PASS
- build: PASS
- build warnings: **0**
- build errors: **0**
- tests: **885 PASS / 23 FAIL / 0 SKIP / 908 TOTAL**

The 23 failures are the new Task 4 tests only.

Failure cause is intentionally missing production types:
- `SGPdf.App.Features.Utilities.WatermarkPlanner`
- `SGPdf.App.Features.Utilities.WatermarkAnchor`
- `SGPdf.App.Features.Utilities.PageNumberingPlanner`
- `SGPdf.App.Features.Utilities.PageNumberAnchor`

Representative failure:
- `Assert.NotNull() Failure: Value is null` from the test `RequireType(...)` helpers.

Therefore this is an accepted behavioral RED:
- test harness compiles;
- all 885 pre-existing tests remain green;
- failures are isolated to the new Task 4 behavior not yet implemented.

## 5. Architecture decisions frozen at pause

Task 4 remains a pure managed planning slice.

Frozen decisions:

- planners produce existing `TextOverlayPlacement` values;
- persistence remains owned by Task 3 `PdfTextOverlayWriter`;
- page range remains owned by Task 2 `UtilityPageRange`;
- number formatting remains owned by Task 2 `PageNumberFormatter`;
- no separate Bates writer;
- no UI in Task 4;
- no new packages;
- no PDFium interop additions;
- no `PdfiumNative` changes;
- no workflow changes;
- no merge to `main`.

The RED tests currently treat the anchor coordinate as the text-object origin. No new text-width measurement abstraction has been introduced.

## 6. What was NOT done

At the user's explicit request to create a checkpoint and stop:

- no `WatermarkPlanner` production code was created;
- no `WatermarkAnchor` enum was created;
- no `PageNumberingPlanner` production code was created;
- no `PageNumberAnchor` enum was created;
- no Task 4 GREEN attempt was made;
- no Task 4 integration tests were added yet;
- no Task 4 checkpoint/closure was claimed;
- no Task 5 work was opened;
- no merge was performed.

## 7. Exact resume point

When work resumes:

1. verify branch `feat/f9-offline-utilities` and this checkpoint are still HEAD/current;
2. verify `main` is still frozen;
3. reread Task 4 section of the canonical plan;
4. keep RED SHA `769082301815003320e544ee357c1b06b88201e2` as the valid baseline;
5. implement only the minimum pure managed GREEN for:
   - `WatermarkAnchor`
   - `WatermarkPlanner`
   - `PageNumberAnchor`
   - `PageNumberingPlanner`
6. run exact-head Windows CI;
7. if planner tests are green, add Task 4 integration tests that feed placements into Task 3 writer, Save As, reopen and verify text/render;
8. run full suite again;
9. scope-audit and create the Task 4 completion checkpoint;
10. do not open Task 5 until Task 4 is fully green and checkpointed.

## 8. Pause instruction

**STOP HERE.**

This file intentionally captures a valid RED state so another session can resume without reconstructing Task 3 or re-deriving the Task 4 baseline.
