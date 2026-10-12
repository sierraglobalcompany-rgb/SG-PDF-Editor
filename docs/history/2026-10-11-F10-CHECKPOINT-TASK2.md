# F10 CHECKPOINT — Task 2 Tesseract runtime staging

Date: 2026-10-11
Branch: `feat/f10-local-ocr`

## Status

**PAUSED BY USER REQUEST.**

F10 remains in progress. Task 1 is closed GREEN. Task 2 is mid-implementation and must NOT be treated as complete.

## Task 1 — CLOSED GREEN

PDFium true-invisible searchable-text capability gate is GO.

Final evidence already recorded in:
- `docs/history/2026-10-11-F10-TASK1-INVISIBLE-TEXT-CAPABILITY-GATE.md`

Verified properties include:
- pinned PDFium exports `FPDFTextObj_SetTextRenderMode`;
- invisible render mode `3` works;
- Unicode Spanish text survives Save As + reopen;
- text is searchable and has selectable geometry;
- source PDF remains byte-for-byte unchanged;
- source/output render is byte-for-byte identical in synthetic portrait and 90° fixtures.

Task 1 exact-head CI before Task 2 work:
- SHA `b1a45d381226f3505433c132aa6a97b63c0d08cc`
- workflow `38154194827`
- job `114513662382`
- build: 0 warnings / 0 errors
- tests: 945 PASS / 0 FAIL / 0 SKIP

## Task 2 — CURRENT STATE

Goal: private/offline Tesseract runtime for F10, with explicit provenance, fixed versions, fixed language data, no global PATH fallback, no runtime network dependency.

### Runtime resolver

Added:
- `src/SGPdf.App/Features/Ocr/TesseractRuntime.cs`
- `tests/SGPdf.App.Tests/TesseractRuntimeTests.cs`

Contract:
- only resolves app-owned `tesseract/tesseract.exe`;
- only resolves app-owned `tesseract/tessdata`;
- requires `spa.traineddata` + `eng.traineddata`;
- does not require `osd.traineddata` in F10 V1;
- deliberately fails instead of using a globally installed Tesseract.

Resolver GREEN was reached functionally with 950/950 tests, then nullable warnings were cleaned. The later RED distribution run confirmed build 0 warnings / 0 errors.

### Distribution RED — CONFIRMED

Commit:
`887d4508565c95fdb754e9d1b86b1143b3094a35`

Workflow:
`38155090121`

Job:
`114516308835`

Result:
- Release build: PASS
- 0 warnings / 0 errors
- tests: 950 PASS / 4 FAIL / 0 SKIP
- all 4 failures are the intended new Tesseract distribution gates because the private runtime/manifest has not yet been staged.

The RED tests require:
- exact private runtime version `5.5.3.20260724`;
- runtime under app-owned directory only;
- explicit `--tessdata-dir`;
- `spa` and `eng` available;
- staged `runtime-manifest.json`;
- deterministic OCR recognition in Spanish and English;
- exact provenance fields/hashes.

### Upstream source selected for characterization

Official upstream release:
- Tesseract tag `5.5.3`
- installer `tesseract-ocr-w64-setup-5.5.3.20260724.exe`
- installer SHA-256 fixed in test/script:
  `bee9e3434bd94fd65387d9be28cd467a41f61b1275383b55b0f59a1331270ae4`

`tessdata_fast` is NOT allowed to float on `main`.
Pinned commit:
`87416418657359cb625c412a48b6e1d6d41c29bd`

Pinned Git blob objects:
- `spa.traineddata`: `72e901f13ca52cfe34cf239a368b9ed3c0ddaf26`
- `eng.traineddata`: `bbef4675053b5b468cdb477053e28b1c698ba08e`

Important upstream installer behavior already verified from `nsis/tesseract.nsi`:
- installer includes `tesseract.exe`, runtime DLLs, configs/tessconfigs and license/doc files;
- optional languages are normally downloaded from `tessdata_fast/main`;
- therefore SG PDF must NOT execute that floating-language behavior;
- strategy is to extract the verified installer without installing it, then inject only pinned `spa` + `eng` from the exact tessdata commit.

### Last implementation added before pause

Pre-checkpoint implementation head:
`63281077671f091183fe79948688ef88147e50f0`

Added:
- `tools/setup-tesseract.ps1`

Current script intent:
1. download official upstream 5.5.3 Windows installer;
2. verify exact installer SHA-256;
3. extract NSIS payload with 7-Zip without installing it;
4. locate `tesseract.exe` + private runtime DLLs;
5. stage under `third_party/runtime/tesseract`;
6. download only pinned `spa` + `eng` from exact `tessdata_fast` commit;
7. verify exact expected sizes + Git blob SHA-1;
8. calculate and print SHA-256 for both language files;
9. verify staged `tesseract --version` reports `5.5.3.20260724`;
10. write `runtime-manifest.json` with provenance and calculated hashes.

## VERY IMPORTANT — NOT YET DONE

The new `tools/setup-tesseract.ps1` has **NOT** yet been wired into `.github/workflows/build.yml`.

It has **NOT** yet been executed in CI.

Therefore the following are NOT yet proven:
- that 7-Zip successfully extracts the official NSIS installer in the GitHub Windows runner;
- exact extracted file layout;
- exact DLL set needed by the private runtime;
- actual SHA-256 values of pinned `spa.traineddata` and `eng.traineddata`;
- that `tesseract --version` runs successfully from the staged private directory;
- that deterministic Spanish/English OCR tests pass;
- that distribution RED becomes GREEN;
- that final runtime packaging into app output/publish is correct.

Do NOT claim Task 2 complete until those gates pass on an exact SHA.

## Next step when resuming

Resume exactly here:

1. inspect/validate `tools/setup-tesseract.ps1` against current repo head;
2. wire `Stage pinned Tesseract 5.5.3` into `.github/workflows/build.yml` after qpdf staging and before restore/build;
3. add conditional `Content` mapping in `src/SGPdf.App/SGPdf.App.csproj` for `third_party/runtime/tesseract/**` → output `tesseract/**`;
4. run Windows CI on exact SHA;
5. if extraction/staging fails, diagnose from logs and make the minimum correction;
6. capture actual `spa`/`eng` SHA-256 from CI logs;
7. pin those SHA-256 values explicitly in script/tests/manifest contract;
8. rerun exact-head Windows CI until build 0/0 and all tests GREEN;
9. only then document Task 2 closure and move to Task 3.

## Guardrails

- No merge to `main` without explicit approval.
- Keep `main` frozen.
- No runtime download/network dependency in the product.
- No fallback to global/system Tesseract.
- No floating `tessdata_fast/main`.
- No second PDF engine unless a concrete capability gap is proven.
- Continue RED → minimum GREEN → exact-head CI → scope audit → checkpoint.
- Manual Adobe/Foxit/Edge/real scan corpus QA remains NOT RUN unless actually executed.
