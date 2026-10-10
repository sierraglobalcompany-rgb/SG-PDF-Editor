# F7 — Task 1 Gate PDFium — CHECKPOINT BLOCKED

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 1 — Gate F7.1, capacidad exacta del PDFium pinneado  
**Estado:** **BLOCKED / STOP por capacidad de fallback de fuente**  
**Rama:** `feat/f7-text-v1`  
**Head funcional caracterizado:** `6181e1722dd8f9d758730be5b183865beafbc008`  
**Base aprobada de ejecución:** `471bca74a84b2153c42aa87e6480d201ddf61472`  
**Base F6:** `c6d762efca01d50bfe3932d1f05617190a464fc6`

---

## 1. Regla de gate

La spec aprobada `docs/superpowers/specs/2026-10-10-f7-text-v1-design.md` exige probar el `pdfium.dll` exacto distribuido por `bblanchon.PDFium.Win32 156.0.8076` antes de implementar F7.

Si una API crítica o el probe funcional de edición/fallback no cumple el contrato requerido, F7 debe detenerse y revisar la spec antes de diseñar otro fallback. No se autoriza introducir automáticamente un segundo motor PDF ni relajar Unicode exacto.

Ese STOP se activó en este Task.

---

## 2. Exports críticos — PASS

`PdfiumTextEditApiAvailabilityTests` verificó que el runtime pinneado exporta las APIs críticas requeridas para el spike de F7, entre ellas:

- enumeración y tipo de objetos de página;
- bounds, rotated bounds y matrix;
- `FPDFTextObj_GetText`;
- `FPDFTextObj_GetFontSize`;
- `FPDFTextObj_GetFont`;
- `FPDFFont_GetBaseFontName`;
- `FPDFTextObj_GetTextRenderMode`;
- fill color;
- `FPDFText_SetText`;
- `FPDFText_LoadFont`;
- `FPDFFont_Close`;
- `FPDFPageObj_CreateTextObj`;
- remove / insert-at-index;
- `FPDFPage_GenerateContent`;
- `FPDF_SaveAsCopy`.

La ruta comprobada para nombre de fuente es:

`FPDFTextObj_GetFont` → `FPDFFont_GetBaseFontName`.

El setter de tamaño continúa siendo capability opcional; no fue necesario promoverlo para decidir este gate porque el bloqueo aparece antes, en la fidelidad Unicode del fallback.

---

## 3. Ruta fuente original — PASS

Fixture sintético top-level Helvetica:

`CASA 123` → `CASA 321`

Resultado real con `FPDFText_SetText`:

- Unicode exacto después de save + reopen: PASS;
- mismo `PageObjectIndex`: PASS;
- font size preservado (~18 pt): PASS;
- font name preservado: PASS;
- fill color preservado: PASS;
- matrix preservada dentro de tolerancia: PASS;
- render reabierto válido: PASS.

Conclusión: la edición conservadora in-place con fuente original está soportada por el runtime pinneado para el caso probado.

---

## 4. Fallback TTF `cid=1` — RECHAZADO

Se cargó una TTF de Windows **solo como fixture efímero de CI** para caracterizar `FPDFText_LoadFont`; esto no crea dependencia de producto ni sustituye el Gate legal/provenance de Task 2.

Entrada solicitada:

`NIÑO 1`

Texto extraído después de crear objeto → insertar mismo índice → GenerateContent → save → reopen:

`NIÑO\u00A01`

Es decir, U+0020 SPACE se materializa/reextrae como U+00A0 NO-BREAK SPACE.

La ruta preserva visualmente el texto, pero **no cumple el contrato de Unicode exacto** de F7 y queda explícitamente rechazada para el fallback V1 aprobado.

---

## 5. Fallback TTF `cid=0` — RECHAZADO

Se probó también TrueType simple (`cid=0`) para el alcance inicial Latin/Spanish.

Entrada solicitada:

`NIÑO áé`

Resultado observado al reabrir y extraer:

`NIÿO ÿÿ`

Los caracteres no ASCII no sobreviven semánticamente de forma exacta.

La ruta puede guardar, reabrir, renderizar y conservar índice/matriz/color, pero **no cumple Unicode exacto para español** y queda rechazada.

---

## 6. Ownership / cleanup — PASS para la ruta técnica

El probe simple-TTF se repitió 5 veces con contenido ASCII (`NINO 0..4`). Cada output pudo:

- guardarse;
- reabrirse;
- extraerse correctamente en ASCII;
- cerrarse;
- borrarse inmediatamente después.

Esto da evidencia de que font/page/document handles del harness no quedan retenidos entre iteraciones.

El bloqueo no es ownership ni publicación; es fidelidad semántica Unicode del fallback.

---

## 7. CI exacto de caracterización

Run: `38068782545`  
SHA: `6181e1722dd8f9d758730be5b183865beafbc008`

Resultado:

- repository hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: PASS;
- warnings: **0**;
- errors: **0**;
- tests: **686 passed / 0 failed / 0 skipped**.

Los tests verdes conservan tanto capacidades positivas como las dos caracterizaciones negativas del fallback. No se convirtió una diferencia Unicode en una equivalencia falsa.

---

## 8. Archivos añadidos por Task 1

Test-only / evidencia:

- `tests/SGPdf.App.Tests/PdfiumTextEditApiAvailabilityTests.cs`
- `tests/SGPdf.App.Tests/TextEditNativeCharacterizationHarness.cs`
- `tests/SGPdf.App.Tests/TextEditNativeCharacterizationTests.cs`
- `tests/SGPdf.App.Tests/TextEditSimpleTtfCharacterization.cs`

Documentación:

- `docs/history/2026-10-10-F7-TASK1-INPROGRESS-CHECKPOINT.md`
- este checkpoint.

**No hay código de producto F7.**

---

## 9. Gobernanza / lo que NO debe hacerse

Mientras este checkpoint siga vigente:

1. **NO iniciar Task 2 / incorporar DejaVu Sans.**
2. NO añadir un segundo motor PDF.
3. NO normalizar NBSP→SPACE silenciosamente para fingir Unicode exacto.
4. NO aceptar corrupción de `ñ/á/é` como limitación silenciosa.
5. NO implementar UI/workspace/writer F7 sobre una estrategia de fallback no demostrada.
6. NO mergear PR #25/F6.
7. NO mergear a `main`.
8. Manual Windows QA sigue `NOT RUN`.

---

## 10. Próximo paso permitido

**Revisar la spec/diseño de F7 antes de continuar la implementación.**

La revisión debe elegir y demostrar una estrategia explícita, por ejemplo:

- investigar otra ruta PDFium de font embedding/text object que produzca ToUnicode/Unicode exacto;
- o reducir Texto V1 a ediciones que puedan permanecer en fuente original y diferir nuevos code points/fallback a una fase posterior;
- o aprobar otra estrategia solo después de un spike técnico separado.

Ninguna de esas rutas está aprobada o implementada por este checkpoint.

**Task 1 no es GREEN de producto: es un Gate correctamente ejecutado que terminó en BLOCKED con evidencia reproducible.**
