# F9 Task 4 — Watermark and Numbering Product Services

**Date:** 2026-10-11  
**Branch:** `feat/f9-offline-utilities`  
**Base checkpoint (Task 3 closure):** `250f91875b495f0cfae9620983cd23f3cfc31dc5`  
**Functional GREEN SHA:** `35aebc537cb5e7c31e9bea5ea99ae252c32dcd64`  
**Status:** AUTOMATED PASS / Task 4 CLOSED  
**Manual Windows / external-reader QA:** NOT RUN

## Scope

Task 4 implements the pure managed product planners that feed the transactional writer from Task 3:

- `WatermarkPlanner`
- `PageNumberingPlanner`
- 9 watermark anchor origins
- 6 header/footer numbering anchor origins
- page-range resolution through `UtilityPageRange`
- deterministic numbering through `PageNumberingSettings` + `PageNumberFormatter`
- offsets, watermark rotation, opacity and foreground/background propagation
- configurable fill color for watermark and numbering
- real Save As / reopen / render integrations through `PdfTextOverlayWriter`

No Task 5 UI was opened in this gate.

## Original Task 4 RED

Tests-only RED was completed at:

`769082301815003320e544ee357c1b06b88201e2`

CI:

- workflow: `38112594597`
- job: `114391066672`
- build: PASS, 0 warnings / 0 errors
- tests: 885 PASS / 23 FAIL / 0 SKIP / 908 total

All 23 failures belonged to the new Task 4 planner contracts because the following production types did not yet exist:

- `WatermarkPlanner`
- `WatermarkAnchor`
- `PageNumberingPlanner`
- `PageNumberAnchor`

This was a valid behavioral RED.

The RED state was frozen in:

`docs/history/2026-10-11-F9-TASK4-RED-CHECKPOINT.md`

## Planner GREEN

Pure planner implementation reached GREEN at:

`c6c0f57dff588367fe0928c49c6e6b6e219871a0`

CI:

- workflow: `38112737204`
- job: `114391488728`
- build: 0 warnings / 0 errors
- tests: 908 PASS / 0 FAIL / 0 SKIP

### Geometry contract

Standard margin: **36 pt**.

Watermark origins:

- BottomLeft
- BottomCenter
- BottomRight
- MiddleLeft
- Center
- MiddleRight
- TopLeft
- TopCenter
- TopRight

Numbering origins:

- HeaderLeft
- HeaderCenter
- HeaderRight
- FooterLeft
- FooterCenter
- FooterRight

Offsets are added to the selected anchor origin. Watermark rotation uses the standard matrix:

`(cos θ, sin θ, -sin θ, cos θ, x, y)`

Numbering uses the identity rotation matrix at the selected anchor + offsets.

## Integration GREEN

Real writer integrations were then added for:

1. one-page text watermark -> Save As -> reopen -> exact text-object discovery -> render smoke;
2. three-page Bates-style numbering -> Save As -> reopen -> exact per-page numbering -> per-page render smoke.

Integration head:

`7f4aa0a3d8f807eaa27a6315d188572e0c03ef45`

CI:

- workflow: `38112860643`
- job: `114391855363`
- build: 0 warnings / 0 errors
- tests: 910 PASS / 0 FAIL / 0 SKIP

## Ruling — color is part of the Task 4 contract

Before closure, the approved F9 spec was reviewed against the actual interfaces. The spec explicitly requires **color** for both text watermark and page numbering.

The pre-ruling implementation had a real interface gap:

- `TextOverlaySettings` / `TextOverlayPlacement` did not carry a fill color;
- `PageNumberingSettings` did not carry a fill color;
- `PdfTextOverlayWriter` hard-coded RGB to black.

Task 4 was therefore **not** closed at the 910-test integration GREEN.

### Color RED

A focused behavioral RED was added at:

`fb276c86458d89a6d56298985a6896bd13e7157a`

CI:

- workflow: `38113016384`
- job: `114392306601`
- build: 0 warnings / 0 errors
- tests: 910 PASS / 2 FAIL / 0 SKIP / 912 total

The only failures were the two new color contracts, caused by the still-missing color constructor/property path.

### Minimal color ruling

Existing `PdfTextFillColor` is reused; no second color model was introduced.

Compatibility rule:

- existing constructors remain valid;
- default fill remains opaque black `(0, 0, 0, 255)`;
- watermark color is carried by `TextOverlaySettings`;
- numbering color is carried by `PageNumberingSettings`;
- both planners propagate it into `TextOverlayPlacement`;
- writer persists requested RGB;
- effective output alpha is `round(baseColorAlpha × opacity)`;
- validator checks RGB and effective alpha after reopen;
- every channel fails closed outside `0..255`.

This keeps color and opacity independent while preserving all previous default behavior.

## Final functional GREEN

Functional head:

`35aebc537cb5e7c31e9bea5ea99ae252c32dcd64`

Workflow:

`38113175641`

Attempt 1 / job `114392771004`:

- build: 0 warnings / 0 errors
- Task 4 color tests passed
- suite: 911 PASS / 1 FAIL / 0 SKIP
- sole failure: pre-existing WPF timing-sensitive test `MainWindowOrganizeThumbnailTests.RebuildAfterReorder_ReusesBitmapWhenSourceAndRotationAreUnchanged`
- failure was outside Task 4 and unrelated to the changed files

The exact same SHA was rerun without code changes.

Attempt 2 / job `114392977155`:

- build: **0 warnings / 0 errors**
- tests: **912 PASS / 0 FAIL / 0 SKIP**
- conclusion: **success**

The attempt-1 WPF failure is therefore classified as CI/test flake evidence, not a Task 4 regression.

External runner-only warnings about Node.js 20 / `punycode` deprecation are GitHub Actions maintenance noise and are not C# product warnings.

## Scope audit

Comparison:

`250f91875b495f0cfae9620983cd23f3cfc31dc5..35aebc537cb5e7c31e9bea5ea99ae252c32dcd64`

Result:

- ahead: 14 commits
- behind: 0
- merge base: exact Task 3 closure

Changed paths:

1. `docs/history/2026-10-11-F9-TASK4-RED-CHECKPOINT.md`
2. `src/SGPdf.App/Features/Utilities/PageNumberingPlanner.cs`
3. `src/SGPdf.App/Features/Utilities/PageNumberingSettings.cs`
4. `src/SGPdf.App/Features/Utilities/TextOverlaySettings.cs`
5. `src/SGPdf.App/Features/Utilities/WatermarkPlanner.cs`
6. `src/SGPdf.App/Pdf/PdfTextOverlayOutputValidator.cs`
7. `src/SGPdf.App/Pdf/PdfTextOverlayWriter.cs`
8. `tests/SGPdf.App.Tests/PageNumberingPlannerTests.cs`
9. `tests/SGPdf.App.Tests/UtilityOverlayColorTests.cs`
10. `tests/SGPdf.App.Tests/WatermarkPlannerTests.cs`

The settings/writer/validator modifications are the minimal cross-task interface repair required by the approved color requirement. There are no package, `.csproj`, XAML, workflow, native-interop, network, cloud or account changes.

## Main branch

`main` remains frozen at:

`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge was performed.

## Manual QA

Manual Windows UI QA: **NOT RUN**.  
Adobe/Foxit/Edge external-reader QA: **NOT RUN**.

No manual claim is inferred from automated render/reopen tests.

## NEXT — Task 5 only

Next canonical task:

**F9 Task 5 — F9.1 focused WPF commands**

Expected scope from the canonical plan:

- focused dialogs/partials under `src/SGPdf.App/Features/Utilities/`
- `MainWindow.Utilities.cs`
- thin `Herramientas` command wiring in `MainWindow.xaml`
- structural/behavior tests for Watermark and Number Pages commands
- validation messages
- Save As cancellation
- dirty/error state behavior

Task 5 must consume the Task 2/4 managed models and delegate persistence to Task 3. It must not create another writer or utility workspace.

## STOP

Task 4 is closed only after this checkpoint receives exact-head CI.

Do not start Task 5 inside the Task 4 closure gate. Do not merge to `main` without explicit approval.
