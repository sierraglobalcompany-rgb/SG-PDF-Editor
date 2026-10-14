# F12.4 Compare UI — exact-head closure

Date: 2026-10-14  
Slice: F12.4 Compare — minimal UI  
Functional branch: `feat/f12-4-task3-compare-ui`

## Result

**CLOSED / AUTO PASS** for the approved minimal Compare UI scope.

This closure does not claim universal PDF comparison fidelity and does not close representative real/private/manual QA.

## Closed user flow

```text
PDF A already open
→ Archivo > Comparar con otro PDF...
→ choose local PDF B
→ existing PdfCompareService.Compare(A,B)
→ summary in existing right properties panel
→ close/reset comparison
```

## Closed requirements

- `PRO-COMPARE-UI-01`: menu action with A already open.
- `PRO-COMPARE-UI-02`: local B selection; cancel is no-op.
- `PRO-COMPARE-UI-03`: reuse existing `PdfCompareService.Compare(A,B)`.
- `PRO-COMPARE-UI-04`: existing right panel shows A/B names, page counts, text signal, visual-independent signal, structural status and overall conclusion.
- `PRO-COMPARE-UI-05`: close/reset comparison without replacing/reloading A unnecessarily.
- `PRO-COMPARE-UI-06`: existing Edit/Organize/Sign/Form lifecycle/dirty guards are respected.
- `PRO-COMPARE-UI-07`: invalid/unreadable/protected B fails in a controlled way and A remains active.

## Semantics preserved

`VisualChanged` keeps the existing core meaning: **visual difference independent of text**. It is not a claim that simultaneous text and additional graphic changes are decomposed independently.

Overall conclusion is `Sin diferencias detectadas` only when:
- page counts are equal;
- `TextChanged == false`;
- `VisualChanged == false`.

Otherwise the UI reports `Se encontraron diferencias`.

## Lifecycle implementation

The final lifecycle design stayed intentionally small:

- Compare owns its focused `ResetCompareUi()` and close action.
- Existing workspace transitions already converge through `ResetAcroFormUi()`.
- Compare reset is attached to that existing shared owner instead of duplicating reset calls across Reader/Edit/Organize/Sign.
- Forms explicitly resets Compare after its existing dirty-mode guards pass.

This avoided five mode-specific patches and kept one existing transition owner.

## TDD evidence for Task 3.3

RED exact head: `b9cf55d086ea9ecd5f63297ee4780a83efd7e257`.

RED CI:
- workflow run `38503947021`;
- Release build succeeded with `0` warnings / `0` errors;
- existing tests remained green;
- exactly three new Compare lifecycle/guard contract tests failed for the intended missing behavior;
- total: `1183` passed, `3` failed, `0` skipped.

GREEN functional exact head before documentation closure:
`488d59f7a9e47ae0ccaafe474e7215cdc4cdc1d2`.

GREEN CI:
- workflow run `38504445630`;
- job `115658596617`;
- repository hygiene PASS;
- pinned Labelize 1.7.0 staging PASS;
- pinned qpdf 12.4.2 staging PASS;
- pinned Tesseract 5.5.3 staging PASS;
- locked restore PASS;
- Release build PASS: `0` warnings / `0` errors;
- full test suite PASS: **1186/1186**, `0` failed, `0` skipped.

## KISS / anti-noise audit

PASS:
- no second viewer;
- no second sidebar;
- no new `ModeManager` / state machine;
- no new PDF engine;
- no cloud/backend/network dependency;
- no new NuGet/runtime dependency;
- no global XAML redesign;
- no duplicate comparison service;
- no alternate PDF-open owner for B;
- no patch-per-mode reset pattern.

The UI is a thin adapter over the already-closed Compare core.

## Explicitly not baseline

Still out of scope unless separately approved:
- side-by-side viewers;
- A/B toggling;
- per-page detailed result list;
- overlay/heatmap;
- OCR compare;
- semantic diff;
- page matching/reordering;
- deep signature/metadata comparison;
- report export;
- password credential UI dedicated to PDF B;
- universal fidelity claims.

## Open QA

Representative real/private/manual Compare QA remains open and should be executed later inside the combined OCR/Forms/Compare QA pack, including representative normal, protected and malformed PDFs where legally/safely available.

## Git governance

- `main` was not merged, rebased or force-updated by this slice.
- No destructive branch/PR cleanup was performed.
- Integration remains a separate release gate requiring explicit user approval before changing `main`.

## Next

F12.5 Batch starts with a **capability/scope gate**, not immediate architecture or implementation.

First question is which already-safe existing operations deliver enough batch value to justify the smallest MVP. Generic workflow/job/pipeline architecture is prohibited unless a demonstrated requirement proves it necessary.