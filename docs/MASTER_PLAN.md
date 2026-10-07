# SG PDF Editor — Plan maestro de ejecución

**Versión:** 2.2 operativa  
**Fecha:** 2026-10-07  
**Autoridad:** este archivo define el orden de trabajo. `docs/MASTER_CONTEXT.md` contiene el contexto completo y las decisiones arquitectónicas. `.planning/STATE.md` contiene el estado operativo diario.

## Principios de ejecución

1. KISS/YAGNI: implementar el slice mínimo útil.
2. Offline: ninguna función principal depende de red.
3. Gratis: sin API keys, SaaS ni licencias comerciales obligatorias.
4. PDFium primero: no añadir otro motor PDF sin evidencia de una carencia concreta.
5. **Labelize 1.7.0 es el motor ZPL aprobado por Gate ZPL-A sintético**; se integra como ejecutable local pinneado desde F2.2, no como servicio HTTP.
6. BinaryKits queda como evidencia histórica del Gate, no como fallback runtime preventivo.
7. `Guardar como` por defecto hasta madurar preservación/guardado.
8. CI, pruebas y QA son parte del feature, no una fase posterior.
9. No merge automático a `main`.
10. Datos reales privados nunca se versionan ni se indexan en herramientas de desarrollo.
11. Cada sesión importante cierra con estado/resumen + Markdown histórico portable.
12. GSD Core + Graphify son dev-only y nunca requisitos para compilar o usar SG PDF Editor.
13. El contexto diario debe ser mínimo: `STATE` + plan de fase + Graphify cuando aporte valor + archivos concretos.

---

# A0 — Higiene, trazabilidad y reproducibilidad

**Estado:** ✅ COMPLETADA.  
**Rama:** `feat/kiss-vertical-slice`.  
**Commit de cierre:** `4333674`.  
**PR:** #2 draft, sin merge.

Resultado: fuentes de verdad, solución App + Tests, third-party manifest/licencias, datos privados ignorados, lock de dependencias, CI Windows y arquitectura offline/libre.

---

# A1 — Development Intelligence

**Estado:** ✅ COMPLETADA.  
**Rama:** `feat/a1-dev-intelligence`.  
**PR:** #6 draft, sin merge.

Tooling validado:

- GSD Core `1.15.0`, MIT, project-scoped, Node 24+;
- Graphify `0.9.77`, Apache-2.0, AST local sobre `src/` + `tests/`;
- `.codex/` y `graphify-out/` son regenerables/ignorados;
- setup reproducible: `tools/setup-dev.ps1`;
- GSD/Graphify nunca entran al runtime/build del producto.

Evidencia final A1: run `37530638793` success.

Jerarquía operativa:

```text
Arquitectura → docs/MASTER_CONTEXT.md + docs/MASTER_PLAN.md
Estado       → .planning/STATE.md
Roadmap      → .planning/ROADMAP.md
Requisitos   → .planning/REQUIREMENTS.md
Fase         → .planning/phases/<fase>/...
Código       → Git/GitHub
Relaciones   → Graphify local
Histórico    → docs/history
```

Flujo normal de contexto: `STATE → plan de fase → Graphify si aporta → archivos concretos`.

---

# F0 — PDF base

**Estado automatizado:** ✅ F0.1–F0.6 PASS.  
**Estado físico:** ⏳ smoke Windows/impresión/offline todavía NOT RUN.

## Resultado implementado

1. abrir PDF local desde UI;
2. lifecycle PDFium seguro;
3. page count/tamaños/render real;
4. navegación anterior/siguiente/ir a página;
5. zoom + 100 % + fit page/width;
6. scheduler latest-request-wins y cancelación cooperativa;
7. refit con debounce 150 ms;
8. impresión estándar Windows con todas/actual/rango;
9. hardening multipágina/PDF inválido;
10. guard automatizado de runtime offline.

## Decisiones

- PDFium globalmente serializado con `SemaphoreSlim(1,1)`;
- `FPDF_RenderPageBitmap` sigue síncrono; progressive rendering solo si la medición real lo justifica;
- impresión arranca en raster 200 DPI y usa `PrintDialog`/`DocumentPaginator`;
- Microsoft Print to PDF usa el mismo flujo;
- no auto-ajustar orientación ni escala física silenciosamente.

## Pendiente físico

PDFs reales/heavy, UI rápida, Microsoft Print to PDF, impresora física cuando exista, red deshabilitada y medición de DPI/memoria/latencia.

---

# F1 — Gate ZPL-A

**Estado del motor:** ✅ decisión aprobada.  
**Cierre formal F1:** ⏳ falta corpus real privado de Mercado Libre.

## Motores evaluados

- BinaryKits.Zpl Viewer 1.3.1;
- Labelize 1.7.0 Windows x64 CLI.

## Probe

Branch throwaway: `spike/f1-zpl-gate-a`.  
Head verificado: `2df2f374ae6124729389640425dc8334d0647f9f`.  
Run final: `37577735748` success.

Corpus sintético:

- `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF`, `^XF`;
- tildes/ñ;
- direcciones largas;
- etiqueta compuesta;
- QR con `^FT`.

Resultado:

- ambos motores renderizaron el corpus y generaron Code128/QR decodificables tras comparación alpha-aware;
- Labelize: ~59–61 diseños/s;
- BinaryKits: ~26–28 diseños/s;
- Labelize exe: 5,860,352 bytes;
- BinaryKits probe publish: ~16.9 MB / 14 archivos;
- BinaryKits mostró desplazamiento vertical de 60 px en el probe `^FT + ^BQ` frente a Labelize.

## Decisión

**Labelize 1.7.0 gana el Gate y es el único motor previsto para F2.**

Integración escogida:

```text
WPF / C#
  ↓
parser/orquestación managed
  ↓
LabelizeProcessRenderer
  ↓ proceso local controlado
labelize.exe 1.7.0 convert
  ↓
PNG/PDF temporal local
```

No `labelize serve`, no puerto local, no C ABI Rust, no WASM host, no dual engine.

Pin release archive SHA-256:

`cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`

Riesgo residual: `ZplGSCustom.ttf` está documentada como heredada de zebrash/MIT pero con procedencia exacta imperfecta. Re-auditar antes de instalador público; si fuera necesario, redibujar ese pequeño set de glifos.

Los ZPL reales privados no entran al repo/CI/Graphify.

---

# F2 — Etiquetas ZPL

**Objetivo:** reemplazar el flujo manual de Labelary de forma totalmente local/offline.

## Arquitectura congelada

- C# conserva parsing, metadatos, cantidades, UI y orquestación;
- Labelize renderiza desde F2.2 mediante sidecar local pinneado;
- `^PQ` es cantidad, no multiplicador de render;
- se conserva un `NormalizedRenderSource` a nivel documento para no romper `^DF/^XF`;
- no JPEG ni escalado silencioso de códigos;
- dimensiones físicas son autoridad;
- Windows driver printing, no RAW genérico sin lenguaje conocido.

## F2.1 — Parse + Open

**Estado:** implementación funcional automatizada PASS; cierre documental/final CI en curso.

- `.zpl/.txt/.prn` local;
- `ZplDocument` / `ZplDesign`;
- parser `^XA/^XZ` managed;
- `^DF` support blocks y `^XF` preservados;
- `^PQ` extraído/removido del render source;
- UTF-8 estricto + BOM UTF-8;
- WPF open flow candidate-first;
- placeholder ZPL cargado;
- ninguna dependencia Labelize/PDFsharp/ZXing nueva.

PR: #13 draft, sin merge.

## F2.2 — Labelize adapter + preview

- empaquetar/pinnear `labelize.exe` 1.7.0;
- verificar digest en setup/packaging;
- `ProcessStartInfo.ArgumentList`, sin shell concatenado;
- request-scoped temp directory;
- cancelación mata process tree;
- timeout acotado;
- PNG preview local;
- cleanup en éxito/error/cancelación;
- regresiones `^DF/^XF`, Code128, QR y `^FT + ^BQ`;
- ningún download en runtime.

## F2.3 — Cantidades + dimensiones

- cantidad archivo / una de cada / personalizada;
- presets térmicos + custom;
- dpmm 6/8/12/24 según Labelize;
- rerender ante cambio de dimensiones, no estirar barcode.

## F2.4 — Layout + PDF export

- layouts 1/2/3/4/6/8/10/12/custom;
- térmico/A4/Carta/custom;
- márgenes/gaps/rotación;
- añadir PDFsharp solo si en esta slice sigue siendo la solución KISS más fiable;
- export verificable sin deformación de códigos.

## F2.5 — Windows thermal print

- tamaño físico exacto;
- `PrintQueue.GetPrintCapabilities()`;
- PageMediaSize cuando driver lo permita;
- imageable area solo para advertir clipping;
- nunca shrink-to-fit silencioso;
- fallback a raster a DPI nativo mediante driver Windows si hace falta.

## F2.6 — Validation + hardening

- decode automático barcode/QR;
- corpus privado real;
- impresión/scanner físico;
- offline/privacy/temp residue audit;
- documentación/estado/histórico.

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
10. actualizar `STATE`/histórico;
11. actualizar PR;
12. no merge sin aprobación.

# Definition of Done global

Una función está terminada solo si:

- funciona offline;
- build/test Windows tienen evidencia fresca;
- errores están controlados;
- no destruye original/estado válido previo;
- tiene pruebas/fixtures representativos;
- dependencias/licencias están registradas cuando entran al runtime;
- temporales/recursos se limpian;
- documentación/STATE/histórico queda alineado.

Para etiquetas además:

- `^PQ` no multiplica renders;
- barcode/QR decode automático cuando entra el renderer;
- corpus privado real antes del cierre formal del Gate;
- QA física de tamaño/códigos antes de cerrar F2.
