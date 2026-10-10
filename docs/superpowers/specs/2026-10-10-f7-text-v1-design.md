# F7 — Texto V1 — Diseño canónico

**Fecha:** 2026-10-10  
**Estado:** **DISEÑO ENMENDADO tras Gate F7.1 + spike Unicode exitoso; requiere revisión del usuario antes de reanudar implementación.**  
**Base F6:** `feat/f6-images` @ `c6d762efca01d50bfe3932d1f05617190a464fc6`  
**Rama F7:** `feat/f7-text-v1`  
**Checkpoint de evidencia:** `docs/history/2026-10-10-F7-UNICODE-FALLBACK-SPIKE-CHECKPOINT.md`  
**Producto:** SG PDF Editor — Windows x64 / C# / .NET 10 / WPF / offline-first.

---

## 1. Objetivo

Añadir edición conservadora de **objetos de texto PDF reales** dentro del único modo `EDITAR`, sin convertir el PDF en Word, sin overlays que oculten texto viejo y sin introducir un segundo motor PDF.

F7 debe permitir:

1. detectar/seleccionar objetos `FPDF_PAGEOBJ_TEXT` top-level de la página activa;
2. editar el contenido completo de un objeto cuando sea seguro;
3. conservar matriz/rotación, origen, tamaño y color cuando sea viable;
4. usar una única TTF fallback redistribuible cuando la fuente original no sea segura;
5. materializar imágenes + texto en una sola apertura/escritura transaccional;
6. reabrir y validar Unicode exacto, geometría y render;
7. conservar las garantías F6: offline, Save As, fingerprint, preflight, temp/cleanup y cero handles nativos persistentes.

No hay reflow de párrafos en F7 V1.

---

## 2. Requisitos

- `TEXT-01` — detectar/seleccionar objetos de texto.
- `TEXT-02` — edición in-place conservadora donde sea segura.
- `TEXT-03` — fallback TTF redistribuible para nuevos code points/subset dudoso.
- `TEXT-04` — propiedades básicas + Save As + reopen validation.

Nada de F8–F12 entra preventivamente.

---

## 3. Principios KISS/YAGNI

- PDFium sigue siendo el **único motor de edición PDF**.
- No añadir PdfPig, qpdf, pdfcpu, MuPDF, iText, Ghostscript ni motor equivalente.
- No `Domain/Application/Infrastructure`, DI nueva, command bus, plugin framework ni object graph PDF genérico.
- No OCR, reading order, columnas, párrafos, Form XObject text, creación libre de cajas, auto-fit o síntesis bold/italic.
- No depender de fuentes instaladas en Windows para producto.
- No descargar fuentes/componentes en runtime.
- Trabajar solo sobre la página activa para discovery/UI.
- Estado persistente managed; ningún `IntPtr`/handle sobrevive a una operación nativa.
- Candidate-first: el fuente no se muta hasta `Guardar como...`.
- Source != destination; temp sibling; reopen/validate antes de publicación atómica.

---

## 4. UX congelada

Existe un solo modo `EDITAR` para imágenes F6 + texto F7.

Al hacer clic:

1. device → PDF;
2. evaluar imágenes + textos editables de la página activa;
3. si hay solapamiento, gana el mayor `PageObjectIndex`;
4. existe una sola selección autoritativa del modo EDITAR.

Para texto seleccionado se muestran:

- contorno quad/bounds;
- contenido completo;
- tamaño en puntos;
- color de relleno;
- nombre de fuente informativo;
- estrategia prevista: `Fuente original` o `Fuente compatible`.

Controles V1:

- `Texto`;
- `Tamaño`;
- color básico;
- `Aplicar`.

`Aplicar` solo modifica workspace lógico. Texto vacío/solo whitespace es inválido porque V1 no ofrece delete.

---

## 5. Objeto editable V1

Solo texto top-level de página.

Un objeto es candidato cuando:

- type `TEXT`;
- Unicode legible/no vacío;
- matriz legible;
- bounds/rotated bounds legibles;
- font size legible;
- fill color legible;
- render mode dentro del subconjunto permitido.

Texto dentro de `FPDF_PAGEOBJ_FORM` queda fuera de alcance. Render modes no probados pueden descubrirse/seleccionarse como read-only, pero no materializarse.

---

## 6. Gate F7.1 — resultado real del runtime pinneado

Runtime probado: `bblanchon.PDFium.Win32 156.0.8076`.

### 6.1 Exports/capacidades confirmadas

Se confirmó la ruta necesaria para:

- enumerar page objects;
- leer type/bounds/rotated bounds/matrix;
- extraer texto;
- leer font size/font handle/base font name;
- leer render mode y fill color;
- `FPDFText_SetText`;
- `FPDFPageObj_CreateTextObj`;
- remove/insert-at-index;
- `FPDFPage_GenerateContent`;
- `FPDF_SaveAsCopy`;
- `FPDFFont_Close`;
- **`FPDFText_LoadCidType2Font`**.

La ruta para nombre de fuente es:

`FPDFTextObj_GetFont` → `FPDFFont_GetBaseFontName`.

### 6.2 Fuente original — PASS

Caso probado:

`CASA 123` → `CASA 321`

Con `FPDFText_SetText`:

- texto exacto tras save/reopen: PASS;
- mismo índice: PASS;
- tamaño: PASS;
- nombre de fuente: PASS;
- color: PASS;
- matriz: PASS;
- render reabierto: PASS.

### 6.3 Rutas `FPDFText_LoadFont` — RECHAZADAS

`cid=1`:

- entrada `NIÑO 1`;
- reapertura `NIÑO\u00A01`;
- U+0020 se convierte en NBSP.

`cid=0`:

- entrada `NIÑO áé`;
- reapertura `NIÿO ÿÿ`.

Por tanto **`FPDFText_LoadFont` NO es la ruta de fallback de producto F7**.

### 6.4 CID Type2 con mapas explícitos — PASS

El spike separado probó:

`FPDFText_LoadCidType2Font(document, fontBytes, ..., toUnicodeCMap, cidToGidMap, ...)`

con:

- `ToUnicode` generado explícitamente;
- `CIDToGIDMap` generado desde los glyph IDs de la TTF;
- objeto recreado en el mismo índice;
- matriz/color/tamaño conservados;
- texto `NIÑO áé` exacto tras save/reopen;
- render válido.

CI funcional del spike: **687/687**, build 0 warnings / 0 errors.

Conclusión: **F7 ya no está bloqueada técnicamente** y sigue siendo PDFium-only.

---

## 7. Estrategia de fuente

### 7.1 `OriginalFont`

Regla conservadora inicial:

- whitespace nuevo puede aceptarse;
- cada rune no-whitespace nuevo debe haber aparecido ya en el texto original del mismo objeto;
- si hay duda, fallback.

Ejemplo:

- `CASA 123` → `CASA 321` = potencial `OriginalFont`;
- `CASA` → `NIÑO` = fallback.

No se adivina cobertura real de subsets.

### 7.2 `FallbackTtf`

Cuando aparezcan runes nuevos/dudosos:

1. usar una TTF local pinneada y redistribuible;
2. reunir **la unión de runes** de todos los edits fallback de esa materialización;
3. verificar cobertura de cada rune;
4. obtener glyph IDs desde la TTF;
5. construir una sola vez `ToUnicode` y `CIDToGIDMap` explícitos;
6. cargar **un solo font handle CID Type2 por documento/materialización**;
7. recrear cada text object con `FPDFPageObj_CreateTextObj` + `FPDFText_SetText`;
8. copiar color/matriz/tamaño;
9. remove + insert en el mismo `PageObjectIndex`;
10. cerrar font handle al final de la materialización.

No se normaliza NBSP↔SPACE ni se aceptan glifos corruptos silenciosamente.

### 7.3 Fuente candidata

Candidata inicial: **DejaVu Sans 2.37**, solo después de Gate de procedencia/licencia/hash/cobertura.

La presencia de Arial/Segoe/Tahoma en Windows solo fue fixture efímero del spike; no crea dependencia de producto.

---

## 8. Font asset / glyph mapping

La implementación de producto puede usar APIs managed ya disponibles en .NET/WPF para comprobar cobertura/glyph ID del archivo TTF pinneado; no se añade parser de fuentes externo ni NuGet nuevo salvo evidencia futura que obligue a revisar esta spec.

Reglas:

- tamaño máximo explícito antes de leer la TTF;
- bytes managed;
- buffers nativos solo durante la carga del font;
- `ToUnicode` generado desde `Rune`, no `char` individual;
- soportar inicialmente ASCII + español + cobertura Latin demostrada por la fuente;
- rune fuera de cobertura → `Aplicar` inválido, sin dirty-state.

Emoji/CJK/RTL no forman parte de la promesa F7.

---

## 9. Modelo managed

### `PdfTextObjectInfo`

Snapshot inmutable:

- `PageIndex`;
- `PageObjectIndex`;
- `Text`;
- `Matrix`;
- `Bounds`/quad;
- `FontName`;
- `FontSize`;
- `FillColor`;
- `TextRenderMode`.

### `TextObjectKey`

`(PageIndex, PageObjectIndex)`.

### `TextEditState`

Original + candidato actual + tamaño + color + `TextFontStrategy`.

### `TextEditWorkspace`

- baseline;
- `IsDirty`;
- candidate-first commit;
- `EditedStates`;
- `MarkSavedBaseline()`.

No historial global nuevo de texto en F7.

---

## 10. Promoción KISS de piezas F6 compartidas

No crear clones `TextEditSourceFingerprint`, `TextEditPreflight` o `PdfTextEditWriter`.

Promociones permitidas behavior-preserving:

- `ImageEditSourceFingerprint` → `PdfEditSourceFingerprint`;
- `ImageEditPreflight` → `PdfEditPreflight`;
- `PdfImageEditWriter` → `PdfEditWriter`;
- `ImageEditOutputValidator` → `PdfEditOutputValidator`;
- `MainWindow.EditImages.cs` → `MainWindow.Edit.cs`;
- `MainWindow.EditImages.Hardening.cs` → `MainWindow.Edit.Hardening.cs`.

Workspaces y comandos específicos de imagen/texto permanecen específicos.

No `IPdfWriter`, base writer, factory/strategy framework ni registry genérico.

---

## 11. Discovery / hit-test

`PdfDocumentSession.GetTextObjects(pageIndex)`:

- `NativeGate`;
- LoadPage + text page;
- enumerar solo page objects top-level;
- snapshots managed;
- cerrar handles antes de retornar;
- cancelación entre objetos.

Hit-test texto usa rotated quad cuando esté disponible. Arbitraje imagen/texto = mayor `PageObjectIndex`.

Complejidad objetivo: O(objetos de página activa), no O(documento).

---

## 12. Writer combinado

`Guardar como...` abre el PDF fuente una sola vez.

Flujo:

```text
fingerprint + preflight
→ reopen original
→ por página editada
    resolver TODOS los objetos referenciados antes de mutar
    aplicar image edits
    aplicar text edits OriginalFont
    aplicar text edits FallbackTtf/CID Type2
    GenerateContent una sola vez
→ save temp
→ reopen + validate
→ atomic publish
→ cleanup
→ marcar baseline de ambos workspaces
```

No ejecutar writer de imagen y luego writer de texto en cadena.

---

## 13. Materialización `OriginalFont`

1. resolver text object;
2. validar tipo/snapshot;
3. `FPDFText_SetText`;
4. aplicar tamaño/color solo con setter fiable confirmado;
5. mantener matriz salvo cambio explícitamente soportado;
6. `GenerateContent` al final de la página.

Si no hay setter seguro de tamaño, un cambio de tamaño fuerza recreación/fallback; no simular font size mediante hacks de matriz.

---

## 14. Materialización `FallbackTtf`

Por documento/materialización:

1. cargar la TTF pinneada;
2. calcular unión de runes requeridos;
3. obtener glyph ID para cada rune;
4. asignar CIDs deterministas;
5. construir `ToUnicode` + `CIDToGIDMap`;
6. `FPDFText_LoadCidType2Font` una sola vez;
7. por objeto: crear text object, set text/color/matrix/tamaño, remove original, insert same index;
8. destruir ownership correcto;
9. cerrar font al final;
10. `GenerateContent` una sola vez por página.

Si cualquier paso falla, el destino final no se publica.

---

## 15. Geometría / métricas

F7 conserva origen, matriz, rotación, tamaño y color.

No promete ancho idéntico cuando cambia texto/fuente.

No auto-fit, squeeze, reflow ni reducción silenciosa de tamaño.

---

## 16. Dirty-state / guards / UI Save As

Dirty general:

`imageWorkspace.IsDirty || textWorkspace.IsDirty`.

Mismos guards para:

- LEER;
- FIRMAR;
- ORGANIZAR;
- abrir PDF/ZPL;
- Ctrl+O;
- cerrar ventana.

Un solo `Guardar como...` del modo EDITAR.

Cancel/Block/decline → writer 0 calls. Éxito marca ambos baselines. Fallo conserva dirty-state.

No segundo `Loaded` hook ni segunda cadena de guards.

---

## 17. Preflight / preservación

F7 vuelve a medir preservación con el writer combinado real:

- forms;
- bookmarks;
- named destinations;
- internal links;
- tagged structure;
- page labels;
- attachments;
- metadata.

Estados:

- `ProvenPreserved`;
- `ProvenChangedOrLost`;
- `Unknown`.

Firma criptográfica/password-opened → `Block / Unknown`.

No copiar resultados F6 sin evidencia.

---

## 18. Validación de salida

Por text edit materializado:

- output abre;
- objeto esperado existe y type = TEXT;
- Unicode exacto = solicitado;
- matriz dentro de tolerancia;
- tamaño esperado;
- color esperado;
- fallback usa font/ruta esperada;
- render de página válido/no vacío.

Solo después se publica el destino.

---

## 19. Seguridad / threading / memoria

Toda llamada PDFium:

```csharp
PdfiumRuntime.NativeGate.Wait(...);
try { /* native */ }
finally { PdfiumRuntime.NativeGate.Release(); }
```

Nunca `lock(NativeGate)` ni reacquire dentro de la misma sección.

Se conservan:

- source fingerprint;
- source != destination;
- temp sibling;
- cancelación antes/después de etapas costosas;
- cleanup en `finally`;
- fallo nunca marca baseline;
- source nunca se modifica.

---

## 20. Fixtures / QA automatizada mínima

Debe cubrir:

- Gate exports + in-place + CID Type2 explicit Unicode;
- discovery top-level, Unicode, rotated quad, cancelación, active-page-only;
- objetos contiguos separados;
- render mode no soportado read-only;
- overlap texto/texto e imagen/texto;
- workspace/policy por `Rune`;
- original-font writer;
- CID Type2 fallback exacto con `ñ/á/é`;
- same-index replacement;
- dos fallback edits compartiendo un solo font handle/map plan;
- mixed image+text same page;
- stale/cancel/temp/publication failure;
- output validation;
- matriz de preservación F7;
- dirty guards;
- LEER/FIRMAR/ORGANIZAR/ZPL/F6 images/offline regressions;
- cero native handles en estado persistente.

No introducir PDFs privados.

---

## 21. QA manual

Permanece `NOT RUN` hasta ejecución física.

Checklist final incluye selección/zoom/rotación, español, tamaño/color, mixed image+text, Save As/cancel/failure, lector externo, documento pesado, guards y red físicamente deshabilitada.

Automated PASS no implica manual PASS.

---

## 22. Dependencias

Esperado:

- ningún NuGet nuevo;
- mismo PDFium pinneado;
- una TTF redistribuible pinneada después del Gate legal/provenance.

El spike usa `SkiaSharp` solo porque ya existe como dependencia de **tests**; producto no debe adquirir esa dependencia por accidente. Para producto preferir capacidades WPF/.NET ya disponibles para cobertura/glyph IDs.

---

## 23. Gobernanza

F7 no autoriza:

- merge de PR #25/F6;
- merge a `main`;
- cerrar QA manual sin ejecutarla;
- introducir fuentes sin licencia/hash/provenance;
- borrar checkpoints históricos;
- reescribir commits previos.

`main` debe permanecer intacto hasta aprobación explícita.

---

## 24. Criterio de cierre automatizado

F7 solo puede declararse `AUTOMATED CLOSURE PASS` cuando:

1. Gate F7.1 + spike CID Type2 = PASS;
2. Gate de fuente/provenance = PASS;
3. `TEXT-01..04` tienen evidencia;
4. writer combinado es transaccional;
5. preservación F7 está medida;
6. build 0 warnings / 0 errors;
7. suite completa PASS;
8. exact-head Windows CI PASS;
9. draft PR apilado abierto/unmerged;
10. `main` intacto;
11. QA manual declarada honestamente PASS o NOT RUN.

---

## 25. Decisiones congeladas de esta enmienda

1. Texto real, no overlay.
2. Sin reflow.
3. Top-level text only.
4. Un solo modo EDITAR.
5. Topmost por object index.
6. Estado persistente managed.
7. `OriginalFont` solo bajo política conservadora.
8. Runes nuevos/duda → `FallbackTtf`.
9. **Fallback usa `FPDFText_LoadCidType2Font` + `ToUnicode` + `CIDToGIDMap` explícitos.**
10. **`FPDFText_LoadFont` queda prohibido como fallback F7 por evidencia de corrupción/normalización Unicode.**
11. Una sola TTF fallback pinneada inicialmente.
12. Una sola carga/map plan CID Type2 por documento/materialización, construida con la unión de runes requeridos.
13. Writer combinado abre/materializa una sola vez.
14. Preservación F7 se vuelve a medir.
15. Firmas/password continúan Block.
16. No nuevo NuGet esperado.
17. No merge automático.

---

## 26. Riesgos residuales

- PDFs pueden fragmentar texto en muchos objetos; V1 acepta granularidad.
- Fallback puede cambiar métricas visuales; no se promete ancho idéntico.
- Política de fuente original puede caer a fallback más veces de lo necesario.
- Form XObject text queda fuera.
- Mapping CID debe mantenerse determinista y validado por reopen Unicode exacto.
- Preservación documental se prueba de nuevo con el writer combinado.

---

## 27. Gate para reanudar implementación

Esta spec enmendada sustituye las secciones antiguas que usaban `FPDFText_LoadFont` como fallback.

**No reanudar Task 2 hasta que el usuario revise esta spec enmendada y el plan TDD actualizado.**
