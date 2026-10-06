# SG PDF Editor — Contexto maestro

**Última consolidación:** 2026-10-06  
**Estado:** arquitectura auditada y congelada; A0 completada; A1 Development Intelligence en preparación.  
**Repositorio:** `sierraglobalcompany-rgb/SG-PDF-Editor`  
**Rama A0:** `feat/kiss-vertical-slice`  
**PR A0:** #2, borrador, sin merge.  
**Rama A1 actual:** `feat/a1-dev-intelligence`.

> Este documento existe para poder abrir otro chat/agente y continuar sin reconstruir el proyecto desde cero. GitHub actual tiene prioridad para el estado real de ejecución; este documento y `docs/MASTER_PLAN.md` tienen prioridad para las decisiones arquitectónicas salvo cambio explícitamente aprobado.

## 1. Producto

SG PDF Editor es una aplicación Windows local, rápida y simple para dos trabajos diarios:

1. **PDF:** leer, imprimir, firmar visualmente, organizar y editar de forma práctica.
2. **Etiquetas térmicas:** abrir ZPL/TXT/PRN, previsualizar, respetar cantidades, maquetar, convertir a PDF e imprimir sin Internet.

No pretende comenzar como un clon completo de Acrobat. El objetivo es resolver primero los flujos reales de uso frecuente con la menor cantidad de piezas posible.

## 2. Requisitos no negociables

- Windows x64 inicialmente.
- C# + .NET 10 + WPF.
- KISS + YAGNI.
- Funcionamiento normal 100 % offline.
- Sin API keys, SaaS, cuentas ni servidores obligatorios.
- Sin componentes que exijan licencia comercial para mantener el producto cerrado.
- Priorizar MIT, BSD y Apache-2.0.
- No introducir AGPL/GPL fuerte sin aprobación expresa.
- `Guardar como` por defecto mientras las operaciones de guardado maduran.
- No hacer merge automático a `main`.
- Los archivos reales de Mercado Libre son privados y no se versionan.

## 3. Arquitectura congelada

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

`*` BinaryKits es el candidato ZPL preferente, sujeto a un Gate técnico contra Labelize con archivos reales y sintéticos.

### Lo que NO forma parte del núcleo actual

- qpdf/pdfcpu: solo si PDFium demuestra una carencia concreta.
- Tesseract: solo al llegar OCR.
- PdfPig: solo al llegar texto/layout avanzado.
- Labelize: fallback ZPL condicionado; no motor principal por defecto.
- PoDoFo: fuera del plan actual.
- MuPDF/iText/Ghostscript: descartados por modelo AGPL/comercial.

## 4. Estructura de solución

KISS: solo dos proyectos mientras sea suficiente.

```text
src/SGPdf.App/
tests/SGPdf.App.Tests/
```

No recrear `Core`, `Infrastructure`, `Domain`, `Application`, CQRS, event bus, plugin framework o DI complejo sin una necesidad demostrable.

Dentro de la aplicación se organiza por carpetas/feature:

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

PDFium es el motor PDF primario. La distribución actual del proyecto usa `bblanchon.PDFium.Win32` y debe permanecer fijada a una versión explícita.

Se prefiere un P/Invoke mínimo propio en vez de un wrapper grande. Cada API nativa se añade cuando una feature real la necesita.

### Thread safety

PDFium no es thread-safe dentro de un mismo proceso. Todas las llamadas pasan por un gate global:

```text
PdfiumRuntime
  └─ SemaphoreSlim(1,1)
```

La UI no ejecuta PDFium directamente. El flujo es:

```text
WPF UI → tarea background → scheduler → mutex global → PDFium
```

Prioridades del scheduler:

1. página visible;
2. acción explícita;
3. páginas vecinas;
4. miniaturas/background.

Renders pesados podrán usar el API progresivo (`FPDF_RenderPageBitmap_Start`, `Continue`, `Close`, `IFSDK_PAUSE`) para cancelar trabajo obsoleto. En cancelación deben liberarse tanto recursos nativos como bitmaps managed parciales.

## 6. UX PDF

Un PDF abre siempre en `Leer`.

Navegación principal prevista:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

### Leer

Evoluciona hacia: abrir, páginas, zoom, fit width/page, scroll, thumbnails, bookmarks, búsqueda, copiar, links, password e impresión.

### Firmar

Alta prioridad. MVP: importar PNG transparente, arrastrar, redimensionar proporcionalmente, mover, eliminar, duplicar y guardar copia. Después: biblioteca local, firma predeterminada, iniciales, fecha, nombre y sello.

La firma visual no es la firma criptográfica con certificado. La firma criptográfica es una fase profesional independiente.

### Editar

Menús contextuales por objeto.

Imagen: reemplazar, extraer/guardar, copiar, girar, tamaño, opacidad, orden y eliminar.

Texto: editar, copiar, fuente/tamaño/color cuando sea seguro y eliminar cuando corresponda.

El PDF no se tratará como Word. Texto V1 será conservador; párrafos/reflow pertenecen a una fase posterior.

## 7. Texto y fuentes

APIs PDFium previstas incluyen `FPDFText_SetText`, `FPDFTextObj_GetFontName`, `FPDFText_LoadFont`, `FPDFText_LoadStandardFont` y `FPDFPageObj_CreateTextObj`.

Fuentes subset requieren cautela. Regla inicial: si el texto nuevo introduce code points que no estaban en el texto original, usar fallback con una TTF redistribuible auditada (p. ej. Roboto) y un nuevo objeto de texto, en vez de asumir que el subset contiene el glifo.

Guardar como copia, reabrir y validar render/extracción forma parte del flujo de edición.

## 8. Organizar PDF

PDFium se prueba primero.

Intra-documento: mover, rotar y eliminar con APIs PDFium apropiadas.

Inter-documento: `FPDF_ImportPages`/`FPDF_ImportPagesByIndex` para merge/insert/extract cuando sea viable.

Existe preflight antes de operaciones estructurales:

- firmas: `FPDF_GetSignatureCount`;
- formularios: `FPDF_GetFormType`;
- bookmarks: `FPDFBookmark_GetFirstChild(document, NULL)`;
- además probar named destinations, links internos, tagged PDF, page labels, attachments y metadata.

No asumir que las estructuras de catálogo se preservan al importar páginas. En MVP se advierte/bloquea cuando no podamos garantizar integridad.

## 9. ZPL y Mercado Libre

El usuario recibe archivos ZPL de Mercado Libre y hoy usa Labelary manualmente para convertirlos antes de imprimirlos en una impresora térmica que sí acepta PDF pero no esos ZPL directamente.

SG PDF Editor debe reemplazar ese flujo offline.

Entradas:

- `.zpl`;
- `.txt` con ZPL;
- `.prn` con ZPL.

Bloques básicos: `^XA ... ^XZ`.

`^PQ28` significa **un diseño con 28 copias**, no 28 renders. El sistema conserva diseño y cantidad por separado.

Debe poder maquetar 1, 2, 3, 4, 6, 8, 10, 12 o un grid personalizado por página y trabajar con tamaños térmicos, A4, Carta y tamaños personalizados.

## 10. Gate ZPL-A

Antes de construir toda la UI de etiquetas se comparan:

- **BinaryKits.Zpl** — candidato preferente;
- **Labelize** — fallback.

Se prueban archivos reales privados y fixtures sintéticos con `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF` y `^XF`.

Benchmark: 10, 100 y 500 diseños, sin multiplicar por `^PQ`.

Criterios: fidelidad, barcode/QR, UTF-8, velocidad, RAM, CPU, I/O, packaging, mantenimiento y licencias.

Si BinaryKits tiene fidelidad suficiente, gana por KISS: .NET in-process, sin proceso externo ni Rust en runtime.

## 11. BinaryKits.Zpl

Es candidato preferente porque el código actual ya puede producir bitmap y PDF mediante Skia (`SKDocument.CreatePdf`). Sus barcodes se dibujan como bitmaps y usan nearest-neighbor en el resize, mientras texto/formas pueden conservar primitivas vectoriales.

`^CI28` está representado como UTF-8 en su modelo, pero se debe verificar físicamente con tildes y ñ.

Si gana el Gate:

```text
ZPL → BinaryKits preview bitmap → WPF
ZPL → BinaryKits PDF → PDFsharp XPdfForm → hoja final → imprimir
```

## 12. Labelize

Fallback si BinaryKits no logra la fidelidad necesaria. Tiene buena cobertura y funciona offline, pero el camino CLI añade proceso/temporales y existe una duda de procedencia sobre `ZplGSCustom.ttf`. No se distribuye en una release estable mientras ese activo no quede resuelto o eliminado/reemplazado de forma verificable.

## 13. Impresión térmica

El PDF debe mantener tamaño físico exacto. No se restan automáticamente los hardware margins al MediaBox.

Consultar `PrintQueue.GetPrintCapabilities()` y, cuando el driver lo admita, usar tamaño exacto, scaling 100 %, una página por hoja y resolución nativa. `PageImageableArea` se usa para advertir recortes; no se escala silenciosamente un barcode.

Si un driver real destruye la escala, el primer fallback será rasterización directa a la resolución nativa usando el driver Windows, no mandar ZPL/PDF como RAW a una impresora cuyo lenguaje no conocemos.

La validación ZPL incluye decode automático con ZXing.Net y prueba física de impresión.

## 14. Licencias y terceros

El código propio sigue con `All rights reserved` mientras no se decida otra licencia.

El repositorio es público; eso NO convierte automáticamente el código en open source.

Toda dependencia runtime/development relevante se registra en `third_party/manifest.json` y conserva las licencias/notices requeridos.

## 15. Development Intelligence: GSD Core + Graphify

GSD Core y Graphify son herramientas de desarrollo, no dependencias de SG PDF Editor.

### GSD Core

Repositorio aprobado:

```text
open-gsd/gsd-core
```

Se usará project-scoped para persistir estado, fases, planes y resúmenes sin contaminar otros repositorios. No sustituye los MASTER docs: gestiona estado operativo.

### Graphify

Repositorio aprobado:

```text
Graphify-Labs/graphify
```

Se usará project-scoped para construir un grafo local de `src/` y `tests/`, localizar dependencias y reducir lecturas amplias. No se habilitan por defecto servicios semánticos externos ni se indexan fixtures privados.

### Jerarquía de contexto prevista después de A1.2

```text
Arquitectura: docs/MASTER_CONTEXT.md + docs/MASTER_PLAN.md
Estado:       .planning/STATE.md
Fase actual:  .planning/phases/<fase>/PLAN.md
Código real:  Git/GitHub
Relaciones:   Graphify
Histórico:    SUMMARY + Markdown portable
```

Flujo normal de contexto:

```text
STATE → plan de fase → Graphify query → archivos concretos
```

Los MASTER docs completos se leen cuando la tarea toca arquitectura, licencias, cambio de fase o aparece una contradicción.

GSD/Graphify deben poder fallar o desinstalarse sin impedir compilar/usar SG PDF Editor.

## 16. Roadmap congelado

```text
A0  Higiene/fuente de verdad                                  ✅ completada
A1  GSD Core + Graphify                                      ▶ actual
F0  PDF base: open/render/cancel/zoom/navigation/print
F1  Gate ZPL-A
F2  Etiquetas ZPL completas
F3  Firma visual
F4  Lector completo
F5  Organizar
F6  Imágenes
F7  Texto V1
F8  Comentarios
F9  Utilidades justificadas
F10 OCR
F11 Texto V2
F12 Profesional
```

## 17. Flujo GitHub

- `main` representa estado estable.
- una rama por vertical slice cuando sea razonable;
- Issue con alcance/aceptación/riesgos;
- PR draft temprano;
- build/tests/QA antes de declarar terminado;
- no auto-merge;
- merge solo con aprobación explícita del usuario.

## 18. Datos privados

Los ZPL reales y documentos de clientes NO entran al repo público. Usar `tests/PrivateFixtures/` local (ignorado) y fixtures sintéticos equivalentes en CI.

Graphify tampoco debe indexar esos fixtures privados.

## 19. Papel de cada participante

**ChatGPT:** arquitectura aplicada, código, GitHub, CI, tests, auditoría, documentación y mantenimiento de históricos portables.

**Codex/Antigravity:** especialmente valiosos para runtime/UX real en Windows, drag/drop, native debugging e impresión física. GSD puede instalarse para ambos runtimes en A1.2.

**Usuario:** aceptación funcional/UX, impresora térmica y aprobación de merge.

**Otra IA:** auditor independiente puntual, no arquitecto paralelo permanente.

## 20. Definition of Done

Una función solo se considera terminada si:

- funciona offline;
- build/test Windows están verdes;
- errores están controlados;
- no destruye original;
- tiene fixture/prueba representativa;
- dependencias/licencias están auditadas;
- temporales se limpian;
- documentación queda alineada.

ZPL además exige barcode/QR legible automáticamente y QA físico cuando corresponda.

Cada cierre de trabajo relevante debe dejar un Markdown histórico portable con fase, branch, commits, PR, verificaciones, decisiones, pendientes y siguiente acción.

## 21. Estado al actualizar este documento

- `main` sigue en el baseline estable inicial.
- A0 cerró en `feat/kiss-vertical-slice`, commit `4333674`, con PR #2 draft sin merge.
- Windows CI de A0 quedó verde con restore locked, build Release y tests.
- Nueva rama A1: `feat/a1-dev-intelligence`.
- A1.1 está preparando documentación/reglas; todavía NO instala GSD ni Graphify.
- La instalación/onboarding real de GSD + Graphify queda para A1.2.
- F0 todavía no ha comenzado.

## 22. Instrucción para un chat nuevo

1. Consultar GitHub primero para conocer el estado real más reciente.
2. Mientras A1.2 no exista, leer este documento, `docs/MASTER_PLAN.md` y la fase activa.
3. Después de A1.2, comenzar por `.planning/STATE.md` y el plan de fase; usar Graphify antes de lecturas amplias cuando aporte valor.
4. No reabrir decisiones congeladas salvo evidencia nueva material.
5. Continuar desde la primera fase incompleta.
6. Mantener KISS/YAGNI/offline/gratis.
7. Nunca hacer merge a `main` sin aprobación del usuario.
