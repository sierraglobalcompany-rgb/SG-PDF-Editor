# F7 — Task 9 CID Type2 Fallback — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 9 — writer combinado, ruta `FallbackTtf` CID Type2  
**Estado:** **CLOSED / AUTOMATED PASS**  
**Rama:** `feat/f7-text-v1`  
**Base Task 9:** `3b4592a78495e832264732aba63529398195e5c4`  
**Head funcional GREEN:** `034553cedc772cdcdf63edd54d36593e2f1d52d1`

## Alcance entregado

Task 9 materializa `FallbackTtf` dentro del `PdfEditWriter` combinado existente usando exclusivamente la ruta explícita CID Type2 que había sido demostrada por el spike de capacidad.

### Mapas CID Type2

Nuevo `CidType2FontMapBuilder`:

- une los `Rune` requeridos por todas las ediciones fallback del documento;
- elimina duplicados;
- ordena por `Rune.Value` para que el resultado no dependa del orden de selección/edición;
- asigna CIDs deterministas `1..N`;
- genera `ToUnicode` exacto en UTF-16BE;
- genera `CIDToGIDMap` big-endian;
- divide `beginbfchar` en bloques de máximo 100 entradas;
- rechaza glyph id `0` antes de abrir la ruta nativa;
- usa por defecto `FallbackFontAsset.GetGlyphId` sobre el DejaVu Sans 2.37 pinneado.

### Writer fallback

`PdfEditWriter` ahora:

- mantiene intactos los constructores de 4 y 5 parámetros usados por F6/Task 8;
- añade un constructor de 7 parámetros únicamente como seam de test para carga CID Type2 y remove;
- ya no rechaza `FallbackTtf` en el overload combinado;
- construye **un único** plan CID Type2 para la unión de todas las ediciones fallback del documento;
- carga los bytes del fallback pinneado una vez;
- llama `FPDFText_LoadCidType2Font` una sola vez por documento;
- mantiene el font handle y sus buffers vivos hasta terminar de guardar;
- conserva el gate global `PdfiumRuntime.NativeGate`;
- sigue resolviendo todos los handles de imagen + texto de una página antes de cualquier mutación;
- deja `OriginalFont` in-place sin cambios de arquitectura;
- para fallback crea un objeto real con `FPDFPageObj_CreateTextObj`, aplica `FPDFText_SetText`, color y matriz, retira el original e inserta el reemplazo en el mismo `PageObjectIndex`;
- conserva el tamaño solicitado mediante el tamaño del nuevo text object;
- mantiene `GenerateContent` una sola vez por página;
- destruye correctamente el objeto retirado y solo destruye el replacement si no llegó a quedar bajo ownership de la página;
- cierra `FPDFFont_Close` una sola vez al finalizar;
- mantiene temp -> validación existente -> publicación atómica.

### Cancelación entre remove/insert

Después de retirar exitosamente el text object original se ejecuta un gate explícito de cancelación antes de insertar el replacement.

Si se cancela en ese punto:

- se destruyen los objetos nativos que quedaron fuera de ownership de la página;
- la operación sobre el documento temporal aborta;
- no se publica destino parcial;
- el `.sgpdf.tmp` se elimina en el `finally` transaccional existente.

## Bindings nativos promovidos

Únicamente:

- `FPDFText_LoadCidType2Font`;
- `FPDFPageObj_CreateTextObj`;
- `FPDFFont_Close`.

**No se usa `FPDFText_LoadFont` para fallback.**

## RED / GREEN del map builder

### RED

Commit: `23b981c794b16b8c856319f03f9e2bd6509e1271`  
Workflow: `38081598629`  
Job: `114299470787`

Fallo esperado: el proyecto de tests no compiló porque `CidType2FontMapBuilder` todavía no existía. El fallo quedó limitado a cuatro `CS0103` de ese símbolo ausente.

### GREEN

Commit: `202c2999ed6f36eb01deb39ad1ddcfa4e3c7697b`  
Workflow: `38081680338`  
Job: `114299713956`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **730 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

## RED / GREEN del writer fallback

### Intento de harness descartado

Commit `8c4d63ea2c5b3504462086e9492fa20931f2f44d`, workflow `38081816643`, no se considera RED válido de producto: CI detectó que el test usaba por error `NativeTextSnapshot.Index` en vez de `PageObjectIndex`. Se corrigió únicamente el test antes de evaluar producción.

### RED válido

Commit: `7c429e7e0a0c32086794cc3c1772f16a2e002071`  
Workflow: `38081930381`  
Job: `114300465214`

Resultado:

- Release build: **0 warnings / 0 errors**;
- tests: **730 PASS / 3 FAIL / 733 total**;
- los tres fallos fueron exactamente por funcionalidad Task 9 ausente:
  1. el writer todavía bloqueaba `FallbackTtf`;
  2. no existía el constructor/seam de 7 parámetros para demostrar una sola carga CID Type2;
  3. no existía ese seam para cancelar determinísticamente después de `remove`.

### GREEN funcional

Bindings: `cddc720e2ded36dd4a7fa6c8014dfc2cebbecb2f`  
Writer: `034553cedc772cdcdf63edd54d36593e2f1d52d1`  
Workflow: `38082199594`  
Job: `114301244411`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **733 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

Los gates automatizados demuestran:

1. `CASA 123 -> NIÑO áé` con fallback reabre con Unicode exacto;
2. el replacement permanece en el mismo `PageObjectIndex` y conserva matriz, tamaño, fill color y render Fill;
3. el PDF resultante vuelve a renderizar a bitmap no vacío;
4. dos ediciones fallback del mismo documento comparten un solo plan/mapa y una sola carga `FPDFText_LoadCidType2Font`;
5. ambas ediciones reabren exactamente (`NIÑO` y `áé CASA`);
6. cancelación justo después de retirar el original no publica destino ni deja `.sgpdf.tmp`.

## Auditoría KISS / scope

Diff neto Task 9 desde Task 8: exactamente seis archivos.

### Producto

1. `src/SGPdf.App/Features/Edit/Text/CidType2FontMapBuilder.cs`;
2. `src/SGPdf.App/Pdf/PdfEditWriter.cs`;
3. `src/SGPdf.App/Pdf/PdfiumNative.cs`.

### Tests

4. `tests/SGPdf.App.Tests/CidType2FontMapBuilderTests.cs`;
5. `tests/SGPdf.App.Tests/PdfEditWriterTextFixtureFactory.cs`;
6. `tests/SGPdf.App.Tests/PdfEditWriterTextTests.cs`.

No hubo cambios de:

- `.csproj`;
- lockfiles;
- dependencias/NuGet;
- segundo motor PDF;
- red/cloud;
- framework genérico de writers/objetos;
- UI de texto;
- validator combinado de Task 10;
- OCR/reflow/free-text;
- Task 10+.

## QA manual

**NOT RUN.** Task 9 queda validada por gates automatizados; no se presenta como prueba visual/manual de Windows.

## Gate final

Checkpoint commit: `1d821000f82039eb0170bd3b95becfaf27d6c51e`  
Workflow exacto: `38082326517`  
Job: `114301621424`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **733 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

Task 9 queda **CLOSED / AUTOMATED PASS**.  
No iniciar Task 10 hasta una nueva autorización del usuario.
