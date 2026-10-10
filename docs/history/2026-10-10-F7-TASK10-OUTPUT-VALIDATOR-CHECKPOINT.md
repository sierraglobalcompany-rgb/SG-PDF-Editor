# F7 Task 10 — Combined PdfEditOutputValidator — Checkpoint

**Date:** 2026-10-10 16:02 America/Bogota  
**Repository:** `sierraglobalcompany-rgb/SG-PDF-Editor`  
**Branch:** `feat/f7-text-v1`  
**Base at Task 10 start:** `30e1297ec56449dc3738c3e90709c5e109953b8e`  
**Functional GREEN head:** `d52194d70017d12ac94765cb68a5a8618f685216`  
**Status:** CLOSED / AUTOMATED PASS

## Scope executed

Task 10 was limited to the combined `PdfEditOutputValidator` gate from the approved F7 plan.

Delivered:

- combined image + text output validation;
- exact Unicode validation for edited text;
- edited object must resolve as top-level `TEXT`;
- matrix validation;
- font-size validation;
- fill-color validation;
- font-route validation:
  - `OriginalFont` preserves the original font route;
  - `FallbackTtf` must resolve to the pinned DejaVu Sans fallback route;
- valid 36 DPI render for edited pages only;
- existing document structural checks remain active;
- source/output are each opened once by the validator;
- unedited pages are no longer rendered unnecessarily;
- `PdfEditWriter` passes both workspaces to the combined validator before atomic publication;
- existing image-only validator injection seam remains compatible for F6 tests.

## TDD evidence

### RED

RED head:

`845e5fd0fe9034cce3a719724cda62b87c15ff56`

Windows CI:

`38085490408` — expected FAILURE

Evidence:

- build succeeded with `0 warnings / 0 errors`;
- `741` tests discovered;
- `9` failed / `732` passed / `0` skipped;
- failures demonstrated the intended missing behavior:
  - current validator rendered all pages even with no edits;
  - no combined `Validate(output, imageWorkspace, textWorkspace, cancellationToken)` contract existed.

This was a behavioral RED, not a compile/harness failure.

### First GREEN attempt / root-cause correction

Intermediate head:

`affb29d6c3ccf3c1dbc5f4ab2729633c903f4715`

Windows CI:

`38085820166` — FAILURE

Evidence:

- build `0 warnings / 0 errors`;
- `740/741` tests passed;
- only failure: `SaveAs_MixedDeleteImageAndEditText_ResolvesAllBeforeMutationAndGeneratesOnce`.

Root cause:

When a structural image edit deletes/reorders an object before an edited text object, the final page-object ordinal of the text can legitimately shift. The first validator implementation treated the original text ordinal as invariant.

Correction:

- keep strict original-ordinal validation when image edits do not structurally alter page-object ordering;
- when structural image edits may shift ordinals, resolve the edited text conservatively by a unique exact match of Unicode + matrix + size + color + expected font route;
- ambiguous/no match remains a validation failure.

No retry/workaround was added.

### GREEN final

Functional head:

`d52194d70017d12ac94765cb68a5a8618f685216`

Windows exact-head CI:

`38085960475` — SUCCESS

Evidence:

```text
build = 0 warnings / 0 errors
tests = 741 PASS / 0 FAIL / 0 SKIPPED
```

The real Task 9 CID Type2 writer output is included in the Task 10 validator tests and passes exact Unicode + fallback-route + render validation.

## Scope audit

Diff from Task 10 start `30e1297...` to functional GREEN `d52194d...`:

```text
4 files changed

src/SGPdf.App/Pdf/PdfEditOutputValidator.cs
tests/SGPdf.App.Tests/ImageEditOutputValidatorTests.cs
tests/SGPdf.App.Tests/PdfEditOutputValidatorTextTests.cs
src/SGPdf.App/Pdf/PdfEditWriter.cs
```

No new PDF engine, NuGet package, network/runtime dependency, DI framework, alternate writer, or alternate EDITAR mode was introduced.

`main` was rechecked after GREEN and remains unchanged at:

`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## Explicitly NOT done

- F7 Task 11 preservation matrix / preflight;
- F7 Task 12 UI Texto V1;
- F7 Task 13 hardening;
- F7 Task 14 closure/docs/PR;
- F8;
- F7 PR creation;
- merge;
- push or merge to `main`;
- manual Windows QA claim.

## Next exact gate

> **F7 Task 11 — preservation F7 + preflight.**

Do not execute Task 11 until the next explicit user `continua` / instruction.

## Stop condition

Task 10 is closed at automated level only. This checkpoint intentionally stops before Task 11.
