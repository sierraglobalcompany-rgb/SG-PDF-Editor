# F8 — Task 3 Managed Comment Discovery — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F8 — COMENTAR V1  
**Task:** 3 — descubrimiento managed de anotaciones existentes  
**Rama:** `feat/f8-comments-v1`  
**Base Task 2:** `d5125e2bdf1207fea1e15cb397fadd73c2205e85`  
**Estado:** funcionalmente GREEN; este checkpoint requiere su propio Windows CI exact-head antes de declarar Task 3 cerrada.

## Objetivo

Leer anotaciones existentes por página desde el `PdfDocumentSession` actual y devolver snapshots managed sin handles nativos persistentes, usando exclusivamente PDFium y `PdfiumRuntime.NativeGate`.

Task 3 no crea, modifica, elimina ni publica anotaciones. No contiene writer, validator, hit testing ni UI.

## RED

El primer commit RED fue:

`494f18e57d255b7d447ea73bd5932b8cb53615a5`

Ese intento detectó un defecto del propio test: dos asserts de precisión sobre `double?` seleccionaban una sobrecarga incorrecta de xUnit y el build falló con 2 errores de compilación. No se aceptó como RED válido.

El test fue corregido sin tocar código productivo en:

`3d96cbfa6987e0fcc332dae31b4238009bf4c44c`

Windows CI RED válido:

- workflow: `38093789868`
- job: `114335394011`
- checkout exacto: `3d96cbfa6987e0fcc332dae31b4238009bf4c44c`
- hygiene: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **786 PASS / 5 FAIL / 0 skipped / 791 total**

Los 5 fallos nuevos fueron exactamente por ausencia de Task 3:

1. `GetCommentsOnPage_ReadsSevenSupportedAnnotationsIntoManagedSnapshots`
2. `GetCommentsOnPage_RepeatedCallsReturnEquivalentManagedSnapshots`
3. `GetCommentsOnPage_HonorsPreCanceledToken`
4. `GetCommentsOnPage_RejectsInvalidPageIndex`
5. `PdfCommentInfo_DoesNotPersistNativeHandles`

Las causas observadas fueron `MissingMethodException` para `PdfDocumentSession.GetCommentsOnPage` y `TypeLoadException` para `PdfCommentInfo`. Los 786 tests previos permanecieron GREEN.

## GREEN funcional

Head funcional:

`402a80d91be3e114f584e7df638b80c316be16d2`

Archivos productivos nuevos:

- `src/SGPdf.App/Pdf/PdfCommentInfo.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.Comments.cs`

Test de contrato:

- `tests/SGPdf.App.Tests/PdfCommentDiscoveryTests.cs`

Windows CI:

- workflow: `38093907010`
- job: `114335737047`
- checkout exacto: `402a80d91be3e114f584e7df638b80c316be16d2`
- hygiene: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **791 PASS / 0 FAIL / 0 skipped / 791 total**

## Contrato implementado

`PdfDocumentSession.GetCommentsOnPage(pageIndex, cancellationToken)`:

- valida cancelación y page bounds;
- toma `PdfiumRuntime.NativeGate`;
- carga solo la página solicitada;
- enumera con `FPDFPage_GetAnnotCount` / `FPDFPage_GetAnnot`;
- convierte cada anotación a `PdfCommentInfo` managed;
- cierra cada handle de anotación en `finally`;
- cierra la página en `finally`;
- libera `NativeGate` en `finally`.

No existe escaneo eager de todo el documento.

## Snapshot managed

`PdfCommentInfo` conserva únicamente valores managed:

- `PageIndex`
- `AnnotationIndex` de la apertura actual
- `NativeSubtype`
- `CommentSubtype?`
- `IsEditable`
- `CommentRect?`
- `CommentColor?`
- quadpoints
- `Contents`
- Ink strokes
- border width

No contiene `IntPtr`, `UIntPtr` ni handle PDFium durable.

El índice nativo se conserva solo como descriptor de esta lectura. **No se promueve como identidad durable entre reaperturas**; ese problema pertenece a Task 4.

## Subtipos V1

Se mapean como editables cuando tienen subtipo V1 reconocido y rect válido:

- TEXT
- SQUARE
- CIRCLE
- HIGHLIGHT
- UNDERLINE
- STRIKEOUT
- INK

Un subtipo desconocido queda explícitamente con:

- `Subtype = null`
- `IsEditable = false`

Task 3 no lo reconstruye, elimina ni altera. La preservación transaccional real se medirá posteriormente con el writer F8.

## Propiedades leídas

- rect: `FPDFAnnot_GetRect`
- RGB + alpha: `FPDFAnnot_GetColor`
- markup quadpoints: `Count/GetAttachmentPoints`
- sticky note Unicode: `FPDFAnnot_GetStringValue("Contents")`
- Ink strokes: `GetInkListCount/GetInkListPath`
- Square/Circle border width: `FPDFAnnot_GetBorder`

Las propiedades ausentes o no legibles se representan conservadoramente como `null`, vacío o read-only según corresponda; no se inventan valores.

## Evidencia del fixture

Sobre el fixture nativo de Task 1, Task 3 recupera en orden:

1. Highlight
2. Underline
3. Strikeout
4. Text
5. Ink
6. Square
7. Circle

Además se verifican:

- Highlight rect `55,305,185,335`;
- color RGBA `255,220,0,160`;
- quadpoints exactos del fixture;
- Note `Contents = "Nota NIÑO áé"`;
- Ink con un stroke de tres puntos;
- Square/Circle border width `2.5`;
- dos lecturas consecutivas devuelven snapshots managed equivalentes pero objetos distintos;
- cancelación previa;
- page index inválido;
- ausencia de handles nativos persistentes en `PdfCommentInfo`.

## Auditoría de alcance

Compare Task 2 checkpoint → head funcional:

- base: `d5125e2bdf1207fea1e15cb397fadd73c2205e85`
- head: `402a80d91be3e114f584e7df638b80c316be16d2`
- status: ahead
- 4 commits
- 0 behind
- merge base exacta = Task 2 checkpoint

Archivos tocados únicamente:

1. `src/SGPdf.App/Pdf/PdfCommentInfo.cs`
2. `src/SGPdf.App/Pdf/PdfDocumentSession.Comments.cs`
3. `tests/SGPdf.App.Tests/PdfCommentDiscoveryTests.cs`

No hubo cambios de:

- `.csproj` / lockfiles / NuGet;
- segundo motor PDF;
- network/cloud;
- `CommentWorkspace`;
- `PdfCommentWriter`;
- `PdfCommentOutputValidator`;
- MainWindow/UI;
- F9+;
- `main`.

## QA manual

**NOT RUN.**

Task 3 prueba descubrimiento managed automatizado. No equivale a QA visual, stylus/touch ni verificación en lectores externos.

## Siguiente gate

Cuando este checkpoint obtenga Windows CI exact-head GREEN, Task 3 queda **CLOSED / AUTOMATED PASS** y el siguiente trabajo permitido es **Task 4 — Comment identity + deterministic hit testing**.
