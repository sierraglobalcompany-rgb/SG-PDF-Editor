# F12.2 Checkpoint — Task 1 capability/dependency gate

Date: 2026-10-13
Branch: `feat/f12-professional`

## Exact branch state before checkpoint

Pre-checkpoint HEAD: `32335b1b3403fe663dca349ed73c5c4086f73950`
Commit: `test(f12.2): require pinned PKCS runtime dependency`

## Status

F12.2 Task 1 remains **IN PROGRESS**.

The capability gate has now reached a valid RED:

- pinned x64 PDFium loads successfully;
- required exports are present:
  - `FPDF_GetSignatureObject`
  - `FPDFSignatureObj_GetContents`
  - `FPDFSignatureObj_GetByteRange`
  - `FPDFSignatureObj_GetSubFilter`
- PDFsharp 6.2.4 exposes `DigitalSignatureHandler`;
- PDFsharp 6.2.4 exposes `IDigitalSigner` from assembly `PdfSharp.System`;
- .NET 10 runner can load `System.Security.Cryptography.Pkcs` major 10;
- the valid RED fails because `src/SGPdf.App/SGPdf.App.csproj` does **not yet declare** `System.Security.Cryptography.Pkcs` 10.0.12 as a direct runtime dependency.

CI evidence for valid RED:

- workflow run: `38340446791`
- exact SHA: `32335b1b3403fe663dca349ed73c5c4086f73950`
- restore: PASS
- Release build: PASS, 0 warnings, 0 errors
- tests: 1111 PASS + 1 intentional RED failure = 1112 total
- failing assertion: missing direct `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="10.0.12" />`

## Earlier RED corrections

Two earlier failures were diagnostic noise and were corrected before accepting RED:

1. native DLL discovery selected the wrong architecture and threw `BadImageFormatException`;
2. `IDigitalSigner` was initially looked up in assembly `PdfSharp`; the correct assembly for PDFsharp 6.2.4 is `PdfSharp.System`.

These are test-gate corrections only, not runtime architecture changes.

## GREEN plan — NOT APPLIED

Next narrow step is one atomic GREEN commit only:

1. add direct `System.Security.Cryptography.Pkcs` `10.0.12` PackageReference to `src/SGPdf.App/SGPdf.App.csproj`;
2. update app + tests lock files to resolve 10.0.12 consistently;
3. add dependency entry to `third_party/manifest.json`;
4. add MIT license notice file;
5. run capability test + full Windows CI exact-head.

No signing implementation, UI, certificate loading, writer, validator, or Task 2 work should start until Task 1 is GREEN.

## Important implementation note

A GitHub blob containing a candidate `SGPdf.App.csproj` with the PKCS reference was created while preparing the GREEN, but it is **not referenced by any tree or commit** and therefore is **not part of the branch/repository state**. Resume from committed HEAD/checkpoint, not from that orphan blob.

## Guardrails

- KISS / Ley del Ruido remain mandatory.
- No second PDF engine.
- No BouncyCastle unless future evidence proves it necessary.
- No timestamp/TSA in this MVP slice.
- No merge to `main` without explicit user approval.
- STOP after this checkpoint until the user explicitly asks to continue.
