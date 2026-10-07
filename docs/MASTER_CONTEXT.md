# SG PDF Editor — Contexto maestro

**Última consolidación:** 2026-10-07  
**Estado:** A0/A1 completadas; F0 automatizado PASS con smoke físico pendiente; Gate ZPL-A sintético seleccionó Labelize 1.7.0; F2.1 Parse + Open en cierre automatizado.  
**Repositorio:** `sierraglobalcompany-rgb/SG-PDF-Editor`

> Este documento permite abrir otro chat/agente y continuar sin reconstruir el proyecto. GitHub actual tiene prioridad para el estado ejecutado; `.planning/STATE.md` es el estado operativo; este archivo y `docs/MASTER_PLAN.md` son la autoridad arquitectónica salvo cambio explícitamente aprobado.

## 1. Producto

SG PDF Editor es una aplicación Windows local-first y offline para dos trabajos diarios:

1. **PDF:** leer, imprimir, firmar visualmente, organizar y editar de forma práctica.
2. **Etiquetas térmicas:** abrir ZPL/TXT/PRN, previsualizar, respetar cantidades, maquetar, convertir a PDF e imprimir sin Internet.

No pretende iniciar como clon completo de Acrobat. Se construye por vertical slices útiles y verificables.

## 2. Requisitos no negociables

- Windows x64 inicialmente.
- C# + .NET 10 LTS + WPF.
- KISS + YAGNI.
- Funciones principales 100 % offline.
- Sin API keys, SaaS, cuentas, servidores, créditos ni activación obligatoria.
- Sin licencia comercial obligatoria en runtime.
- Priorizar MIT/BSD/Apache-2.0.
- No introducir AGPL/GPL fuerte sin aprobación expresa.
- `Guardar como` por defecto durante primeras fases de edición.
- No hacer merge automático a `main`.
- Datos reales de Mercado Libre/clientes son privados y no se versionan.
- Ningún corpus privado se adjunta a CI ni se indexa en Graphify.

## 3. Arquitectura runtime aprobada

```text
SG PDF Editor
│
├─ WPF + .NET 10
│
├─ PDFium
│  ├─ lectura/render
│  ├─ texto/objetos
│  ├─ firma visual
│  ├─ edición práctica
│  └─ organización de páginas primero
│
├─ Labels (managed C#)
│  ├─ ZplDocumentParser
│  ├─ cantidades ^PQ como metadata
│  ├─ layout/orquestación
│  └─ LabelizeProcessRenderer (desde F2.2)
│       ↓ child process local
│     labelize.exe 1.7.0
│
├─ PDFsharp
│  └─ solo si F2.4 demuestra que sigue siendo la vía KISS para composición de hojas
│
└─ Windows Printing
```

### Decisiones explícitas

- **Labelize 1.7.0** es el motor ZPL aprobado tras Gate ZPL-A sintético.
- BinaryKits.Zpl no queda como fallback runtime ni se construye una abstracción dual-engine.
- Labelize se integra como ejecutable local sidecar pinneado, no como `labelize serve`, no mediante HTTP localhost y no mediante C ABI Rust/WASM.
- El runtime final nunca descarga Labelize automáticamente.
- PDFsharp no entra preventivamente: se añade solo en la slice de layout/export si sigue siendo necesario.

No forman parte del núcleo actual:

- qpdf/pdfcpu: solo ante carencia concreta de PDFium;
- Tesseract: F10 OCR;
- PdfPig: solo texto/layout avanzado si reduce complejidad;
- BinaryKits: solo evidencia histórica del Gate;
- MuPDF/iText/Ghostscript: descartados por AGPL/comercial.

## 4. Solución KISS

Mientras sea suficiente existen solo:

```text
src/SGPdf.App/
tests/SGPdf.App.Tests/
```

No recrear `Core`, `Infrastructure`, `Domain`, `Application`, CQRS, event bus, plugin framework o DI complejo sin necesidad real.

Feature structure permitida cuando aporte claridad:

```text
Pdf/
Navigation/
Printing/
Features/
  Labels/
  Reader/
  Sign/
  Edit/
  Organize/
  Comments/
Utilities/
```

La integración WPF de etiquetas de F2.1 usa `MainWindow.Labels.cs` como partial de la ventana existente. No crea ViewModel, mode service ni framework de navegación.

## 5. PDFium

PDFium es el motor PDF primario y se distribuye mediante `bblanchon.PDFium.Win32` con versión explícita/pinneada.

Se prefiere P/Invoke mínimo propio en vez de wrapper grande. Cada API nativa entra cuando una feature la necesita.

### Thread safety

PDFium se trata como no thread-safe dentro del proceso. Todas las llamadas pasan por exclusión global:

```text
WPF UI
  ↓
background task
  ↓
PdfRenderScheduler
  ↓
SemaphoreSlim(1,1)
  ↓
PDFium
```

F0.4 usa latest-request-wins + cancelación cooperativa alrededor del render síncrono. Progressive rendering (`FPDF_RenderPageBitmap_Start/Continue/Close`) queda diferido hasta que un PDF real demuestre que hace falta.

## 6. PDF base implementado

F0.1–F0.6 están automatizadamente PASS:

- abrir PDF local;
- render PDFium real;
- anterior/siguiente/ir a página;
- zoom presets / 100 % / Fit Page / Fit Width;
- refit con debounce 150 ms;
- scheduler latest-request-wins;
- impresión Windows all/current/range mediante `PrintDialog` + `DocumentPaginator`;
- Microsoft Print to PDF por la misma ruta;
- multipágina e invalid-PDF regressions;
- guard offline automatizado.

### Pendientes físicos F0

El entorno actual no tiene escritorio Windows/impresora interactiva. Siguen NOT RUN:

- UI con PDFs reales/heavy;
- navegación/zoom/resize rápido;
- Microsoft Print to PDF + reopen;
- impresora física si está disponible;
- 200 DPI print quality/memory/latency;
- red deshabilitada durante flujo PDF.

No marcar `PDF-BASE-01..07` como aceptados físicamente hasta ese smoke.

## 7. UX PDF

Un PDF abre en `Leer` para evitar modificaciones accidentales.

Modos finales previstos:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

### Leer

Objetivo final: abrir, navegación, zoom, fit page/width, scroll continuo, thumbnails, bookmarks, búsqueda, copiar texto, links, password e impresión.

### Firmar

Alta prioridad. MVP: PNG transparente, drag, resize proporcional, mover, eliminar/duplicar, insertar y `Guardar como`. Después: biblioteca local, firma principal, iniciales, fecha/nombre y sellos.

Firma visual y firma criptográfica son funciones distintas. La firma criptográfica es profesional/posterior.

### Editar imágenes

Menú contextual previsto: reemplazar, extraer/guardar, copiar, recortar, girar, tamaño, opacidad, frente/fondo, propiedades y eliminar. Reemplazo intenta conservar posición/tamaño/rotación/orden.

### Editar texto

No tratar PDF como Word. Texto V1 es conservador; reflow/párrafos llegan después.

APIs relevantes: `FPDFText_SetText`, `FPDFTextObj_GetFontName`, `FPDFText_LoadFont`, `FPDFText_LoadStandardFont`, `FPDFPageObj_CreateTextObj`.

Si aparecen nuevos code points o subset dudoso, usar fallback con TTF redistribuible auditada y nuevo text object. Guardar copia, reabrir y validar.

## 8. Organizar PDF

PDFium se intenta primero.

Intra-documento: mover/reordenar, rotar, eliminar, duplicar cuando aplique.

Inter-documento: `FPDF_ImportPages`/`FPDF_ImportPagesByIndex` para insert/merge/extract cuando sea viable.

Preflight previsto:

- firmas: `FPDF_GetSignatureCount`;
- formularios: `FPDF_GetFormType`;
- bookmarks: `FPDFBookmark_GetFirstChild(document, NULL)`;
- named destinations, links internos, tagged PDF, page labels, attachments y metadata según corpus.

No prometer preservación de estructuras document-level que no se hayan probado.

## 9. ZPL / Mercado Libre

El usuario recibe ZPL de Mercado Libre y hoy usa Labelary manualmente antes de imprimir en una impresora térmica que acepta PDF pero no esos ZPL directamente.

SG PDF Editor debe reemplazar ese proceso totalmente offline.

Entradas:

- `.zpl`;
- `.txt` con ZPL;
- `.prn` con ZPL.

Bloques principales: `^XA ... ^XZ`.

`^PQ28` significa un diseño con 28 copias, no 28 renders. Se renderiza el diseño una vez y la cantidad se aplica posteriormente al layout/print.

Layouts previstos: 1/2/3/4/6/8/10/12/custom. Tamaños: térmicos, A4, Carta y custom.

## 10. Gate ZPL-A — resultado

Se compararon:

- `BinaryKits/BinaryKits.Zpl` Viewer 1.3.1;
- `GOODBOY008/labelize` 1.7.0 Windows x64 CLI.

Spike throwaway:

- branch `spike/f1-zpl-gate-a`;
- head final `2df2f374ae6124729389640425dc8334d0647f9f`;
- run final `37577735748` success.

Corpus sintético validó:

- `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF`, `^XF`;
- tildes/ñ;
- direcciones largas;
- etiqueta compuesta;
- riesgo `^FT + ^BQ`.

### Resultado medido

- ambos motores produjeron Code128/QR decodificables tras corregir el comparador para componer el fondo transparente de BinaryKits sobre blanco;
- Labelize ~59–61 diseños/s;
- BinaryKits ~26–28 diseños/s;
- Labelize exe 5,860,352 bytes;
- BinaryKits probe framework-dependent publish ~16.9 MB / 14 archivos;
- en `^FT + ^BQ`, BinaryKits renderizó el QR 60 px más abajo que Labelize, aunque ambos decodificaron.

### Decisión

Labelize 1.7.0 es el motor oficial planeado para F2.

El Gate sintético es suficiente para seleccionar arquitectura. **F1 formal todavía requiere ejecutar corpus real privado de Mercado Libre.** Esos archivos nunca se versionan.

## 11. Labelize runtime design

Pin aprobado: **Labelize 1.7.0 Windows x64 MSVC**.

SHA-256 del archive verificado en el spike:

`cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`

Pipeline desde F2.2:

```text
ZPL local
  ↓
ZplDocumentParser / normalized document source
  ↓
LabelizeProcessRenderer
  ↓
labelize.exe convert --width ... --height ... --dpmm ...
  ↓
PNG/PDF en temp request-scoped
  ↓
cargar/copiar output
  ↓
cleanup temp
```

`LabelizeProcessRenderer` deberá:

- resolver el exe desde `AppContext.BaseDirectory`/subdirectorio fijo de app;
- usar `ProcessStartInfo.ArgumentList`;
- no concatenar shell commands;
- escribir ZPL temporal UTF-8 sin BOM;
- capturar stdout/stderr;
- soportar cancelación matando process tree;
- timeout acotado;
- validar outputs existentes/no vacíos;
- limpiar temp en `finally`;
- no usar red ni `labelize serve`.

No auto-update. Upgrade de Labelize exige nueva auditoría + regresiones representativas.

## 12. F2.1 — Parse + Open

Rama: `feat/f2-1-zpl-parse-open`.  
PR: #13 draft, base `design/f2-labelize-architecture`, no merge.

Implementado:

- `ZplDesign` inmutable;
- `ZplDocument` con `TotalQuantityFromFile: long`;
- parser managed de bloques completos `^XA...^XZ`;
- rechazo controlado de boundaries rotos/nested;
- `^DF` tratado como support block y `^XF` preservado;
- `NormalizedRenderSource` document-level;
- extracción de `^PQ` como cantidad;
- `^PQ0`/ausente => 1;
- múltiples `^PQ` => último gana;
- max 99,999,999;
- malformed/out-of-range => error controlado;
- `^PQ` dentro de `^DF` se rechaza en F2.1 para no inventar semántica;
- strict UTF-8 loader, BOM UTF-8 permitido;
- solo `.zpl/.txt/.prn`;
- WPF `Abrir etiquetas ZPL...`;
- candidate-first state swap: invalid ZPL conserva workspace válido anterior;
- PDF ↔ ZPL swap sin tabs/modos/servicios nuevos;
- regresiones WPF ejecutadas en STA dentro de CI.

F2.1 **no** contiene Labelize, PDFsharp, ZXing ni nuevos paquetes runtime.

## 13. Slices F2 siguientes

### F2.2 — Labelize adapter + preview

Pinned exe, process runner, PNG preview, cancellation, timeout, temp cleanup, `^DF/^XF`, Code128/QR y `^FT + ^BQ` regressions.

### F2.3 — Quantity UX + dimensiones

Cantidad archivo/una/custom, presets térmicos/custom, dpmm y rerender por tamaño físico.

### F2.4 — Layout + PDF

Layouts 1/2/3/4/6/8/10/12/custom; térmico/A4/Carta/custom; PDFsharp solo si sigue siendo el camino mínimo fiable.

### F2.5 — Thermal print

Tamaño físico exacto, capabilities/imageable-area, no shrink silencioso, Windows driver.

### F2.6 — Validation + hardening

ZXing/decoder validation, corpus real privado, printer/scanner físico, offline/temp/privacy audit.

## 14. Impresión térmica

Mantener tamaño físico exacto. No restar automáticamente hardware margins al PDF ni escalar silenciosamente un barcode.

Consultar `PrintQueue.GetPrintCapabilities()`; cuando el driver lo permita usar PageMediaSize exacto, scaling 100 %, una página por hoja y resolución nativa. `PageImageableArea` sirve para advertir recortes.

Si un driver destruye la escala, primer fallback: raster directo a DPI nativo mediante el driver Windows, no RAW genérico a un lenguaje desconocido.

QA ZPL final: decode automático + impresión/scanner físico.

## 15. Licencias y terceros

Código propio: `All rights reserved` hasta decisión distinta. Repo público NO significa open source.

Toda dependencia runtime/dev relevante se registra en `third_party/manifest.json` y conserva licencias/notices requeridos.

Labelize 1.7.0 es MIT. Roboto Condensed usada como substitute es Apache-2.0; DejaVu es permisiva. `ZplGSCustom.ttf` se documenta como heredada de zebrash/MIT pero con origen exacto imperfectamente trazado. Tratamiento:

- aceptable para desarrollo y selección del Gate;
- registrar provenance note al entrar al runtime en F2.2;
- re-auditar antes de instalador público;
- si la confianza legal no es suficiente, redibujar ese pequeño set de glifos en vez de cambiar de motor sin evidencia.

## 16. Development Intelligence — A1 completada

GSD Core + Graphify son tooling de desarrollo; nunca requisitos del producto.

### GSD Core

- upstream `open-gsd/gsd-core`;
- pin `1.15.0`;
- MIT;
- Node 24+;
- project-scoped;
- `.planning/` versionado;
- `.codex/` generado/ignorado.

### Graphify

- upstream `Graphify-Labs/graphify`;
- pin `0.9.77`;
- Apache-2.0;
- grafo `src/` + `tests/`;
- `tests/PrivateFixtures/` excluido;
- `graphify-out/` regenerable;
- auto-update OFF;
- AST/query offline sin backend LLM/API.

Probe final A1 `37530638793` success.

Setup reproducible: `tools/setup-dev.ps1`.

Jerarquía de contexto:

```text
Arquitectura → docs/MASTER_CONTEXT.md + docs/MASTER_PLAN.md
Estado       → .planning/STATE.md
Roadmap      → .planning/ROADMAP.md
Requisitos   → .planning/REQUIREMENTS.md
Fase         → .planning/phases/<fase>/...
Código       → Git/GitHub
Relaciones   → Graphify
Histórico    → docs/history
```

## 17. Roadmap actual

```text
A0  Higiene/fuente de verdad               ✅ completada
A1  GSD Core + Graphify                    ✅ completada
F0  PDF base                               ✅ automatizado / ⏳ físico
F1  Gate ZPL-A                             ✅ motor elegido / ⏳ corpus privado
F2  Etiquetas ZPL                          ▶ F2.1 en cierre automatizado
F3  Firma visual
F4  Lector completo
F5  Organizar
F6  Imágenes
F7  Texto V1
F8  Comentarios
F9  Utilidades
F10 OCR
F11 Texto V2
F12 Profesional
```

## 18. GitHub / ramas

- `main`: baseline; no tocar/mergear sin aprobación explícita del usuario.
- A0: `feat/kiss-vertical-slice`, PR #2 draft.
- A1: `feat/a1-dev-intelligence`, PR #6 draft.
- F0: PRs #7–#12 draft, stacked, sin merge.
- F1 spike: `spike/f1-zpl-gate-a`, throwaway evidence.
- diseño Labelize: `design/f2-labelize-architecture`, spec + plan aprobados.
- F2.1: `feat/f2-1-zpl-parse-open`, PR #13 draft.
- una rama/vertical slice cuando sea razonable;
- build/tests/QA antes de declarar terminado;
- nunca auto-merge.

## 19. Datos privados

ZPL reales/documentos de clientes no entran al repo.

`tests/PrivateFixtures/` permanece local/ignorado. Graphify no debe indexarlo. CI artifacts tampoco pueden contener customer labels.

Corpus sintético versionable debe usar nombres/direcciones/datos inventados.

## 20. Roles

**ChatGPT:** planificación aplicada, código, GitHub, CI, tests, auditoría, documentación e históricos.

**Codex/Antigravity:** ejecución/runtime/UX Windows, native debugging, impresión y trabajo de código cuando se use el host correspondiente.

**Usuario:** prioridad/UX, corpus real privado, pruebas físicas e instrucción final de merge.

**Otra IA:** auditor puntual, no arquitecto paralelo continuo.

## 21. Definition of Done

Una feature termina solo si:

- funciona offline;
- build/tests Windows tienen evidencia fresca;
- errores controlados;
- original/estado válido previo protegido;
- pruebas/fixtures representativos;
- dependencias/licencias registradas cuando aplican;
- temporales/recursos limpios;
- docs/STATE/histórico alineados.

ZPL además exige:

- `^PQ` como metadata, no renders duplicados;
- decode automático cuando entra el renderer;
- corpus real privado antes del cierre formal F1;
- tamaño/códigos verificados físicamente antes de cerrar F2.

## 22. Instrucción para un chat nuevo

1. Verificar GitHub primero.
2. Leer `.planning/STATE.md`.
3. Leer el plan de la slice activa y `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md` si se toca ZPL.
4. Usar Graphify antes de lecturas amplias cuando aporte valor.
5. Leer MASTER docs completos solo ante arquitectura/licencias/cambio de fase/contradicción.
6. No reabrir A0/A1 salvo evidencia material.
7. No declarar F0 físicamente cerrado sin smoke Windows.
8. No declarar F1 formalmente cerrado sin corpus real privado.
9. F2.2 debe usar Labelize 1.7.0 sidecar local; no reintroducir BinaryKits ni servicio HTTP sin nueva evidencia/aprobación.
10. No merge a `main` sin aprobación explícita del usuario.
