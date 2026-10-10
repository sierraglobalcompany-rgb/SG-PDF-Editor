# F6 Task 9 — Checkpoint

Fecha: 2026-10-10
Rama: `feat/f6-images`
Task: **F6.7 — hardening, regressions, offline and temp safety**
Estado: **CLOSED / GREEN automatizado**

## Base

Task 9 comenzó desde el cierre exacto de Task 8:

- base: `c15bc3b0cf64cc23f3ae79123da0acc99cdcbb94`
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`
- no PR de F6
- no merge

## RED

Commit:

- `22e9bf8323362a25c78fdcfce9468cab103e249c`
- `test(images): freeze F6 Task 9 lifecycle and bounds hardening`

CI:

- run `38056810629`
- Release build: **0 warnings / 0 errors**
- tests: **673 PASS / 5 FAIL / 0 skipped**

Las 5 fallas eran las esperadas y congelaron huecos reales:

1. Ctrl+O podía ejecutar la acción de apertura antes de resolver cambios dirty de EDITAR.
2. Abrir ZPL podía llegar al flujo de apertura antes de resolver cambios dirty de EDITAR.
3. No existía un guard explícito de memoria/formato/stride antes de copiar bitmaps de PDFium.

En el mismo RED quedaron verdes dos caracterizaciones del writer ya existente:

- cancelación después de validar conserva el destino previo y limpia el temporal;
- fallo de publicación limpia el temporal y no reemplaza silenciosamente el destino.

No fue necesario modificar `PdfImageEditWriter` para esos casos.

## GREEN funcional

Commit:

- `77477f66adcec97b38953133b2143cd90daf1dcb`
- `fix(images): harden open guards and bitmap copy bounds`

CI:

- run `38057132921`
- Release build: **0 warnings / 0 errors**
- tests: **678 PASS / 0 FAIL / 0 skipped**

### Lifecycle / dirty guards

- Ctrl+O ahora ejecuta `TryLeaveImageEditModeWithGuard()` antes de invocar la acción de abrir PDF.
- Los menús `Abrir PDF` y `Abrir ZPL` reciben un guard de clase previo a sus handlers normales.
- Si el usuario rechaza descartar cambios dirty:
  - no se abre el selector;
  - no se reemplaza el documento/workspace;
  - EDITAR y su historial permanecen intactos.
- El guard existente de cierre de ventana y cambio de modo sigue cubierto.

### Bitmap / memoria

`PdfDocumentSession.Images` ahora valida antes de reservar memoria:

- formato PDFium soportado;
- stride mínimo coherente con Gray/BGR/BGRx/BGRA;
- cálculos de tamaño con `long`;
- límites de `int` requeridos por los buffers gestionados;
- presupuesto combinado de copia gestionada de **256 MiB** para buffer fuente + salida BGRA.

Una imagen que exceda el presupuesto se rechaza con `NotSupportedException` antes de crear buffers gigantes.

## 9.4 — performance y ownership

Commit:

- `7f70a68b2148504888040483a4d9a9e9dd6b4a03`
- `test(images): lock active-page discovery and managed ownership`

CI:

- run `38057258528`
- Release build: **0 warnings / 0 errors**
- tests: **680 PASS / 0 FAIL / 0 skipped**

Pruebas añadidas:

1. **Active-page-only discovery**
   - PDF sintético de 2 páginas;
   - al entrar en EDITAR, `_getImageObjects` se consulta exactamente una vez para la página activa (`0`);
   - no hay enumeración eager de imágenes de todo el documento al entrar al modo.

2. **Managed ownership**
   - reflexión sobre `ImageEditWorkspace`, `ImageEditState`, `ImageEditMutation`, `ImageObjectRef`, `ImageObjectKey`, `ImageReplacementAsset` y snapshots PDF relacionados;
   - ningún estado persistente contiene campos/propiedades `IntPtr` o `UIntPtr`.

La prueba existente `SaveAs_ReplacementAfterSourceAssetDeleted_UsesCapturedBytes` sigue demostrando que el reemplazo conserva bytes gestionados capturados y no depende del archivo externo después de cargarlo.

## Regresiones y offline

La suite completa sigue ejecutando conjuntamente los tests existentes de:

- LEER;
- FIRMAR;
- ORGANIZAR;
- ZPL;
- runtime/offline;
- EDITAR;
- writer/Save As/preservación.

No se añadieron duplicados cuando ya existía cobertura equivalente.

## Auditoría de alcance Task 9

Comparación `c15bc3b...` → `7f70a68...`:

- 3 commits ahead / 0 behind;
- producción modificada solamente:
  - `src/SGPdf.App/MainWindow.EditImages.Hardening.cs`
  - `src/SGPdf.App/MainWindow.ReaderShortcuts.cs`
  - `src/SGPdf.App/Pdf/PdfDocumentSession.Images.cs`
- tests añadidos/modificados solamente:
  - `tests/SGPdf.App.Tests/ImageEditPerformanceOwnershipTests.cs`
  - `tests/SGPdf.App.Tests/MainWindowEditImageHardeningTests.cs`
  - `tests/SGPdf.App.Tests/PdfImageBitmapBoundsTests.cs`
  - `tests/SGPdf.App.Tests/PdfImageEditWriterHardeningTests.cs`
- sin paquetes nuevos;
- sin cambios de lockfiles;
- sin cambios de runtime/cloud/network;
- sin cambios funcionales al writer F6;
- sin cambios ajenos a F6 Task 9.

## Estado de QA

- QA automatizado Windows: **PASS**
- QA manual Windows/UX: **NOT RUN**
- corpus privado: **NOT RUN** salvo que se ejecute explícitamente fuera de CI

No interpretar la suite automatizada como sustituto de QA manual/físico.

## Gobernanza

Al cierre funcional de Task 9:

- `feat/f6-images` = `7f70a68b2148504888040483a4d9a9e9dd6b4a03`
- `main` sigue en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`
- PR F6: **NO EXISTE**
- merge: **NO REALIZADO**

## Siguiente paso exacto

**Task 10 NO INICIADO.**

NEXT_EXACT_TASK:

> **Task 10 — F6 closure: documentación, auditoría final y stacked draft PR `feat/f5-organize` → `feat/f6-images`.**

No hacer merge a `main` sin aprobación explícita.
