# F10 Task 2 — Tesseract Windows distribution gate

Date: 2026-10-11
Branch: `feat/f10-local-ocr`
Decision: **GO**

## Contract

F10 uses one private, pinned Windows x64 Tesseract sidecar. Product runtime never downloads OCR binaries/models and never falls back to PATH, registry, or a global Tesseract installation. PDFium remains the PDF owner/writer.

## Selected runtime

- Upstream: `tesseract-ocr/tesseract`
- Release tag: `5.5.3`
- Windows installer archive: `tesseract-ocr-w64-setup-5.5.3.20260724.exe`
- Product version reported by staged runtime: `tesseract v5.5.3.20260724`
- Installer SHA-256: `bee9e3434bd94fd65387d9be28cd467a41f61b1275383b55b0f59a1331270ae4`
- License: Apache-2.0

Development/CI downloads the exact installer, verifies SHA-256, extracts the NSIS archive with 7-Zip without installing it, and stages the private runtime under `third_party/runtime/tesseract`. The staged directory is ignored by Git and copied conditionally into application output/publish.

## Language data

Official `tesseract-ocr/tessdata_fast` is pinned to commit:

`87416418657359cb625c412a48b6e1d6d41c29bd`

### Spanish

- File: `spa.traineddata`
- Size: `2294433` bytes
- Git blob SHA-1: `72e901f13ca52cfe34cf239a368b9ed3c0ddaf26`
- SHA-256: `6f2e04d02774a18f01bed44b1111f2cd7f3ba7ac9dc4373cd3f898a40ea6b464`

### English

- File: `eng.traineddata`
- Size: `4113088` bytes
- Git blob SHA-1: `bbef4675053b5b468cdb477053e28b1c698ba08e`
- SHA-256: `7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2`

`osd.traineddata` is intentionally not required by F10 V1.

## TDD evidence

### RED

Commit `887d4508565c95fdb754e9d1b86b1143b3094a35`

Windows workflow `38155090121`, job `114516308835`:

- Build: PASS, 0 warnings / 0 errors.
- Existing tests: 950 PASS.
- New distribution gate: 4 expected FAIL because no Tesseract runtime/manifest had been staged yet.
- Failure mode was explicit missing private runtime/manifest; no unrelated regression.

### Characterization failure

The first real staging attempt successfully extracted the official NSIS archive (139 files, `Everything is Ok`) but failed before build because the provisional Spanish model size was wrong. The pinned Git tree established the exact size as `2294433` bytes. The setup script was corrected without weakening Git blob verification.

### Intermediate GREEN

Commit `ae4e39f83469d0d5fd496d797aa7232edafbbe49`

Windows workflow `38156277288`, job `114519808386`:

- Official installer extraction: PASS.
- Private `tesseract.exe --version`: PASS.
- Exact pinned `spa` / `eng`: PASS.
- Deterministic Spanish recognition: PASS.
- Deterministic English recognition: PASS.
- Build: 0 warnings / 0 errors.
- Tests: 954 PASS / 0 FAIL / 0 SKIP.

That run measured the final traineddata SHA-256 values listed above.

### Hardened exact-head GREEN

Exact head: `81234aa8f0aa80e9006c5aa57e0a5e813f222f6e`

Windows workflow `38156684777`, job `114521000242`:

- Repository hygiene: PASS.
- Labelize 1.7.0 staging: PASS.
- qpdf 12.4.2 staging: PASS.
- Tesseract 5.5.3 staging: PASS.
- Installer SHA-256 verification: PASS.
- `spa.traineddata` exact size + Git blob SHA-1 + SHA-256: PASS.
- `eng.traineddata` exact size + Git blob SHA-1 + SHA-256: PASS.
- Locked restore: PASS.
- Release build: **0 warnings / 0 errors**.
- Tests: **954 PASS / 0 FAIL / 0 SKIP**.

The only warning in the Actions log is external GitHub Actions Node.js deprecation noise; it is not a product/compiler warning.

## Repository changes accepted by the gate

- `tools/setup-tesseract.ps1`
- `.github/workflows/build.yml`
- `src/SGPdf.App/SGPdf.App.csproj`
- `src/SGPdf.App/Features/Ocr/TesseractRuntime.cs`
- `tests/SGPdf.App.Tests/TesseractRuntimeTests.cs`
- `tests/SGPdf.App.Tests/TesseractDistributionGateTests.cs`
- `third_party/licenses/Tesseract-Apache-2.0.txt`
- `third_party/licenses/Tessdata-Apache-2.0.txt`
- `third_party/manifest.json`

`third_party/runtime/**` remains untracked and reproducibly staged.

## Result

**GO.** The selected Windows runtime has traceable upstream provenance, exact installer/model digests, recorded Apache-2.0 licenses, private-path resolution, deterministic Spanish/English recognition, and a green Windows suite. Task 3 may build the managed OCR request/eligibility/process layer on top of this runtime.

Manual installed-application OCR UX, Adobe Reader, Foxit, Edge, and private real-scan corpus QA remain **NOT RUN** and are not claimed by this gate.
