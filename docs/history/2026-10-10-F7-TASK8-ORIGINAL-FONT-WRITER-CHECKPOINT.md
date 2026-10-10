# F7 — Task 8 Combined Writer / OriginalFont — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 8 — writer combinado, ruta `OriginalFont`  
**Estado:** **FUNCTIONAL_GREEN / FINAL_CI_PENDING**  
**Rama:** `feat/f7-text-v1`  
**Base Task 8:** `6b41e387ce719b11e06060e519f2c4432d816ef4`  
**RED funcional:** `fa3374af8d51f5e68f5c9b0158b359937ff38882`  
**RED adicional color:** `f6820df7cfdf3ae1ce5d2cc5dc7f8559425ddecb`  
**Head funcional GREEN:** `8909272ff987e3e2ae3d471602c0d2897934a7f4`

## Alcance entregado

Task 8 convierte `PdfEditWriter` en el writer combinado mínimo requerido por F7 para imágenes + texto `OriginalFont`, sin abrir todavía la ruta `FallbackTtf`.

### Writer combinado

- conserva el overload F6 de imágenes para no romper compatibilidad;
- añade `SaveAsCopy(ImageEditWorkspace, TextEditWorkspace, ...)`;
- exige que ambos workspaces pertenezcan al mismo path y al mismo `PdfEditSourceFingerprint` antes de preflight/firma/mutación;
- rechaza cualquier `TextEditState` con estrategia distinta de `OriginalFont`;
- reabre el PDF original una sola vez bajo `PdfiumRuntime.NativeGate`;
- agrupa la unión de páginas editadas por imágenes y texto;
- en cada página resuelve **todos** los handles de imagen y texto antes de la primera mutación;
- aplica imágenes + texto en la misma apertura;
- llama `FPDFPage_GenerateContent` exactamente una vez por página editada;
- conserva el flujo transaccional F6: temp -> reopen/render validation existente -> publicación atómica.

### Ruta `OriginalFont`

Se añadió únicamente el binding nativo confirmado:

- `FPDFText_SetText`.

Antes de mutar, el writer valida el snapshot original del texto por:

- ordinal y tipo de objeto;
- texto exacto;
- matriz;
- bounds;
- tamaño;
- base font name;
- fill color;
- render mode.

La mutación `OriginalFont`:

- usa `FPDFText_SetText` cuando cambia el texto;
- usa `FPDFPageObj_SetFillColor` cuando cambia el color;
- preserva fuente, tamaño y matriz;
- bloquea un cambio de tamaño inesperado en esta ruta.

### Shell Save As

`MainWindow.Edit.cs` ahora mantiene opcionalmente `_textEditWorkspace` y un seam `_saveCombinedEditCopy`.

- sin text workspace, el flujo F6 de imagen sigue intacto;
- con text workspace, Save As pasa ambos workspaces al writer combinado;
- los dos baselines se marcan guardados **solo después** de una publicación exitosa;
- si el writer falla, ambos permanecen dirty;
- `ResetEditState()` limpia también el text workspace.

Task 8 no crea todavía el workspace de texto desde UI; eso queda para la tarea de integración UI correspondiente.

## RED

Commit principal RED: `fa3374af8d51f5e68f5c9b0158b359937ff38882`  
Workflow: `38078457541`  
Job: `114290248232`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **721 PASS / 5 FAIL / 726 total**;
- los cinco fallos fueron exclusivamente seams ausentes de Task 8:
  - overload combinado de `SaveAsCopy`;
  - constructor de test con contador de `GenerateContent`;
  - `_textEditWorkspace`;
  - `_saveCombinedEditCopy`.

El fixture mixto PDF abrió y descubrió correctamente sus objetos; el RED no provenía del fixture.

Antes de producción GREEN se añadió en `f6820df7cfdf3ae1ce5d2cc5dc7f8559425ddecb` el caso de fill color para asegurar la ruta `OriginalFont` completa disponible en Task 8.

## GREEN

Commits productivos:

- `9c11f48572826830e75edaae0fde21cbaedc4eae` — binding `FPDFText_SetText`;
- `4cdc36d908fdc7d7bb496c42d69b3429f0324db7` — writer combinado + resolución previa + ruta `OriginalFont`;
- `8909272ff987e3e2ae3d471602c0d2897934a7f4` — shell Save As + doble baseline.

Workflow exacto del head funcional: `38079099320`  
Job: `114292136470`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **727 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

Los tests Task 8 prueban:

1. `CASA 123 -> CASA 321` persiste exacto tras save/reopen y conserva font name, font size, color, matriz y render mode;
2. cambio de fill color persiste y conserva font/size/matrix;
3. fixture mixto con imagen antes que texto: borrar imagen + editar texto solo funciona si ambos handles se resuelven antes de mutar;
4. `GenerateContent` ocurre una sola vez en la página mixta;
5. fingerprints distintos fallan antes de signature/preflight/mutación;
6. Save As del shell pasa ambos workspaces y limpia ambos baselines únicamente tras éxito;
7. fallo del writer deja ambos workspaces dirty.

## Auditoría KISS / scope

Diff neto Task 8 desde la base Task 7: exactamente siete archivos:

### Producto

1. `src/SGPdf.App/Pdf/PdfiumNative.cs`;
2. `src/SGPdf.App/Pdf/PdfEditWriter.cs`;
3. `src/SGPdf.App/MainWindow.Edit.cs`.

### Tests

4. `tests/SGPdf.App.Tests/PdfEditWriterTextFixtureFactory.cs`;
5. `tests/SGPdf.App.Tests/PdfEditWriterTextTests.cs`;
6. `tests/SGPdf.App.Tests/PdfEditWriterTextColorTests.cs`;
7. `tests/SGPdf.App.Tests/MainWindowEditCombinedSaveAsTests.cs`.

No se implementó:

- `FallbackTtf` / CID Type2;
- `FPDFText_LoadFont` de producto;
- replacement de objeto de texto;
- validator combinado de Task 10;
- panel/overlay UI de texto;
- global undo/redo de texto;
- OCR/reflow/free text;
- Task 9+.

No hubo cambios de `.csproj`, lockfiles ni dependencias.

## QA manual

**NOT RUN.** Este gate valida materialización automatizada; no se presenta como QA visual/manual de Windows.

## Gate final

CI exacto sobre el commit de este checkpoint: **PENDING**.

No iniciar Task 9 hasta que este checkpoint quede verificado y sellado `CLOSED`.
