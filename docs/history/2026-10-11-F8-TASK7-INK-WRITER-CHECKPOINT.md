# F8 — Task 7 — Ink writer/validator checkpoint

**Fecha:** 2026-10-11  
**Rama:** `feat/f8-comments-v1`  
**Base Task 6:** `9ca276c8fbe4679d0f27edc74a434805f82a76c8`  
**Estado al crear este checkpoint:** GREEN funcional demostrado; pendiente únicamente CI exact-head del propio checkpoint.

## Alcance ejecutado

Task 7 se mantuvo estrictamente en el slice de datos/writer/validator para `INK`, sin UI:

- crear Ink real con uno o múltiples strokes;
- preservar orden de strokes y orden de puntos;
- rect y color/alpha;
- save -> reopen -> discovery/validator;
- editar Ink existente mediante identidad estable;
- reemplazar InkList existente de forma explícita;
- mover mediante rect actualizado;
- eliminar Ink existente sin tocar las otras anotaciones;
- cancelación previa no publica destino y conserva `CommentWorkspace.IsDirty`.

No se añadió grosor Ink: Task 1 no demostró un contrato de width estable para `INK`, por lo que V1 no lo inventa.

## TDD

### Intento RED inicial no válido

Commit: `7d573bf7bbe247e3c87d108443fd670a6488ef9c`  
Workflow: `38099621051`  
Job: `114352662277`

El build falló antes de ejecutar tests porque el nuevo harness usó `.Length` sobre `IReadOnlyList<PdfiumNative.PointF[]>`. Ese resultado no se contó como RED válido. Se aplicó systematic debugging y se corrigió únicamente el harness de test.

### RED válido

Commit: `e2c49c17b3bec6f98a2be17893b83265431e85a7`  
Workflow: `38099703248`  
Job: `114352910287`

Evidencia:

- build: `0 warnings / 0 errors`;
- total: `816`;
- PASS: `813`;
- FAIL: `3`;
- skipped: `0`;
- los tres fallos nuevos fueron exactamente `NotSupportedException: Task 6 solo guarda marcado de texto y notas TEXT.`;
- el test de cancelación previa ya pasó sobre la implementación Task 6.

Fallos esperados:

1. creación Ink multi-stroke;
2. edición/reemplazo de InkList existente;
3. eliminación de Ink existente preservando las otras anotaciones.

### GREEN mínimo

Commit funcional: `6233e836d9120b78686640394aa8768bc10c5c7d`  
Workflow: `38099826351`  
Job: `114353266316`

Cambio de producción limitado a `PdfCommentWriter.cs`:

- `CommentSubtype.Ink` entra en el scope permitido;
- creación con `FPDFAnnot_AddInkStroke`;
- edición existente con `FPDFAnnot_RemoveInkList` + reconstrucción ordenada de strokes;
- rect/color actualizables;
- delete reutiliza la ruta segura existente;
- se rechazan quadpoints, Contents y BorderWidth para Ink en Task 7;
- cada stroke respeta `CancellationToken` antes de materializarse.

Evidencia Windows x64 exact-head:

- checkout exacto `6233e836d9120b78686640394aa8768bc10c5c7d`;
- build `0 warnings / 0 errors`;
- tests `816 PASS / 0 FAIL / 0 skipped`;
- save/reopen demuestra múltiples strokes y orden de puntos;
- `FPDFAnnot_RemoveInkList` + re-add demuestra reemplazo real sobre el runtime pinneado;
- delete deja intactas las otras seis anotaciones de la fixture de siete subtipos.

## Validator

`PdfCommentOutputValidator` no necesitó cambios: desde Tasks previas ya comparaba subtype, rect, color y `CommentStroke`/puntos, y el GREEN real demostró que ese contrato valida Ink correctamente tras reopen.

## Cancelación y límites de Task 7

El test de Task 7 prueba cancelación antes de entrar a materialización: no aparece destination y el workspace sigue dirty.

Una cancelación determinista inyectada exactamente después de crear temporal o en un punto intermedio de publicación requeriría hooks artificiales en este slice. Para mantener KISS, esa evidencia de fronteras transaccionales queda explícitamente en **Task 9 — Transaction hardening + preflight integration**, cuyo plan ya exige cancelación before/after temp, cleanup y preservación del destino existente. No se declara aquí una prueba que no se ejecutó.

## Scope audit

Comparación `9ca276c8... -> 6233e836...`:

- `src/SGPdf.App/Pdf/PdfCommentWriter.cs` — modificado;
- `tests/SGPdf.App.Tests/PdfCommentWriterInkTests.cs` — nuevo.

No hubo cambios en:

- UI / `MainWindow`;
- `PdfCommentOutputValidator`;
- modelos managed existentes;
- PDFium interop;
- paquetes/NuGet;
- F8 Task 8+;
- `main`.

## Nota de higiene del repositorio

Durante la ejecución se creó accidentalmente la rama `tmp-do-not-use`, apuntando al checkpoint de Task 6 `9ca276c8...`. No contiene cambios propios y no interviene en `feat/f8-comments-v1` ni en `main`. El conector disponible en esta sesión no expone eliminación de refs/branches, por lo que no se afirma que haya sido eliminada; debe borrarse cuando exista una acción de eliminación de rama disponible.

## QA manual

**NOT RUN.**

Task 7 todavía no incluye input WPF de mouse/stylus/touch; esa interacción pertenece a Task 10. Este checkpoint solo cubre datos managed, writer, reopen y validación automatizada.
