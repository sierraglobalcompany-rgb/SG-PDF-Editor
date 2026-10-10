# Roadmap KISS

> El detalle y criterios de aceptación viven en `MASTER_PLAN.md`. El estado operativo diario vive en `.planning/STATE.md`.

## A0 — Higiene y reproducibilidad ✅
Repo reproducible, manifest/licencias, lock NuGet, privacidad de fixtures y CI Windows verificable.

## A1 — Development Intelligence ✅
GSD + Graphify project-scoped, regenerables y fuera del runtime del producto.

## F0 — PDF base — automated PASS / physical QA pending
Abrir/render/navegar/zoom/fit/imprimir con PDFium. Real Windows UI/print/offline smoke: **NOT RUN**.

## F1 — Gate ZPL-A — synthetic PASS / private corpus pending
Labelize 1.7.0 seleccionado. Corpus privado Mercado Libre: **NOT RUN**.

## F2 — Etiquetas ZPL offline — automated PASS / private+physical QA pending
Abrir ZPL, preview, cantidades, layouts, PDF e impresión. Thermal/ruler/scanner/private corpus: **NOT RUN**.

## F3 — Firma visual — automated PASS / manual QA pending
PNG, drag/resize/duplicate/delete, photo prep, InkCanvas, biblioteca local y Save As. Draft PR #22 open/unmerged.

## F4 — Lector completo — automated closure PASS / manual QA pending
Scroll virtualizado, thumbnails, búsqueda/copia, bookmarks/links, password, shortcuts/recientes. Draft PR #23 open/unmerged.

## F5 — Organizar — automated closure PASS / manual QA pending
Mover/reordenar/rotar/eliminar/duplicar, insertar/extract/merge/split y preflight estructural. Draft PR #24 open/unmerged.

## F6 — Imágenes — automated closure PASS / manual QA pending
Objetos imagen reales, extract/replace, move/resize/rotate/delete, opacity/z-order, undo/redo y Save As transaccional. Closure `c6d762efca01d50bfe3932d1f05617190a464fc6`; draft PR #25 open/unmerged.

## F7 — Texto V1 — automated functional closure PASS / Task 14 finalization

Entregado:

- discovery de objetos TEXT top-level de página activa;
- selección mixta imagen/texto determinista;
- edición conservadora con read-only explícito;
- `OriginalFont` cuando es segura;
- fallback DejaVu Sans 2.37 offline mediante CID Type2 + `ToUnicode` + `CIDToGIDMap` explícitos;
- propiedades básicas Texto V1;
- writer/validator combinados imagen+texto;
- preservación estructural medida de nuevo con writer F7 real;
- hardening de lifecycle/memoria/handles/temp/baselines.

`TEXT-01..04`: **AUTO PASS**.  
Task-13 checkpoint: `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`; CI `38089770428`; build 0/0; tests 766/766.

Manual Windows text-edit/offline QA: **NOT RUN**.

## F8 — Comentarios — pending
Highlight, underline/strikeout, notas, dibujo/formas. Próxima fase solo después del cierre F7; no iniciar dentro de Task 14.

## F9 — Utilidades — pending
Solo las justificadas y offline.

## F10 — OCR — pending
Tesseract local, documento escaneado → texto buscable.

## F11 — Texto V2 — pending
Líneas/párrafos, reading order y reflow limitado.

## F12 — Profesional — pending
Redacción real, formularios, firma criptográfica, compare, batch y conversiones auditadas.

## Regla de avance
Cada fase debe quedar usable y probada. Ninguna función principal puede requerir Internet. No añadir infraestructura preventiva. No mergear a `main` sin aprobación explícita.
