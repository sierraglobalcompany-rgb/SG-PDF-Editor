# F9 — Task 1 Overlay Capability Gate Checkpoint

**Date:** 2026-10-11  
**Status:** AUTOMATED PASS  
**Branch:** `feat/f9-offline-utilities`  
**Base:** F8 closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`

## Scope

Task 1 was characterization only. No product writer, UI, package, PDF engine or runtime-network behavior was added.

Canonical design:
- `docs/superpowers/specs/2026-10-11-f9-offline-utilities-design.md`

Canonical implementation plan:
- `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`

Phase plan:
- `.planning/phases/10-f9-utilities/PLAN.md`

## Font-scope correction before RED

The initial design probe mentioned CJK text. Inspection of the pinned DejaVu Sans 2.37 fallback showed that F9 must not claim universal Unicode coverage.

The gate therefore uses representative supported Unicode:

`NIÑO Bogotá — € Ω`

and explicitly proves that known unsupported CJK rune `東` is rejected by the pinned font policy.

Decision: F9 watermark/numbering accepts Unicode only within the pinned offline font's proven glyph coverage. Missing glyphs fail closed. No second font asset or font engine was introduced.

## RED

Exact RED head:

`f128874670480e439cb945faeb4240927d6d96ab`

Windows workflow:
- run `38106060668`
- job `114371733944`

Evidence:
- repository hygiene PASS
- pinned Labelize staging PASS
- locked restore PASS
- Release build PASS: **0 warnings / 0 errors**
- tests: **852 PASS / 2 FAIL / 0 skipped / 854 total**

The two failures were exactly the intentionally unimplemented capability harness paths:
- text overlay characterization
- image overlay characterization

The font-coverage gate already passed in RED.

## GREEN characterization

Files:
- `tests/SGPdf.App.Tests/PdfiumUtilityOverlayCapabilityTests.cs`
- `tests/SGPdf.App.Tests/UtilityOverlayNativeHarness.cs`

No new native interop file was necessary: the exact pinned PDFium runtime already exposed every native function required by this gate.

### Text overlay — GO

Measured on existing page content using the exact pinned PDFium binary:
- load pinned DejaVu Sans through existing CID Type2 path;
- create new PDF text page objects;
- supported Unicode round-trip after Save As + reopen;
- RGBA fill round-trip, including alpha `96`;
- non-zero rotation through object matrix;
- explicit background insertion at object index 0;
- foreground append;
- deterministic reopened object order;
- source page content remains present;
- low-DPI reopen/render smoke succeeds.

The synthetic Helvetica base object reopens through `FPDFTextObj_GetText` as `"BASE "` with a terminal extraction space; the gate normalizes only that pre-existing base object's terminal whitespace. The two new Unicode overlay objects round-trip exactly.

### Image overlay — GO with measured readback caveat

Measured:
- create new image object;
- assign an in-memory bitmap;
- assign matrix/position;
- set alpha `112`;
- explicit background insertion and foreground append;
- Save As + reopen;
- image object type and matrices round-trip;
- reopened page render proves semi-transparent blending at interior sample points;
- render smoke succeeds.

Caveat: after Save As + reopen, this PDFium build does not expose image opacity through `FPDFPageObj_GetFillColor` for the image object. The first GREEN attempt correctly exposed that limitation. The final gate validates persisted image opacity by the reopened rendered pixels instead of fabricating structural readback.

This caveat does **not** block image watermark V1, provided the future writer validates image alpha visually/render-wise rather than depending on `GetFillColor` after reopen.

## Intermediate characterization refinement

Initial implemented harness head:

`04fba7426c12f5245a69ce485eec264f323c5972`

Workflow `38106172611`, job `114372065890`:
- build PASS, 0 warnings / 0 errors;
- tests **852 PASS / 2 FAIL / 854 total**.

Root causes:
1. PDFium adds terminal whitespace when extracting the synthetic base Helvetica object; new overlay text was exact.
2. `FPDFPageObj_GetFillColor` is not readable for the reopened image object, so image opacity must be validated through rendering.

No product behavior was changed while debugging; only the characterization observation method was corrected.

## Functional GREEN

Exact functional head:

`81d9de1b4a579f6029d6dfd7a7d5aa938505cf40`

Windows workflow:
- run `38106390184`
- job `114372729560`

Evidence:
- exact checkout PASS
- repository hygiene PASS
- pinned Labelize staging PASS
- locked restore PASS
- Release build: **0 warnings / 0 errors**
- tests: **854 PASS / 0 FAIL / 0 skipped / 854 total**
- job conclusion: **SUCCESS**

The external GitHub Actions Node.js deprecation warning is runner/action infrastructure and is not a project compiler warning.

## Scope audit

Comparison:

`7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3..81d9de1b4a579f6029d6dfd7a7d5aa938505cf40`

Result:
- ahead 10
- behind 0
- merge base exactly F8 closure
- only five paths changed:
  1. `.planning/phases/10-f9-utilities/PLAN.md`
  2. `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`
  3. `docs/superpowers/specs/2026-10-11-f9-offline-utilities-design.md`
  4. `tests/SGPdf.App.Tests/PdfiumUtilityOverlayCapabilityTests.cs`
  5. `tests/SGPdf.App.Tests/UtilityOverlayNativeHarness.cs`

No production source under `src/` changed. No NuGet/package/lock changes. No qpdf/pdfcpu. No second PDF engine. No network/cloud/account/API-key behavior.

## Decision for F9.1

**GO:** text watermark / page-number overlay foundation.  
**GO:** image watermark foundation, with opacity validated by render rather than reopened image fill-color getter.  
**NO-GO:** unsupported font glyphs; fail closed.  
**NO-GO:** claiming universal Unicode support.

## Gates not run

- Manual Windows UI QA: **NOT RUN**
- Adobe Acrobat external-reader QA: **NOT RUN**
- Foxit external-reader QA: **NOT RUN**
- Edge external-reader QA: **NOT RUN**

## Next task

Task 2 — pure managed utility settings, page-range resolution and page/Bates numbering formatter.

Task 2 is not started by this checkpoint.
