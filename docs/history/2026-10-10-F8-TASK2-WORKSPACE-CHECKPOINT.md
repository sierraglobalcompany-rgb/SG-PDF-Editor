# F8 — Task 2 Managed Comment Domain + Workspace — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F8 — COMENTAR V1  
**Task:** 2 — dominio managed + workspace/history  
**Rama:** `feat/f8-comments-v1`  
**Base Task 1:** `5b03b9a78fa4301ce08d48851fbfff0a4f4b0d88`  
**Estado:** funcionalmente GREEN; este checkpoint requiere CI Windows exact-head antes de declarar Task 2 cerrada.

## Objetivo

Crear el estado durable managed de COMENTAR sin introducir todavía discovery nativo, writer, validator ni UI.

El contrato implementado cubre:

- source path normalizado;
- `PdfEditSourceFingerprint` capturado al crear workspace;
- flag `SourceOpenedWithPassword`;
- baseline original independiente del baseline guardado;
- snapshots managed por comentario;
- candidate-first create/update/move/resize/note-text/delete;
- undo/redo;
- `IsDirty`;
- `MarkSaved()`;
- detección de candidatos obsoletos;
- geometría finita/válida;
- cero handles nativos persistentes en `Features.Comments`.

## RED válido

Commit RED:

`39e8d3c2b535e18b0fd0519411285a862ef540b9`

Windows CI:

- workflow: `38092999397`
- job: `114333095139`
- checkout exacto: `39e8d3c2b535e18b0fd0519411285a862ef540b9`
- hygiene: PASS
- Labelize staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **771 PASS / 15 FAIL / 0 skipped / 786 total**

Los 15 fallos nuevos correspondieron al contrato F8 todavía inexistente (`CommentWorkspace`, `CommentRect`, `CommentTool` y tipos asociados). Los 771 tests previos permanecieron GREEN.

## GREEN implementado

Head funcional:

`ab644f6977d26022dabae18c8d61b951fd0484b7`

Windows CI:

- workflow: `38093147408`
- job: `114333521421`
- checkout exacto: `ab644f6977d26022dabae18c8d61b951fd0484b7`
- hygiene: PASS
- Labelize staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **786 PASS / 0 FAIL / 0 skipped / 786 total**
- suite duration: 15 s

## Modelo entregado

### Herramientas

`CommentTool` contiene únicamente el alcance F8 V1:

- Select
- Highlight
- Underline
- Strikeout
- Note
- Ink
- Rectangle
- Ellipse

### Modelos managed

Se añadieron:

- `CommentSubtype`;
- `CommentOperationKind`;
- `CommentKey`;
- `CommentPoint`;
- `CommentRect`;
- `CommentColor`;
- `CommentQuad`;
- `CommentStroke`;
- `CommentState`;
- `CommentCandidate`;
- `CommentMutation`.

`CommentRect` rechaza coordenadas no finitas y rectángulos sin ancho/alto positivo. `CommentState` rechaza identidad vacía, página negativa y borde no finito/no positivo.

Los arrays/listas recibidos por el snapshot se copian a colecciones read-only para no conservar buffers mutables suministrados por el caller.

## Workspace

`CommentWorkspace` mantiene tres conceptos separados:

1. `_originalBaseline`: cómo estaba el source original;
2. `_savedBaseline`: último estado publicado/confirmado por el caller;
3. `_states`: estado lógico actual de la sesión.

Esto permite dos comportamientos importantes:

- un comentario nuevo creado y eliminado antes de guardar vuelve a estado limpio y no produce `EditedStates`;
- una edición previamente guardada sigue apareciendo en `EditedStates` frente al source original, porque futuros Save As parten otra vez del source original.

### Candidate-first

Preparar una operación no modifica el workspace.

Solo `CommitCandidate()` cambia `_states` y crea historia.

El candidato captura la referencia exacta del snapshot esperado. Si otro cambio fue committed después de preparar el candidato, el commit stale falla cerrado con `InvalidOperationException` y no sobreescribe el estado más reciente.

### Undo / redo

- create: undo elimina la nueva identidad; redo la restaura;
- update/move/resize/note-text/delete: before/after exactos;
- una nueva edición después de undo limpia redo;
- no-op no crea historia;
- `MarkSaved()` conserva estado actual, actualiza baseline guardado y limpia undo/redo.

### Delete

- comentario existente: se conserva un `CommentState` con `Deleted=true` para que el writer futuro pueda materializar la eliminación;
- comentario nuevo aún no perteneciente al source: delete lo retira del estado actual; si no existen otros cambios, `IsDirty=false`.

## Seguridad de identidad

Task 2 usa `CommentKey` solo como identidad managed de sesión.

No se afirma todavía que ese key resuelva una anotación preexistente tras reabrir el PDF. La identidad estructural durable de anotaciones existentes sigue reservada a **Task 4 — Comment identity + deterministic hit testing** y deberá probarse contra fresh reopen.

## Cero handles nativos

Los tests inspeccionan todos los tipos bajo:

`SGPdf.App.Features.Comments`

y fallan si un field/property persiste `IntPtr` o `UIntPtr`.

Task 2 no llama PDFium. `PdfEditSourceFingerprint` es la única reutilización concreta de infraestructura previa.

## Auditoría de alcance

Compare desde Task 1 `5b03b9a78fa4301ce08d48851fbfff0a4f4b0d88` hasta head funcional `ab644f6977d26022dabae18c8d61b951fd0484b7`:

- status: ahead;
- 5 commits;
- 0 behind;
- merge base exacta = Task 1 checkpoint.

Archivos tocados únicamente:

1. `src/SGPdf.App/Features/Comments/CommentTool.cs`
2. `src/SGPdf.App/Features/Comments/CommentModels.cs`
3. `src/SGPdf.App/Features/Comments/CommentState.cs`
4. `src/SGPdf.App/Features/Comments/CommentWorkspace.cs`
5. `tests/SGPdf.App.Tests/CommentWorkspaceTests.cs`

No hubo cambios de:

- `.csproj`;
- lockfiles;
- paquetes/NuGet;
- PDFium interop;
- discovery de anotaciones;
- `PdfCommentWriter`;
- `PdfCommentOutputValidator`;
- MainWindow/UI;
- runtime network/cloud;
- F9+;
- `main`.

## Estado de integración

- `main` verificado aún en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
- no merge;
- no PR F8 todavía;
- Task 3 no iniciada.

## QA manual

**NOT RUN.**

Task 2 es lógica managed sin UI. No equivale a QA Windows de interacción, stylus/touch ni lectores externos.

## Siguiente gate

Cuando este checkpoint obtenga Windows CI exact-head GREEN, Task 2 queda **CLOSED / AUTOMATED PASS**.

Siguiente trabajo permitido tras una nueva continuación del usuario:

**Task 3 — managed discovery of existing annotations**.
