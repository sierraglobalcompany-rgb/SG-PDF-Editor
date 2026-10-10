# F7 — Texto V1 — Diseño

**Fecha:** 2026-10-10  
**Estado:** diseño escrito para revisión; **NO IMPLEMENTAR hasta aprobación explícita de esta spec**.  
**Base:** `feat/f6-images` @ `c6d762efca01d50bfe3932d1f05617190a464fc6`  
**Rama de diseño:** `design/f7-text-v1`  
**Producto:** SG PDF Editor — Windows x64 / C# / .NET 10 / WPF / offline-first.

---

## 1. Objetivo

Añadir **edición conservadora de objetos de texto PDF reales** dentro del modo `EDITAR`, sin convertir el PDF en un documento tipo Word y sin introducir un segundo motor PDF.

F7 debe permitir:

1. detectar y seleccionar objetos de texto editables en la página activa;
2. editar el contenido completo de un objeto de texto cuando sea seguro;
3. conservar posición, matriz/rotación, tamaño y color cuando sea viable;
4. usar una única TTF fallback redistribuible cuando la fuente original/subset no pueda representar de forma segura el nuevo contenido;
5. guardar imágenes + texto en **una sola materialización transaccional**;
6. reabrir y verificar texto, geometría y render del resultado;
7. mantener intactas las garantías F6: offline, Save As, fingerprint, Block para firma/contraseña, temporales, validación y ausencia de handles nativos persistentes.

No se busca reflow de párrafos ni comportamiento Word-like.

---

## 2. Requisitos trazables

La fase satisface únicamente los requisitos ya aprobados:

- `TEXT-01` — detectar/seleccionar objetos de texto.
- `TEXT-02` — edición in-place conservadora donde sea segura.
- `TEXT-03` — fallback TTF redistribuible para nuevos code points/subset dudoso.
- `TEXT-04` — propiedades básicas + Save As + reopen validation.

Nada de F8–F12 entra preventivamente.

---

## 3. Principios KISS/YAGNI

1. **PDFium sigue siendo el único motor de edición PDF.**
2. No añadir PdfPig, qpdf, pdfcpu, MuPDF, iText, Ghostscript ni otro motor para F7.
3. No crear `Domain/Application/Infrastructure`, command bus, plugin framework, generic PDF object graph ni nueva DI.
4. No reconstruir párrafos, líneas ni reading order en V1.
5. No OCR en F7.
6. No white rectangles, raster flatten ni overlay que deje el texto original oculto debajo.
7. No depender de fuentes instaladas en Windows para garantizar resultados reproducibles.
8. No descargar fuentes ni componentes en runtime.
9. Trabajar solo con la página activa; no escanear todo el documento al entrar en `EDITAR`.
10. No mantener `IntPtr` o handles PDFium en estado persistente.
11. Candidate-first: los cambios son lógicos hasta `Guardar como...`.
12. `Guardar como...` protege siempre el original.

---

## 4. Referente UX

El patrón de referencia es el de editores PDF comerciales maduros:

- el usuario entra en `EDITAR`;
- hace clic sobre contenido real;
- el objeto queda seleccionado con un contorno;
- aparecen propiedades básicas;
- la edición se aplica al objeto, no a un documento reconstruido como Word;
- si la fuente original no permite una edición segura, el producto debe advertir o usar una alternativa controlada, no fingir compatibilidad.

F7 adopta ese patrón, pero reduce alcance para conservar simplicidad y seguridad.

---

## 5. Alcance UX exacto

### 5.1 Un solo modo `EDITAR`

No se crea un modo `EDITAR TEXTO` aparte.

`EDITAR` contendrá:

- imágenes F6;
- texto F7.

La superficie central y el mismo transform dispositivo↔PDF continúan siendo autoridad.

### 5.2 Selección

Al hacer clic:

1. convertir punto dispositivo → PDF;
2. evaluar objetos editables de la página activa;
3. considerar imágenes F6 y textos F7;
4. si hay solapamiento, seleccionar el objeto editable con mayor `PageObjectIndex` que contenga el punto;
5. no mantener selección si el objeto ya no existe o quedó invalidado.

Esto evita dos sistemas de selección independientes peleando por el mismo clic.

### 5.3 Texto seleccionado

Un texto seleccionado muestra:

- contorno sobre su quad/bounds;
- panel de propiedades `Texto`;
- contenido completo del objeto;
- tamaño en puntos;
- color de relleno;
- nombre de fuente como información;
- indicador de ruta prevista:
  - `Fuente original`, o
  - `Fuente compatible`.

### 5.4 Edición

V1 edita el **contenido completo de un objeto PDF**, no caracteres arbitrarios dentro de una reconstrucción de párrafo.

Controles mínimos:

- `Texto` — `TextBox` multilínea;
- `Tamaño` — valor numérico validado;
- `Color` — paleta WPF simple de colores básicos + valor actual;
- `Aplicar`.

No se añade una dependencia de color picker.

`F2`/`Enter` pueden enfocar el campo de texto después de seleccionar un objeto, pero los atajos no son requisito para cerrar F7.

### 5.5 Estado lógico

`Aplicar` modifica solo el workspace lógico.

El PDF fuente no se toca hasta `Guardar como...`.

Si el usuario deja una edición inválida:

- no se sustituye el último estado válido;
- se muestra error local;
- no se ensucia el workspace con un estado imposible.

---

## 6. Limitaciones deliberadas de Texto V1

F7 **NO** hace:

- reflow de párrafos;
- ajuste automático entre objetos vecinos;
- detección de columnas;
- reading order;
- edición de una palabra reconstruida desde varios objetos;
- edición de texto dentro de imagen/escaneo;
- OCR;
- modificación de contenido dentro de Form XObjects anidados;
- kerning/tracking manual;
- tipografía avanzada;
- fuentes arbitrarias elegidas por el usuario;
- bold/italic synthesis;
- edición de text render modes exóticos;
- texto sobre trayectorias;
- auto-fit horizontal que deforme glifos;
- creación libre de cajas de texto nuevas.

Esos problemas se difieren a F10/F11 o a slices posteriores justificadas.

---

## 7. Qué objeto de texto es editable en V1

F7 enumera únicamente `FPDF_PAGEOBJ_TEXT` **top-level** de la página activa.

Un objeto es candidato editable cuando:

1. es `FPDF_PAGEOBJ_TEXT`;
2. `FPDFTextObj_GetText` devuelve Unicode no vacío de forma válida;
3. se puede leer su matriz;
4. se pueden leer bounds/rotated bounds;
5. se puede leer tamaño de fuente;
6. se puede leer color de relleno;
7. su text render mode está dentro del subconjunto soportado por el Gate F7.1.

Texto dentro de `FPDF_PAGEOBJ_FORM` queda fuera de V1 y debe tratarse como no editable, no como error global.

---

## 8. Gate F7.1 — capacidad real del PDFium pinneado

Antes de implementar UI o writer se debe probar contra **el `pdfium.dll` exacto distribuido por `bblanchon.PDFium.Win32 156.0.8076`**.

La documentación upstream solo orienta; la decisión de producto depende del binario pinneado.

### 8.1 Exports a comprobar

Mínimo:

- `FPDFTextObj_GetText`
- `FPDFTextObj_GetFontSize`
- `FPDFTextObj_GetFontName` o equivalente disponible en el binario
- `FPDFTextObj_GetTextRenderMode`
- `FPDFPageObj_GetFillColor`
- `FPDFPageObj_GetMatrix`
- `FPDFPageObj_GetRotatedBounds`
- `FPDFText_SetText`
- `FPDFText_LoadFont`
- `FPDFFont_Close`
- `FPDFPageObj_CreateTextObj`
- `FPDFPageObj_SetMatrix`
- `FPDFPageObj_SetFillColor`
- `FPDFPage_RemoveObject`
- `FPDFPage_InsertObjectAtIndex`
- `FPDFPage_GenerateContent`
- `FPDF_SaveAsCopy`

### 8.2 Probe funcional, no solo export table

El Gate debe crear/usar un fixture sintético y demostrar:

1. descubrir texto real como page object;
2. leer Unicode exacto;
3. leer fuente/tamaño/color/matriz;
4. `FPDFText_SetText` → `GenerateContent` → save → reopen → texto exacto;
5. cargar una TTF desde bytes locales;
6. crear un nuevo text object;
7. aplicar texto, matriz, tamaño/color necesarios;
8. insertarlo en índice controlado;
9. save → reopen → texto exacto + render válido;
10. cerrar correctamente font/page/document handles.

### 8.3 Regla de decisión

Si falta una API crítica o falla el probe real:

- **STOP F7**;
- actualizar la spec antes de diseñar un fallback;
- no introducir automáticamente un segundo motor.

---

## 9. Modelo de datos F7

Se añade estado específico de texto, sin crear un object graph PDF genérico.

### 9.1 `PdfTextObjectInfo`

Snapshot managed e inmutable del objeto descubierto:

- `PageIndex`
- `PageObjectIndex`
- `Text`
- `Matrix`
- `Bounds`
- `RotatedBounds/Quad`
- `FontName`
- `FontSize`
- `FillColor`
- `TextRenderMode`

No contiene handles nativos.

### 9.2 `TextObjectKey`

Identidad lógica mínima:

```text
(PageIndex, PageObjectIndex)
```

### 9.3 `TextEditState`

Contiene:

- referencia/snapshot original;
- texto actual;
- tamaño actual;
- color actual;
- estrategia de fuente;
- `Deleted` no entra en F7 V1 salvo que evidencia concreta lo justifique durante diseño posterior; por defecto no se ofrece eliminar texto en esta fase.

### 9.4 `TextEditWorkspace`

Responsabilidades:

- estado actual por `TextObjectKey`;
- baseline guardado;
- `IsDirty`;
- candidate-first mutation;
- `MarkSavedBaseline()`.

No duplica fingerprint ni preflight general.

### 9.5 Undo/redo

F7 no introduce un historial global nuevo.

- el `TextBox` conserva su undo local antes de `Aplicar`;
- una vez aplicado al workspace, el cambio de texto V1 no entra aún al undo/redo global de imágenes;
- Ctrl+Z de F6 no debe modificar imágenes mientras el foco está dentro del editor de texto.

Un historial unificado solo se añade cuando haya evidencia de que el comportamiento actual es insuficiente; no forma parte de `TEXT-01..04`.

---

## 10. Promoción de piezas F6 que ahora son generales

Para evitar parche sobre parche, F7 **no** crea clones `TextEditSourceFingerprint`, `TextEditPreflight` y `PdfTextEditWriter` paralelos.

Se promocionan únicamente las piezas que ya son realmente transversales a `EDITAR`.

### 10.1 Fingerprint

`ImageEditSourceFingerprint` → `PdfEditSourceFingerprint`.

Imagen y texto comparten el mismo origen y la misma comprobación stale-source.

### 10.2 Preflight

`ImageEditPreflight` → `PdfEditPreflight`.

La política sigue siendo:

- firma criptográfica → `Block / Unknown`;
- PDF abierto con contraseña → `Block / Unknown`;
- estructuras no criptográficas → clasificación únicamente según evidencia del writer combinado.

No se crean dos matrices de warning simultáneas para el mismo `Guardar como...`.

### 10.3 Writer

`PdfImageEditWriter` evoluciona a `PdfEditWriter`.

Debe contener rutas explícitas:

```text
ApplyImageEdits(...)
ApplyTextEdits(...)
```

No se crea:

- `IPdfWriter`;
- base class;
- strategy framework;
- registry de objetos;
- universal object pipeline.

### 10.4 MainWindow

El partial que hoy inicializa el modo general:

- `MainWindow.EditImages.cs`

se convierte en:

- `MainWindow.Edit.cs`

porque ya no será correcto que el modo `EDITAR` esté nombrado por una sola subfeature.

Los archivos puramente de imágenes pueden conservar nombres específicos:

- `MainWindow.EditImages.Commands.cs`
- `MainWindow.EditImages.ExtractReplace.cs`

El hardening general de salida/dirty-state debe quedar en un único lugar del modo `EDITAR`.

---

## 11. Descubrimiento de texto

Se añade una extensión/partial de `PdfDocumentSession` de lectura de objetos de texto de **una página**.

Flujo:

```text
GetTextObjects(pageIndex)
→ NativeGate
→ LoadPage
→ FPDFText_LoadPage
→ CountObjects
→ por ordinal: GetObject
→ type == TEXT
→ leer snapshot managed
→ CloseTextPage
→ ClosePage
→ Release NativeGate
```

No se devuelve ningún handle.

Cancelación se observa entre objetos.

---

## 12. Hit-testing texto + imágenes

No se construye un motor geométrico nuevo completo.

### 12.1 Texto

Preferencia:

- usar `FPDFPageObj_GetRotatedBounds` para obtener quad;
- punto dentro de quad convexo → candidato.

Si rotated bounds no está disponible en el runtime exacto, F7.1 debe decidir si bounds axis-aligned son aceptables para V1 o si la feature se limita; no se improvisa un cálculo alternativo sin evidencia.

### 12.2 Arbitraje

Crear un helper pequeño del modo EDITAR que reciba candidatos ya descubiertos de imágenes/texto y elija el `PageObjectIndex` superior.

No generalizarlo a todos los tipos PDF.

---

## 13. Estrategia de fuente

El problema central de F7 no es cambiar una cadena: es evitar texto corrupto por subsets/fuentes incompletas.

### 13.1 Estrategia A — fuente original

`OriginalFont` solo se usa cuando la edición es conservadoramente demostrable.

Regla inicial KISS:

- whitespace nuevo puede aceptarse;
- todo code point no-whitespace del texto nuevo debe haber aparecido ya en el texto original del mismo objeto.

Ejemplo:

- `CASA 123` → `CASA 321`: potencial `OriginalFont`;
- `CASA` → `NIÑO`: contiene code points nuevos para ese objeto → fallback.

La regla es intencionalmente conservadora. No intenta adivinar el contenido real del subset.

Si el Gate descubre una API fiable para probar cobertura Unicode del font object, la spec se puede ampliar antes de implementación; no se añade un parser TTF/PDF preventivo.

### 13.2 Estrategia B — TTF fallback

Si aparecen code points nuevos o hay duda:

- reemplazar el objeto por un text object nuevo;
- usar una TTF local, redistribuible y pinneada;
- preservar origen, matriz/rotación, tamaño y color;
- insertar en el mismo `PageObjectIndex` cuando PDFium lo permita;
- no deformar horizontalmente para forzar el texto dentro de los bounds originales.

El nuevo texto puede ocupar más/menos ancho. Eso es comportamiento explícito de V1, no un bug de reflow.

### 13.3 Fuente fallback candidata

**DejaVu Sans** es el candidato inicial porque ya existe como fuente permisiva documentada en el contexto arquitectónico del proyecto.

Pero F7 no la incorpora hasta completar un Gate de procedencia/licencia:

- upstream verificable;
- licencia redistribuible guardada en `third_party/licenses`;
- versión/fuente/hash registrado;
- cobertura mínima de español/Latin-1 demostrada;
- sin descarga runtime.

Si ese Gate falla, escoger otra fuente permisiva antes de escribir código de producto dependiente de ella.

### 13.4 No usar fuentes del sistema como fallback principal

La presencia de Arial/Calibri/etc. puede variar por máquina y no da un output reproducible.

---

## 14. Text render mode

F7 V1 empieza conservador.

Solo se habilita edición para los render modes que el Gate pruebe con preservación suficiente.

Default recomendado:

- permitir `FILL`;
- seleccionar pero mostrar read-only para stroke/clip/exóticos hasta tener una prueba específica.

No se cambia silenciosamente un render mode complejo a fill simple.

---

## 15. Materialización unificada

`Guardar como...` debe abrir el fuente **una sola vez** y aplicar todas las ediciones pendientes.

Flujo:

```text
source fingerprint
→ signature/password/preflight
→ reopen original
→ por página editada
    resolve all referenced objects before destructive mutation
    apply image edits
    apply text edits
    GenerateContent once for that page
→ save temp
→ reopen + validate
→ atomic publish
→ cleanup
→ MarkSavedBaseline() de ambos workspaces
```

No ejecutar primero `PdfImageEditWriter` y después otro writer sobre su output.

Eso evita pérdida acumulativa, resolución por índices obsoletos y dos ciclos de reescritura.

---

## 16. Resolución segura de objetos antes de mutar

Como F6, F7 no confía ciegamente en el ordinal después de modificar la página.

Por página:

1. tomar todos los estados editados de imagen y texto;
2. resolver cada objeto original antes de remover/reinsertar cualquiera;
3. validar tipo + snapshot mínimo esperado;
4. conservar handles solo dentro de la sección nativa;
5. aplicar mutaciones;
6. destruir únicamente handles cuya ownership haya pasado a la app tras removerlos;
7. `GenerateContent` una vez.

La resolución debe detectar stale/objeto distinto y abortar transaccionalmente.

---

## 17. Materialización de texto — ruta A

Para `OriginalFont`:

1. resolver text object original;
2. validar tipo/texto/matriz dentro de tolerancias definidas;
3. `FPDFText_SetText`;
4. aplicar tamaño/color solo si fueron modificados y el runtime exacto ofrece setter fiable;
5. mantener matriz original salvo cambio soportado explícitamente;
6. `GenerateContent` al final de la página.

### Setter de tamaño

No se asume una API upstream moderna.

El Gate F7.1 debe comprobar el setter exacto disponible en `156.0.8076`.

Si no existe setter seguro:

- cambiar tamaño fuerza ruta de recreación con TTF fallback;
- no hackear matriz para simular `font size` si eso cambia semántica de forma difícil de validar.

---

## 18. Materialización de texto — ruta B

Para `FallbackTtf`:

1. resolver objeto original;
2. cargar bytes de la TTF pinneada en memoria managed;
3. `FPDFText_LoadFont(document, ...)`;
4. `FPDFPageObj_CreateTextObj(document, font, fontSize)`;
5. `FPDFText_SetText(newObject, ...)`;
6. copiar fill color;
7. copiar matrix/origen/rotación;
8. retirar objeto original;
9. insertar objeto nuevo en el mismo índice;
10. destruir original según reglas de ownership PDFium;
11. cerrar font handle cuando corresponda;
12. `GenerateContent` al final de la página.

Si cualquier paso falla, el destino final no se publica.

---

## 19. Tamaño, posición y ancho

F7 conserva:

- punto/origen;
- matriz;
- rotación;
- tamaño de fuente solicitado;
- color.

No promete conservar ancho exacto cuando cambia el texto o la fuente.

No se aplica:

- horizontal squeeze;
- auto-fit;
- reflow;
- reducción silenciosa de tamaño.

La selección/overlay se refresca usando bounds reales del estado materializado solo después de reabrir; durante edición lógica puede usar estimación basada en el snapshot actual.

---

## 20. Propiedades básicas

`TEXT-04` incluye únicamente:

- contenido;
- font size;
- fill color;
- información de font name/estrategia;
- matriz/posición preservada, no editable manualmente en V1.

No se incluye selector completo de familias tipográficas.

---

## 21. Dirty-state y salida

`EDITAR` está dirty cuando:

```text
imageWorkspace.IsDirty || textWorkspace.IsDirty
```

Los mismos guards cubren:

- LEER;
- FIRMAR;
- ORGANIZAR;
- abrir PDF;
- abrir ZPL;
- Ctrl+O;
- cerrar ventana.

No se añaden handlers paralelos de `Loaded` o una segunda cadena de guards.

KISS-A de F6 debe permanecer intacto.

---

## 22. Save As UI

Se conserva **un solo** `Guardar como...` del modo `EDITAR`.

No aparece `Guardar imágenes` / `Guardar texto` por separado.

Disponibilidad:

- visible mientras `EDITAR` está activo;
- deshabilitado mientras materializa;
- no requiere selección activa;
- cancelación de diálogo no modifica baseline;
- Block/decline no invocan writer;
- éxito marca baseline de imágenes y texto;
- fallo conserva dirty-state e historial existente.

---

## 23. Preflight y preservación F7

Aunque F6 probó preservación, F7 modifica page content de forma distinta.

Por tanto se crea una **matriz F7 independiente usando el writer combinado real**.

Fixture representativo debe medir:

- forms;
- bookmarks;
- named destinations;
- internal links;
- tagged structure;
- page labels;
- attachments;
- representative metadata values.

Clasificación exacta:

- `ProvenPreserved`
- `ProvenChangedOrLost`
- `Unknown`

Política:

- firma criptográfica → Block / Unknown;
- password-opened → Block / Unknown;
- preserved no-crypto → Info;
- changed/lost → Warning + confirmación;
- unknown → Warning + confirmación.

No copiar ciegamente la matriz F6.

---

## 24. Validación de salida

La validación F7 no se limita a “el PDF abre”.

Para cada text edit materializado:

1. abrir output;
2. cargar página;
3. resolver objeto esperado;
4. confirmar type `TEXT`;
5. extraer Unicode y comparar exactamente con texto solicitado;
6. comparar matriz dentro de tolerancia;
7. comparar tamaño esperado dentro de tolerancia;
8. comparar color;
9. verificar estrategia/font name cuando aplique fallback;
10. renderizar la página editada y verificar bitmap válido/no vacío.

Para imágenes continúan las validaciones F6.

Solo después se publica atómicamente el destino.

---

## 25. Unicode

Internamente el modelo usa `.NET string` y enumeración por `Rune` para la decisión de code points.

No iterar cobertura mediante `char` individual para decisiones de fuente.

Primer alcance de fallback:

- ASCII;
- español;
- Latin-1/Latin Extended cubierto por la fuente elegida.

Emoji/CJK/RTL quedan fuera de la promesa F7 salvo que el Gate de fuente los demuestre y la implementación siga siendo KISS.

Si el usuario introduce contenido fuera de cobertura demostrada:

- impedir `Aplicar` con mensaje claro;
- no generar glifos faltantes silenciosos.

---

## 26. PDFs complejos

### 26.1 Firma criptográfica

Block, igual que F6.

### 26.2 PDF abierto con contraseña

Block para materialización, igual que F6.

### 26.3 Texto dentro de Form XObject

No editable en V1.

### 26.4 Texto con render mode complejo

Read-only si no está probado.

### 26.5 Fuente no identificable

Fallback TTF si el objeto y texto sí pueden resolverse de forma segura; si no, read-only.

### 26.6 Texto vacío/artefacto

No presentar como objeto editable útil.

---

## 27. Rendimiento

Al entrar en `EDITAR`:

- render de página activa ya existente;
- enumerar imágenes de página activa;
- enumerar texto de página activa;
- crear snapshots managed;
- ningún scan de páginas no visibles.

Objetivo: complejidad O(objetos de página activa), no O(documento completo).

No cachear bitmaps/text handles nativos fuera de la operación.

---

## 28. Threading PDFium

Toda llamada PDFium sigue usando:

```csharp
PdfiumRuntime.NativeGate.Wait(...);
try
{
    // native calls
}
finally
{
    PdfiumRuntime.NativeGate.Release();
}
```

No `lock(NativeGate)`.

No reacquirir el mismo semaphore dentro de una sección nativa.

---

## 29. Font asset y memoria

La TTF fallback se carga desde un recurso local pinneado.

Reglas:

- archivo con límite de tamaño explícito antes de `ReadAllBytes`;
- bytes managed;
- PDFium copia los datos según su contrato de `FPDFText_LoadFont`;
- no mantener pointer a bytes managed después de la llamada;
- font handle no sobrevive al writer;
- no recargar la TTF una vez por objeto si varios edits del mismo documento pueden reutilizar el mismo font handle durante una materialización.

KISS: un handle de fallback por documento/materialización cuando se necesite.

---

## 30. Licencia/provenance Gate de fuente

Antes de añadir el archivo TTF al repo/runtime:

1. identificar upstream oficial;
2. registrar versión/commit/release;
3. calcular SHA-256;
4. guardar licencia/notices;
5. actualizar `third_party/manifest.json`;
6. verificar que redistribución binaria está permitida;
7. verificar que el instalador futuro pueda incluirla;
8. comprobar cobertura de glifos mínima con test automatizado.

Ninguna fuente se incorpora solo porque “está instalada” en la máquina del desarrollador.

---

## 31. Seguridad y archivos

Se conservan las reglas F6:

- destination != source;
- destino en directorio existente;
- source fingerprint obligatorio;
- temp sibling del destino;
- reopen/validate antes de publish;
- replace/move atómico según existencia;
- cleanup en `finally`;
- cancelación antes/después de etapas costosas;
- fallo nunca marca baseline;
- source nunca se modifica.

---

## 32. Fixtures F7

Corpus sintético mínimo versionable:

1. texto Helvetica/standard simple;
2. texto con rotación;
3. texto solapado con imagen para hit-test/z-order;
4. dos text objects visualmente contiguos para demostrar que V1 no los fusiona;
5. texto con caracteres españoles;
6. subset/edición con code point nuevo que fuerza fallback;
7. texto con render mode no soportado → read-only;
8. página con form XObject conteniendo texto → fuera de alcance;
9. documento con estructuras de preservación;
10. documento con firma/password para Blocks mediante fixtures ya disponibles cuando sea viable.

No introducir PDFs privados.

---

## 33. QA automatizada mínima

### Gate/API

- exports exactos;
- probe in-place;
- probe fallback TTF;
- ownership/cleanup.

### Discovery

- top-level text only;
- Unicode;
- matrix/bounds/quad;
- size/color/font;
- cancelación;
- active-page-only.

### Hit testing

- normal;
- rotado;
- overlap texto/texto;
- overlap texto/imagen;
- topmost por `PageObjectIndex`.

### Workspace

- candidate-first;
- dirty baseline;
- code-point strategy;
- tamaño/color validation;
- no native handles.

### Writer

- original-font route;
- fallback route;
- same-index replacement;
- image+text in same page/save;
- multi-page edits;
- stale source;
- Block signature/password;
- cancel/temp cleanup;
- publication failure;
- output validation.

### Preservation

- matriz F7 independiente.

### UI

- selección text/image;
- properties panel;
- Apply valid/invalid;
- dirty guards;
- Save As one writer call;
- baseline only after success;
- no duplicate Loaded hooks.

### Regression

- LEER;
- FIRMAR;
- ORGANIZAR;
- ZPL;
- F6 imágenes;
- offline guard.

---

## 34. QA manual Windows requerida pero separada

Debe quedar `NOT RUN` hasta ejecutarla realmente.

Checklist final F7 manual:

- selección de textos a varios zooms;
- texto rotado;
- texto encima/debajo de imágenes;
- editar nombres/direcciones/números;
- ñ/á/é/í/ó/ú/ü;
- cambio tamaño/color;
- fallback visible y advertencia clara;
- Save As/cancel/failure dialogs;
- output abierto en SG PDF Editor y lector externo;
- documento pesado;
- cambios mixtos imagen + texto;
- salir a LEER/FIRMAR/ORGANIZAR/ZPL con dirty state;
- red físicamente deshabilitada.

Automated PASS no implica este PASS.

---

## 35. Dependencias

Esperado para F7:

- **ningún paquete NuGet nuevo**;
- mismo PDFium pinneado;
- un único asset TTF redistribuible solo después del Gate legal/provenance.

Si aparece necesidad de un paquete para parsear fuentes, primero se debe demostrar que el enfoque conservador actual no basta y actualizar la spec.

---

## 36. Estructura de código prevista

Sin fijar todos los nombres del plan, la estructura objetivo es pequeña:

```text
Features/Edit/Text/
  PdfTextObjectInfo.cs
  TextEditState.cs
  TextEditWorkspace.cs
  TextEditPolicy.cs

Pdf/
  PdfDocumentSession.TextObjects.cs
  PdfEditPreflight.cs
  PdfEditWriter.cs
  PdfEditOutputValidator.cs

MainWindow.Edit.cs
MainWindow.Edit.Text.cs
MainWindow.Edit.Hardening.cs
```

Archivos F6 puramente de imágenes siguen separados.

No crear `Features/Edit/Common/` salvo que durante implementación exista al menos una pieza realmente compartida que no tenga ya hogar claro.

---

## 37. Migraciones/renombres permitidos

Para evitar deuda nominal y duplicación, F7 puede hacer refactor behavior-preserving de:

- `ImageEditSourceFingerprint` → `PdfEditSourceFingerprint`;
- `ImageEditPreflight` → `PdfEditPreflight`;
- `PdfImageEditWriter` → `PdfEditWriter`;
- `ImageEditOutputValidator` → `PdfEditOutputValidator`;
- `MainWindow.EditImages.cs` → `MainWindow.Edit.cs`;
- `MainWindow.EditImages.Hardening.cs` → `MainWindow.Edit.Hardening.cs`.

Cada rename debe tener tests verdes antes/después y no debe introducir abstracciones nuevas.

---

## 38. Qué NO se debe mergear accidentalmente

F7 no autoriza:

- merge de PR #25/F6;
- merge a `main`;
- cierre de QA manual pendiente;
- modificación de F5/F6 history para “hacerlo ver terminado”;
- introducir fuentes sin licencia registrada;
- borrar checkpoints históricos;
- reescribir commits previos.

F7 se apila sobre el head F6 cerrado y mantiene gobernanza existente.

---

## 39. Criterio de cierre automatizado F7

F7 puede declararse `AUTOMATED CLOSURE PASS` solo cuando:

1. Gate F7.1 exact-runtime PASS;
2. Gate de fuente/provenance PASS;
3. `TEXT-01..04` tienen evidencia automatizada;
4. writer combinado imagen+texto es transaccional;
5. matriz de preservación F7 está medida, no asumida;
6. Release build 0 warnings / 0 errors;
7. suite completa PASS;
8. exact-head Windows CI PASS;
9. PR apilado draft/open/unmerged;
10. `main` intacto;
11. QA manual está declarada honestamente como PASS o NOT RUN.

---

## 40. Secuencia conceptual aprobable

Si esta spec es aprobada, el plan TDD deberá seguir aproximadamente:

```text
Gate F7.1 exact PDFium
→ Gate fuente/provenance
→ discovery snapshots managed
→ text hit-test + arbitraje con imágenes
→ TextEditWorkspace/policy
→ UI properties candidate-first
→ promoción KISS de fingerprint/preflight/writer general
→ writer ruta original-font
→ writer fallback TTF
→ Save As unificado image+text
→ independent preservation matrix
→ hardening/performance/regressions
→ closure docs + exact-head CI + stacked draft PR
```

La implementación real debe descomponer esto en tareas pequeñas RED → GREEN; esta sección no sustituye el plan TDD escrito.

---

## 41. Decisiones congeladas al aprobar esta spec

1. F7 edita objetos de texto reales, no overlays.
2. F7 no intenta reflow.
3. Solo top-level text objects en V1.
4. Imágenes y texto comparten un único modo `EDITAR`.
5. Hit-test mixto resuelve topmost por object index.
6. Estado persistente managed; cero handles nativos.
7. Fuente original solo bajo política conservadora.
8. Code point nuevo/duda → TTF fallback.
9. Una sola fuente fallback pinneada inicialmente.
10. No fuentes del sistema como garantía de salida.
11. Writer combinado abre/materializa una sola vez.
12. Preservación F7 se vuelve a medir.
13. Firmas criptográficas/password continúan Block.
14. No nuevo NuGet esperado.
15. No merge automático.

---

## 42. Riesgos residuales explícitos

### R1 — PDFs fragmentan texto en muchos objetos

V1 puede sentirse granular. Se acepta para evitar reconstrucción de layout prematura.

### R2 — fallback cambia métricas visuales

Se conserva origen/tamaño/matriz, pero no se promete ancho idéntico.

### R3 — subset font original

La política deliberadamente conservadora puede usar fallback más veces de las estrictamente necesarias.

### R4 — text dentro de Form XObjects

No editable en V1.

### R5 — APIs PDFium experimentales/versionadas

El Gate exact-runtime es obligatorio antes de product code.

### R6 — preserving document-level structures

No se asume desde F6; se mide de nuevo con el writer combinado.

---

## 43. Definition of Done de diseño

Esta spec está lista para pasar a planificación solo si el usuario confirma explícitamente que acepta:

- alcance conservador por objeto;
- no reflow;
- top-level text only;
- fallback TTF controlado;
- cambio potencial de métricas cuando entra fallback;
- writer unificado para imágenes + texto;
- refactor/rename KISS de piezas F6 que ahora son generales;
- QA manual separada de CI.

**Sin esa aprobación escrita/conversacional de la spec, no se escribe el plan TDD y no se implementa F7.**
