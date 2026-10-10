# F7 Task 1 — Checkpoint intermedio durable

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 1 — Gate F7.1 capacidad exacta del PDFium pinneado  
**Estado:** EN PROGRESO — exports GREEN; probes funcionales todavía NO iniciados  

## Base y ramas

- Base F6 cerrada: `c6d762efca01d50bfe3932d1f05617190a464fc6`
- Spec + plan aprobados: `design/f7-text-v1` @ `471bca74a84b2153c42aa87e6480d201ddf61472`
- Rama de ejecución: `feat/f7-text-v1`
- Head funcional antes de este checkpoint: `11b937495128fba43176b632a472a23e58723584`

## Trabajo realizado

Se añadió únicamente:

- `tests/SGPdf.App.Tests/PdfiumTextEditApiAvailabilityTests.cs`

No se ha añadido todavía código de producción F7, P/Invoke nuevo, fuente redistribuible, UI, writer de texto ni cambios de arquitectura.

## Gate de exports — resultado

Commit: `11b937495128fba43176b632a472a23e58723584`  
CI Windows exact-head: run `38067838439`  
Resultado: **SUCCESS**

Evidencia del job:

- checkout: PASS
- repository hygiene: PASS
- Labelize staging: PASS
- restore locked dependencies: PASS
- Release build: PASS
- build warnings: **0**
- build errors: **0**
- suite completa: **682 passed / 0 failed / 0 skipped**

El test exige que el `pdfium.dll` pinneado de `bblanchon.PDFium.Win32 156.0.8076` exporte las APIs críticas F7. Ese contrato pasó, por lo que quedaron confirmadas como disponibles en el binario pinneado las rutas críticas exigidas por la prueba, incluyendo:

- page-object enumeration/type/bounds/matrix/rotated bounds;
- text object text/font-size/font handle/base-font-name/render mode;
- fill color;
- `FPDFText_SetText`;
- `FPDFText_LoadFont`;
- `FPDFFont_Close`;
- `FPDFPageObj_CreateTextObj`;
- page-object matrix/fill setters;
- remove/insert-at-index;
- `FPDFPage_GenerateContent`;
- `FPDF_SaveAsCopy`.

El test también registra como capabilities opcionales:

- `FPDFTextObj_SetFontSize`
- `FPDFFont_GetFamilyName`

Su presencia exacta debe registrarse en el checkpoint final de Task 1 después del probe funcional; el job global no imprime el `ITestOutputHelper` en el log de éxito.

## Ruling importante del plan

Task 1 necesita demostrar funcionalmente `FPDFText_LoadFont`, pero Task 2 es el Gate legal/provenance que autoriza incorporar la TTF redistribuible real al producto.

**Ruling:** para Task 1 se puede usar una fuente de Windows únicamente como fixture efímero del runner/CI para caracterizar la API nativa. Esa fuente:

- NO se añade al repositorio;
- NO se convierte en dependencia del producto;
- NO sustituye Task 2;
- NO autoriza usar fuentes del sistema como fallback runtime.

Task 2 seguirá siendo el único Gate que puede autorizar una TTF pinneada y redistribuible para producto.

## Próximo paso exacto

Continuar dentro de **Task 1 solamente** con:

1. crear `tests/SGPdf.App.Tests/TextEditNativeCharacterizationHarness.cs`;
2. crear `tests/SGPdf.App.Tests/TextEditNativeCharacterizationTests.cs`;
3. probar funcionalmente:
   - edición in-place (`FPDFText_SetText` → `GenerateContent` → save → reopen → texto exacto);
   - carga de TTF efímera + creación de text object + inserción controlada + save/reopen/render;
   - repetición del probe para demostrar cleanup/ownership sin handles persistentes;
4. ejecutar CI Windows exact-head;
5. si cualquier probe crítico falla: **STOP F7 y revisar spec; no avanzar a Task 2**;
6. si pasa: crear el checkpoint final de Task 1 con exports/capabilities exactas observadas.

## No hacer todavía

- NO iniciar Task 2.
- NO añadir DejaVu Sans ni otra TTF al repo.
- NO modificar `third_party/manifest.json` todavía.
- NO crear `PdfTextObjectInfo`, workspace, hit-test o UI.
- NO modificar `PdfImageEditWriter`/`PdfEditWriter` todavía.
- NO abrir PR F7 todavía.
- NO mergear PR #25/F6 ni `main`.

## Gobernanza

- F7 está apilada sobre el head F6 cerrado.
- `main` no debe modificarse.
- PR #25/F6 permanece draft/open/unmerged.
- QA manual Windows F7 todavía no aplica; implementación funcional no ha comenzado.

Este checkpoint existe para permitir recuperación exacta del estado sin repetir Task 1 ni reinterpretar decisiones ya tomadas.
