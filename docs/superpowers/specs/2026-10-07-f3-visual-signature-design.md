# F3 — Visual Signature — Design Specification

**Date:** 2026-10-07  
**Status:** written spec awaiting user review  
**Base:** F2.6 final head `6c7d60a5bad22db20860685e955aa5ef03fbfa19`  
**Branch:** `feat/f3-visual-signature`

## 1. Intent

F3 adds a practical, fully local **visual signature** workflow to SG PDF Editor. The user must be able to create or import a visual signature, place it precisely on a PDF page, move/resize/duplicate/delete it, and save a new PDF without modifying the original.

F3 is **not** cryptographic/digital signing. Cryptographic signatures remain a later professional slice.

The expanded F3 design supports three ways to obtain a signature:

1. import an already-transparent PNG;
2. import a photo/scan of a signature on white paper and remove/clean the background locally;
3. draw a signature directly with mouse, touch, or stylus.

All three sources converge to the same in-memory `SignatureAsset` so placement and PDF writing are implemented only once.

## 2. Product requirements covered

Existing requirements:

- **SIGN-01** Import transparent PNG.
- **SIGN-02** Drag, move, proportional resize, duplicate, delete.
- **SIGN-03** Correct UI ↔ PDF coordinate conversion.
- **SIGN-04** Save as a copy and reopen preserving position/transparency.

Approved F3 expansion:

- photo/scan → transparent signature from a dark/blue signature on white or near-white paper;
- local brightness/contrast/background-threshold cleanup and automatic crop;
- direct drawing using WPF ink input;
- optional local reusable signature library after the core creation/placement flows are stable.

## 3. Delivery slices

### F3.1 — Core placement + PDF save

Minimum complete signing loop:

- enter **Firmar** mode;
- import transparent PNG;
- place one or more copies on the current PDF page;
- drag/move;
- proportional resize;
- duplicate;
- delete;
- keep placement authoritative in PDF page coordinates;
- `Guardar como...` only;
- transactional save;
- reopen/render validation;
- original PDF remains untouched.

F3.1 intentionally limits pending visual-signature edits to the **current page**. If the user tries to navigate while unsaved signature edits exist, the app asks to save/discard/cancel rather than silently losing or moving edits. Multi-page signature sessions are deferred until this base proves stable.

### F3.2 — Photo/scan preparation

Create `SignatureAsset` from PNG/JPG photo or scan:

- assume signature is dark or blue on white/near-white paper;
- automatic background removal;
- gradual alpha edge rather than hard jagged threshold;
- automatic crop around non-background pixels;
- brightness;
- contrast;
- background-removal strength / threshold;
- output style: original ink, black, or blue;
- reset to original;
- checkerboard transparency preview;
- no AI, cloud API, OCR, or complex-background segmentation.

### F3.3 — Draw signature

Create `SignatureAsset` using WPF `InkCanvas`:

- mouse;
- touch;
- stylus/digital pen;
- black / blue;
- small set of stroke widths;
- undo;
- redo;
- clear;
- apply/cancel;
- transparent cropped output.

Advanced pressure-sensitive brush behavior is not required for F3.3. Native WPF stylus support may be used, but the first version must not grow into a general drawing engine.

### F3.4 — Local signature library

After F3.1–F3.3 are stable:

- save a prepared/drawn signature locally;
- choose a saved signature quickly;
- rename/delete saved assets;
- keep data under the current Windows user profile only;
- no account, cloud sync, telemetry, or runtime network.

F3.4 does not block SIGN-01..04 acceptance and may remain a later sub-slice if schedule/complexity argues for deferral.

## 4. Non-goals

F3 does **not** include:

- cryptographic/certificate signatures;
- requesting signatures from other people;
- remote signing workflows;
- cloud signature storage;
- AI/background removal for complex scenes;
- perspective correction for badly photographed pages;
- arbitrary photo editor features;
- general image-object editing (belongs to F6);
- date/name/text signature fields;
- stamps/seals beyond signature assets;
- automatic placement on many pages;
- freeform PDF vector-path signature authoring;
- flattening/rasterizing entire PDF pages.

## 5. Architecture

```text
PDF opened in current PdfDocumentSession
               ↓
            FIRMAR mode
               ↓
┌─────────────────────────────────────────┐
│ Signature source                        │
│  A. transparent PNG                     │
│  B. photo/scan → local cleanup          │
│  C. InkCanvas → local rasterization     │
└─────────────────────────────────────────┘
               ↓
        SignatureAsset (BGRA/alpha)
               ↓
      SignaturePlacement[]
      (PDF point coordinates)
               ↓
 WPF overlay derived from PDF coordinates
               ↓
          Guardar como...
               ↓
    PdfVisualSignatureWriter
      opens source separately
      inserts image objects
      generates page content
      saves to temporary file
               ↓
     close writer/native handles
     release PDFium NativeGate
               ↓
     reopen/render with PdfDocumentSession
               ↓
      atomic destination replace/move
```

No second PDF engine is introduced for F3. PDFium remains the writer/editor authority for visual signature insertion. PDFsharp remains limited to the already-approved label PDF composition use case.

## 6. Core models

Keep models small and feature-local under `Features/Sign/`.

### `SignatureAsset`

Represents a prepared signature independent of its source.

Conceptual fields:

- immutable pixel width/height;
- BGRA pixel bytes with alpha;
- stride;
- optional display/source name;
- aspect ratio derived from pixel dimensions.

It must not contain WPF controls or PDF document handles.

### `SignaturePlacement`

Represents one placed signature.

Conceptual fields:

- page index;
- PDF-space rectangle in points: left, bottom, width, height;
- reference to the active in-memory signature asset;
- stable local identifier for selection/duplicate/delete.

The PDF rectangle is authoritative. Screen coordinates are always derived.

### `SignatureEditState`

For F3.1, contains the current-page placement list, selected placement, and dirty state. It is in-memory only until save.

No general undo/redo framework is introduced in F3.1. F3.3 drawing undo/redo is scoped to the drawing surface only. General document undo/redo remains for later editing phases.

## 7. Coordinate authority

### Principle

Never store signature position or size in WPF pixels as durable edit state.

The PDF page rectangle in **PDF points** is the single source of truth. This makes placements stable across:

- 100% zoom;
- zoom in/out;
- Fit Page;
- Fit Width;
- viewer resize;
- rerender at a different DPI.

### Overlay alignment

The signature overlay canvas must occupy exactly the same displayed page rectangle as `PdfImage`, not the full `ScrollViewer` and not the page margin.

For the current full-page, rotation-0 rendering path, coordinate mapping can be pure and deterministic:

```text
pdfX = uiX / displayedPageWidth  * pageWidthPoints
pdfTop = uiY / displayedPageHeight * pageHeightPoints
pdfWidth = uiWidth / displayedPageWidth * pageWidthPoints
pdfHeight = uiHeight / displayedPageHeight * pageHeightPoints
pdfBottom = pageHeightPoints - pdfTop - pdfHeight
```

The inverse mapping derives the WPF overlay rectangle from the PDF rectangle.

The implementation must cross-check this mapper against representative PDFium page/device conversion behavior or known geometric fixtures. If rotated/cropped-page evidence reveals that pure mapping is insufficient, use PDFium page/device conversion APIs rather than inventing ad-hoc corrections.

Dragging and resizing may update a temporary UI rectangle continuously, but commit back to PDF-space coordinates at the end of the interaction and whenever needed to maintain model consistency.

## 8. F3.1 UX

A PDF still opens in **Leer**. F3 adds/activates the existing planned mode strip concept:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

Only `LEER` and `FIRMAR` need to become functional in this phase. Do not scaffold empty subsystems for later modes.

### Entering Firmar

When a valid PDF is open:

- user selects `FIRMAR`;
- right properties panel switches to signature actions;
- page navigation/zoom remain available when no unsaved placement blocks navigation;
- no PDF mutation occurs merely by entering the mode.

### Signature source actions

Initial actions:

- `Cargar PNG transparente...`
- `Crear desde foto...` (F3.2)
- `Dibujar firma...` (F3.3)
- `Firmas guardadas` only after F3.4.

### Placement

After obtaining an asset:

- a new signature is placed near the center of the visible current page;
- initial size is reasonable and never larger than the page; preserve aspect ratio;
- selected signature shows a visible selection outline and one or more resize handles;
- dragging the body moves it;
- dragging a resize handle preserves aspect ratio;
- placement is clamped to remain within the PDF page for F3.1;
- `Duplicar` creates another placement on the same page with a small visible offset;
- `Eliminar` / Delete removes selected placement;
- selection chrome is UI-only and is never written into the PDF.

No arbitrary rotation is required in F3.1.

### Dirty navigation guard

If current-page signature edits are dirty and the user attempts:

- next/previous/go-to-page;
- open another PDF/ZPL;
- exit Firmar;
- close the app;

show a simple three-way choice:

- `Guardar como...`
- `Descartar`
- `Cancelar`

If save succeeds, continue the originally requested navigation/close/mode action. If save fails or is canceled, remain on the current page with edits intact.

The original source PDF is never overwritten as part of this flow.

## 9. PNG import

F3.1 accepts PNG for direct import.

Rules:

- decode locally using existing Windows/WPF image facilities;
- normalize to a predictable BGRA pixel buffer;
- preserve source alpha;
- reject zero-size/corrupt/unsupported images cleanly;
- impose a reasonable decoded-pixel safety limit to avoid accidental huge-memory images;
- no runtime web access;
- no persistent copy unless the user later explicitly saves the signature to the F3.4 library.

If a PNG has no useful alpha and appears to contain a white background, do not silently alter it in F3.1; offer/use the F3.2 preparation flow instead.

## 10. F3.2 photo/scan cleanup

### Input

Accept PNG/JPEG image selected locally.

### Processing pipeline

All processing is in-memory and local:

```text
decode to BGRA
  ↓
optional luminance normalization
  ↓
estimate whiteness / distance from white
  ↓
background strength threshold + soft transition
  ↓
combine with original alpha
  ↓
brightness / contrast transform for visible ink
  ↓
optional output recolor: original / black / blue
  ↓
find non-transparent bounds
  ↓
automatic crop with small padding
  ↓
SignatureAsset
```

### Background removal

The intended case is paper white/near-white, not arbitrary semantic segmentation.

Use a soft alpha ramp around the background threshold so antialiased pen edges remain smooth. Avoid a single binary threshold that creates jagged strokes.

The controls must be bounded and understandable rather than exposing dozens of image-processing parameters.

Suggested controls:

- `Automático` preset;
- `Quitar fondo` strength slider;
- `Brillo`;
- `Contraste`;
- `Tinta`: Original / Negro / Azul;
- `Recorte automático` on by default;
- `Restablecer`;
- `Aplicar` / `Cancelar`.

A checkerboard preview indicates transparency.

### Failure boundary

F3.2 may produce imperfect results for:

- strong shadows crossing the signature;
- colored/textured paper;
- table/background visible around the sheet;
- severe blur;
- folds/creases through the ink.

Do not add AI or network fallback to solve these cases. Provide a clear recommendation to retake the photo on white paper with even lighting.

## 11. F3.3 drawing

Use WPF `InkCanvas`; do not add a third-party drawing SDK.

### Interaction

- white/checkerboard signing area;
- black/blue ink choice;
- 2–3 simple stroke width presets;
- Undo;
- Redo;
- Clear;
- Apply;
- Cancel.

Mouse, touch, and stylus all feed the same ink surface. Advanced stylus pressure tuning is deferred.

### Output

On Apply:

- compute stroke bounds;
- render only the required area plus small padding into a transparent bitmap;
- normalize to BGRA/alpha;
- create a normal `SignatureAsset`;
- no InkCanvas-specific object leaks into the PDF writer or placement model.

## 12. F3.4 local library

A later sub-slice may persist `SignatureAsset` data under a dedicated SG PDF Editor directory within the current user's local application-data profile.

Minimal metadata:

- generated ID;
- user-visible name;
- asset file name;
- created/updated timestamp if useful.

Storage remains local to the Windows user profile. No automatic sync, upload, telemetry, account, or external backup integration.

Because a signature image is sensitive personal content, normal logs/tests must never dump the image bytes or persist user signatures in repo fixtures. Synthetic test assets only.

Encryption-at-rest beyond normal Windows profile permissions is not required for F3.4 MVP unless a later security review/user requirement justifies it.

## 13. PDF writing strategy

### F3.1 alpha insertion gate

Before building the full writer/UI, implementation must prove with a synthetic test that the currently packaged PDFium build can:

1. receive the prepared BGRA/alpha signature bitmap;
2. insert it as an image page object at a known rectangle;
3. save the document;
4. reopen/render it;
5. preserve the underlying page pixels through transparent parts of the signature.

This is a technical gate, not a second engine comparison. If alpha cannot be preserved using the intended minimal PDFium path, stop and return to design instead of flattening the entire page or silently losing transparency.

### Do not mutate the active read session

The current `PdfDocumentSession` remains a read/render session for the UI. `Guardar como...` uses a dedicated write operation that opens the source PDF independently under the existing global PDFium native gate.

Benefits:

- cancel leaves active workspace untouched;
- write failure cannot corrupt the open session;
- original stays unchanged;
- transactional output is simpler;
- reopened validation is independent.

### Native gate rule

PDFium remains globally serialized. The writer may hold `PdfiumRuntime.NativeGate` while its native document/page/bitmap/save handles are active, but it must **close those native handles and release the gate before** reopening the temporary output through `PdfDocumentSession` for validation. Do not call a `PdfDocumentSession` method that reacquires the same gate while already holding it.

### Writer responsibilities

A focused `PdfVisualSignatureWriter` should:

1. validate source path, destination path, placement/page bounds, asset pixels;
2. create a unique temporary output beside the destination when possible;
3. open the source document via PDFium;
4. for each affected page:
   - load page;
   - create PDF image object;
   - create/copy a PDFium bitmap from prepared BGRA/alpha pixels;
   - set image bitmap;
   - set object matrix from placement rectangle;
   - insert object into page;
   - regenerate page content;
   - close page;
5. save the modified document to the temporary output using PDFium save-as-copy APIs;
6. close all native handles even on failure;
7. release the global native gate;
8. reopen the temporary PDF with the existing PDFium reader;
9. validate page count and render each affected page;
10. only then move/replace the requested destination;
11. remove temporary output on failure/cancel.

The exact P/Invoke surface is added minimally when F3.1 is implemented. No generic PDF editing abstraction/plugin system.

### Existing cryptographic signatures

A visual signature changes page content and can invalidate existing certificate/digital signatures. Before writing, F3.1 must perform a minimal preflight for existing PDF signatures when supported by the packaged PDFium API surface.

If one or more existing cryptographic signatures are detected:

- show a clear warning that saving a modified copy can invalidate those signatures;
- require explicit confirmation before continuing;
- never describe the F3 visual mark itself as a cryptographic/digital signature.

If reliable signature detection is unavailable in the current PDFium build, do not claim preservation; document the limitation and stop for design review before silently treating a signed PDF as ordinary.

### Image transparency

The PDF image object must receive a BGRA/alpha bitmap path that preserves transparent pixels. Tests must prove transparency survives save/reopen by rendering the result over controlled page content, not merely by checking that a file was created.

### Save semantics

F3 uses **Guardar como...** only.

- destination may not silently equal/overwrite the source in F3;
- existing destination follows explicit overwrite confirmation;
- save is temp → validate → destination;
- if any step fails, keep existing destination and original source intact.

## 14. Validation after save

Automated acceptance must prove more than the PDFium save function returning success.

For synthetic PDFs:

- save;
- reopen with `PdfDocumentSession`;
- assert same page count;
- render affected page;
- compare expected signature presence/position within a tolerance;
- verify transparent areas still reveal underlying page content;
- verify placement under at least two different viewer zoom states before save produces the same PDF-space output.

Manual Windows QA later covers visual drag/resize feel, real signatures, stylus hardware, and photo quality.

## 15. Error handling / state safety

Errors must not destroy valid prior state.

Examples:

- invalid PNG/JPEG → current PDF and placements unchanged;
- failed photo processing → source image can be adjusted/retried, PDF unchanged;
- drawing cancel → no asset/placement added;
- failed save → placements remain available for retry, source/destination prior state preserved;
- reopen validation failure → destination is not replaced;
- navigation guard cancel → remain on current page with edits intact.

No temporary user signature image should be written to disk for F3.1–F3.3 unless required by an API and then it must be request-scoped and cleaned. Prefer managed in-memory pixels.

## 16. Offline / privacy

F3 must remain fully offline:

- no HTTP;
- no AI API;
- no cloud background-removal service;
- no remote signature service;
- no telemetry containing signature pixels;
- no uploads;
- no signature/customer fixtures committed to the repository.

All signature processing occurs in memory/on the local machine.

## 17. Testing strategy

### Pure/model tests

- `SignatureAsset` validation and aspect ratio;
- placement duplicate/delete/selection state;
- UI↔PDF coordinate round-trip;
- mapping invariant across display sizes/zoom;
- bounds/clamping;
- proportional resize;
- dirty-state navigation decisions.

### Image preparation tests

Synthetic only:

- white background becomes transparent;
- near-white soft transition preserves antialiased edge;
- black ink remains visible;
- blue ink remains/recolors correctly;
- brightness/contrast bounded behavior;
- auto crop;
- all-white input fails/returns no usable signature clearly;
- original alpha combines correctly with cleanup mask.

### Ink tests

Test the conversion logic independently from physical stylus hardware:

- stroke bounds → transparent cropped bitmap;
- empty drawing rejected;
- color/width settings reflected in output;
- undo/redo model/seam where practical.

Interactive pressure/stylus behavior remains manual QA.

### PDF integration tests

- alpha insertion gate on synthetic PDF before full F3.1 writer/UI;
- insert transparent asset into synthetic PDF;
- exact placement in PDF points;
- multiple placements on same page;
- duplicate produces two visible instances;
- deleted placement not written;
- save/reopen/render;
- alpha transparency visible over underlying graphics;
- portrait + landscape pages;
- coordinate behavior for representative rotated/cropped pages or explicit stop if current mapping cannot support them safely;
- output remains stable regardless of viewer zoom used to create placement;
- original source hash unchanged;
- existing destination preserved on injected write/validation failure;
- temp cleanup on success/failure;
- pre-existing cryptographic-signature preflight/warning seam.

### WPF tests

Focused STA seams only:

- Firmar availability with/without valid PDF;
- signature panel mode switch;
- import/cancel behavior;
- selection/move/resize command wiring;
- navigation dirty guard;
- save/cancel workspace preservation.

Do not build brittle pixel-perfect UI automation in CI.

## 18. Manual QA

F3 automated PASS must remain distinct from real UI/hardware acceptance.

Manual checklist should include:

- transparent PNG from a real signature;
- JPG phone photo on white paper;
- weak/strong room lighting;
- black and blue pen;
- Fit Page / Fit Width / 100% placement consistency;
- drag and resize feel;
- duplicate/delete;
- save/reopen in SG PDF Editor and another standard viewer;
- mouse drawing;
- touch if available;
- real stylus/digital pen if available;
- warning behavior on a PDF that already contains a cryptographic signature, if such a safe test file is available;
- network disabled during complete signing flow.

If stylus/photo/signed-PDF test material is unavailable, mark those items `NOT RUN`; CI must not imply physical/manual PASS.

## 19. Expected implementation footprint

Likely F3.1 files:

- `src/SGPdf.App/Features/Sign/SignatureAsset.cs`
- `src/SGPdf.App/Features/Sign/SignaturePlacement.cs`
- `src/SGPdf.App/Features/Sign/SignatureEditState.cs`
- `src/SGPdf.App/Features/Sign/PdfPageCoordinateMapper.cs`
- `src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs`
- `src/SGPdf.App/MainWindow.Sign.cs`
- minimal `PdfiumNative.cs` additions;
- `MainWindow.xaml` overlay/mode/signature controls;
- focused tests.

Likely F3.2:

- `SignatureImageProcessor.cs`;
- focused preparation dialog/control code;
- synthetic image tests.

Likely F3.3:

- drawing dialog/control using WPF `InkCanvas`;
- `InkSignatureRenderer.cs` or equivalently small conversion helper;
- tests around conversion, not hardware.

F3.4 storage files are introduced only when that sub-slice begins.

No new runtime NuGet package is expected for F3.1–F3.3. If implementation proves otherwise, stop and return to design review before adding one.

## 20. Acceptance criteria

### F3.1

1. transparent PNG can become a reusable in-memory `SignatureAsset`;
2. one or more signatures can be placed on current page;
3. move/resize/duplicate/delete work without mutating source PDF;
4. placement is authoritative in PDF coordinates and survives zoom/view changes;
5. PDFium alpha insertion gate passes before full writer/UI completion;
6. save writes a copy through PDFium, not rasterized full pages;
7. transparency survives save/reopen;
8. source stays unchanged and destination write is transactional;
9. pre-existing cryptographic signatures are detected/warned when the current PDFium build can reliably expose them; otherwise implementation stops for design review rather than claiming safety;
10. build/tests/CI green with fresh evidence;
11. PR remains draft/unmerged without explicit user approval.

### F3.2

1. white/near-white background can be removed locally;
2. soft alpha preserves useful ink edges;
3. brightness/contrast/background strength and original/black/blue output work;
4. auto crop produces a usable transparent asset;
5. no AI/network/new runtime service.

### F3.3

1. mouse/touch/stylus can use the WPF ink surface where hardware supports them;
2. black/blue + simple widths + undo/redo/clear are available;
3. Apply yields the same normal `SignatureAsset` contract;
4. no third-party drawing subsystem.

### F3.4

1. saved signature assets remain local to current Windows user;
2. create/list/rename/delete are simple and deterministic;
3. no signature pixels leak into logs/repo/CI;
4. F3.4 remains optional for core SIGN-01..04 close if intentionally deferred.

## 21. Stop conditions requiring new design approval

Stop implementation and return to design if any of these becomes necessary:

- new commercial/AGPL/GPL runtime dependency;
- second PDF editing engine;
- rasterizing the entire PDF page to make signature writing work;
- cloud/AI service for background removal;
- persistent temp images containing real signatures outside user-approved library storage;
- general-purpose undo/redo/document object framework before F6/F7;
- complex multi-page pending-edit architecture;
- cryptographic signing;
- arbitrary image-object editor behavior beyond visual signatures;
- silent modification of a PDF whose existing cryptographic-signature state cannot be assessed safely.

## 22. Continuation after written-spec approval

After the user approves this written specification, invoke the `writing-plans` workflow. The implementation plan should preserve small vertical slices and TDD. Recommended execution order is F3.1 core first, then F3.2 photo cleanup, then F3.3 drawing; F3.4 only after the first three are stable or when explicitly prioritized.
