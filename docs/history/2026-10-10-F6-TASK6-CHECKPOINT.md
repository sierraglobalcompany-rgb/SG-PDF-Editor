# F6 — Task 6 checkpoint — transactional image writer + validator

**Date:** 2026-10-10  
**Branch:** `feat/f6-images`  
**Task-5 checkpoint:** `09246b43e6c5036ab7ff05b51e3068672cbb9930`  
**RED head:** `f219fa9d9805ee9e4c2676ed4e3864e4a2201b07`  
**Functional GREEN head before this checkpoint:** `449c1da93114d4faf0bc5cd035137e63a9402b35`  
**Scope:** Task 6 only — F6.5 transactional writer and post-write validation.

## 1. Status

Task 6 functional code/tests are GREEN on exact-head Windows CI before this checkpoint commit.

- Functional workflow run: `38021330652`
- Exact functional head: `449c1da93114d4faf0bc5cd035137e63a9402b35`
- Restore locked dependencies: PASS
- Release build: PASS
- Build warnings: 0
- Build errors: 0
- Tests: 631 passed / 0 failed / 0 skipped

## 2. RED witness

- RED commit: `f219fa9d9805ee9e4c2676ed4e3864e4a2201b07`
- RED workflow run: `38021122886`
- Exact checkout: `f219fa9d9805ee9e4c2676ed4e3864e4a2201b07`
- Build: PASS, 0 warnings / 0 errors
- Existing suite: 609 PASS
- New Task-6 cases: 22 intended FAIL
- Total: 609 passed / 22 failed / 0 skipped / 631

Every new failure was caused by the intentionally absent `PdfImageEditWriter` or `ImageEditOutputValidator`; no pre-existing test failed.

## 3. GREEN implementation

Created:

1. `src/SGPdf.App/Pdf/PdfImageEditWriter.cs`
2. `src/SGPdf.App/Pdf/ImageEditOutputValidator.cs`
3. `tests/SGPdf.App.Tests/PdfImageEditWriterTests.cs`
4. `tests/SGPdf.App.Tests/ImageEditOutputValidatorTests.cs`

The writer:

- blocks destination == source;
- blocks password-opened workspaces;
- blocks signed PDFs;
- validates source fingerprint before materialization;
- writes to a temporary file in the destination directory;
- always reopens the unchanged original source;
- replays the complete logical `EditedStates`;
- resolves all original image handles for a page before any remove/reinsert operation;
- applies matrix, replacement bitmap, delete and exact-index reorder changes;
- regenerates page content once per touched page;
- saves through PDFium `FPDF_SaveAsCopy` with a local `FILEWRITE` callback;
- releases native resources before managed reopen/render validation;
- publishes only after validation with `File.Replace` / `File.Move`;
- cleans temporary files best-effort in `finally`;
- never modifies the source PDF.

The writer intentionally does **not** call `MarkSavedBaseline()` itself; save orchestration/UI remains responsible for advancing the saved baseline only after a successful save.

## 4. Original-object resolution invariant

Before mutating a touched page, all edited source objects are resolved and validated using:

- original page-object ordinal;
- object type must still be image;
- original matrix within tolerance;
- original bounds within tolerance;
- original pixel dimensions.

This prevents ordinal drift when delete/reorder operations would otherwise alter subsequent object indices.

A regression test proves a page with `[image, vector, image]` can delete the first image and move the second image to object index 0 without losing or misidentifying it.

## 5. Replacement + alpha evidence

Replacement uses the in-memory `ImageReplacementAsset`; the original PNG/JPEG file is not required after selection.

Representative GREEN tests prove:

- an opaque PNG replacement survives save/reopen/extraction;
- a transparent PNG replacement survives save/reopen/extraction with both alpha < 255 and alpha == 255 represented;
- replacement still works after the selected source image file is deleted.

## 6. Transform and second-save evidence

GREEN tests prove save/reopen persistence for:

- move;
- resize;
- rotate;
- delete;
- exact-index reorder;
- replacement;
- transparent replacement.

The second-save test proves the writer always reopens the **original source** and replays the complete current logical state, including states that already belong to the saved baseline plus later edits.

## 7. Validator

`ImageEditOutputValidator` reopens the temporary output and validates:

- page count;
- page size;
- page rotation;
- sequential render of every page at 36 DPI;
- edited image count on touched pages;
- stable-ordinal edited matrices where delete/reorder has not changed ordinals;
- finite valid edited bounds;
- plausible replacement image metadata.

Invalid/reopen-failing output is rejected as `InvalidDataException`.

## 8. Transactional safety evidence

Tests cover:

- destination equals source blocked;
- missing destination directory blocked;
- stale source blocked;
- missing source blocked;
- password workspace blocked;
- signed source blocked;
- native mutation failure preserves existing destination;
- native save failure preserves existing destination;
- validation failure preserves existing destination;
- cancellation preserves existing destination;
- temporary cleanup on failure;
- source bytes remain untouched.

## 9. Scope audit

Relative to Task-5 checkpoint `09246b43...`, Task 6 touches exactly four functional/test files:

- `src/SGPdf.App/Pdf/PdfImageEditWriter.cs`
- `src/SGPdf.App/Pdf/ImageEditOutputValidator.cs`
- `tests/SGPdf.App.Tests/PdfImageEditWriterTests.cs`
- `tests/SGPdf.App.Tests/ImageEditOutputValidatorTests.cs`

No UI, `PdfiumNative`, FIRMAR, ORGANIZAR, LEER, ZPL, package/lockfile or workflow changes were made.

`main` remains unchanged at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.

## 10. Boundaries preserved

- PDFium remains the only PDF editing engine.
- No new dependency added.
- No cloud/network dependency added.
- Source PDF is never overwritten.
- No native handles are stored in UI/domain state.
- No merge to `main`.
- No F6 PR created.
- Manual Windows UX QA remains NOT RUN.
- Task 7 has NOT STARTED.

## 11. Resume point

Stop here.

On the next explicit `continua`, first verify the exact branch head and read the exact Task-7 section from the F6 plan/spec. Do not infer or start Task 7 from this checkpoint alone.
