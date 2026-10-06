# SG PDF Editor — Plan maestro de ejecución

**Versión:** 2.1 operativa  
**Fecha:** 2026-10-06  
**Autoridad:** este archivo define el orden de trabajo. `docs/MASTER_CONTEXT.md` contiene el contexto completo y las decisiones arquitectónicas. `.planning/STATE.md` contiene el estado operativo diario.

## Principios de ejecución

1. KISS/YAGNI: implementar el slice mínimo útil.
2. Offline: ninguna función principal depende de red.
3. Gratis: sin API keys, SaaS ni licencias comerciales obligatorias.
4. PDFium primero: no añadir otro motor PDF sin evidencia de una carencia concreta.
5. BinaryKits.Zpl es candidato preferente para ZPL, pero debe ganar un Gate real.
6. `Guardar como` por defecto hasta madurar preservación/guardado.
7. CI, pruebas y QA son parte del feature, no una fase posterior.
8. No merge automático a `main`.
9. Datos reales privados nunca se versionan ni se indexan en herramientas de desarrollo.
10. Cada sesión importante cierra con estado/resumen + Markdown histórico portable.
11. GSD Core + Graphify son dev-only y nunca requisitos para compilar o usar SG PDF Editor.
12. El contexto diario debe ser mínimo: `STATE` + plan de fase + Graphify cuando aporte valor + archivos concretos.

---

# A0 — Higiene, trazabilidad y reproducibilidad

**Estado:** ✅ COMPLETADA.  
**Rama:** `feat/kiss-vertical-slice`.  
**Commit de cierre:** `4333674`.  
**PR:** #2 draft, sin merge.

## Resultado

- fuentes de verdad creadas/alineadas;
- solución KISS reducida a App + Tests;
- third-party manifest/licencias;
- datos privados ignorados;
- NuGet lock + restore locked;
- CI Windows verde;
- arquitectura offline/libre congelada.

---

# A1 — Development Intelligence

**Estado:** ✅ COMPLETADA.  
**Rama:** `feat/a1-dev-intelligence`.  
**PR:** #6 draft, sin merge.

## Objetivo cumplido

Reducir pérdida de contexto y lecturas repetidas usando GSD Core + Graphify sin contaminar el runtime ni el build del producto.

## Tooling validado

### GSD Core

- repo: `open-gsd/gsd-core`;
- versión pinneada: `1.15.0`;
- licencia: MIT;
- instalación project-scoped para Codex;
- Node 24+ validado;
- estado/roadmap/requisitos/config viven en `.planning/` y sí se versionan;
- payload generado `.codex/` es regenerable y se ignora.

### Graphify

- repo: `Graphify-Labs/graphify`;
- versión pinneada: `0.9.77`;
- licencia: Apache-2.0;
- instalación project-scoped;
- grafo AST local, sin API/LLM requerido;
- corpus limitado a `src/` + `tests/` mediante `.graphifyignore`;
- `graphify-out/` es regenerable y se ignora;
- auto-update desactivado inicialmente.

## Evidencia A1.2

Probe en entorno limpio:

- GSD `validate health`: `healthy`, 0 warnings, 0 errors;
- GSD `state-snapshot`: reconoce Phase 1 / `F0 PDF Base` y estado `planning`;
- Graphify: 12 archivos de código, 161 nodos, 187 aristas, 16 comunidades;
- query `PdfDocumentSession`: devuelve relaciones reales con `PdfiumRuntime`, `PdfiumNative` y APIs FPDF;
- `tests/PrivateFixtures/` ausente del grafo;
- `.codex/`: ~17 MB / 833 archivos → no versionar;
- `graphify-out/`: ~472 KB / 22 archivos → no versionar;
- setup reproducible: `tools/setup-dev.ps1`.

Run de validación tooling: `37530638793` = success.

## Jerarquía operativa después de A1

```text
Arquitectura → docs/MASTER_CONTEXT.md + docs/MASTER_PLAN.md
Estado       → .planning/STATE.md
Roadmap      → .planning/ROADMAP.md
Requisitos   → .planning/REQUIREMENTS.md
Fase         → .planning/phases/<fase>/PLAN.md cuando exista
Código       → Git/GitHub
Relaciones   → Graphify local
Histórico    → SUMMARY + docs/history + Markdown portable
```

Flujo de contexto normal:

```text
STATE → plan de fase → Graphify query → archivos concretos
```

Los MASTER docs completos se leen solo si la tarea toca arquitectura, licencias, cambio de fase o una contradicción.

## Notas de compatibilidad

- GSD 1.15.0 acepta `graphify.auto_update`, `build_timeout` y `graph_path`, pero no `graphify.enabled`; no usar claves de versiones futuras sin auditar upgrade.
- Antigravity está soportado upstream por GSD/Graphify, pero A1 validó Codex; validar en host real cuando se use.
- Graphify sin backend LLM deja nombres de comunidades genéricos; esto es intencional y no afecta las consultas AST offline.

---

# F0 — PDF base

**Estado:** ▶ SIGUIENTE.  
**GSD:** Phase 1, listo para `discuss/plan`.

## Objetivo

Primer vertical slice funcional del lector PDF.

## Alcance

1. abrir PDF desde UI;
2. lifecycle PDFium seguro;
3. `PdfDocumentSession` con page count/tamaños;
4. render real de una página a bitmap WPF;
5. navegación anterior/siguiente/ir a página;
6. zoom;
7. fit page / fit width;
8. scheduler PDFium con exclusión global;
9. cancelación de renders obsoletos;
10. progressive render donde aporte valor;
11. impresión Windows básica;
12. errores comprensibles.

## Fuera de alcance

- thumbnails completos;
- búsqueda;
- firma;
- edición;
- ZPL;
- organización.

## Estrategia recomendada de ejecución

Dividir F0 en slices pequeños. Primer candidato para la próxima sesión:

```text
F0.1 abrir PDF desde UI + renderizar una página real en WPF
```

Después, en bloques separados: navegación/zoom → scheduler/cancelación → fit → print.

La fase debe pasar primero por GSD discuss/plan antes de implementar.

## QA mínimo F0

- PDF 1 página;
- multipágina;
- documento grande;
- cambio rápido de página/zoom;
- cancelación;
- Microsoft Print to PDF;
- red deshabilitada.

**Resultado:** lector mínimo estable que ya abre/renderiza/imprime PDF.

---

# F1 — Gate ZPL-A

## Objetivo

Elegir un único motor ZPL mediante evidencia.

### A — BinaryKits.Zpl

Preferente por KISS: .NET in-process, MIT, bitmap preview, PDF vía Skia, soporte relevante (`^DF`, `^XF`, `^CI28`, barcodes).

### B — Labelize

Fallback: offline y buena cobertura, pero CLI/Rust añade fricción y el release sigue condicionado por `ZplGSCustom.ttf` hasta resolver su procedencia a satisfacción del proyecto.

## Gate

- corpus real privado + sintético;
- 10/100/500 diseños, sin multiplicar por `^PQ`;
- `^XA/^XZ`, `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF`, `^XF`;
- tildes/ñ, logos y direcciones largas;
- fidelidad, barcode/QR, CPU/RAM/tiempo/I/O, cold/warm, packaging, mantenimiento y licencias.

Si BinaryKits reproduce correctamente los ZPL reales, gana y Labelize sale del runtime.

---

# F2 — Etiquetas ZPL

- abrir `.zpl/.txt/.prn`;
- diseños + `^PQ` como cantidad;
- preview;
- cantidad archivo/una/custom;
- tamaños térmicos/A4/Carta/custom;
- layouts 1/2/3/4/6/8/10/12/custom;
- márgenes/gaps/rotación;
- export PDF;
- impresión Windows;
- validación ZXing + QA física.

Pipeline preferido si gana BinaryKits:

```text
ZPL → BinaryKits bitmap → preview WPF
ZPL → BinaryKits PDF → PDFsharp XPdfForm → hoja final → imprimir
```

No deformar barcode, no JPEG, tamaño físico exacto.

---

# F3 — Firma visual

- PNG transparente;
- overlay;
- drag/move/resize proporcional;
- eliminar/duplicar;
- coordenadas pantalla↔PDF;
- insertar en PDF;
- `Guardar como`;
- reabrir/verificar.

Después: biblioteca local, firma predeterminada, iniciales, fecha/nombre, sellos y varias páginas.

---

# F4 — Lector completo

- scroll continuo;
- thumbnails;
- bookmarks/links;
- search/copy text;
- password;
- atajos/recientes;
- pestañas solo si no degradan KISS/estabilidad.

---

# F5 — Organizar

PDFium primero.

- intra: move/reorder/rotate/delete/duplicate;
- inter: insert/extract/merge/split;
- preflight: firmas, formularios, bookmarks, links/destinations/tagged/page labels según corpus;
- no prometer preservación de estructuras document-level no probadas.

---

# F6 — Imágenes

- hit-test/menu contextual;
- extraer/guardar;
- reemplazar preservando geometría cuando sea viable;
- move/resize/rotate/opacity/z-order/delete;
- undo/redo.

---

# F7 — Texto V1

Edición simple y segura, no Word-like reflow:

- seleccionar objetos;
- font/tamaño/matriz;
- `FPDFText_SetText` en casos conservadores;
- fallback con TTF redistribuible para nuevos code points/subset dudoso;
- nuevo text object con `FPDFText_LoadFont` + `FPDFPageObj_CreateTextObj`;
- propiedades básicas;
- save/reopen/render validation.

---

# F8 — Comentarios

Highlight, underline/strikeout, notes, ink y shapes según soporte estable.

# F9 — Utilidades

Solo offline y justificadas: watermark, numeración, protección autorizada, optimize/repair. qpdf/pdfcpu solo si PDFium demuestra una carencia concreta.

# F10 — OCR

Tesseract local: scan → render → OCR → capa buscable. Español primero.

# F11 — Texto V2

Reading order, líneas, párrafos, columnas y reflow limitado. PdfPig solo si reduce complejidad.

# F12 — Profesional

Slices independientes: firma criptográfica, formularios, redacción real, compare, batch, generador de códigos y conversiones auditadas.

---

# Flujo de ejecución por slice

1. consultar GitHub + `.planning/STATE.md`;
2. revisar roadmap/requisitos/plan de fase;
3. rama/worktree aislado;
4. usar Graphify si reduce lectura/impacto;
5. test/reproducción primero para comportamiento nuevo;
6. implementar mínimo;
7. build/tests/CI;
8. QA manual/real cuando aplique;
9. actualizar docs/licencias;
10. actualizar `STATE`/`SUMMARY`;
11. actualizar PR;
12. generar Markdown histórico portable;
13. no merge sin aprobación.

# Definition of Done global

Una función está terminada solo si:

- funciona offline;
- build/test Windows tienen evidencia fresca;
- errores están controlados;
- no destruye original;
- tiene pruebas/fixtures representativos;
- dependencias/licencias están registradas;
- recursos/temporales se limpian;
- documentación queda alineada.

Para etiquetas: además decode automático y QA físico cuando corresponda.
