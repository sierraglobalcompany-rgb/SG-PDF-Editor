# F9 Task 2 — Managed settings, page range and numbering formatter

## DATE

- Local: 2026-10-10 23:19 — America/Bogota
- GitHub/UTC date: 2026-10-11

## STATUS

`AUTOMATED PASS` at the functional head. This checkpoint commit must also receive exact-head CI before the task is considered fully closed by the handoff protocol.

Manual/physical Windows QA: `NOT RUN` — Task 2 is managed-only and no manual result is claimed.

## BRANCH

`feat/f9-offline-utilities`

## STACKED BASE

- F8 closure: `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`
- F9 Task 2 base / Task 1 checkpoint: `e972a84944d1c9da2f037cb66ff8064d2afeaf85`

## HEAD FUNCIONAL

`50581b4265266936ad751ebc175cfe0e798c9b8b`

Commit:

`feat(f9): add managed utility settings and numbering formatter`

## HEAD CHECKPOINT

This document commit. Resolve the exact SHA from Git history/GitHub after commit creation.

The SHA is intentionally not self-embedded by rewriting this file after creation: doing so would create a new SHA and violate the no-auto-churn checkpoint rule. The exact checkpoint SHA and its CI are authoritative in GitHub.

## RED

### First RED attempt — harness failure, not accepted as behavioral RED

- SHA: `ac206f41d44e7ba8b5d8a1d21818b2d7709d77cf`
- Workflow: `38110885252`
- Job: `114386024511`
- Classification: `TEST/HARNESS`
- Root cause: the repository's xUnit version has `Assert.NotNull(...)` returning `void`; three reflection helpers attempted to return that assertion result.
- Result: build failed before tests, so this was **not** used as behavioral RED evidence.
- Production files touched: none.

### Valid behavioral RED

- SHA: `9087fdd8906faace5245ed3bc6e6f3f96194b42a`
- Workflow: `38110959804`
- Job: `114386247800`
- Restore: PASS
- Build: PASS — 0 warnings / 0 errors
- Tests: 854 PASS / 20 expected FAIL / 0 SKIP / 874 total
- Witness: the 20 Task 2 tests failed because the managed utility types/behavior did not yet exist; the complete pre-existing suite remained green.

## GREEN / CI exact-head — functional head

- SHA: `50581b4265266936ad751ebc175cfe0e798c9b8b`
- Workflow: `38111140621`
- Job: `114386780998`
- Restore: PASS
- Build: PASS — 0 warnings / 0 errors
- Tests: 874 PASS / 0 FAIL / 0 SKIP
- Result: SUCCESS

External runner note: GitHub emitted the existing Node.js deprecation warning for `actions/checkout@v4` / `actions/setup-dotnet@v4`. It is CI maintenance noise, not a C# warning or Task 2 product failure.

## SCOPE exacto

Compare:

`e972a84944d1c9da2f037cb66ff8064d2afeaf85..50581b4265266936ad751ebc175cfe0e798c9b8b`

Result:

- 3 commits ahead / 0 behind
- exact merge-base = Task 2 base
- exactly 6 changed paths at the functional head

### Production

- `src/SGPdf.App/Features/Utilities/UtilityPageRange.cs`
- `src/SGPdf.App/Features/Utilities/TextOverlaySettings.cs`
- `src/SGPdf.App/Features/Utilities/PageNumberingSettings.cs`
- `src/SGPdf.App/Features/Utilities/PageNumberFormatter.cs`

### Tests

- `tests/SGPdf.App.Tests/UtilityPageRangeTests.cs`
- `tests/SGPdf.App.Tests/PageNumberFormatterTests.cs`

### Documentation

- this Task 2 checkpoint only, after the functional head

## FILES / PATH FAMILIES

Task 2 stayed in the approved managed-only families:

- `src/SGPdf.App/Features/Utilities/**`
- Task 2 unit tests
- `docs/history/**` checkpoint

No changes to:

- PDFium/native interop
- writers
- WPF/XAML/UI
- `.csproj`
- package lock files
- third-party manifest
- CI workflow
- `main`

## IMPLEMENTED CONTRACT

### `UtilityPageRange`

- typed `All`, `Current`, `Explicit` range model
- `Resolve(int pageCount)` returns ordered 0-based indices
- inclusive explicit ranges
- page-count and bounds validation
- deterministic output
- read-only returned collection
- no textual page-range parser or grammar

### `PageNumberingSettings`

- start number
- prefix
- suffix
- zero-padding width
- null/negative-padding validation
- `NumberAt(int selectedPageOrdinal)` with checked arithmetic

### `PageNumberFormatter`

- invariant decimal formatting
- prefix + number + suffix
- zero-padding without truncation
- large `long` values
- deterministic output independent of current culture
- prefix/suffix Unicode validation using `string.EnumerateRunes()`
- glyph coverage authority reused: `FallbackFontAsset.SupportsRune`
- unsupported rune `東` fails closed with `U+6771`
- supported sentinel remains `NIÑO Bogotá — € Ω`

### `TextOverlaySettings`

Small immutable managed settings record for the later overlay planner/writer path; it does not perform PDF work and does not create a utility framework.

## BUGS / INCIDENTS

1. Initial RED harness compile error described above.
   - Root cause isolated before touching production.
   - Fixed in test-only commit `9087fdd8906faace5245ed3bc6e6f3f96194b42a`.
   - No workaround or production patch was added.
2. No product bug found during GREEN.

## DECISIONS

- Reuse `FallbackFontAsset.SupportsRune`; no second font coverage table.
- Use `Rune`, not `char`, for Unicode coverage.
- Keep range input typed; no `1,3-5,7` parser in Task 2.
- Keep settings small and immutable; no base class/factory/provider/framework.
- Bates remains numbering configuration, not a separate subsystem/writer.
- Checked arithmetic is owned by numbering settings where the selected-page ordinal advances the start number.
- No new package/dependency.

## WHAT WAS NOT DONE

Intentionally not implemented in Task 2:

- PDFium calls
- PDF writer / output validator / preflight
- watermark materialization
- planners / geometry
- UI / WPF / XAML
- image watermark product path
- qpdf / pdfcpu
- protection
- optimize / repair
- global documentation reconciliation
- repository/PR/branch cleanup
- `main` changes
- merge
- F9 Task 3 or later

## MANUAL QA STATE

`NOT RUN`

No manual Windows, Adobe/Foxit/Edge, printer, hardware, private-corpus or physical-network QA is claimed by Task 2.

## NEXT EXACT TASK

`F9 Task 3 — transactional text overlay writer`

Expected owners from the canonical plan:

- `PdfTextOverlayWriter`
- `PdfTextOverlayOutputValidator`
- focused `PdfUtilityPreflight`

## PROHIBITIONS FOR NEXT TASK

Before/while executing Task 3:

- do not start Task 4 planners or Task 5 UI
- do not add qpdf/pdfcpu
- do not touch `main` or merge
- reuse `PdfEditSourceFingerprint` semantics; do not create per-utility fingerprint variants
- reuse `PdfiumRuntime.NativeGate`; do not introduce parallel synchronization
- inspect/reuse `CidType2FontMapBuilder` / `FallbackFontAsset` rather than duplicating CID/font logic
- block signed/password-opened sources according to the approved F9 preflight policy
- preserve destination atomicity, cancellation cleanup, validator-before-publish and source-race recheck
- audit rotation/CropBox geometry before claiming overlay placement PASS

## STOP

Task 2 stops here after checkpoint exact-head CI is verified. Do not begin Task 3 in the same gate.
