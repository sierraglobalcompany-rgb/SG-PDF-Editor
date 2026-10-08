# F3.3 — Draw Signature with WPF InkCanvas — Design Specification

**Date:** 2026-10-08  
**Status:** written spec awaiting user review  
**Base:** F3.2 final head `2aa1f58a6e01397e84d8f8cfcf7eb6e0e168cf62`  
**Branch:** `feat/f3-3-drawn-signature`

## 1. Intent

F3.3 adds a third local way to create a visual signature: draw it directly inside SG PDF Editor with mouse, touch, or a digital pen/stylus.

The output is the existing F3.1 `SignatureAsset`. F3.3 does not introduce a second placement model, PDF writer, coordinate system, or persistence format. Once the user presses **Aplicar**, the result enters the same `AddSignatureAsset(...)` flow already used by transparent PNG import and F3.2 photo preparation.

Success means a user can open **FIRMAR → Dibujar firma...**, draw naturally, make basic corrections, apply the result, and then move/resize/duplicate/delete/save it through the already-proven F3.1 workflow.

F3.3 remains a visual-signature feature. It is not cryptographic signing and it is not a general drawing editor.

## 2. Approved product scope

The drawing dialog provides:

- WPF `InkCanvas` as the drawing surface;
- mouse input;
- touch input through native WPF ink handling when supported by the Windows device;
- stylus/digital pen input through native WPF ink handling;
- black ink;
- blue ink using the same F3.2 blue `#194196`;
- three stroke widths: thin, medium, thick;
- Undo;
- Redo;
- Clear;
- Cancel;
- Apply;
- transparent cropped output converted to `SignatureAsset`.

The FIRMAR panel gains one action:

`Dibujar firma...`

The three supported creation paths then converge:

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

F3.3 does not include:

- cryptographic signatures;
- pressure-sensitive brush width;
- tilt/azimuth stylus behavior;
- custom brush engines;
- advanced stroke smoothing/stabilization;
- vector/SVG/PDF-path signature output;
- partial pixel eraser;
- lasso/select/move individual strokes;
- shape tools;
- text/name/date fields;
- arbitrary colors or a color picker;
- arbitrary brush widths;
- image/photo editing;
- cloud storage or upload;
- signature requests to other people;
- persistence/library behavior from F3.4;
- general document undo/redo.

If native WPF ink proves materially insufficient for common stylus hardware, F3.3 must return to design before introducing another drawing/input framework.

## 4. Architecture

F3.3 is a feature-local source adapter only:

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
- F3.2 photo processor;
- ZPL/Labelize;
- PDFsharp scope;
- runtime networking.

No new NuGet/runtime dependency is expected. WPF built-in ink APIs are the implementation authority.

## 5. Drawing surface behavior

### 5.1 InkCanvas

The dialog uses one WPF `InkCanvas` in ink mode. It visually presents a light neutral/white signing area suitable for drawing, but that visual surface is **not** part of the exported signature.

Only the `StrokeCollection` is authoritative for the resulting signature image.

The canvas background, checkerboard, borders, buttons, cursor, selection adorners, and dialog chrome must never be rasterized into the resulting `SignatureAsset`.

### 5.2 Input devices

Use WPF's native input path rather than writing a custom pointer engine.

Expected product behavior:

- left mouse drag draws;
- a Windows-supported stylus/digital pen draws;
- touch may draw when WPF/Windows promotes it through the InkCanvas ink path on the device;
- no special multi-touch gesture engine is added;
- no pressure-width promise is made in F3.3.

Automated CI cannot establish real stylus/touch hardware compatibility. Physical mouse/touch/stylus QA remains a separate manual gate.

### 5.3 Ink colors

Supported colors are fixed:

- Black: `#000000`;
- Blue: `#194196`.

The selected color applies to newly created strokes. Existing strokes preserve the drawing attributes with which they were created. This avoids hidden retroactive mutation and keeps Undo/Redo exact.

### 5.4 Stroke widths

Freeze the first version to three WPF logical widths:

- Thin: `2.0 DIP`;
- Medium: `3.5 DIP`;
- Thick: `5.0 DIP`.

Medium is the default.

The selected width applies to newly created strokes. Existing strokes retain their original width.

Round stylus tips/caps are preferred through normal WPF drawing attributes. No custom brush renderer is introduced.

## 6. Drawing state and history

F3.3 history is **dialog-local only**. It does not create a generic application command system.

Conceptually the dialog maintains:

- current `StrokeCollection`;
- undo stack for removed strokes;
- redo stack for strokes restored by redo;
- current ink color;
- current ink width.

### New stroke

When a completed stroke is added:

1. it remains in the canvas stroke collection;
2. it becomes the newest undoable stroke;
3. any redo stack is cleared;
4. Apply state is recalculated.

### Undo

Undo removes the most recently drawn/restored stroke from the current stroke collection and pushes it onto the redo stack.

### Redo

Redo restores the most recently undone stroke, preserving its original points, color, width, and drawing attributes.

### New stroke after Undo

Drawing a new stroke after one or more Undo operations clears the redo stack.

### Clear

`Limpiar` removes all current strokes and clears both history stacks. In F3.3 MVP, Clear itself is not undoable. This is intentionally simpler than introducing snapshot history.

### Buttons

- Undo enabled only when a stroke can be undone;
- Redo enabled only when a stroke can be restored;
- Clear enabled only when at least one stroke exists;
- Apply enabled only when the current strokes satisfy the useful-signature rule.

## 7. Useful-signature validation

F3.3 must reject accidental taps, tiny dots, and effectively empty drawings.

Before Apply, the union of current stroke bounds must:

- contain at least one stroke with at least two stylus points; and
- have useful ink bounds at least `4 DIP` wide and `4 DIP` high.

If the user has no useful drawing, Apply remains disabled. The renderer also repeats validation so correctness does not depend only on UI state.

A failed validation must not clear the user's strokes.

## 8. Transparent rasterization

### 8.1 Source of truth

Do **not** render the visible `InkCanvas` control directly.

On Apply:

1. clone/snapshot the current strokes required for the render;
2. compute union ink bounds from the strokes;
3. include a small transparent padding region;
4. draw the strokes onto a transparent WPF drawing surface/visual;
5. rasterize only that padded region;
6. normalize to top-to-bottom BGRA with alpha;
7. create the existing immutable `SignatureAsset`.

This guarantees that the dialog background does not become a white rectangle in the PDF.

### 8.2 Padding

Use padding in WPF logical units:

```text
paddingDip = clamp(max(8, maxStrokeWidth * 2), 8, 24)
```

Apply the padding on all sides and crop to the resulting bounds.

### 8.3 Raster quality

WPF uses 96 logical DPI. F3.3 final output rasterization uses a fixed scale corresponding to **300 DPI**:

```text
rasterScale = 300 / 96 = 3.125
```

Output pixel dimensions are derived from the padded DIP bounds times `3.125`, rounded upward to ensure the full stroke/padding area is preserved.

The generated bitmap uses transparency/alpha and must not use JPEG.

The existing decoded-image safety policy remains authoritative: the final BGRA asset must not exceed **20,000,000 pixels**. Validate geometry before allocating the full output buffer where practical.

If the result would exceed that limit, Apply fails with a controlled message while preserving the strokes in the dialog.

No temporary signature-image file is created.

## 9. Proposed feature-local contracts

Keep names focused and small; exact implementation can be finalized in the implementation plan.

Conceptual contracts:

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

The dialog may also use a tiny feature-local history helper if that keeps stroke-stack semantics independently testable. Do not introduce an application-wide command framework.

## 10. Dialog UX

Title:

`Dibujar firma`

Recommended layout:

```text
┌────────────────────────────────────────────────────┐
│ Dibujar firma                                     │
│                                                    │
│  Color: [Negro] [Azul]   Grosor: [Fino|Medio|Gr.] │
│                                                    │
│  ┌──────────────────────────────────────────────┐  │
│  │                                              │  │
│  │              InkCanvas                      │  │
│  │                                              │  │
│  └──────────────────────────────────────────────┘  │
│                                                    │
│ [Deshacer] [Rehacer] [Limpiar]   [Cancelar] [Aplicar]│
└────────────────────────────────────────────────────┘
```

Named controls for testability should be conventional and explicit, for example:

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

The exact visual styling may reuse existing WPF conventions. Do not introduce a new design framework.

## 11. Apply / Cancel semantics

### Apply

Apply:

1. validates current ink;
2. renders the current strokes into a transparent `SignatureAsset`;
3. stores the resulting asset as the dialog result;
4. closes successfully;
5. MainWindow passes it to existing `AddSignatureAsset(...)`;
6. the new signature is centered/selected/dirty exactly like PNG/photo assets.

If rendering fails:

- keep the dialog open;
- preserve all current strokes and history;
- show controlled status/error;
- do not return an asset;
- do not touch the PDF or current placement state.

### Cancel

Cancel returns no asset and changes nothing outside the dialog.

Closing the window with the window-close control is equivalent to Cancel.

## 12. MainWindow integration

F3.3 adds one narrow seam next to the existing F3.1/F3.2 source seams, conceptually:

```csharp
private Func<Window, SignatureAsset?> _drawSignature =
    static owner => SignatureDrawDialog.Draw(owner);
```

The FIRMAR properties panel adds `Dibujar firma...` below the existing source actions.

If the dialog returns `null`, MainWindow must not modify:

- existing placement list;
- current selection;
- dirty state;
- active PDF;
- current page.

If it returns a valid `SignatureAsset`, MainWindow calls the same `AddSignatureAsset(...)` path used by the other signature sources.

No new PDF save path is added.

## 13. Failure and privacy behavior

All F3.3 processing is local/in-memory.

No:

- network;
- HTTP;
- telemetry;
- cloud API;
- upload;
- temp PNG/JPEG file;
- clipboard requirement;
- automatic persistent signature storage.

A rendering/allocation failure must preserve the user's current strokes until they Cancel, Clear, or retry.

## 14. Automated acceptance

At minimum, automated tests must prove:

### Rendering

- empty strokes are rejected;
- too-small/tap-like input is rejected;
- valid black stroke produces visible black pixels with alpha;
- valid blue stroke uses exactly `#194196` with alpha;
- background pixels remain transparent;
- rendered result is cropped around ink with padding;
- output respects the 20M-pixel safety limit;
- final rasterization is deterministic for equivalent stroke input;
- Thin / Medium / Thick produce different visible stroke widths.

### History

- one completed stroke enables Undo;
- Undo removes only the most recent stroke;
- Redo restores the exact removed stroke;
- repeated Undo/Redo preserves order;
- a new stroke after Undo clears Redo;
- Clear leaves zero strokes and resets both histories.

### Dialog

- opens empty with Medium + Black defaults;
- Apply disabled without useful ink;
- valid ink enables Apply;
- Cancel returns no asset;
- Apply returns a normal `SignatureAsset`;
- render failure preserves strokes and keeps the dialog usable.

### FIRMAR integration

- `Dibujar firma...` action exists in the FIRMAR panel;
- cancel produces no placement and preserves existing state;
- failure produces no placement and preserves existing state;
- successful Apply adds exactly one normal selected dirty placement;
- existing PNG and photo flows remain unchanged.

### Regression / architecture

Full locked restore/build/test must remain GREEN with:

- 0 build warnings;
- 0 build errors;
- no new runtime package;
- no `.csproj`/lock mutation unless separately approved;
- no PDFium writer/coordinate changes;
- no ZPL changes;
- no runtime network surface.

## 15. Manual hardware QA

Automated PASS does not claim real input-device quality.

Manual Windows QA remains a separate gate for:

1. mouse drawing;
2. Windows touch screen, when available;
3. Surface/Wacom/other stylus, when available;
4. thin/medium/thick feel;
5. black/blue output;
6. Undo/Redo/Clear usability;
7. Apply → move/resize/save through F3.1;
8. visual inspection in an external PDF viewer;
9. offline operation.

If touch or stylus behavior varies by hardware, record the device/driver rather than generalizing the result to all Windows hardware.

## 16. Non-regression boundaries

F3.3 must leave unchanged:

- F3.1 PDF placement/write semantics;
- F3.2 photo preparation behavior;
- original-PDF preservation;
- cryptographic-signature warning behavior;
- ZPL/Labelize flows;
- PDFsharp scope;
- offline/privacy policy;
- existing no-auto-merge rule.

## 17. Stop conditions

Stop implementation and return to design if any of the following becomes necessary:

- new runtime drawing/image dependency;
- custom low-level pointer/stylus framework;
- PDFium changes just to support drawn signatures;
- temporary image files required for correctness;
- pressure-sensitive brush engine required for acceptable MVP quality;
- F3.4 persistent storage needed to make F3.3 function;
- cloud/network service required;
- general application-wide undo/redo framework required.

## 18. Completion boundary

F3.3 automated PASS requires:

- approved written spec and implementation plan;
- TDD evidence for renderer/history/dialog/integration;
- full locked restore/build/test PASS;
- scope audit proving no unauthorized dependency/PDF/ZPL/network expansion;
- closure documentation and exact-head CI.

Physical mouse/touch/stylus QA may remain `NOT RUN` without blocking **automated F3.3 PASS**, but it must be reported separately and never implied by CI.

After F3.3 automated closure, the next planned sub-slice is **F3.4 Local signature library**, which requires its own design/approval gate before implementation.
