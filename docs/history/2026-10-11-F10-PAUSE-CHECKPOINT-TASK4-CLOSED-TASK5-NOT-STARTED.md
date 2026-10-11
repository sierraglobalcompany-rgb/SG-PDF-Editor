# F10 — Pause checkpoint after Task 4

Fecha: 2026-10-11  
Rama: `feat/f10-local-ocr`  
Estado al pausar: **Task 4 cerrado; Task 5 no iniciado en código**

## Punto exacto de recuperación

Último HEAD funcional verificado antes de este checkpoint:

`b94c9bc4d394075aab1ff2001811d478f149d9a5`

Commit:

`docs(f10): checkpoint OCR raster TSV geometry`

Padre funcional de Task 4:

`98da53ab504066475108b64046456277728209de`

## Verificación exact-head de Task 4

Workflow Windows:

`38162740522`

Job:

`114538878464`

Resultado:

- repository hygiene: PASS;
- Labelize 1.7.0 staging: PASS;
- qpdf 12.4.2 staging: PASS;
- Tesseract 5.5.3 staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **992 PASS / 0 FAIL / 0 SKIP**;
- duración de tests: 32 s.

El único ruido del workflow fue la advertencia deprecatoria Node.js/punycode de GitHub Actions; no es una advertencia C# del producto.

## Qué quedó cerrado

### Task 1

Gate PDFium de texto invisible buscable: cerrado previamente con GO.

### Task 2

Runtime Tesseract local/pinneado y staging Windows: cerrado previamente con GO.

### Task 3

Modelos OCR, elegibilidad de páginas y adaptador de proceso Tesseract: cerrado previamente.

### Task 4

Cerrado y documentado en:

`docs/history/2026-10-11-F10-TASK4-RASTER-TSV-GEOMETRY-CHECKPOINT.md`

Incluye:

1. raster OCR fijo a 300 DPI;
2. límites de 20.000 px por eje y 100.000.000 píxeles totales antes del render;
3. parser TSV UTF-8 estricto;
4. filas nivel 5 únicamente como palabras OCR;
5. Unicode español preservado;
6. confianza `0` aceptada; sin confidence cutoff en F10 V1;
7. geometría OCR validada;
8. mapeo affine píxeles → PDF mediante `PdfRenderedPage.DeviceTransform`;
9. pruebas de retrato, paisaje y equivalentes 90/180/270°;
10. suite completa 992/992 GREEN en el HEAD funcional.

## Task 5 — estado al pausar

**NO iniciado en código ni tests comprometidos.**

El siguiente trabajo previsto es exactamente:

`Task 5: Transactional searchable-layer writer and semantic validator`

Primer subbloque:

`OcrTextLayerPlanner` RED → GREEN.

Contrato ya fijado por el plan:

- `OcrTextLayerPlanner.Build(IReadOnlyList<OcrWord> words, PdfRenderedPage renderedPage)`;
- agrupar por TSV `(block, paragraph, line)` en orden de primera aparición;
- unir palabras de la misma línea con un único espacio;
- preservar Unicode;
- preservar palabras no vacías incluso con confianza `0`;
- conservar geometría por palabra para validación;
- producir `OcrTextLinePlacement` con page index, texto Unicode, bounds PDF y metadata de orden;
- usar `OcrCoordinateMapper` ya probado; no duplicar lógica de rotación;
- probar bounds rotados dentro de la geometría PDF;
- todavía no implementar párrafos, columnas, reflow ni heurísticas F11.

Después del planner, y sólo después de su GREEN, el plan continúa con:

1. writer transaccional `PdfOcrWriter`;
2. validador semántico `PdfOcrOutputValidator`;
3. prueba end-to-end de capa OCR invisible buscable;
4. suite completa + exact-head Windows CI;
5. checkpoint Task 5.

## Importante sobre el intento inmediatamente anterior a la pausa

Se alcanzó a preparar conceptualmente el RED del planner, pero **no se creó ni comprometió ningún archivo Task 5** antes de esta pausa. La rama seguía en `b94c9bc4...` al recibir la orden de crear checkpoint y parar.

Por tanto, al reanudar no hay que limpiar cambios parciales ni recuperar un commit intermedio de Task 5.

## Guardrails al reanudar

- no merge a `main`;
- `main` permanece congelado;
- continuar en `feat/f10-local-ocr`;
- verificar HEAD antes de escribir;
- comenzar Task 5 con RED limpio del planner;
- TDD estricto: RED → mínimo GREEN → suite completa → exact-head Windows CI → checkpoint;
- no tocar writer mientras el planner no esté GREEN;
- no usar Tesseract PDF como salida final;
- PDFium sigue siendo propietario del PDF;
- Save As transaccional y validación semántica permanecen obligatorios;
- QA manual Windows/Adobe/Foxit/Edge/corpus privado sigue **NOT RUN** hasta ejecución real.

## Comando mental de reanudación

Al recibir `continua`:

1. verificar rama/HEAD;
2. leer este checkpoint y el plan `docs/superpowers/plans/2026-10-11-f10-local-ocr.md`;
3. confirmar que no apareció un commit Task 5 inesperado;
4. crear únicamente `tests/SGPdf.App.Tests/OcrTextLayerPlannerTests.cs` como RED;
5. ejecutar CI exacto y demostrar que falla por ausencia del planner;
6. implementar GREEN mínimo del planner;
7. no avanzar al writer hasta cerrar ese subbloque.

## Estado final de esta pausa

**STOP intencional después de crear este checkpoint. No continuar Task 5 en este turno.**
