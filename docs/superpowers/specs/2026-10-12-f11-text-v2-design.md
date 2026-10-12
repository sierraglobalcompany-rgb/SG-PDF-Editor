# F11 — Texto V2: diseño

**Fecha:** 2026-10-12  
**Rama:** `feat/f11-text-v2`  
**Base apilada:** cierre F10 `42935a865c898dea9e3548fbf94e1b400c8a5732`  
**Estado:** diseño aprobado en conversación; pendiente revisión escrita antes del plan de implementación.

## 1. Objetivo

F11 amplía la edición de texto existente sin convertir SG PDF Editor en un procesador Word-like. El objetivo es reconstruir estructura visual suficientemente fiable para trabajar con:

1. orden de lectura;
2. líneas;
3. párrafos;
4. una o dos columnas;
5. reflow limitado únicamente cuando la geometría sea simple y segura.

El producto sigue siendo offline, gratuito, PDFium-first y conservador ante ambigüedad.

## 2. Fuera de alcance

F11 no intentará:

- reflow libre de página completa;
- maquetación tipo Word/LibreOffice;
- tablas complejas;
- texto vertical como flujo editable;
- layouts editoriales arbitrarios o flotantes;
- reconstrucción semántica de documentos escaneados más allá de lo que ya entrega F10 OCR;
- adivinar estructura cuando la geometría sea ambigua;
- introducir PdfPig u otro motor PDF preventivamente.

Los casos fuera de política deben clasificarse explícitamente como no soportados o ambiguos, nunca modificarse silenciosamente.

## 3. Restricciones heredadas

- `Guardar como` sigue siendo la ruta por defecto para cambios destructivos.
- El original no se modifica.
- PDFium mantiene la propiedad principal de lectura/escritura PDF.
- Las operaciones nativas siguen bajo el `NativeGate` existente.
- CI y pruebas forman parte del feature.
- Ningún dato privado real entra al repositorio.
- No merge a `main` sin aprobación explícita.

## 4. Arquitectura propuesta

F11 se divide en dos capas independientes:

```text
PDFium text/geometría existente
        ↓
TextLayoutAnalyzer
        ↓
TextLayoutPage
  ├─ TextColumn
  │   └─ TextParagraph
  │       └─ TextLine
  │           └─ TextRun/fragmentos
        ↓
TextReflowPolicy
        ↓
solo casos seguros
        ↓
writer/transacción ya existente de Texto V1
```

La primera mitad de F11 es estrictamente de análisis y no escribe PDFs. La escritura entra únicamente después de probar la clasificación geométrica.

## 5. Modelo administrado

Se introduce un modelo pequeño, sin dependencia del motor PDF:

- `TextLayoutPage`: ancho/alto, columnas detectadas, estado general.
- `TextColumn`: bounds y bloques de esa columna.
- `TextParagraph`: bounds, líneas, alineación aproximada, estilo dominante y confianza.
- `TextLine`: bounds, baseline aproximada, runs/objetos, texto compuesto y orden.
- `TextRun`: referencia al objeto/rango fuente, texto, bounds, quad, fuente, tamaño y matriz disponibles.
- `TextLayoutClassification`: `Simple`, `Ambiguous`, `Unsupported`.

El modelo debe ser determinista y testeable sin WPF.

## 6. Fuente de datos

### 6.1 PDFium primero

F11 reutilizará la infraestructura de texto ya existente:

- extracción de texto por rango;
- rectángulos de texto;
- hit testing;
- objetos de texto de F7 con matriz, bounds, quad, fuente, tamaño, color y render mode.

Antes de añadir dependencia nueva, se caracteriza si estos datos son suficientes para producir un orden geométrico estable.

### 6.2 Gate opcional de PdfPig

PdfPig solo podrá entrar si una prueba concreta demuestra simultáneamente que:

1. PDFium exige una cantidad claramente mayor de código heurístico frágil para reading order/segmentación;
2. PdfPig mejora de forma medible el corpus definido;
3. la dependencia/licencia/runtime siguen siendo compatibles con las leyes del proyecto;
4. no duplica innecesariamente responsabilidades de PDFium.

Si PDFium es suficiente, PdfPig queda fuera de F11.

## 7. Heurísticas KISS

### 7.1 Normalización

Antes de agrupar:

- ignorar fragmentos vacíos/degenerados;
- trabajar en coordenadas PDF normalizadas por página;
- tolerancias relativas al tamaño de fuente/altura del fragmento, no píxeles mágicos globales;
- conservar referencia al origen para trazabilidad.

### 7.2 Líneas

Fragmentos pueden pertenecer a la misma línea cuando:

- su baseline/centro vertical está dentro de una tolerancia derivada de su altura;
- no presentan rotación incompatible;
- existe proximidad horizontal razonable;
- el orden horizontal es determinable.

Una línea se ordena por geometría, no por el orden bruto de objetos del archivo.

### 7.3 Párrafos

Líneas consecutivas se agrupan cuando son compatibles en:

- columna;
- separación vertical;
- alineación/sangría;
- tamaño de texto dominante;
- orientación;
- solapamiento horizontal razonable.

Cambios fuertes de tamaño, grandes espacios verticales o ruptura de columna cierran párrafo.

### 7.4 Columnas

V1 de F11 soporta:

- una columna;
- dos columnas claramente separadas.

La detección usa clusters horizontales de líneas/bloques y exige un gutter estable. Si no existe separación clara o hay cruces significativos, la página se marca `Ambiguous`.

El orden esperado para dos columnas simples es columna izquierda completa y después columna derecha, salvo evidencia geométrica inequívoca de otro patrón.

### 7.5 Reading order

El orden de lectura se calcula a partir del modelo estructural, no del orden interno de objetos PDF. Debe ser estable ante un fixture donde los objetos se guarden deliberadamente en orden incorrecto.

## 8. Reflow limitado

El reflow solo se habilita para un `TextParagraph` que cumpla todos los guardrails:

- clasificación `Simple`;
- una única columna;
- orientación horizontal compatible;
- bounds rectangulares claros;
- estilo dominante suficientemente homogéneo;
- sin objetos/fragmentos solapados problemáticos;
- sin tabla detectada ni patrón de múltiples cajas;
- fuente/escritura soportada por la política de Texto V1;
- preflight general permite edición;
- el resultado puede validarse después de guardar y reabrir.

Si una condición falla, no se refluye; la UI debe explicar que el bloque no es seguro para edición avanzada.

## 9. Escritura y preservación

F11 no crea un segundo writer general.

La edición reutilizará la infraestructura transaccional de Texto V1 y sus guardrails:

1. analizar;
2. determinar bloque seguro;
3. preparar mutación;
4. guardar a destino nuevo;
5. reabrir;
6. validar texto/render/estructura relevante;
7. solo entonces considerar éxito.

Para el primer slice de implementación no habrá escritura: analyzer + tests únicamente.

## 10. Corpus de pruebas

El corpus sintético mínimo debe cubrir:

1. párrafo simple de una columna;
2. varias líneas con sangría;
3. título + párrafo con tamaños distintos;
4. dos columnas con gutter claro;
5. objetos PDF almacenados en orden incorrecto;
6. dos columnas con orden bruto entremezclado;
7. texto rotado;
8. bloques solapados/ambiguos;
9. página OCR generada por F10;
10. caracteres españoles y puntuación;
11. párrafo cuyo reflow debe ser permitido;
12. párrafo visualmente complejo cuyo reflow debe ser rechazado.

Las pruebas deben validar estructura y orden, no depender solo de screenshots.

## 11. Error handling

- El analyzer no debe lanzar por layout desconocido; devuelve clasificación conservadora.
- Datos nativos inválidos/degenerados se descartan de forma trazable.
- Errores reales de PDFium/documento se propagan mediante los patrones existentes.
- Cancelación debe respetarse en análisis de páginas grandes.
- Nunca modificar el documento durante análisis.

## 12. Rendimiento

F11 comienza page-scoped y bajo demanda.

No se preanaliza todo el documento al abrirlo. El análisis de la página actual puede cachearse únicamente si existe una invalidación clara tras cambios; si eso complica la primera slice, se recalcula.

No se optimiza prematuramente con paralelismo porque PDFium ya está serializado por `NativeGate`.

## 13. UI prevista

La UI no entra en el primer slice.

Cuando el analyzer esté probado, la integración mínima será sobre el flujo de edición de texto existente:

- selección de un bloque/párrafo reconocido;
- indicación de estado `Simple` / no seguro;
- edición/reflow solo para `Simple`;
- sin paneles de maquetación complejos.

## 14. Fases internas propuestas

### F11.1 — Characterization + Layout Analyzer

- fixtures sintéticos;
- modelo administrado;
- reconstrucción de runs/líneas;
- reading order básico de una columna;
- sin UI, sin writer.

### F11.2 — Párrafos + columnas

- agrupación de líneas;
- detección conservadora de una/dos columnas;
- casos ambiguos;
- reading order estable.

### F11.3 — Reflow policy

- reglas `Simple/Ambiguous/Unsupported`;
- decisión explícita de cuándo permitir reflow;
- todavía separable del writer.

### F11.4 — Integración de escritura

- reutilizar Texto V1;
- Save As;
- reopen/validate;
- no alterar documentos no soportados.

### F11.5 — UI + hardening

- integración mínima WPF;
- cancelación/errores;
- corpus ampliado;
- CI exact-head;
- documentación/cierre.

## 15. Definition of Done de F11

F11 podrá declararse automatizadamente cerrado cuando:

- reading order simple sea determinista;
- líneas/párrafos se reconstruyan correctamente en el corpus;
- una y dos columnas simples estén soportadas;
- casos ambiguos se rechacen de forma conservadora;
- reflow solo opere en bloques clasificados como seguros;
- Save As + reopen/validate pase;
- OCR de F10 no se degrade en el corpus probado;
- build/tests Windows exact-head estén verdes;
- docs/estado/histórico estén alineados;
- no se haya añadido una dependencia PDF nueva sin gate documentado.

QA manual/real seguirá diferenciada de la evidencia automatizada.

## 16. Decisiones congeladas por este diseño

- F11 empieza por análisis, no por writer ni UI.
- Una/dos columnas simples sí; layouts arbitrarios no.
- Ambigüedad implica fail-closed.
- PDFium primero.
- PdfPig es un gate opcional, no una decisión previa.
- El primer bloque implementable será pequeño: modelo + analyzer + tests de una columna/reading order.
- No merge a `main` durante F11 sin autorización expresa.
