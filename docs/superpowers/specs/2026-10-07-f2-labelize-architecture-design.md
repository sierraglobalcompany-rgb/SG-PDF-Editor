# F2 Labelize Architecture — Design

**Date:** 2026-10-07  
**Status:** proposed after approved Gate ZPL-A engine decision  
**Scope:** runtime architecture for F2 Etiquetas ZPL; no product code in this document

## 1. Intent

SG PDF Editor must replace the current manual Labelary workflow with a private, offline Windows flow for Mercado Libre ZPL files. The user opens `.zpl`, `.txt` or `.prn`, previews each printable design, keeps `^PQ` as quantity metadata instead of rendering duplicate copies, chooses quantity/layout/paper size, exports PDF and prints through Windows without sending customer data to a web service.

Non-negotiable constraints remain: Windows x64, C#/.NET 10/WPF, KISS/YAGNI, main features fully offline, no commercial runtime license, no customer ZPL committed to Git, and no merge to `main` without explicit approval.

## 2. Gate ZPL-A decision

The approved engine is **Labelize 1.7.0**, replacing BinaryKits.Zpl as the preferred F2 runtime engine.

Decision evidence from the throwaway spike branch `spike/f1-zpl-gate-a`:

- final verified head: `2df2f374ae6124729389640425dc8334d0647f9f`;
- final Windows workflow run: `37577735748` = success;
- both engines rendered the synthetic command corpus and both produced decodable Code128/QR after alpha-aware comparison;
- Labelize CLI throughput in the final timing pass was ~59–61 designs/s versus ~26–28 designs/s for the BinaryKits in-process probe;
- Labelize Windows executable size: 5,860,352 bytes;
- BinaryKits probe framework-dependent publish: ~16.9 MB / 14 files;
- on `^FT + ^BQ`, BinaryKits produced a 60 px vertical displacement versus Labelize while both symbols remained decodable; this matches a known open BinaryKits QR positioning risk;
- Labelize 1.7.0 has current golden/reference work against Labelary and active 2026 maintenance.

The first spike comparison accidentally treated BinaryKits transparent pixels as black. The final verification composites both renderers over white before fidelity/decode checks. Only the corrected verification is authoritative for fidelity.

Gate status after this design: synthetic evidence is sufficient to select the engine; **private real Mercado Libre samples and physical print QA remain acceptance work and are never versioned**.

## 3. Chosen integration model

Use Labelize as a **bundled local sidecar executable**, not as an HTTP server and not through a custom Rust/C ABI wrapper.

```text
WPF / C#
   ↓
ZplDocumentParser (managed, pure)
   ↓
LabelizeProcessRenderer
   ↓ local child process
labelize.exe 1.7.0 `convert`
   ↓
PNG / PDF files in request-scoped temp directory
```

Why this model:

- smallest integration surface;
- no local server lifecycle, port, firewall or HTTP concerns;
- no Rust toolchain required to build normal C# product code;
- process isolation contains native/Rust failures outside the WPF process;
- measured CLI startup overhead is already acceptable for the expected workflow because each unique design renders once, not once per `^PQ` copy;
- easy to pin and replace one executable if a later audited Labelize release is adopted.

Rejected for F2:

1. **BinaryKits in-process:** simpler C# dependency, but lost the gate on fidelity risk, footprint and measured throughput.
2. **`labelize serve` local HTTP:** avoids process-per-render overhead but adds service/port/lifecycle complexity without demonstrated need.
3. **Rust/C ABI DLL or WASM host:** tighter in-process integration but adds build/deployment complexity with no current product benefit.

## 4. Runtime packaging and version pin

Runtime engine pin: **Labelize 1.7.0 Windows x64 MSVC**.

The spike verified release archive SHA-256:

`cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`

Development/CI may download the pinned release archive through an explicit setup/packaging step and must verify this digest before use. Product runtime must never download Labelize automatically. Final packaged builds must ship the verified `labelize.exe` beside SG PDF Editor or in a fixed application subdirectory.

No auto-update. A future Labelize upgrade requires a deliberate dependency audit plus representative ZPL regressions.

`third_party/manifest.json` must record Labelize as a runtime dependency and the distribution must preserve its MIT license and required third-party notices.

### Font/license residual risk

Labelize 1.7.0 replaced its former proprietary Helvetica substitute with Apache-2.0 Roboto Condensed and also embeds permissively licensed DejaVu fonts. `ZplGSCustom.ttf` is documented by Labelize as inherited from the MIT zebrash project but its exact upstream origin is not perfectly documented.

Project treatment:

- acceptable for F2 development and Gate selection;
- record the provenance note in the third-party manifest/notices;
- before a public production installer, re-audit that file and, if legal/provenance confidence is insufficient, replace/redraw the small `^GS` glyph set rather than reverting the engine decision.

## 5. Managed document model

Keep parsing/orchestration in C# and rendering in Labelize.

Proposed minimal types:

```text
ZplDocument
  SourcePath
  Designs[]

ZplDesign
  Index
  OriginalBlock
  RenderBlock / normalized render input
  QuantityFromFile

ZplRenderOptions
  WidthMm
  HeightMm
  Dpmm
  OutputType

ZplRenderedLabel
  DesignIndex
  WidthMm
  HeightMm
  Dpmm
  LocalOutputPath or managed bytes
```

Do not create Domain/Application/Infrastructure layers. These types live under the existing KISS feature structure, e.g. `Features/Labels/`.

## 6. `^PQ` semantics

`^PQ` is application quantity metadata, not a render multiplier.

For each printable label design:

1. parse quantity parameter `a` from `^PQa,...`;
2. default to `1` when absent;
3. retain the value in `ZplDesign.QuantityFromFile`;
4. remove/neutralize `^PQ` in the render input sent to Labelize;
5. render that design once;
6. quantity selection later chooses:
   - file quantity;
   - one of each;
   - user custom quantity.

The parser must test `^PQ28` explicitly and must not generate 28 preview renders.

## 7. Multi-label and stored-format handling

Labelize CLI supports multiple `^XA…^XZ` blocks and writes one numbered output per rendered label. Stored formats `^DF/^XF` require parser state across the source stream.

Therefore F2 must **not blindly render isolated `^XA…^XZ` strings when doing so would remove required preceding template definitions**.

KISS rule for the first implementation:

- parse blocks in C# for metadata and quantities;
- build one normalized document stream that preserves source order/template definitions while removing print multiplication commands;
- invoke Labelize against that normalized stream;
- map Labelize outputs back to printable designs in source/render order;
- template-definition-only blocks are metadata/support blocks, not user-visible printable designs.

The `^DF/^XF` synthetic regression from Gate ZPL-A becomes a permanent F2 regression before preview UI is considered complete.

## 8. `LabelizeProcessRenderer`

Responsibilities only:

- resolve the trusted bundled executable path from `AppContext.BaseDirectory`;
- create one request-scoped temp directory;
- write normalized ZPL as UTF-8 without BOM;
- invoke `labelize.exe convert` using `ProcessStartInfo.ArgumentList`;
- pass explicit width, height, dpmm, format and output type;
- capture stdout/stderr;
- support `CancellationToken` by terminating the child process tree when needed;
- enforce a bounded timeout;
- validate expected output files exist and are non-empty;
- return controlled errors without crashing WPF;
- clean temp files/directories in `finally` after outputs have been loaded/copied.

Never:

- use shell command concatenation;
- use `labelize serve`;
- make network requests;
- write customer ZPL outside the request-scoped local temp area;
- leave temp labels behind after normal completion/cancellation.

## 9. Preview data flow

```text
Open file
  ↓
read locally
  ↓
ZplDocumentParser
  ↓
ZplDocument + quantities
  ↓
normalize render stream
  ↓
LabelizeProcessRenderer -> PNG
  ↓
load PNG fully into managed/WPF bitmap
  ↓
delete temp request
  ↓
show design + quantity metadata
```

Preview must display the design once regardless of `^PQ`.

No JPEG conversion. Preserve nearest-neighbor/thermal semantics where scaling is needed; prefer rerender at target label dimensions/DPMM instead of stretching barcode pixels.

## 10. PDF/export and print architecture

F2 separates rendering from sheet layout.

### Thermal single-label path

Labelize may render a label at the exact requested physical size/DPMM. Windows printing receives a page at that physical size with no silent fit-to-page scaling.

### A4/Carta/multi-label layout path

A later F2 slice composes already-normalized label outputs into 1/2/3/4/6/8/10/12/custom layouts. PDFsharp remains the planned composition dependency but is **not added until the export/layout slice actually needs it**.

Rules:

- physical dimensions are authoritative;
- do not silently distort barcode aspect ratio;
- no JPEG;
- no automatic shrink to printer imageable area;
- warn when driver imageable area clips requested content;
- use Windows driver printing, not RAW printer-language assumptions.

## 11. Error handling

User-facing failures must distinguish at least:

- file cannot be read / contains no printable ZPL;
- malformed or unsupported ZPL;
- Labelize executable missing or integrity/setup problem;
- renderer timeout/cancellation;
- renderer exit failure with sanitized diagnostic;
- expected output missing;
- barcode/QR validation failure;
- unsupported/impossible physical layout.

A render failure must not replace an already-valid preview with invalid state.

## 12. Testing strategy

### Automated synthetic corpus committed to repo

No customer data. Cover at minimum:

- multiple `^XA/^XZ` blocks;
- `^CI28` with Spanish accents/ñ;
- `^FH`;
- `^FB` long address;
- `^FR`;
- `^GFA`;
- `^BC` Code128 decode;
- `^BQ` QR decode;
- `^FT + ^BQ` positioning regression;
- `^PQ28` => one render + quantity 28;
- `^DF/^XF` stored format;
- composite shipping-style label.

ZXing remains test/validation tooling for Code128/QR decodability where applicable.

### Private local corpus

Real Mercado Libre labels stay under ignored private-fixture paths and never enter commits, CI artifacts or Graphify.

### Physical acceptance

Before F2 is fully accepted:

- real labels preview correctly;
- printed Code128/QR scan successfully;
- exact thermal dimensions verified;
- A4/Carta layouts verified;
- red disabled network test passes;
- customer data leaves no persistent temp residue after normal use.

## 13. Implementation slices

Keep sessions small:

### F2.1 — Parse/open/quantity

- open `.zpl/.txt/.prn`;
- pure C# `ZplDocumentParser`;
- designs + `^PQ` quantity metadata;
- synthetic tests only;
- no Labelize runtime call yet.

### F2.2 — Labelize adapter + preview

- pin/setup sidecar executable;
- process renderer;
- PNG preview;
- cancellation/error/temp cleanup;
- `^DF/^XF`, QR/Code128 regressions;
- no PDF composition yet.

### F2.3 — Quantity UX + label dimensions

- file/one/custom quantity;
- thermal presets/custom physical size;
- rerender when physical render options change.

### F2.4 — Layout + PDF export

- layouts 1/2/3/4/6/8/10/12/custom;
- A4/Carta/custom;
- add PDFsharp only here if still the smallest reliable composition path;
- export verification.

### F2.5 — Windows thermal print

- exact physical size;
- capabilities/imageable-area checks;
- no silent scaling;
- Microsoft Print to PDF only as supplemental diagnostic, not a substitute for thermal QA.

### F2.6 — Validation + hardening

- automatic barcode/QR decode checks;
- private corpus run;
- physical scanner/printer QA;
- offline/temp/privacy audit;
- docs/history/state closure.

## 14. Definition of Done for the engine decision

Labelize becomes the official F2 engine once this design is accepted and the implementation plan is approved. BinaryKits is removed from preferred runtime architecture and remains only historical Gate evidence; it is not carried as a fallback runtime dependency.

No dual-engine abstraction is built. If Labelize later fails a real requirement, reopen the decision with evidence instead of maintaining two engines preventively.
