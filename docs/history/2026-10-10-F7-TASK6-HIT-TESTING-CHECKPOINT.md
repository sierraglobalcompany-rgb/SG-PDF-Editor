# F7 — Task 6 Mixed Text/Image Hit Testing — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 6 — hit-testing texto + arbitraje imagen/texto  
**Estado:** **CLOSED / AUTOMATED PASS**  
**Rama:** `feat/f7-text-v1`  
**Base Task 6:** `6d6800fe15fa2435c54275a7180992fb119e0c34`  
**RED:** `d8cfcc215e4caeb63684349ff358876ec816de95`  
**Head funcional:** `3cea025d1e3ca200a740700a53d99525e662136e`  
**Checkpoint pre-seal:** `fe5653dd8bfaee9e392349862f10d346b267d638`

## Alcance entregado

Task 6 añade únicamente selección geométrica managed de objetos de texto y arbitraje mixto con imágenes ya descubiertas por F6/F7.

### Texto

`TextHitTester.HitTest(...)`:

- recibe snapshots `PdfTextObjectInfo` ya managed;
- usa `PdfTextObjectQuad` como geometría real de selección;
- prefiltra por bounds;
- valida coordenadas finitas;
- rechaza quads degenerados;
- acepta orientación clockwise/counter-clockwise mediante producto cruzado;
- selecciona el candidato topmost por mayor `PageObjectIndex`;
- punto no finito devuelve `null`;
- no usa handles nativos ni llama PDFium.

### Arbitraje imagen/texto

`EditObjectHitTester.HitTest(...)`:

- reutiliza `ImageHitTester.HitTest(...)` existente de F6;
- reutiliza `TextHitTester.HitTest(...)` nuevo;
- compara únicamente los dos candidatos finales;
- mayor `PageObjectIndex` gana;
- devuelve `EditObjectHit` con `Image` o `Text`;
- no introduce motor geométrico genérico ni modifica `ImageHitTester`.

## RED

Commit: `d8cfcc215e4caeb63684349ff358876ec816de95`  
Workflow: `38075266128`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **699 PASS / 6 FAIL / 705 total**;
- los 6 fallos pertenecían exclusivamente a `EditObjectHitTesterTests`;
- causa esperada: todavía no existían `TextHitTester` ni `EditObjectHitTester`.

Cobertura RED:

1. quad normal: inside/select + outside/reject;
2. quad rotado: rechazo de falso positivo del AABB;
3. solapamiento texto/texto: mayor `PageObjectIndex`;
4. punto NaN/Infinity: `null`;
5. solapamiento texto/imagen: gana el mayor índice en ambos sentidos;
6. sin candidato/no-finito mixto: `null`.

## GREEN funcional verificado

Commits de producto:

- `057d2b02bb04b5b22ddc7fe41f5fd9dfcf76eb34` — text quad hit-testing;
- `3cea025d1e3ca200a740700a53d99525e662136e` — mixed edit-object arbitration.

Workflow exacto: `38075383726`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **705 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

## Auditoría KISS / scope

Diff neto desde la base Task 5 sellada hasta el head funcional:

1. `src/SGPdf.App/Features/Edit/Text/TextHitTester.cs`;
2. `src/SGPdf.App/Features/Edit/EditObjectHitTester.cs`;
3. `tests/SGPdf.App.Tests/EditObjectHitTesterTests.cs`.

No se modificó código F6 existente.

No se implementó en Task 6:

- workspace de texto;
- política `OriginalFont` / `FallbackTtf`;
- read-only por render mode;
- UI/overlays;
- edición de contenido/tamaño/color;
- writer/materialización;
- Save As de texto;
- undo global;
- reflow/OCR;
- Task 7+.

## Decisiones deliberadas

- No se extrajo una abstracción geométrica común con imágenes: F6 ya tiene un hit-test correcto por matriz y Task 6 solo necesita el quad real de texto.
- `PageObjectIndex` sigue siendo la única regla de z-order para objetos top-level de la página activa.
- El arbitraje mixto no reenumera objetos ni consulta PDFium; opera sobre candidatos managed.

## Gate del checkpoint

Checkpoint pre-seal: `fe5653dd8bfaee9e392349862f10d346b267d638`  
Workflow: `38075499138`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **705 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

## QA manual

**NOT RUN.** Task 6 no añade UI; su alcance es geometría/arbitraje managed. El PASS automatizado no se presenta como validación manual Windows.

## Cierre

Task 6 queda cerrada con RED real, GREEN exacto, auditoría de scope y CI limpio sobre el checkpoint pre-seal.

El commit de sellado es documentación únicamente y se verifica de forma independiente antes de autorizar Task 7.
