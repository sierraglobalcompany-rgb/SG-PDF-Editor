# F8 — Task 10 — COMENTAR UI + interaction ownership checkpoint

Fecha local del proyecto: 2026-10-10

## Estado

**CERRADA / AUTOMATED PASS**

Rama: `feat/f8-comments-v1`

Base Task 9:
`ac1a7cd7948d38fbb5b4b793bcf1a0b6fbf90e11`

Functional GREEN:
`9eecd3cff13d6f2c67df26db1e2ef4f01faa62a1`

No merge. `main` sigue intencionalmente congelado.

## Objetivo de Task 10

Integrar el core managed y writer ya probados de COMENTAR con la UI principal sin crear un segundo sistema de navegación ni mezclar COMENTAR con EDITAR.

Contrato:
- modo superior `COMENTAR`;
- toolbar contextual;
- panel derecho de propiedades;
- ownership exclusivo de gestos;
- creación de markup desde selección real de LEER;
- Nota, Ink y figuras con preview managed;
- selección/edición conservadora;
- undo/redo/Delete/Esc;
- dirty guard al abandonar el modo;
- Save As con preflight F8;
- `MarkSaved()` únicamente después de writer exitoso.

## RED

Archivo:
- `tests/SGPdf.App.Tests/MainWindowCommentModeTests.cs`

RED SHA:
`b2926523df474cf6859f3eb0e77a8d1962bda165`

Windows CI:
- workflow `38101947131`
- job `114359518317`
- build: 0 warnings / 0 errors
- tests: 837 PASS / 9 FAIL / 0 skipped / 846 total

Los 837 tests previos permanecieron verdes. Los 9 fallos pertenecían exclusivamente al nuevo contrato Task 10 todavía ausente.

El fallo de cleanup observado en una prueba de dirty guard durante RED fue consecuencia de la salida anticipada por superficie inexistente; no fue una regresión en los tests previos.

## GREEN implementado

### Superficie COMENTAR

Archivos:
- `src/SGPdf.App/MainWindow.Comments.cs`
- `src/SGPdf.App/MainWindow.Comments.Integration.cs`

Se añadió:
- botón superior `COMENTAR`;
- toolbar contextual:
  - Seleccionar
  - Resaltar
  - Subrayar
  - Tachado
  - Nota
  - Lápiz
  - Rectángulo
  - Óvalo
  - Deshacer
  - Rehacer
  - Guardar como...
- panel derecho de propiedades;
- paleta conservadora de siete colores;
- opacidad;
- contenido de Nota;
- grosor de borde para figuras;
- eliminar comentario;
- overlay propio sobre el lector continuo.

### Ownership de interacción

Archivo modificado:
- `src/SGPdf.App/MainWindow.ReaderSelection.cs`

Regla:
- Resaltar/Subrayar/Tachado dejan que LEER sea dueño del drag de selección textual y, al completar la selección, COMENTAR crea exactamente una anotación managed.
- Nota/Lápiz/Rectángulo/Óvalo/Seleccionar bloquean el owner de selección textual y poseen el gesto correspondiente.
- cambios de modo físicos usan preview guards;
- `Ctrl+O` y recientes quedan guardados antes de abandonar COMENTAR.

No se creó un segundo motor de selección textual.

### Creación y edición

- markup: usa `ReaderTextSelection` + `TextMarkupQuadBuilder`;
- Nota: click crea TEXT y abre editor inline;
- Ink: down/move/up usa preview managed y un único commit;
- Square/Circle: drag preview y un único commit;
- selección: usa `CommentHitTester`;
- move: Nota/Square/Circle;
- resize: Square/Circle;
- anotaciones externas no suficientemente seguras permanecen read-only.

### Discovery perezoso

COMENTAR carga anotaciones por página cuando se necesitan, reutilizando `GetCommentsOnPage` y `CommentIdentity.CreateForPage`.

Solo se promueve a estado editable una anotación existente si el snapshot contiene la información que el writer necesita para re-resolverla y preservarla de forma conservadora. El resto se representa read-only.

### Shortcuts y lifecycle

- Ctrl+Z: workspace undo
- Ctrl+Y: workspace redo
- Delete: elimina selección
- Esc: cancela editor/gesto/selección/herramienta por prioridad
- TextBox conserva sus propios shortcuts
- dirty guard bloquea cambio de modo/cierre/apertura cuando el usuario no confirma descarte
- no autosave

### Save As

Flujo UI:
1. seleccionar destino;
2. ejecutar preflight F8;
3. bloquear si existe finding Block;
4. pedir confirmación explícita si hay warnings;
5. invocar `PdfCommentWriter`;
6. solo si el writer termina exitosamente: `workspace.MarkSaved()`.

Si el writer falla, el workspace sigue dirty.

## Incidencia GREEN

Primer functional GREEN candidato:
`5b8c00f68250e341ad89c588888e8d867f94f561`

Windows CI:
- workflow `38102281894`
- job `114360506507`
- build FAIL

Causa exacta:
`Path.GetFileName` quedó ambiguo entre `System.IO.Path` y `System.Windows.Shapes.Path` en la nueva UI.

Corrección mínima:
- `src/SGPdf.App/GlobalUsings.Path.cs`
- alias explícito de `Path` a `System.IO.Path`.

No hubo cambios de lógica por esta corrección.

## Functional GREEN verificado

SHA:
`9eecd3cff13d6f2c67df26db1e2ef4f01faa62a1`

Windows CI:
- workflow `38102393301`
- job `114360835844`
- hygiene PASS
- locked restore PASS
- build: **0 warnings / 0 errors**
- tests: **846 PASS / 0 FAIL / 0 skipped**

## Scope audit

Comparación:
`ac1a7cd7948d38fbb5b4b793bcf1a0b6fbf90e11` → `9eecd3cff13d6f2c67df26db1e2ef4f01faa62a1`

Cambios únicamente en:
1. `src/SGPdf.App/GlobalUsings.Path.cs`
2. `src/SGPdf.App/MainWindow.Comments.Integration.cs`
3. `src/SGPdf.App/MainWindow.Comments.cs`
4. `src/SGPdf.App/MainWindow.ReaderSelection.cs`
5. `tests/SGPdf.App.Tests/MainWindowCommentModeTests.cs`

Sin cambios en:
- `PdfiumNative*`
- `PdfCommentWriter`
- `PdfCommentPreflight`
- NuGet / paquetes
- lockfiles
- F9
- matriz de preservación Task 11

## Lo que este checkpoint NO afirma

- No afirma QA manual Windows.
- No afirma prueba real de stylus/touch.
- No afirma validación visual en Adobe/Edge/Chrome/Foxit.
- No afirma aún la matriz de preservación F8 de estructuras ricas.
- No afirma merge a `main`.

## Siguiente unidad autorizable

**Task 11 — F8 preservation matrix + lifecycle proof**

Debe probar preservación de estructuras preexistentes y lifecycle de COMENTAR antes del hardening/closure final de Task 12.
