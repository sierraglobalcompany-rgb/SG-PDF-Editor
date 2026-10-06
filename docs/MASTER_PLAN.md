# SG PDF Editor — Plan maestro de ejecución

**Versión:** 1.1 consolidada  
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

---

# A0 — Higiene, trazabilidad y reproducibilidad

**Estado:** ✅ COMPLETADA en `feat/kiss-vertical-slice` el 2026-10-06.  
**Evidencia Windows CI:** run `37518112465` — hygiene + `dotnet restore --locked-mode` + Release build + tests = success.

## Objetivo

Dejar el repo comprensible para cualquier chat/agente y evitar que documentación, dependencias o CI diverjan.

## Tareas

- [x] Ignorar `tests/PrivateFixtures/` y caches locales.
- [x] Crear `docs/MASTER_CONTEXT.md`.
- [x] Crear/actualizar este `docs/MASTER_PLAN.md`.
- [x] Alinear `README.md`.
- [x] Alinear `AGENTS.md`.
- [x] Alinear `docs/ARCHITECTURE.md`.
- [x] Alinear `docs/ROADMAP.md`.
- [x] Simplificar `docs/DEVELOPMENT_PLAN.md` para que no duplique decisiones.
- [x] Actualizar `docs/LABELS_ZPL.md` con Gate BinaryKits/Labelize.
- [x] Actualizar `docs/REUSE_AUDIT.md`.
- [x] Crear `third_party/manifest.json`.
- [x] Crear carpeta/documentación de licencias y notices.
- [x] Activar lock NuGet reproducible y commitear locks generados por Windows CI.
- [x] Ajustar CI con higiene + restore locked + build + test.
- [x] Actualizar descripción del PR #2.
- [x] Verificar CI de la cabeza final de A0.

## Resultado

Repositorio reproducible, sin contradicciones materiales conocidas, con fuentes de verdad claras y preparación lista para F0.

---

# F0 — PDF base

**Estado:** ⏭️ SIGUIENTE.

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
- CPU;
- RAM;
- tiempo;
- I/O;
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

1. Issue/alcance.
2. Revisar estado GitHub.
3. Rama aislada.
4. Test/reproducción primero cuando sea código de comportamiento.
5. Implementación mínima.
6. Verificación local disponible.
7. CI Windows.
8. QA manual/real.
9. Documentación y dependencias.
10. Actualizar PR.
11. Crear Markdown histórico descargable con lo implementado, verificaciones, pendientes y siguiente paso.
12. No merge sin aprobación.

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
