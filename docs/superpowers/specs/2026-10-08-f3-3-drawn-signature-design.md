# F3.3 — Draw Signature with WPF InkCanvas — Design Specification

**Date:** 2026-10-08  
**Status:** written spec awaiting user review  
**Base:** F3.2 final head `2aa1f58a6e01397e84d8f8cfcf7eb6e0e168cf62`  
**Branch:** `feat/f3-3-drawn-signature`

## 1. Intent

F3.3 adds a third fully local way to create a visual signature: draw it directly inside SG PDF Editor with mouse, touch, or a digital pen/stylus.

The output remains the existing F3.1 `SignatureAsset`. F3.3 introduces no second placement model, PDF writer, coordinate system, or persistence format. After **Aplicar**, the asset enters the same `AddSignatureAsset(...)` flow already used by transparent PNG import and F3.2 photo preparation.

Success means the user can open **FIRMAR → Dibujar firma...**, draw naturally, correct basic mistakes, apply the result, and then move/resize/duplicate/delete/save it through the proven F3.1 flow.

F3.3 is visual signing, not cryptographic signing and not a general drawing editor.

## 2. Approved scope

The drawing dialog provides:

- WPF `InkCanvas`;
- mouse input;
- touch through native WPF ink handling when supported by the Windows device;
- stylus/digital pen through native WPF ink handling;
- black ink;
- blue ink `#194196`;
- three stroke widths;
- Undo;
- Redo;
- Clear;
- Cancel;
- Apply;
- transparent cropped output as `SignatureAsset`.

The FIRMAR panel gains:

`Dibujar firma...`

All three creation paths converge:

```text
Cargar PNG transparente...
Crear desde foto...
Dibujar firma...
          ↓
    SignatureAsset
          ↓
  AddSignatureAsset(...)
          ↓
move / resize / duplicate / delete
          ↓
     Guardar como...
```

## 3. Non-goals

F3.3 excludes:

- cryptographic signatures;
- pressure-sensitive width;
- stylus tilt/azimuth behavior;
- custom brush engines;
- advanced smoothing/stabilization;
- SVG/vector/PDF-path signature output;
- partial eraser;
- lasso/select/move individual strokes;
- shapes;
- text/name/date fields;
- arbitrary color picker;
- arbitrary brush widths;
- image/photo editing;
- cloud storage/upload;
- signature requests;
- F3.4 persistence/library;
- general document undo/redo.

If native WPF ink is materially insufficient for common stylus hardware, return to design before introducing another input/drawing framework.

## 4. Architecture

F3.3 is a feature-local signature-source adapter:

```text
SignatureDrawDialog
      ↓
 WPF InkCanvas
      ↓
 StrokeCollection
      ↓
SignatureInkRenderer
      ↓
 transparent BGRA pixels
      ↓
 existing SignatureAsset
      ↓
 existing F3.1 placement flow
```

F3.3 must not modify:

- `PdfVisualSignatureWriter`;
- PDFium P/Invoke;
- PDF coordinate mapping;
- `SignaturePlacement` semantics;
- F3.2 photo processing;
- ZPL/Labelize;
- PDFsharp scope;
- runtime networking.

No new NuGet/runtime dependency is expected. Built-in WPF ink APIs are authoritative.

## 5. Drawing surface

### 5.1 InkCanvas

Use one `InkCanvas` in ink mode. It may visually show a light/white signing surface, but that visual surface is **never** part of the exported signature.

Only the `StrokeCollection` is authoritative for output.

Canvas background, checkerboard, border, cursor, selection adorners, buttons, and dialog chrome must never be rasterized into `SignatureAsset`.

### 5.2 Input devices

Use WPF's native pointer/ink path:

- left mouse drag draws;
- Windows-supported stylus/digital pen draws;
- touch may draw when Windows/WPF exposes it through the InkCanvas path on that device;
- no custom multi-touch gesture engine;
- no pressure-width guarantee.

CI cannot prove hardware compatibility. Mouse/touch/stylus QA remains a separate manual gate.

### 5.3 Ink colors

Fixed colors:

- Black `#000000`;
- Blue `#194196`.

The selected color applies to newly created strokes. Existing strokes keep their original drawing attributes so history restoration is exact.

### 5.4 Stroke widths

Freeze the first version to WPF logical widths:

- Thin `2.0 DIP`;
- Medium `3.5 DIP`;
- Thick `5.0 DIP`.

Medium is default. Width selection applies to new strokes only. Existing strokes keep their original width.

Use normal WPF drawing attributes with round pen behavior where available; no custom brush renderer.

## 6. Dialog-local history

F3.3 history is local to the draw dialog and must not create an app-wide command system.

Conceptual state:

- current `StrokeCollection`;
- undo stack;
- redo stack;
- current ink color;
- current ink width.

### New stroke

A completed stroke:

1. remains in the current collection;
2. becomes newest undoable stroke;
3. clears redo history;
4. refreshes button/Apply state.

### Undo

Remove the most recent current stroke and push it to redo.

### Redo

Restore the most recent undone stroke, preserving points, color, width, and attributes.

### New stroke after Undo

Clear redo history.

### Clear

`Limpiar` removes all current strokes and clears both history stacks. Clear itself is not undoable in the MVP; this avoids snapshot-history complexity.

### Button state

- Undo enabled only when available;
- Redo enabled only when available;
- Clear enabled only when strokes exist;
- Apply enabled only for useful ink.

## 7. Useful-signature rule

Reject empty drawings, accidental taps, and tiny dots.

Before Apply, current ink must:

- include at least one stroke containing at least two stylus points; and
- have union ink bounds at least `4 DIP` wide and `4 DIP` high.

The UI uses this rule to enable Apply, and the renderer repeats validation so correctness does not depend on button state alone.

Validation failure preserves all current strokes/history.

## 8. Transparent rasterization

### 8.1 Never render the visible InkCanvas

On Apply:

1. snapshot/clone the strokes used for rendering;
2. compute their union bounds;
3. add transparent padding;
4. draw strokes onto a transparent WPF drawing surface/visual;
5. rasterize only that padded region;
6. normalize to top-to-bottom BGRA with alpha;
7. construct the existing immutable `SignatureAsset`.

This prevents a white rectangle from entering the PDF.

### 8.2 Padding

```text
paddingDip = clamp(max(8, maxStrokeWidth * 2), 8, 24)
```

Apply padding on every side.

### 8.3 Raster quality

WPF logical DPI is 96. Final signature rendering uses a fixed 300-DPI-equivalent scale:

```text
rasterScale = 300 / 96 = 3.125
```

Output pixel dimensions derive from padded DIP bounds × `3.125`, rounded upward so strokes/padding are not clipped.

Output must preserve transparency and must not use JPEG.

The shared decoded-image safety policy remains authoritative: final output must not exceed **20,000,000 pixels**. Validate dimensions before allocating the full raster buffer where practical.

If the limit is exceeded, Apply fails with a controlled message and keeps the drawing intact.

No temporary signature image file is created.

## 9. Feature-local contracts

Exact signatures may be finalized in the implementation plan, but the model should remain small:

```csharp
internal enum SignatureInkColor
{
    Black,
    Blue
}

internal enum SignatureInkWidth
{
    Thin,
    Medium,
    Thick
}

internal static class SignatureInkRenderer
{
    internal static bool HasUsefulInk(StrokeCollection strokes);
    internal static SignatureAsset Render(StrokeCollection strokes, string? sourceName = null);
}
```

A small feature-local history helper is allowed if it makes stack semantics independently testable. No app-wide command framework.

## 10. Dialog UX

Title: `Dibujar firma`

Recommended conventional layout:

```text
┌────────────────────────────────────────────────────┐
│ Dibujar firma                                     │
│ Color: [Negro] [Azul]  Grosor: [Fino|Medio|Gr.]   │
│                                                    │
│ ┌────────────────────────────────────────────────┐ │
│ │                 InkCanvas                      │ │
│ └────────────────────────────────────────────────┘ │
│                                                    │
│ [Deshacer] [Rehacer] [Limpiar] [Cancelar] [Aplicar]│
└────────────────────────────────────────────────────┘
```

Suggested named controls:

- `SignatureDrawInkCanvas`;
- `SignatureDrawBlackButton`;
- `SignatureDrawBlueButton`;
- `SignatureDrawWidthComboBox`;
- `SignatureDrawUndoButton`;
- `SignatureDrawRedoButton`;
- `SignatureDrawClearButton`;
- `SignatureDrawCancelButton`;
- `SignatureDrawApplyButton`;
- `SignatureDrawStatusText`.

Reuse existing WPF styling/conventions. Do not introduce a design framework.

## 11. Apply / Cancel

### Apply

Apply:

1. validates useful ink;
2. renders strokes into transparent `SignatureAsset`;
3. stores it as the successful dialog result;
4. closes;
5. MainWindow passes it to `AddSignatureAsset(...)`;
6. the new placement becomes centered/selected/dirty exactly like PNG/photo assets.

If rendering fails:

- dialog stays open;
- strokes/history remain intact;
- status/error is controlled;
- no asset is returned;
- PDF/placement state is untouched.

### Cancel

Cancel returns no asset and changes nothing outside the dialog. Window-close is equivalent to Cancel.

## 12. MainWindow integration

Add one narrow seam next to F3.1/F3.2 source seams, conceptually:

```csharp
private Func<Window, SignatureAsset?> _drawSignature =
    static owner => SignatureDrawDialog.Draw(owner);
```

FIRMAR adds `Dibujar firma...` below the existing source actions.

If the dialog returns `null`, preserve:

- placements;
- selection;
- dirty state;
- active PDF;
- current page.

A valid returned asset uses the existing `AddSignatureAsset(...)` path. There is no new PDF save path.

## 13. Privacy / failure behavior

All F3.3 processing remains local and in-memory.

No:

- network/HTTP;
- telemetry;
- cloud API;
- upload;
- temp PNG/JPEG;
- clipboard requirement;
- persistent signature storage.

Rendering/allocation failure preserves the drawing until the user retries, clears, or cancels.

## 14. Automated acceptance

### Rendering

Tests must prove:

- empty strokes rejected;
- tiny/tap-like input rejected;
- valid black ink produces visible black pixels with alpha;
- valid blue ink uses `#194196` with alpha;
- background remains transparent;
- result is cropped around ink with padding;
- output respects the 20M-pixel limit;
- same logical stroke geometry/settings produce stable output dimensions, color semantics, alpha/background semantics and crop bounds;
- Thin / Medium / Thick produce distinguishable visible widths.

Tests must **not** require every antialiased edge pixel to be byte-identical across Windows/WPF renderer versions.

### History

Tests must prove:

- completed stroke enables Undo;
- Undo removes only newest stroke;
- Redo restores exact stroke data/attributes;
- repeated Undo/Redo preserves order;
- new stroke after Undo clears Redo;
- Clear leaves zero strokes and resets histories.

### Dialog

Tests must prove:

- empty start with Medium + Black defaults;
- Apply disabled without useful ink;
- valid ink enables Apply;
- Cancel returns no asset;
- Apply returns normal `SignatureAsset`;
- rendering failure preserves strokes and dialog usability.

### FIRMAR integration

Tests must prove:

- `Dibujar firma...` exists in FIRMAR;
- cancel adds nothing and preserves existing state;
- failure adds nothing and preserves existing state;
- success adds exactly one normal selected dirty placement;
- PNG/photo flows remain unchanged.

### Regression / architecture

Full locked restore/build/test must remain GREEN with:

- 0 warnings;
- 0 errors;
- no new runtime package;
- no `.csproj`/lock mutation unless separately approved;
- no PDFium writer/coordinate changes;
- no ZPL changes;
- no runtime network surface.

## 15. Manual hardware QA

Automated PASS does not claim hardware quality. Separate Windows QA should cover:

1. mouse;
2. touch screen when available;
3. Surface/Wacom/other stylus when available;
4. thin/medium/thick feel;
5. black/blue;
6. Undo/Redo/Clear;
7. Apply → move/resize/save through F3.1;
8. external PDF viewer inspection;
9. offline operation.

Record device/driver for touch/stylus findings instead of generalizing to all Windows hardware.

## 16. Non-regression boundaries

F3.3 leaves unchanged:

- F3.1 placement/write semantics;
- F3.2 photo preparation;
- original-PDF preservation;
- cryptographic-signature warning behavior;
- ZPL/Labelize;
- PDFsharp scope;
- offline/privacy policy;
- no-auto-merge rule.

## 17. Stop conditions

Return to design if implementation requires:

- new runtime drawing/image dependency;
- custom low-level pointer/stylus framework;
- PDFium changes solely for drawn signatures;
- temp image files for correctness;
- pressure-sensitive brush engine for acceptable MVP quality;
- F3.4 persistence to make F3.3 work;
- cloud/network service;
- app-wide undo/redo framework.

## 18. Completion boundary

F3.3 automated PASS requires:

- approved written spec and implementation plan;
- TDD evidence for renderer/history/dialog/integration;
- locked restore/build/test PASS;
- scope audit proving no unauthorized dependency/PDF/ZPL/network expansion;
- closure docs and exact-head CI.

Physical mouse/touch/stylus QA may remain `NOT RUN` without blocking **automated F3.3 PASS**, but it must remain separately reported.

After F3.3 automated closure, next planned sub-slice is **F3.4 Local signature library**, with its own design/approval gate.
