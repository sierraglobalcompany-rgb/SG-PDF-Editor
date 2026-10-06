# SG PDF Editor — Plan maestro de ejecución

**Versión:** 2.0 operativa  
**Fecha:** 2026-10-06  
**Autoridad:** este archivo define el orden de trabajo. `docs/MASTER_CONTEXT.md` contiene el contexto completo y las decisiones de arquitectura.

## Principios de ejecución

1. KISS/YAGNI: implementar el slice mínimo útil.
2. Offline: ninguna función principal depende de red.
3. Gratis: sin API keys, SaaS ni licencias comerciales obligatorias.
4. PDFium primero: no añadir otro motor PDF sin evidencia de una carencia concreta.
5. BinaryKits candidato preferente para ZPL, pero debe ganar un Gate real.
6. `Guardar como` por defecto hasta madurar preservación/guardado.
7. CI, pruebas y QA son parte del feature, no una fase posterior.
8. No merge automático a `main`.
9. Datos reales privados nunca se versionan.
10. Cada sesión importante debe cerrar con un Markdown de estado/histórico actualizado.
11. GSD Core + Graphify son herramientas dev-only y nunca requisitos para compilar/usar SG PDF Editor.
12. El contexto diario debe ser mínimo: estado + plan de fase + consulta puntual + archivos concretos.

---

# A0 — Higiene, trazabilidad y reproducibilidad

**Estado:** ✅ COMPLETADA en `feat/kiss-vertical-slice` el 2026-10-06.  
**Commit de cierre:** `4333674`.  
**PR:** #2 draft, sin merge.  
**Evidencia Windows CI:** run `37518112465` — hygiene + `dotnet restore --locked-mode` + Release build + tests = success.

## Resultado

- fuentes de verdad creadas y alineadas;
- repo reducido a App + Tests;
- third-party manifest/licencias;
- datos privados ignorados;
- NuGet lock + restore locked;
- CI Windows verde;
- arquitectura KISS/offline congelada.

---

# A1 — Development Intelligence

**Estado:** ▶ EN CURSO en `feat/a1-dev-intelligence`.

## Objetivo

Reducir pérdida de contexto, relecturas del repo y gasto de tokens mediante GSD Core + Graphify, sin contaminar la arquitectura runtime ni convertir tooling en requisito del build.

## Herramientas aprobadas

### GSD Core

```text
open-gsd/gsd-core
```

Uso: project-scoped para estado, fases, planes, resúmenes y continuidad entre agentes.

### Graphify

```text
Graphify-Labs/graphify
```

Uso: project-scoped para grafo local de código (`src/` + `tests/`), impacto y navegación dirigida.

## Jerarquía de contexto objetivo

```text
Arquitectura → docs/MASTER_CONTEXT.md + docs/MASTER_PLAN.md
Estado       → .planning/STATE.md
Fase         → .planning/phases/<fase>/PLAN.md
Código       → Git/GitHub
Relaciones   → Graphify
Histórico    → SUMMARY + Markdown portable
```

Flujo normal futuro:

```text
STATE → PLAN fase → Graphify query → archivos concretos
```

Los MASTER docs completos se cargan solo para arquitectura, licencias, cambio de fase o contradicciones.

## A1.1 — preparación documental

- [x] crear rama `feat/a1-dev-intelligence` desde A0 cerrado;
- [x] crear plan A1 versionado;
- [x] actualizar `AGENTS.md` con estrategia de contexto reducido;
- [x] insertar A1 en `docs/ROADMAP.md`;
- [x] mover `docs/MASTER_CONTEXT.md` de A0→A1;
- [x] actualizar este MASTER_PLAN;
- [ ] preparar `.gitignore` para temporales/cachés de grafo sin ocultar planning operativo;
- [ ] verificar que `src/`/dependencias runtime no cambian;
- [ ] verificar build/tests/CI;
- [ ] crear histórico portable A1.1.

## A1.2 — instalación y validación

1. instalar GSD Core project-scoped;
2. hacer onboarding del repo existente;
3. reconciliar `.planning/PROJECT.md`, `ROADMAP.md`, `STATE.md` y config con MASTER docs;
4. instalar Graphify project-scoped;
5. construir grafo inicial de `src/` + `tests/`;
6. habilitar integración GSD↔Graphify;
7. ejecutar query/status/diff de aceptación;
8. mantener auto-update Graphify desactivado inicialmente;
9. medir tamaño/costo del grafo y decidir qué artefactos se versionan;
10. demostrar que borrar/deshabilitar GSD/Graphify no rompe build/test del producto.

## Acceptance A1

- una sesión nueva puede orientarse sin releer todo el repo;
- `STATE` indica fase/branch/PR/siguiente acción;
- Graphify devuelve relaciones útiles del código;
- ningún servicio externo es requerido;
- fixtures privados no se indexan/versionan;
- GSD/Graphify siguen siendo dev-only;
- build/test del producto sigue independiente.

---

# F0 — PDF base

**Estado:** ⏭️ DESPUÉS DE A1.

## Objetivo

Primer vertical slice funcional del lector PDF.

## Alcance

1. abrir archivo PDF desde UI;
2. inicializar/cerrar PDFium de forma segura;
3. `PdfDocumentSession` con page count y tamaños;
4. render real de una página a bitmap WPF;
5. navegación mínima anterior/siguiente/ir a página;
6. zoom;
7. fit page y fit width;
8. scheduler PDFium con exclusión global;
9. cancelación de renders obsoletos;
10. progressive render donde se justifique;
11. impresión Windows básica;
12. mensajes de error comprensibles.

## Fuera de alcance

- thumbnails completos;
- search;
- firma;
- edición;
- ZPL;
- organización.

## QA

- PDF 1 página;
- multipágina;
- documento grande;
- cambio rápido de página/zoom;
- cancelación;
- Microsoft Print to PDF;
- prueba con red deshabilitada.

## Resultado esperado

Lector mínimo real y estable que ya sirve para abrir y mandar PDF a imprimir.

---

# F1 — Gate ZPL-A

## Objetivo

Elegir un único motor ZPL mediante evidencia, no preferencia.

## Candidatos

### A — BinaryKits.Zpl

Preferente por KISS:

- .NET in-process;
- MIT;
- preview bitmap;
- PDF vía Skia;
- soporte `^DF`, `^XF`, `^CI28` y barcodes relevantes.

### B — Labelize

Fallback:

- buena cobertura;
- offline;
- CLI/librería Rust;
- mayor fricción de packaging/proceso;
- release condicionado por activo `ZplGSCustom.ttf` mientras no se resuelva.

## Harness

Crear pruebas/benchmark con:

- 10, 100 y 500 diseños;
- no multiplicar por `^PQ`;
- archivos reales privados;
- fixtures sintéticos públicos.

## Corpus mínimo

- `^XA/^XZ`;
- `^CI28`;
- `^FH`;
- `^FB`;
- `^FR`;
- `^GFA`;
- `^BC`;
- `^BQ`;
- `^PQ`;
- `^DF`;
- `^XF`;
- tildes/ñ;
- logos y direcciones largas.

## Medidas

- fidelidad visual;
- barcode/QR decodificable;
- CPU/RAM/tiempo/I/O;
- cold/warm;
- packaging;
- mantenimiento;
- licencias.

## Decisión

Si BinaryKits reproduce correctamente los ZPL reales, gana y Labelize sale del runtime.

---

# F2 — Etiquetas ZPL

## Objetivo

Reemplazar el flujo manual de Labelary para el uso diario.

## Funciones

- abrir `.zpl/.txt/.prn`;
- detectar/separar diseños;
- interpretar `^PQ` como cantidad;
- preview;
- respetar cantidades / una de cada / personalizada;
- tamaños térmicos, A4, Carta, custom;
- 1/2/3/4/6/8/10/12 por página;
- filas × columnas custom;
- márgenes/gaps;
- rotación;
- export PDF;
- imprimir.

## Pipeline preferido si gana BinaryKits

```text
ZPL → BinaryKits bitmap → preview WPF
ZPL → BinaryKits PDF → PDFsharp XPdfForm → hoja final → imprimir
```

## Calidad

- nunca deformar barcode;
- sin JPEG;
- nearest-neighbor para bitmap;
- tamaño físico exacto;
- validar PDF final con ZXing.Net;
- QA físico 203 dpi y, si existe, 300 dpi.

---

# F3 — Firma visual

## MVP

- importar PNG transparente;
- mostrar overlay;
- drag;
- resize proporcional;
- mover fino;
- eliminar/duplicar;
- convertir coordenadas pantalla/PDF;
- insertar objeto en PDF;
- `Guardar como`;
- reabrir y validar.

## Después

- biblioteca local;
- firma predeterminada;
- iniciales;
- fecha/nombre;
- sellos;
- aplicar a varias páginas.

---

# F4 — Lector completo

- continuous scroll;
- thumbnails;
- bookmarks;
- links;
- search;
- copy text;
- PDFs con password;
- atajos;
- recientes;
- pestañas solo si no degradan simplicidad/estabilidad.

---

# F5 — Organizar

PDFium primero.

## Intra-documento

- move/reorder;
- rotate;
- delete;
- duplicate.

## Inter-documento

- insert;
- extract;
- merge;
- split.

## Preflight

Antes de operaciones sensibles comprobar:

- firmas (`FPDF_GetSignatureCount`);
- formularios (`FPDF_GetFormType`);
- bookmarks (`FPDFBookmark_GetFirstChild(document, NULL)`);
- links/destinations/tagged/page labels según se incorpore corpus.

No prometer preservación de estructuras document-level no probadas.

---

# F6 — Imágenes

- hit-test;
- menú contextual;
- extraer/guardar;
- reemplazar preservando geometría cuando sea viable;
- mover;
- resize;
- rotate;
- opacity;
- z-order;
- delete;
- undo/redo.

---

# F7 — Texto V1

Objetivo: edición simple y segura, no reflow Word-like.

- seleccionar objetos;
- obtener font/tamaño/matriz;
- `FPDFText_SetText` cuando sea conservador;
- si aparecen code points nuevos o subset dudoso, fallback con TTF redistribuible auditada;
- nuevo text object con `FPDFText_LoadFont` + `FPDFPageObj_CreateTextObj`;
- color/tamaño/move básico;
- save/reopen/render verification.

---

# F8 — Comentarios

- highlight;
- underline;
- strikeout;
- notes;
- ink;
- shapes según soporte estable.

---

# F9 — Utilidades

Solo incorporar las que aporten valor y sean offline/libres:

- watermark;
- numbering;
- password protect/unlock autorizado;
- compress/optimize;
- repair/validate.

No añadir qpdf/pdfcpu salvo carencia demostrada de PDFium.

---

# F10 — OCR

Tesseract local:

```text
PDF scan → render → OCR → bounding boxes/text → capa buscable → Guardar como
```

Español primero; inglés opcional.

---

# F11 — Texto V2

- reading order;
- line grouping;
- paragraphs;
- columns;
- reflow limitado;
- PdfPig solo si reduce complejidad frente a implementar análisis propio.

---

# F12 — Profesional

Subproyectos independientes:

- firma criptográfica;
- formularios;
- redacción real;
- compare;
- batch;
- generador de códigos;
- conversiones solo tras auditoría offline/licencias.

---

# Flujo de ejecución por slice

1. revisar estado GitHub;
2. leer `STATE` cuando A1.2 exista;
3. revisar Issue/plan de fase;
4. rama aislada;
5. usar Graphify para impacto cuando aporte valor;
6. test/reproducción primero cuando haya comportamiento;
7. implementación mínima;
8. verificación local disponible;
9. CI Windows;
10. QA manual/real;
11. documentación/dependencias;
12. actualizar `STATE`/`SUMMARY` cuando GSD esté operativo;
13. actualizar PR;
14. crear Markdown histórico descargable;
15. no merge sin aprobación.

# Definition of Done global

Una función está terminada solo si:

- funciona offline;
- build/test Windows tienen evidencia fresca;
- errores están controlados;
- no destruye original;
- tiene pruebas/fixtures representativos;
- dependencias/licencias están registradas;
- recursos/temporales se limpian;
- docs quedan alineadas.

Para etiquetas: además decode automático y QA físico cuando corresponda.
