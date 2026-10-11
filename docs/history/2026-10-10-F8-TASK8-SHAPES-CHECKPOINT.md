# F8 — Task 8 Square/Circle — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F8 — COMENTAR V1  
**Task:** 8 — Square/Circle slice  
**Rama:** `feat/f8-comments-v1`  
**Base Task 7:** `0eeaafbb3b3117e63d5a787761290cd0d618fd84`  
**Estado:** funcionalmente GREEN; este checkpoint requiere su propio CI Windows exact-head antes de declarar Task 8 cerrada.

## Objetivo

Extender el writer de comentarios para materializar anotaciones PDF reales `SQUARE` y `CIRCLE`, manteniendo el alcance KISS de Task 8: datos/writer/validator existente y pruebas automatizadas, sin UI.

El contrato V1 de figuras queda limitado a capacidades demostradas por Task 1 contra `bblanchon.PDFium.Win32 156.0.8076`:

- rect/geometría;
- color RGB + alpha;
- grosor de borde;
- create/edit/delete;
- Save As + reopen/validator.

`InteriorColor`/relleno y appearance streams no se incorporan porque Task 1 no los promovió como contrato estable.

## RED válido

Commit RED:

`d6d4a0a3218ccc2e3c01516ceeb467bedd3885b3`

Windows CI:

- workflow: `38100180876`
- job: `114354324601`
- checkout exacto: `d6d4a0a3218ccc2e3c01516ceeb467bedd3885b3`
- hygiene: PASS
- Labelize staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **816 PASS / 4 FAIL / 0 skipped / 820 total**

Los cuatro fallos fueron exclusivamente los tests nuevos de Shapes y todos fallaron en el guard previo esperado:

`System.NotSupportedException: Task 7 solo guarda marcado de texto, notas TEXT e Ink.`

Por tanto, el RED demostró que el harness compilaba y que el hueco era exactamente la ausencia de `SQUARE/CIRCLE` en `PdfCommentWriter`.

## GREEN mínimo

Commit funcional:

`c0f06741b0956b51089e841933b83087711a1a1a`

Windows CI:

- workflow: `38100312519`
- job: `114354728597`
- checkout exacto: `c0f06741b0956b51089e841933b83087711a1a1a`
- hygiene: PASS
- Labelize staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **820 PASS / 0 FAIL / 0 skipped / 820 total**

## Comportamiento demostrado

### Creación múltiple

Se crean y reabren correctamente múltiples figuras en una misma página:

- dos `SQUARE`;
- un `CIRCLE`;
- geometrías diferentes;
- colores/alpha diferentes;
- grosores de borde diferentes.

El validator existente reabre el temporal y compara subtipo, rect, color y border width antes de publicación.

### Edición de figuras existentes

Con identidad managed estable sobre una apertura fresca del source se demostró para `SQUARE` y `CIRCLE`:

- move;
- resize;
- cambio de color/alpha;
- cambio de border width;
- Save As;
- reopen con geometría y propiedades esperadas.

No se permite convertir una figura en otro subtipo ni introducir quadpoints, Contents o Ink strokes.

### Delete y preservación de anotaciones no objetivo

Se eliminan un `SQUARE` y un `CIRCLE` existentes de forma segura y permanecen las otras cinco anotaciones del fixture:

- `TEXT`;
- `HIGHLIGHT`;
- `UNDERLINE`;
- `STRIKEOUT`;
- `INK`.

### Página rotada

Un `CIRCLE` creado en una página con rotación PDF de 90° conserva después de Save As/reopen:

- las coordenadas PDF del rect;
- border width;
- la rotación de página de 90°.

Task 8 no introduce una transformación visual adicional de coordenadas; el writer persiste la geometría en espacio PDF y deja la rotación de página intacta.

## Cambios de producción

`PdfCommentWriter` ahora:

- acepta `CommentSubtype.Square` y `CommentSubtype.Circle`;
- mapea a `FPDF_ANNOT_SQUARE` / `FPDF_ANNOT_CIRCLE`;
- materializa rect + color + border width en figuras nuevas;
- permite rect/color/borde al editar figuras existentes;
- sigue resolviendo anotaciones existentes por identidad managed antes de mutar;
- sigue eliminando por índices resueltos en orden descendente;
- mantiene Save As transaccional y validator ya existente.

Una figura nueva debe tener `BorderWidth` explícito y válido; no se inventa un default silencioso en el writer.

## Propiedades deliberadamente fuera de Task 8

- `InteriorColor` / relleno;
- personalización/generación manual de appearance streams;
- líneas/flechas/polígonos;
- UI, handles visuales y gestos de drag;
- QA visual en Acrobat/Foxit/PDF-XChange;
- transaction hardening adicional de Task 9.

## Auditoría de alcance

Compare desde `0eeaafbb3b3117e63d5a787761290cd0d618fd84` hasta el GREEN funcional `c0f06741b0956b51089e841933b83087711a1a1a`:

- status: ahead;
- 2 commits;
- 0 behind;
- merge base exacta = checkpoint Task 7.

Archivos funcionales tocados únicamente:

1. `src/SGPdf.App/Pdf/PdfCommentWriter.cs`
2. `tests/SGPdf.App.Tests/PdfCommentWriterShapeTests.cs`

No hubo cambios de:

- `PdfCommentOutputValidator.cs`;
- `PdfiumNative*`;
- `CommentState`/workspace/domain;
- XAML/MainWindow/UI;
- `.csproj` o lockfiles;
- paquetes/NuGet;
- segundo motor PDF;
- red/network/cloud;
- `main`.

## QA manual

**NOT RUN.**

Task 8 es cierre automatizado del slice data/writer. La interacción visual de Rectangle/Oval se implementará en Task 10 y la validación manual comercial permanece pendiente hasta ejecución real.

## Siguiente gate

Tras Windows CI exact-head GREEN sobre este checkpoint, Task 8 queda **CLOSED / AUTOMATED PASS**.

El siguiente trabajo permitido, solo tras `continua`, es **Task 9 — Transaction hardening + preflight integration**.
