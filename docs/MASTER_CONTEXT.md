# SG PDF Editor — Contexto maestro

**Última consolidación:** 2026-10-06  
**Estado:** arquitectura auditada/congelada; A0 y A1 completadas; F0 PDF Base es la siguiente fase.  
**Repositorio:** `sierraglobalcompany-rgb/SG-PDF-Editor`  
**A0:** `feat/kiss-vertical-slice`, PR #2 draft, sin merge.  
**A1:** `feat/a1-dev-intelligence`, PR #6 draft, sin merge.

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

## 3. Arquitectura runtime congelada

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
├─ BinaryKits.Zpl *
│  ├─ preview ZPL
│  └─ PDF ZPL vía Skia
│
├─ PDFsharp
│  └─ composición puntual de hojas de etiquetas
│
└─ Windows Printing
```

`*` BinaryKits es candidato preferente y debe ganar Gate ZPL-A contra Labelize.

No forman parte del núcleo actual:

- qpdf/pdfcpu: solo ante carencia concreta de PDFium;
- Tesseract: F10 OCR;
- PdfPig: solo texto/layout avanzado si reduce complejidad;
- Labelize: fallback ZPL condicionado;
- MuPDF/iText/Ghostscript: descartados por AGPL/comercial.

## 4. Solución KISS

Mientras sea suficiente existen solo:

```text
src/SGPdf.App/
tests/SGPdf.App.Tests/
```

No recrear `Core`, `Infrastructure`, `Domain`, `Application`, CQRS, event bus, plugin framework o DI complejo sin necesidad real.

Dentro de la app:

```text
Pdf/
Features/
  Reader/
  Labels/
  Sign/
  Edit/
  Organize/
  Comments/
Utilities/
```

## 5. PDFium

PDFium es el motor PDF primario y se distribuye actualmente mediante `bblanchon.PDFium.Win32` con versión explícita/pinneada.

Se prefiere P/Invoke mínimo propio en vez de wrapper grande. Cada API nativa entra cuando una feature la necesita.

### Thread safety

PDFium no es thread-safe dentro de un proceso. Todas las llamadas pasan por exclusión global:

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

Prioridad prevista:

1. página visible;
2. acción explícita;
3. páginas vecinas;
4. thumbnails/background.

Renders pesados pueden usar `FPDF_RenderPageBitmap_Start`, `Continue`, `Close` e `IFSDK_PAUSE`. Al cancelar se liberan siempre recursos nativos y managed.

## 6. UX PDF

Un PDF abre en `Leer` para evitar modificaciones accidentales.

Modos principales:

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

Si aparecen nuevos code points o subset dudoso, usar fallback con TTF redistribuible auditada (p. ej. Roboto) y nuevo text object. Guardar copia, reabrir y validar.

## 7. Organizar PDF

PDFium se intenta primero.

Intra-documento: mover/reordenar, rotar, eliminar, duplicar cuando aplique.

Inter-documento: `FPDF_ImportPages`/`FPDF_ImportPagesByIndex` para insert/merge/extract cuando sea viable.

Preflight previsto:

- firmas: `FPDF_GetSignatureCount`;
- formularios: `FPDF_GetFormType`;
- bookmarks: `FPDFBookmark_GetFirstChild(document, NULL)`;
- named destinations, links internos, tagged PDF, page labels, attachments y metadata según corpus.

No prometer preservación de estructuras document-level que no se hayan probado.

## 8. ZPL / Mercado Libre

El usuario recibe ZPL de Mercado Libre y hoy usa Labelary manualmente antes de imprimir en una impresora térmica que acepta PDF pero no esos ZPL directamente.

SG PDF Editor debe reemplazar ese proceso totalmente offline.

Entradas:

- `.zpl`;
- `.txt` con ZPL;
- `.prn` con ZPL.

Bloques: `^XA ... ^XZ`.

`^PQ28` significa un diseño con 28 copias, no 28 renders. Se renderiza el diseño una vez y la cantidad se aplica al layout.

Layouts: 1/2/3/4/6/8/10/12/custom. Tamaños: térmicos, A4, Carta y custom.

## 9. Gate ZPL-A

Antes de UI completa se comparan:

- `BinaryKits/BinaryKits.Zpl` — candidato preferente;
- `GOODBOY008/labelize` — fallback.

Corpus privado real + sintético: `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF`, `^XF`, tildes/ñ, logos y direcciones largas.

Benchmark: 10/100/500 diseños sin multiplicar `^PQ`.

Criterios: fidelidad, barcode/QR, UTF-8, velocidad, RAM/CPU/I/O, packaging, mantenimiento y licencias.

Si BinaryKits es suficientemente fiel, gana por KISS: .NET in-process, MIT, sin proceso externo/Rust en runtime.

## 10. BinaryKits / Labelize

BinaryKits puede producir bitmap y PDF vía Skia (`SKDocument.CreatePdf`); barcodes raster se manejan con nearest-neighbor. `^CI28` existe como UTF-8, pero se debe probar con datos reales/sintéticos.

Pipeline preferido si gana:

```text
ZPL → BinaryKits bitmap → preview WPF
ZPL → BinaryKits PDF → PDFsharp XPdfForm → hoja final → imprimir
```

Labelize queda como fallback. Su distribución final está bloqueada mientras `ZplGSCustom.ttf` no quede resuelto a satisfacción del proyecto.

## 11. Impresión térmica

Mantener tamaño físico exacto. No restar automáticamente hardware margins al PDF.

Consultar `PrintQueue.GetPrintCapabilities()`; cuando el driver lo permita usar PageMediaSize exacto, scaling 100 %, una página por hoja y resolución nativa. `PageImageableArea` sirve para advertir recortes; nunca escalar silenciosamente un barcode.

Si un driver destruye la escala, primer fallback: raster directo a DPI nativo mediante el driver Windows, no RAW genérico a un lenguaje desconocido.

QA ZPL: decode automático con ZXing + impresión física.

## 12. Licencias y terceros

Código propio: `All rights reserved` hasta decisión distinta. Repo público NO significa open source.

Toda dependencia runtime/dev relevante se registra en `third_party/manifest.json` y conserva licencias/notices requeridos.

## 13. Development Intelligence — A1 completada

GSD Core + Graphify son tooling de desarrollo; nunca requisitos del producto.

### GSD Core

- upstream: `open-gsd/gsd-core`;
- pin validado: `1.15.0`;
- MIT;
- Node 24+ validado;
- project-scoped Codex;
- `.planning/` versionado;
- `.codex/` generado/regenerable e ignorado.

### Graphify

- upstream: `Graphify-Labs/graphify`;
- pin validado: `0.9.77`;
- Apache-2.0;
- instalado localmente mediante virtualenv `.devtools/`;
- grafo limitado a `src/` + `tests/` por `.graphifyignore`;
- `graphify-out/` regenerable e ignorado;
- auto-update OFF;
- AST/query funciona offline sin backend LLM/API.

### Evidencia

Probe final `37530638793`:

- GSD install PASS;
- `validate health` = healthy, 0 warnings/errors;
- `state-snapshot` reconoce Phase 1 `F0 PDF Base`, estado planning;
- Graphify install/build/query PASS;
- grafo: 12 archivos, 161 nodos, 187 aristas, 16 comunidades;
- query `PdfDocumentSession` recuperó llamadas/relaciones PDFium reales;
- `PrivateFixtures` no aparece en el grafo.

Medición:

- `.codex/`: ~17 MB / 833 archivos → no versionar;
- `graphify-out/`: ~472 KB / 22 archivos → no versionar.

Setup reproducible: `tools/setup-dev.ps1`.

### Jerarquía de contexto desde A1

```text
Arquitectura → docs/MASTER_CONTEXT.md + docs/MASTER_PLAN.md
Estado       → .planning/STATE.md
Roadmap      → .planning/ROADMAP.md
Requisitos   → .planning/REQUIREMENTS.md
Fase         → .planning/phases/<fase>/PLAN.md cuando exista
Código       → Git/GitHub
Relaciones   → Graphify
Histórico    → SUMMARY + docs/history + Markdown portable
```

Flujo normal:

```text
STATE → plan de fase → Graphify query → archivos concretos
```

MASTER docs completos solo para arquitectura/licencias/cambio de fase/contradicciones.

### Compatibilidad conocida

GSD 1.15.0 no acepta `graphify.enabled`; usa `auto_update/build_timeout/graph_path`. No copiar claves de versiones futuras sin upgrade auditado.

Antigravity está soportado upstream, pero A1 validó Codex. Validar Antigravity en host real cuando se use.

Sin backend LLM, Graphify deja nombres de comunidades genéricos; eso es intencional y no afecta el grafo AST.

## 14. Estado y roadmap actual

```text
A0  Higiene/fuente de verdad               ✅ completada
A1  GSD Core + Graphify                   ✅ completada
F0  PDF base                              ▶ siguiente / GSD Phase 1
F1  Gate ZPL-A
F2  Etiquetas ZPL
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

F0 todavía no tiene plan de implementación definitivo. El siguiente chat debe ejecutar primero GSD discuss/plan. Primer slice recomendado para mantener KISS:

```text
F0.1 — abrir PDF desde UI + renderizar una página real en WPF
```

## 15. GitHub / ramas

- `main`: baseline estable; no tocar sin aprobación.
- A0: `feat/kiss-vertical-slice`, PR #2 draft, sin merge.
- A1: `feat/a1-dev-intelligence`, PR #6 draft, sin merge.
- una rama/vertical slice cuando sea razonable;
- PR draft temprano;
- build/tests/QA antes de declarar terminado;
- nunca auto-merge.

## 16. Datos privados

ZPL reales/documentos de clientes no entran al repo. `tests/PrivateFixtures/` permanece local/ignorado. Graphify tampoco debe indexarlo.

## 17. Roles

**ChatGPT:** planificación aplicada, código, GitHub, CI, tests, auditoría, documentación e históricos.

**Codex/Antigravity:** ejecución/runtime/UX Windows, native debugging, impresión y trabajo de código. Tooling project-scoped puede regenerarse con `tools/setup-dev.ps1`.

**Usuario:** prioridad/UX, pruebas físicas e instrucción final de merge.

**Otra IA:** auditor puntual, no arquitecto paralelo continuo.

## 18. Definition of Done

Una feature termina solo si:

- funciona offline;
- build/tests Windows tienen evidencia fresca;
- errores controlados;
- original protegido;
- pruebas/fixtures representativos;
- dependencias/licencias registradas;
- temporales/recursos limpios;
- docs/STATE/SUMMARY alineados;
- histórico portable generado.

ZPL además exige decode automático y QA física cuando corresponda.

## 19. Instrucción para un chat nuevo

1. Verificar GitHub primero.
2. Leer `.planning/STATE.md`.
3. Leer `.planning/ROADMAP.md` y requisitos de Phase 1.
4. Si `tools/setup-dev.ps1` ya se ejecutó, usar Graphify antes de lecturas amplias cuando aporte valor.
5. Leer MASTER docs completos solo si surge una decisión arquitectónica/licencia/contradicción.
6. No reabrir A0/A1 salvo evidencia material.
7. Continuar con F0 mediante GSD discuss/plan; no implementar toda F0 en un único bloque.
8. No merge a `main` sin aprobación explícita del usuario.
