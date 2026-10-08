# F3.2 — Photo/scan signature preparation — Design Specification

**Date:** 2026-10-08  
**Status:** written spec awaiting user review  
**Base:** F3.1 final head `d559169280f9d9ee2c19f1c245f6657d1b598c6d`  
**Branch:** `feat/f3-2-photo-preparation`

## 1. Intent

F3.2 lets a user create a usable transparent visual-signature asset from a local phone photo or scan of a signature made on white or near-white paper.

The intended outcome is deliberately narrow:

1. choose a local PNG/JPG/JPEG;
2. remove white/near-white paper locally;
3. adjust only a few understandable controls;
4. preview transparency;
5. apply;
6. feed the result into the existing F3.1 `SignatureAsset` + placement workflow.

F3.2 is **not** a generic photo editor, AI background remover, OCR feature, or drawing surface.

## 2. Product scope

F3.2 must provide:

- local PNG/JPG/JPEG input;
- automatic white/near-white paper estimation;
- soft background removal that preserves antialiased pen edges;
- bounded `Quitar fondo`, `Brillo`, and `Contraste` controls;
- ink output style: `Original`, `Negro`, `Azul`;
- automatic crop around useful ink, enabled by default;
- checkerboard transparency preview;
- `Automático`, `Restablecer`, `Aplicar`, `Cancelar`;
- no temporary signature image file during normal processing;
- no runtime network or cloud dependency;
- output as the existing F3.1 `SignatureAsset`.

## 3. Non-goals

Do not add in F3.2:

- AI/ML semantic segmentation;
- cloud background-removal APIs;
- OCR;
- perspective correction;
- deskew/document scanning workflow;
- arbitrary crop handles;
- eraser/brush masking;
- color picker;
- saturation/hue/temperature filters;
- sharpening/deblur pipeline;
- connected-component editor or dust-removal UI;
- general image-object editing;
- signature persistence/library;
- InkCanvas or drawn signatures;
- changes to PDF placement/writer architecture;
- new runtime NuGet package unless implementation evidence forces a new design review.

Poor photos with heavy shadows, folds, textured/colored paper, severe blur, or large non-paper backgrounds remain outside the guaranteed case.

## 4. Existing contracts to reuse

### `SignatureAsset`

F3.1 already defines the canonical in-memory result:

```csharp
SignatureAsset(
    int pixelWidth,
    int pixelHeight,
    int stride,
    byte[] bgraPixels,
    string? sourceName = null)
```

F3.2 must end by creating this same type. No `PhotoSignatureAsset`, inheritance tree, or second placement model.

### Placement / PDF writing

After `Aplicar`, the prepared asset enters the existing F3.1 path:

```text
prepared SignatureAsset
        ↓
AddSignatureAsset(...)
        ↓
SignatureEditState
        ↓
existing overlay / move / resize / duplicate / delete
        ↓
existing PdfVisualSignatureWriter
```

F3.2 must not modify coordinate authority, `SignaturePlacement`, `SignatureEditState`, or PDFium writing semantics except for a minimal UI seam needed to pass the resulting asset into F3.1.

## 5. Input decoding and safety

### Accepted files

- `.jpg`
- `.jpeg`
- `.png`

Use existing Windows/WPF bitmap decoding facilities. Do not add an image codec package.

### Decoded format

Normalize the selected frame to:

- 32-bit BGRA;
- top-to-bottom managed pixel buffer;
- explicit stride;
- original alpha preserved when present.

### Pixel safety limit

Reuse the F3.1 decoded-image safety policy: **maximum 20,000,000 decoded pixels**.

The implementation may extract this value into a tiny shared feature-local constant if that avoids duplication, but it must not introduce a broad image infrastructure refactor.

Reject before large buffer allocation where practical.

### Input state safety

If decode fails, file is unsupported/corrupt, dimensions are invalid, or safety limits are exceeded:

- existing PDF session remains unchanged;
- existing signature placements remain unchanged;
- no new asset is added;
- user receives a controlled Spanish error.

## 6. Processing model

Introduce one small deterministic processor, conceptually:

```csharp
SignatureImageProcessor.Process(
    SignaturePhotoSource source,
    SignatureImageProcessingSettings settings)
    -> SignatureImageProcessingResult
```

Names may be refined during the implementation plan, but responsibilities must remain this small.

The processor contains no WPF controls, file dialogs, PDF handles, network calls, global state, or persistence.

### Settings

Conceptual settings:

- `BackgroundRemoval`: integer 0..100;
- `Brightness`: integer -100..100;
- `Contrast`: integer -100..100;
- `InkStyle`: Original / Black / Blue;
- `AutoCrop`: bool.

Defaults are chosen by `Automático`; manual controls remain bounded.

Invalid/non-finite/out-of-range programmatic settings are rejected or normalized deterministically. UI sliders never emit values outside their supported ranges.

## 7. Paper estimation

The guaranteed input is a dark or blue signature on white/near-white paper.

`Automático` estimates the apparent paper color locally from bright pixels, biased toward the image perimeter so the signature itself has little influence.

Recommended KISS strategy:

1. sample an outer perimeter band;
2. rank/filter for bright candidate pixels;
3. derive a robust representative paper RGB using a median/trimmed statistic;
4. reject/flag auto-estimation only when there is no plausible light background.

Do not introduce semantic segmentation or trainable models.

The paper estimate is calculated from the original decoded source and remains stable while manual brightness/contrast controls move. `Quitar fondo` changes removal strength; it does not continuously redefine what the paper color is.

## 8. Soft background removal

A hard binary white/non-white threshold is forbidden because it creates jagged signature edges.

For each source pixel:

1. compute color-distance / whiteness distance from estimated paper color;
2. map that distance through two bounded thresholds derived from `BackgroundRemoval`;
3. values close to paper become alpha 0;
4. transition-region pixels receive gradual alpha;
5. clearly non-paper ink remains strongly opaque;
6. multiply this cleanup alpha by the pixel's **original alpha**.

Conceptually:

```text
cleanupAlpha = smoothstep(low, high, distanceFromPaper)
finalAlpha   = originalAlpha × cleanupAlpha
```

Exact numeric threshold constants belong in implementation tests and may be tuned during TDD, but the following behavioral invariants are frozen:

- stronger background removal never makes a pure-paper pixel more opaque;
- identical input + settings yields identical output;
- original transparent pixels can never become opaque;
- antialiased ink edges preserve intermediate alpha values in representative synthetic fixtures;
- no white opaque rectangle may remain around a successful prepared signature.

## 9. Brightness and contrast

Brightness and contrast are intentionally basic, bounded image adjustments.

They affect visible RGB/ink appearance but do **not** change the already-estimated paper reference color.

Implementation should use conventional deterministic per-channel transforms with clamping to 0..255. No histogram equalization, adaptive local contrast, HDR, deblur, or sharpening in F3.2.

`Restablecer` returns to the original F3.2 automatic defaults, not to an already-mutated pixel buffer. Processing is always derived from the immutable decoded source.

## 10. Ink styles

### Original

Keep the adjusted original RGB of visible ink and the computed final alpha.

### Negro

Replace visible RGB with black while preserving computed alpha.

### Azul

Replace visible RGB with one predefined dark-blue signature color while preserving computed alpha.

No color picker is added. The exact blue constant is frozen by implementation tests, but should visually resemble ordinary blue-pen ink rather than bright UI blue.

Recoloring never changes geometry, alpha-mask bounds, or placement behavior.

## 11. Automatic crop

When `AutoCrop` is enabled:

1. find the rectangle containing pixels with meaningful final alpha;
2. expand by a small padding;
3. clamp to source bounds;
4. copy only that BGRA region into the resulting `SignatureAsset`.

Padding should be proportional but bounded so high-resolution phone photos do not retain huge white margins.

No connected-component selection is required in F3.2. If a photograph contains unrelated dark marks, they may remain; the product guidance should recommend a clean white sheet.

When `AutoCrop` is disabled, preserve the processed source dimensions.

## 12. Usable-signature validation

`Aplicar` is blocked when processing leaves no meaningful signature content.

Examples:

- all-white image;
- almost entirely removed image;
- result containing only negligible low-alpha noise.

The implementation uses a small deterministic minimum-content rule covered by synthetic tests. It must avoid accepting an all-white/blank page as a valid signature merely because of one noisy pixel.

Failure message should be concise, for example:

> No se pudo aislar una firma útil. Intenta una foto sobre papel blanco, con buena luz y sin sombras fuertes.

The original photo remains available in the preparation dialog for further adjustments; no PDF placement is changed until `Aplicar` succeeds.

## 13. Preview strategy

Interactive preview must feel immediate without processing a 20 MP photo on every slider event.

Use two levels derived from the same immutable source/settings:

### Preview

- downscale only for preview computation;
- maximum side approximately **1200 px**;
- use the exact same processing rules/settings as final output;
- show result over a checkerboard background so transparency is obvious;
- stale preview work must not replace a newer slider state.

A simple local cancellation/version mechanism is sufficient. Do not introduce a general image-processing scheduler framework.

### Apply

On `Aplicar`:

- process the original full-resolution decoded source with the current settings;
- validate useful content;
- auto-crop if enabled;
- create the final `SignatureAsset`;
- close the dialog only after success;
- hand the asset to F3.1 for centered placement.

The preview bitmap itself is never used as the final signature asset.

## 14. UX

The FIRMAR panel gains one real F3.2 action:

- `Crear desde foto...`

Existing F3.1 `Cargar PNG transparente...`, duplicate/delete/save controls remain.

No dead controls for F3.3/F3.4 are added.

### Preparation window

A focused modal WPF preparation window is preferred over expanding the main right panel into an image editor.

Minimum layout:

```text
┌──────────────────────────────────────────────┐
│ Preparar firma                               │
│                                              │
│        checkerboard RESULT preview           │
│                                              │
├──────────────────────────────────────────────┤
│ Automático                                   │
│ Quitar fondo   [---------●------]            │
│ Brillo         [------●---------]            │
│ Contraste      [--------●-------]            │
│ Tinta          Original | Negro | Azul       │
│ ☑ Recorte automático                        │
│                                              │
│ Restablecer           Cancelar    Aplicar    │
└──────────────────────────────────────────────┘
```

Use normal WPF controls. No custom design system or third-party UI toolkit.

### Automatic first experience

Immediately after selecting a photo:

- decode;
- estimate paper;
- run automatic defaults;
- show the processed preview.

The user should normally be able to press `Aplicar` without touching sliders when the photo is good.

## 15. Integration with F3.1

F3.2 returns either:

- one valid `SignatureAsset`; or
- cancel/failure with no asset.

On success, F3.1 calls its existing placement path and creates the normal centered selected placement.

On cancel or processing failure:

- no placement is added;
- current selection and prior placements remain unchanged;
- dirty state is not changed by the aborted F3.2 action.

F3.2 does not save the prepared signature to a persistent library. That remains F3.4.

## 16. Threading / responsiveness

File decode and full-resolution processing may run away from the UI thread where practical. WPF UI objects remain on the UI thread.

The processor itself operates on managed buffers and is independently testable.

Cancellation/stale-preview behavior is request-scoped. Do not add application-wide worker infrastructure.

## 17. Offline / privacy

Signature photos are sensitive personal content.

Mandatory rules:

- no HTTP;
- no AI API;
- no cloud background-removal service;
- no upload;
- no telemetry containing source/result pixels;
- no photo or signature bytes in logs;
- no real user-signature fixtures committed to the repository;
- no temporary image file during the normal F3.2 pipeline;
- synthetic/generated fixtures only in automated tests.

The selected source photo remains wherever the user stored it; SG PDF Editor only reads it.

## 18. Error / state safety

F3.2 must be transactional with respect to signature edit state:

- cancel dialog → no state change;
- invalid image → no state change;
- failed processor → no state change;
- blank/no-ink result → no state change;
- successful Apply → exactly one new normal F3.1 placement;
- failure after a previous valid preview does not silently apply that stale preview.

Existing F3.1 placements and the active PDF must never be discarded because photo preparation failed.

## 19. Testing strategy

### Pure processor tests

Synthetic pixel buffers only:

- pure white becomes transparent;
- near-white receives soft/partial alpha near the transition;
- black ink remains visible;
- blue ink remains visible in Original mode;
- Black recolor preserves alpha;
- Blue recolor preserves alpha and yields the frozen dark-blue output;
- original source alpha multiplies cleanup alpha;
- stronger removal does not restore paper opacity;
- brightness lower/upper bounds clamp correctly;
- contrast lower/upper bounds clamp correctly;
- deterministic same-input/settings output;
- automatic paper estimate tolerates a slight gray/yellow/blue cast in representative fixtures;
- all-white input reports no usable signature;
- tiny isolated noise does not satisfy minimum-content acceptance;
- auto-crop finds useful bounds and adds bounded padding;
- crop-disabled preserves dimensions.

### Loader tests

- JPEG accepted;
- opaque PNG accepted for preparation;
- transparent PNG accepted;
- corrupt input rejected;
- zero/invalid dimensions rejected where decoder exposes them;
- >20,000,000 decoded pixels blocked safely;
- BGRA normalization deterministic.

### Preview/final tests

- preview side <= ~1200 px;
- preview uses same settings semantics;
- stale preview result cannot overwrite newer settings state;
- final Apply processes full source rather than the reduced preview.

### WPF integration tests

Focused STA seams only:

- `Crear desde foto...` available only in valid FIRMAR/PDF context;
- cancel adds no placement;
- invalid processing leaves prior placements unchanged;
- Apply returns one asset to existing F3.1 placement path;
- resulting placement becomes selected/dirty using existing model;
- no real modal dialog is required in headless tests.

Do not build brittle pixel-perfect UI automation.

### Regression

Full existing suite must remain green, including F3.1 PDF alpha writer and ZPL phases.

## 20. Expected implementation footprint

Likely new files:

- `src/SGPdf.App/Features/Sign/SignatureImageProcessingSettings.cs`
- `src/SGPdf.App/Features/Sign/SignaturePhotoSource.cs`
- `src/SGPdf.App/Features/Sign/SignaturePhotoLoader.cs`
- `src/SGPdf.App/Features/Sign/SignatureImageProcessor.cs`
- `src/SGPdf.App/SignaturePhotoDialog.xaml`
- `src/SGPdf.App/SignaturePhotoDialog.xaml.cs`
- focused tests under `tests/SGPdf.App.Tests/`.

Likely modification:

- `src/SGPdf.App/MainWindow.Sign.cs` only for `Crear desde foto...` and a narrow dialog/asset seam.

A tiny shared image safety constant may be extracted from `SignaturePngLoader` if needed. No broad loader framework.

No changes are expected to:

- `PdfVisualSignatureWriter`;
- `SignatureCoordinateMapper`;
- PDFium P/Invoke surface;
- label/ZPL code;
- `.csproj` package references or lockfiles.

## 21. Acceptance criteria

F3.2 automated acceptance requires:

1. PNG/JPEG photo/scan can be decoded locally within the existing safety limit;
2. white/near-white paper can be removed automatically for representative synthetic fixtures;
3. transition pixels demonstrate soft alpha rather than binary jagged edges;
4. original alpha is preserved/combined correctly;
5. background-removal, brightness, contrast and Original/Black/Blue settings behave deterministically;
6. auto-crop returns a compact transparent asset with bounded padding;
7. blank/no-useful-signature output cannot be applied;
8. checkerboard preview uses a reduced image while Apply uses the full source;
9. cancel/failure never changes existing F3.1 placement state;
10. successful Apply produces exactly the normal F3.1 `SignatureAsset`/placement flow;
11. no temp signature image, runtime HTTP, cloud service or new runtime dependency is introduced;
12. fresh locked restore/build/tests/CI pass with 0 build warnings/errors;
13. PR remains draft/unmerged without explicit user approval.

Manual photo-quality acceptance remains separate from automated PASS.

## 22. Manual QA boundary

Automated tests cannot prove arbitrary real-world phone-photo quality.

Manual QA later should include:

- black pen on white paper;
- blue pen on white paper;
- slightly warm/cool room lighting;
- moderate uneven lighting;
- phone JPEG and scanner PNG;
- automatic defaults without manual sliders;
- strong/weak background-removal adjustments;
- brightness/contrast extremes;
- Original/Black/Blue output;
- auto-crop;
- cancel/retry;
- Apply then move/resize/save via F3.1;
- network disconnected for complete flow.

Heavy shadow, colored/textured paper, severe blur, folds, desk/table background and unrelated dark marks may be recorded as expected limitations rather than automated failures.

If real photo material is unavailable, manual photo-quality state is `NOT RUN`; CI must not imply it passed.

## 23. Stop conditions requiring new design approval

Stop and return to design if implementation appears to require:

- new commercial/GPL/AGPL runtime package;
- OpenCV/ImageSharp or another substantial image-processing dependency merely for this scope;
- cloud/AI background removal;
- persistent intermediate signature images;
- general-purpose image editing framework;
- manual mask/eraser subsystem;
- perspective correction/document scanner architecture;
- changes to PDF writer/coordinate model to support photo preparation;
- relaxing privacy/offline requirements;
- silently accepting poor/no-signature output.

## 24. Continuation after written-spec approval

After the user reviews and approves this written specification, invoke `superpowers/writing-plans` and create the F3.2 TDD implementation plan. Do not write product code before that plan is reviewed and an execution method is selected.
