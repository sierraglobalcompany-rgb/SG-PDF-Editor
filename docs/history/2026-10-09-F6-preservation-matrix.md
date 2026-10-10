# F6 — Matriz independiente de preservación del writer de imágenes

**Fecha de plan:** 2026-10-09  
**Evidencia ejecutada:** 2026-10-10  
**Rama:** `feat/f6-images`  
**Task 8 base:** `a4632c2cd21f604c58ea7c9a340d0759eb11fbbe`

## 1. Objetivo

Esta matriz clasifica la preservación de estructuras PDF para **F6 — EDITAR imágenes** usando evidencia del writer F6 real. No hereda la clasificación de F5/ORGANIZAR: F5 reconstruye/importa páginas, mientras F6 reabre el PDF original y muta objetos de imagen de página in-place con PDFium antes de guardar una copia.

La evidencia de esta matriz es deliberadamente independiente.

## 2. Evidencia de caracterización

RED de caracterización: `c12d93b885cb5ed1d708a51d6f323a0b0738b6f2`  
Workflow: `38055253981`

El fixture sintético representativo contiene una imagen editable y, simultáneamente, estructuras no criptográficas que F6 debe preservar. La prueba realiza una edición real mínima sobre la imagen —desplazamiento de 1 punto— mediante `PdfImageEditWriter`, guarda una copia, reabre el resultado y vuelve a inspeccionarlo.

Resultado del run de caracterización:

- Release build: PASS;
- warnings: 0;
- errors: 0;
- tests: **650 passed / 1 failed / 0 skipped / 651 total**;
- la única falla fue el contrato RED que exigía `ImageEditPreflightInspector`, todavía inexistente en ese commit;
- las ocho comprobaciones de preservación del writer F6 pasaron.

Por tanto, las clasificaciones siguientes son observadas en el corpus sintético automatizado representativo y no son una copia de F5.

## 3. Matriz observada

| Estructura | Evidencia fuente | Evidencia tras writer F6 + reopen | Clasificación F6 | Política Save As |
|---|---|---|---|---|
| Formularios | presente | presente | `ProvenPreserved` | `Info` |
| Marcadores | presente | presente | `ProvenPreserved` | `Info` |
| Destinos nombrados | presente | presente | `ProvenPreserved` | `Info` |
| Enlaces internos | presente | presente | `ProvenPreserved` | `Info` |
| Estructura etiquetada | presente | presente | `ProvenPreserved` | `Info` |
| Etiquetas de página | presente | presente | `ProvenPreserved` | `Info` |
| Archivos adjuntos | presente | presente | `ProvenPreserved` | `Info` |
| Metadatos | título/autor conocidos | mismos valores tras reopen | `ProvenPreserved` | `Info` |

La prueba de metadatos compara valores representativos exactos (`Title` y `Author`), no solo la presencia genérica de un diccionario de información.

## 4. Estructuras bloqueadas

| Condición | Clasificación | Política |
|---|---|---|
| Firma criptográfica | `Unknown` | `Block` |
| PDF abierto con contraseña | `Unknown` | `Block` |

Estas dos condiciones no se presentan como “preservadas”: F6 las bloquea antes de materializar, por lo que no existe evidencia de writer que justifique una afirmación de preservación.

Si en el futuro una estructura no criptográfica obtiene evidencia `ProvenChangedOrLost` o permanece `Unknown`, su política es `Warning` y requiere una confirmación explícita antes de `Guardar como...`.

## 5. Política implementada

`ImageEditPreflightInspector` clasifica las ocho estructuras no criptográficas anteriores como `Info / ProvenPreserved` cuando se detectan. Firma criptográfica y fuente abierta con contraseña son `Block / Unknown`.

`ImageEditPreflightResult` permite continuar cuando no existe `Block`; únicamente un finding `Warning` exige confirmación. La UI de EDITAR solicita como máximo una confirmación explícita y el writer recibe `warningsConfirmed=true` solo después de esa aceptación.

El writer mantiene además sus propias defensas de contraseña, fingerprint y firma. `warningsConfirmed` ya no es un parámetro ignorado: el writer ejecuta el preflight no criptográfico antes de materializar y rechaza una advertencia no confirmada.

## 6. Límites de la evidencia

Esta matriz demuestra comportamiento sobre el corpus sintético automatizado representativo de F6 y el runtime PDFium fijado por el proyecto. No constituye una garantía universal para todos los PDFs existentes.

No se ejecutó QA manual de Windows, corpus privado de Mercado Libre, hardware físico ni pruebas privadas de clientes. Esas validaciones continúan marcadas **NOT RUN**.
