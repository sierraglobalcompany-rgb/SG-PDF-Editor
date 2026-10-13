# F12.3 Task 4 — Independent validator + representative preservation

Fecha: 2026-10-13  
Rama de cierre: `feat/f12-3-task4-5-closure`  
Base funcional limpia de cierre 4.4: `c9da1c2d20cbb71af2c2b987efb12198ab63f2c6`

## Resultado

**CLOSED / GREEN condicionado únicamente al exact-head CI de este cierre documental.**

Task 4 añade un validador pre-publicación para los campos de texto AcroForm clásicos soportados y demuestra un **representative preservation gate**. No declara preservación universal, compatibilidad universal con lectores externos ni soporte general de formularios PDF.

## Arquitectura final KISS

Flujo de guardado soportado:

`preflight → supported-field gate → fingerprint → same-directory temp → write /V + /AP/N → PdfAcroFormOutputValidator → fingerprint recheck → atomic publish → cleanup`

Owners de producción:

- `PdfAcroFormSaveService`: transacción y publicación.
- `PdfAcroFormOutputValidator`: validación pre-publicación de salida.

El validator usa dos clases de evidencia:

1. **PDFsharp estructural estrecho** para valor esperado, `/Rect` y `/AP/N` persistido.
2. **PDFium FormFill** como señal visual separada mediante `FPDFDOC_InitFormFillEnvironment`, lifecycle mínimo de página y `FPDF_FFLDraw`.

No se afirma que los checks estructurales PDFsharp sean independientes del writer. La independencia real añadida en Task 4 está en la evidencia renderizada de PDFium FormFill.

No se añadió:

- segundo motor PDF;
- paquete NuGet nuevo;
- backend;
- red;
- cloud;
- DB;
- telemetría;
- framework genérico de formularios;
- parser AcroForm propio;
- UI;
- soporte para tipos de campo adicionales.

## Task 4.1 — capability/design gate

Objetivo: comprobar si PDFium FormFill podía usarse de forma mínima sin crear un subsistema grande.

Resultado: **GO**.

- commit: `e2d245b6945c854b495db3e02083903c192d1779`
- workflow: `38432225516`
- job: `115417065446`
- build: PASS
- tests: **1163/1163 PASS**
- producción: sin cambios

El gate demostró que el render ordinario de `PdfDocumentSession` no dibuja widgets AcroForm, mientras que el lifecycle mínimo FormFill + `FPDF_FFLDraw` sí permite obtener evidencia representativa del widget.

## Task 4.2 — validator RED

Commit:

`68c61f26e5a8c27f3574546acfde07e33f135be1`

Workflow `38433037081`, job `115419867542`.

Resultado esperado:

- build PASS;
- 0 warnings / 0 errors;
- 1163 pruebas previas PASS;
- 4 RED nuevos fallando únicamente porque `PdfAcroFormOutputValidator` todavía no existía.

Los RED congelaron rechazo de:

- `/V` persistido incorrecto;
- `/AP/N` ausente/inválido;
- cambio de valor con apariencia stale;
- cambio de geometría `/Rect` del target.

## Task 4.3 — validator GREEN mínimo

Implementación funcional:

`67d9b83c11bd6d241b65572cc08b126e9ad795ea`

Exact-head corregido:

`7e35fe8599801621a99bb7d2b62a90f05937b7f6`

Workflow `38435244741`, job `115427392807`.

Resultado:

- build PASS;
- 0 warnings / 0 errors;
- **1167/1167 PASS**;
- skipped 0.

Archivo productivo:

- `src/SGPdf.App/Features/Forms/PdfAcroFormOutputValidator.cs`

El validator comprueba los targets solicitados y usa PDFium FormFill para exigir evidencia visual representativa cuando el valor esperado difiere del valor fuente.

## Task 4.4 — representative preservation gate

Commits:

- fixture inicial: `0474586f8a026d224db60acfc7cb79d26bebb719`
- fixture endurecido para `/AP/N` del campo no-target: `c9da1c2d20cbb71af2c2b987efb12198ab63f2c6`

Workflow `38438239079`, job `115437752560`.

Resultado:

- build PASS;
- 0 warnings / 0 errors;
- **1168/1168 PASS**;
- skipped 0.

El fixture representativo demuestra preservación de:

- page count;
- texto de página no-target;
- imagen XObject nativa;
- bookmark y destino representativo;
- URI link;
- segundo campo `/Tx` no editado, incluido valor y `/AP/N`;
- metadata Info seleccionada (`Title`, `Author`);
- source byte-for-byte intacto.

Este resultado se denomina obligatoriamente **representative preservation gate**. No prueba preservación universal de todas las estructuras PDF.

## Task 4.5 — auditoría de cierre e integración real

Durante el cierre se detectó un hueco material: `PdfAcroFormOutputValidator` existía y pasaba sus pruebas directas, pero `PdfAcroFormSaveService` todavía utilizaba el `ValidateTemp` anterior y no ejecutaba el validator nuevo antes de publicar.

No se cerró Task 4 con ese hueco.

### RED de integración

Commit:

`c5f0e29d2540af71dac78c007c2f3bfb73d8f7c7`

Workflow `38439465785`, job `115441952240`.

Resultado RED válido:

- build PASS;
- 0 warnings / 0 errors;
- 1168 baseline PASS;
- 1 fallo intencional nuevo;
- total 1169;
- fallo exacto: se esperaba `InvalidDataException`, pero no se lanzó ninguna excepción tras retirar `/AP` del temporal justo antes de la validación.

Esto probó que un temporal con apariencia invalidada podía atravesar el camino de publicación real.

### GREEN de integración

Commit:

`fbe16db8f5e1dcf2bdd08e608e474763028be431`

Cambio mínimo:

- `PdfAcroFormSaveService` ejecuta `PdfAcroFormOutputValidator.Validate(source, temp, values, cancellationToken)` en el punto pre-publicación;
- conserva después el fingerprint recheck;
- solo publica después de ambas validaciones;
- elimina el `ValidateTemp` redundante.

Workflow `38439821214`, job `115443183657`.

Resultado GREEN:

- repository hygiene PASS;
- locked restore PASS;
- Release build PASS;
- warnings 0;
- errors 0;
- **1169/1169 PASS**;
- skipped 0.

Así queda demostrado que un fallo del validator:

- impide publicación;
- preserva destination existente;
- conserva source exacto;
- limpia el temporal.

## Scope probado de Task 4

AUTO PASS dentro de este task:

- campos classic `/Tx` ya soportados;
- flat / hierarchical / inherited `/FT` dentro del flujo previamente probado;
- valor target esperado;
- `/Rect` target estable;
- `/AP/N` target utilizable y actualizado cuando cambia el valor;
- evidencia visual representativa vía PDFium FormFill;
- preservación estructural representativa enumerada arriba;
- fallo del validator integrado en transacción antes de publish.

## NOT RUN / fuera de alcance

Permanece explícitamente NOT RUN o fuera del MVP actual:

- checkbox;
- radio;
- combo;
- list;
- XFA;
- JavaScript/cálculos/acciones;
- signature fields;
- flatten;
- form designer;
- fidelidad tipográfica universal;
- private real AcroForm corpus;
- jerarquías reales complejas no cubiertas por fixtures;
- unusual embedded fonts;
- validación manual con Adobe Reader;
- validación manual con Foxit;
- validación manual con Edge;
- compatibilidad universal con cualquier lector o formulario.

## Ley del Ruido

PASS.

El cierre no mantuvo dos validadores en paralelo: al integrar `PdfAcroFormOutputValidator` en la transacción se eliminó `ValidateTemp`, que duplicaba reapertura y validación semántica con menor cobertura.

## Gobernanza

- `main`: no tocado.
- merge: NO.
- force push: NO.
- segundo engine: NO.
- Task 5: NOT STARTED.

## Cierre exact-head

Este documento y el plan de F12.3 forman el cierre documental de Task 4. El estado **CLOSED / GREEN** queda efectivo únicamente cuando el workflow exact-head generado después de ambos cambios documentales termina con suite completa verde y build sin warnings/errors.

Después de ese gate: **STOP antes de Task 5**.
