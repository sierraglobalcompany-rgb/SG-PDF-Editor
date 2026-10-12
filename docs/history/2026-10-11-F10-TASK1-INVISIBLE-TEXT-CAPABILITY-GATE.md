# F10 Task 1 — PDFium true-invisible searchable-text capability gate

Date: 2026-10-11

## Decision

**GO** for the PDFium searchable-layer path used by F10.

The exact pinned runtime used by SG PDF Editor (`bblanchon.PDFium.Win32 156.0.8076`) exports and successfully executes `FPDFTextObj_SetTextRenderMode`. True invisible text render mode is fixed to PDFium value `3` (`FPDF_TEXTRENDERMODE_INVISIBLE`). No second PDF engine is justified by this gate.

## Scope proven

The capability fixture is deterministic and synthetic. It creates a PDF page whose visible content is a raster/JPEG image only, with no searchable source text, then adds one Unicode CID Type2 text object using the existing pinned DejaVu Sans path.

Both portrait and `/Rotate 90` variants prove after Save As + reopen that:

- the source PDF has no searchable `NIÑO`, `Bogotá`, or `12345` text before mutation;
- `FPDFTextObj_SetTextRenderMode(..., 3)` succeeds;
- the inserted text object reopens with `FPDFTextObj_GetTextRenderMode == 3`;
- the original source file is byte-for-byte unchanged;
- source and output PDFium renders are byte-for-byte identical at 72 DPI;
- Unicode Spanish text including `número`, `NIÑO`, `Bogotá`, `información`, and `útil` extracts after reopen;
- existing F4 search finds `NIÑO`, `Bogotá`, and `12345`;
- text rectangles are returned and intersect the intended raster-text region;
- the same invariants hold for the 90-degree page.

This is a capability proof only. It does **not** add the F10 product OCR writer, Tesseract, OCR UI, preprocessing, or production page-eligibility behavior.

## TDD evidence

### API availability RED

Commit: `bde6ab3d880ae0bd4ab2857aeb0c80869f49c420`
Workflow: `38153519619`
Job: `114511679784`

- Release build: PASS, 0 warnings / 0 errors.
- Tests: 942 PASS / 1 FAIL / 0 SKIP.
- Failure was the intentionally missing managed OCR constant/interoperability surface.
- The native-export assertion had already passed before that failure, proving the pinned DLL exported `FPDFTextObj_SetTextRenderMode`.

### Minimum interop GREEN

Commit: `d5e2b60f7846b3d10c3f2baa9bdca5f87e197068`
Workflow: `38153613713`
Job: `114511955596`

- Repository hygiene/staging/restore/build/test steps: PASS.
- Production change was limited to `PdfiumNative.Ocr.cs`: invisible render-mode constant plus one P/Invoke setter.

### Behavioral RED

Commit: `4c3e6e31149bc1c24e8edef80cc1604a8e0850e0`
Workflow: `38153851572`
Job: `114512642216`

- Release build: PASS, 0 warnings / 0 errors.
- Tests: 943 PASS / 2 FAIL / 0 SKIP.
- Both intended theory cases failed only on the deliberate `NotImplementedException` in the portrait/90-degree characterization harness.

### Behavioral GREEN

Commit: `c8bad8b2f81ebbcc3afcec8f97961c4c362dd6f1`
Workflow: `38154062544`
Job: `114513268739`

- Release build: PASS, 0 warnings / 0 errors.
- Tests: **945 PASS / 0 FAIL / 0 SKIP** in 20 s.
- Portrait invisible-text proof: PASS.
- 90-degree invisible-text proof: PASS.
- Only external runner noise: GitHub Actions Node 20/punycode deprecation warnings; no product C# warnings.

## Files introduced by Task 1

- `src/SGPdf.App/Pdf/PdfiumNative.Ocr.cs`
- `tests/SGPdf.App.Tests/PdfiumOcrApiAvailabilityTests.cs`
- `tests/SGPdf.App.Tests/OcrInvisibleTextNativeHarness.cs`
- `tests/SGPdf.App.Tests/OcrInvisibleTextCapabilityTests.cs`
- this history record.

## Explicitly not run / not claimed

- Real-user scanned PDFs: **NOT RUN**.
- Adobe Acrobat Reader validation: **NOT RUN**.
- Foxit validation: **NOT RUN**.
- Edge PDF validation: **NOT RUN**.
- Manual Windows UI QA: **NOT RUN**.
- Private malformed/scan corpus: **NOT RUN**.

Those are later F10 gates and are not implied by this Task 1 GO.
