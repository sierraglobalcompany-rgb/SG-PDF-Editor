# F7 — Task 5 Text Object Discovery — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 5 — modelo managed + discovery de objetos de texto reales  
**Estado:** **CLOSED / AUTOMATED PASS**  
**Rama:** `feat/f7-text-v1`  
**Base Task 5:** `62163067e5c2d744e76067dc196e38bec5b03414`  
**RED:** `3f8ad0c33a220a24145fb2afeccbce1ed81cf04d`  
**Head funcional:** `f7dba51b6db1dc448efc4633c99d2be327f19141`  
**Checkpoint pre-seal:** `dacbd51bb4a68a459cc199f2aae4b06b336c45c9`

## Alcance entregado

Task 5 introduce únicamente discovery real de objetos de texto top-level de la página solicitada y un snapshot completamente managed.

Modelo managed:

- `TextObjectKey(PageIndex, PageObjectIndex)`;
- texto Unicode;
- matriz PDF;
- bounds;
- quad rotado;
- nombre base de fuente;
- tamaño de fuente;
- fill RGBA;
- `TextRenderMode` exacto.

Discovery:

- `PdfDocumentSession.GetTextObjects(pageIndex, cancellationToken)`;
- usa la misma serialización `PdfiumRuntime.NativeGate` existente;
- abre únicamente la página solicitada;
- carga una sola text page para esa página;
- enumera `FPDFPage_CountObjects` / `FPDFPage_GetObject` top-level;
- conserva únicamente `FPDF_PAGEOBJ_TEXT`;
- cierra text page y page antes de liberar el gate;
- no persiste `IntPtr` ni handles nativos en estado managed.

## RED

Commit: `3f8ad0c33a220a24145fb2afeccbce1ed81cf04d`  
Workflow: `38073976113`

Resultado:

- Release build: **0 warnings / 0 errors**;
- tests: **693 PASS / 6 FAIL / 699 total**;
- los 6 fallos pertenecían exclusivamente a `PdfTextObjectDiscoveryTests`;
- causa esperada: `GetTextObjects` todavía no existía.

Cobertura RED:

1. Unicode exacto `NIÑO áé` + snapshot managed;
2. objetos contiguos `UNO` / `DOS` permanecen separados;
3. texto dentro de Form XObject no se descubre en V1;
4. render mode no estándar se conserva para política read-only posterior;
5. solo se lee la página solicitada;
6. cancelación pre-cancelled se honra.

## GREEN inicial y diagnóstico

El primer GREEN funcional compiló, pero expuso un error real de interoperabilidad en la lectura de texto.

Workflow exacto: `38074378901` sobre `be157107e4bc2c44df73bc6ce104abbb3d772e33`:

- Release build: **0 warnings / 0 errors**;
- tests: **695 PASS / 4 FAIL / 699 total**;
- Form XObject exclusion y cancelación ya pasaban;
- los 4 fallos restantes mostraban el texto Unicode correcto seguido de NUL/basura.

Causa raíz:

`FPDFTextObj_GetText` expresa tanto `length` como su retorno en **bytes UTF-16LE**, incluyendo el NUL final. La primera implementación interpretó ese conteo como número de `ushort`, duplicando la porción leída del buffer.

Corrección mínima: `f7dba51b6db1dc448efc4633c99d2be327f19141`.

- asigna exactamente `requiredBytes`;
- valida longitud par;
- convierte `copiedBytes / sizeof(ushort)` a cantidad de UTF-16 code units;
- elimina únicamente NUL terminal;
- no modifica discovery, modelo, selección, UI ni persistencia.

## GREEN funcional verificado

Commit funcional: `f7dba51b6db1dc448efc4633c99d2be327f19141`  
Workflow: `38074498789`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **699 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

Quedan probados en fixtures sintéticos:

- Unicode exacto;
- objetos top-level contiguos permanecen independientes;
- texto anidado en Form XObject queda fuera de V1;
- `TextRenderMode` no estándar se conserva sin convertirlo todavía en editable/no editable;
- discovery por página solicitada;
- cancelación.

## Auditoría KISS / scope

Diff neto desde RED `3f8ad0c...` hasta el head funcional `f7dba51...`:

1. `src/SGPdf.App/Features/Edit/Text/PdfTextObjectInfo.cs`;
2. `src/SGPdf.App/Pdf/PdfDocumentSession.TextObjects.cs`;
3. `src/SGPdf.App/Pdf/PdfiumNative.cs`.

Diff neto desde la base Task 4 `6216306...` añade además únicamente:

4. `tests/SGPdf.App.Tests/PdfTextObjectDiscoveryTests.cs`.

No permanecen workflows temporales ni infraestructura auxiliar en el diff final.

No se implementó en Task 5:

- hit testing;
- overlays/UI de texto;
- workspace editable;
- política `CanEdit`/read-only;
- edición de contenido/tamaño/color;
- fallback de fuente;
- writer/materialización de texto;
- Save As de texto;
- Task 6+.

La política de render modes soportados/read-only queda deliberadamente para Task 7, usando el `TextRenderMode` preservado por este snapshot.

## Incidencia de infraestructura durante implementación

Se intentaron dos workflows temporales exclusivamente para aplicar un patch mediante GitHub Actions. Fallaron antes de producir código de producto:

- uno por mismatch LF/CRLF del anchor;
- otro por quoting de PowerShell.

La vía auxiliar se abandonó inmediatamente y ambos workflows se eliminaron. La auditoría del diff neto confirma que no dejaron archivos ni arquitectura residual.

## Gate del checkpoint

Checkpoint pre-seal: `dacbd51bb4a68a459cc199f2aae4b06b336c45c9`  
Workflow: `38074633394`

Attempt 1:

- build: **0 warnings / 0 errors**;
- tests: **697 PASS / 2 FAIL / 699 total**;
- los únicos fallos fueron dos probes heredados `PdfiumOrganizeApiAvailabilityTests` por no poder cargar `pdfium.dll`;
- ningún test Task 5 falló.

Se repitió el **mismo SHA sin cambiar código**.

Attempt 2:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **699 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

La diferencia entre attempts confirma el flake heredado de carga del DLL; no se modificó código de producto para ocultarlo.

## QA manual

**NOT RUN.** Task 5 es infraestructura de discovery sin UI nueva. El PASS automatizado no se presenta como validación manual Windows.

## Cierre

Task 5 queda cerrada con RED real, diagnóstico del fallo funcional, GREEN exacto, auditoría de scope y CI limpio sobre el checkpoint.

No se inició Task 6.
