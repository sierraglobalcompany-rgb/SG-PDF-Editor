# F8 — Task 1 Capability Gate — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F8 — COMENTAR V1  
**Task:** 1 — gate exacto PDFium para anotaciones  
**Rama:** `feat/f8-comments-v1`  
**Base F7:** `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`  
**Plan F8:** `d52a191d0893d779659f0560bee61eed973d3e92`  
**Estado:** funcionalmente GREEN; este checkpoint requiere su propio CI exact-head antes de declarar Task 1 cerrada.

## Objetivo

Demostrar contra el `pdfium.dll` exacto pinneado (`bblanchon.PDFium.Win32 156.0.8076`) que F8 puede usar anotaciones PDF nativas reales antes de crear `CommentWorkspace`, writer, validator o UI.

El gate no confía únicamente en el header actual de PDFium. Primero verifica exports del DLL pinneado y después ejecuta un round-trip real:

`crear -> guardar -> reabrir -> leer -> render smoke`.

## RED válido

Commit RED:

`82e3e23973c79c01f005b83677e673bfbfeb2f36`

Windows CI:

- workflow: `38091965707`
- job: `114330051794`
- checkout exacto: `82e3e23973c79c01f005b83677e673bfbfeb2f36`
- hygiene: PASS
- Labelize staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **767 PASS / 2 FAIL / 0 skipped / 769 total**

Los dos fallos fueron exactamente los esperados:

1. `ProductionInterop_DeclaresF8CriticalCommentFunctions`
2. `ProductionInterop_DeclaresExactF8SubtypeConstants`

La prueba `PinnedPdfium_ExportsAllF8CriticalCommentFunctions` pasó en ese mismo RED. Por tanto, el problema demostrado no era ausencia de capacidad en el DLL: el runtime pinneado sí exportaba toda la superficie crítica y el hueco estaba en `PdfiumNative`.

## GREEN-A — interop mínimo

Se mantuvo `PdfiumNative` como autoridad, pero se dividió físicamente el interop para no seguir engordando el archivo general:

- `PdfiumNative.cs` pasó a `partial` sin cambio funcional adicional;
- nuevo `PdfiumNative.Comments.cs` contiene únicamente constantes/structs/DllImports de anotaciones.

No se creó todavía ningún dominio, workspace, writer, validator o UI F8.

Subtipos exactos expuestos y comprobados:

- `TEXT = 1`
- `SQUARE = 5`
- `CIRCLE = 6`
- `HIGHLIGHT = 9`
- `UNDERLINE = 10`
- `STRIKEOUT = 12`
- `INK = 15`

Superficie crítica comprobada en el DLL y declarada en interop:

- lifecycle: `FPDFAnnot_IsSupportedSubtype`, `FPDFPage_CreateAnnot`, `FPDFPage_GetAnnotCount`, `FPDFPage_GetAnnot`, `FPDFPage_GetAnnotIndex`, `FPDFPage_CloseAnnot`, `FPDFPage_RemoveAnnot`, `FPDFAnnot_GetSubtype`;
- geometry/color: `FPDFAnnot_SetRect`, `FPDFAnnot_GetRect`, `FPDFAnnot_SetColor`, `FPDFAnnot_GetColor`;
- markup: `FPDFAnnot_AppendAttachmentPoints`, `FPDFAnnot_CountAttachmentPoints`, `FPDFAnnot_GetAttachmentPoints`;
- strings: `FPDFAnnot_SetStringValue`, `FPDFAnnot_GetStringValue`;
- ink: `FPDFAnnot_AddInkStroke`, `FPDFAnnot_GetInkListCount`, `FPDFAnnot_GetInkListPath`, `FPDFAnnot_RemoveInkList`;
- shapes: `FPDFAnnot_SetBorder`, `FPDFAnnot_GetBorder`;
- publication primitive existente: `FPDF_SaveAsCopy`.

## GREEN-B — caracterización nativa real

Head funcional caracterizado:

`849b21808982f129d979f55f3006d66863b24c2f`

Windows CI:

- workflow: `38092310142`
- job: `114331072530`
- checkout exacto: `849b21808982f129d979f55f3006d66863b24c2f`
- hygiene: PASS
- Labelize staging: PASS
- locked restore: PASS
- Release build: **0 warnings / 0 errors**
- tests: **771 PASS / 0 FAIL / 0 skipped / 771 total**
- duration suite: 17 s
- workflow/job: SUCCESS

### Resultados del round-trip

Los siete subtipos requeridos reportan `FPDFAnnot_IsSupportedSubtype != 0` y pasan create/save/reopen/read:

1. `HIGHLIGHT`
   - rect válido;
   - quadpoints round-trip;
   - RGB + alpha round-trip.
2. `UNDERLINE`
   - rect válido;
   - quadpoints round-trip;
   - RGB + alpha round-trip.
3. `STRIKEOUT`
   - rect válido;
   - quadpoints round-trip;
   - RGB + alpha round-trip.
4. `TEXT`
   - rect round-trip;
   - color round-trip;
   - `Contents` Unicode exacto: `Nota NIÑO áé`.
5. `INK`
   - rect/color/alpha round-trip;
   - un stroke de tres puntos vuelve a abrirse con misma cantidad y coordenadas.
6. `SQUARE`
   - rect/color round-trip;
   - borde `2.5 pt` round-trip.
7. `CIRCLE`
   - rect/color round-trip;
   - borde `2.5 pt` round-trip.

Después de reabrir el PDF materializado, `PdfDocumentSession.RenderPage()` produce un bitmap válido. Task 1 no afirma todavía QA visual comercial ni apariencia final de anotaciones; solo prueba integridad nativa y render smoke.

## Propiedades promovidas / no promovidas

**Demostradas y aptas para diseño V1:**

- creación de los siete subtipos;
- rect;
- color RGB;
- alpha a través de `FPDFAnnot_SetColor/GetColor` para anotaciones nuevas sin appearance stream;
- quadpoints de text markup;
- `Contents` Unicode de Nota;
- InkList/stroke points;
- border width de Square/Circle.

**No promovidas todavía como contrato V1:**

- `InteriorColor`/relleno de Square/Circle;
- edición de color cuando una anotación ya trae appearance stream externo;
- generación/personalización manual de AP;
- presión variable de Ink;
- comportamiento visual en lector externo comercial.

Estas propiedades quedan fuera hasta pruebas específicas posteriores. En particular, PDFium documenta que `SetColor/GetColor` puede fallar en anotaciones con appearance stream; el writer posterior deberá fallar cerrado o usar únicamente rutas demostradas, nunca asumir que toda anotación existente es modificable por esa API.

## Auditoría de alcance

Compare desde el commit de plan `d52a191d0893d779659f0560bee61eed973d3e92` hasta el head funcional `849b21808982f129d979f55f3006d66863b24c2f`:

- status: ahead;
- 5 commits;
- 0 behind;
- merge base exacta = commit de plan.

Archivos tocados únicamente:

1. `src/SGPdf.App/Pdf/PdfiumNative.cs`
2. `src/SGPdf.App/Pdf/PdfiumNative.Comments.cs`
3. `tests/SGPdf.App.Tests/PdfiumCommentApiAvailabilityTests.cs`
4. `tests/SGPdf.App.Tests/CommentNativeCharacterizationHarness.cs`
5. `tests/SGPdf.App.Tests/CommentNativeCharacterizationTests.cs`

No hubo cambios de:

- `.csproj`;
- lockfiles;
- paquetes/NuGet;
- segundo motor PDF;
- red/network/cloud;
- `CommentWorkspace`;
- `PdfCommentWriter`;
- `PdfCommentOutputValidator`;
- MainWindow/UI;
- F9+;
- `main`.

## Estado de integración

- `main` verificado aún en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
- no merge;
- no PR F8 todavía;
- Task 2 no iniciada.

## QA manual

**NOT RUN.**

Task 1 es un gate automatizado de capacidad/round-trip nativo. No equivale a QA manual de Windows, stylus/touch ni verificación en Acrobat/Foxit/PDF-XChange.

## Siguiente gate

Cuando este checkpoint obtenga Windows CI exact-head GREEN, Task 1 queda **CLOSED / AUTOMATED PASS** y el siguiente trabajo permitido es **Task 2 — managed comment domain + workspace/history**.