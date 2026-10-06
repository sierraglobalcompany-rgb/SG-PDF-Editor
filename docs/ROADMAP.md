# Roadmap KISS

> El detalle y criterios de aceptación viven en `MASTER_PLAN.md`.

## A0 — Higiene y reproducibilidad
- contexto/plan maestro;
- documentos alineados;
- manifest de terceros;
- licenses/notices;
- datos privados ignorados;
- mecanismo de lock NuGet;
- CI Windows verificable.

**Salida:** repo listo para ejecución sin contradicciones.

## F0 — PDF base
- abrir PDF;
- render real;
- scheduler PDFium global;
- cancelación/progressive render;
- zoom;
- navegación mínima;
- fit page/width;
- impresión.

**Salida:** lector PDF básico usable.

## F1 — Gate ZPL-A
- BinaryKits.Zpl vs Labelize;
- corpus real privado + sintético;
- `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF`, `^XF`;
- benchmark 10/100/500 diseños;
- fidelidad, barcode, RAM/CPU/I/O, packaging y licencias.

**Salida:** un único motor ZPL elegido por evidencia. BinaryKits es candidato preferente.

## F2 — Etiquetas ZPL offline
- abrir `.zpl/.txt/.prn`;
- diseños + cantidades `^PQ`;
- preview;
- cantidades archivo/una/custom;
- tamaños térmicos/A4/Carta/custom;
- layout 1/2/3/4/6/8/10/12/custom;
- PDF;
- impresión Windows;
- validación ZXing + prueba física.

**Salida:** reemplazar el flujo manual de Labelary.

## F3 — Firma visual
- PNG transparente;
- drag/resize/move;
- eliminar/duplicar;
- insertar en PDF;
- `Guardar como`;
- reabrir y verificar.

## F4 — Lector completo
- scroll continuo;
- miniaturas;
- búsqueda/copiar;
- bookmarks/links;
- password;
- shortcuts/recientes;
- tabs solo si no complica estabilidad.

## F5 — Organizar
- mover/reordenar/rotar/eliminar/duplicar;
- insertar/extract/merge/split;
- PDFium primero;
- preflight de firmas, formularios, bookmarks y otras estructuras.

## F6 — Imágenes
- clic derecho contextual;
- extraer/guardar;
- reemplazar;
- mover/resize/rotar/opacidad/orden;
- undo/redo.

## F7 — Texto V1
- detectar/seleccionar objeto;
- edición conservadora;
- fallback con TTF redistribuible si aparecen nuevos code points/subset dudoso;
- propiedades básicas;
- save/reopen validation.

## F8 — Comentarios
- highlight;
- underline/strikeout;
- notas;
- dibujo/formas.

## F9 — Utilidades
Solo las justificadas y offline: watermark, numeración, protección autorizada, optimización/reparación. qpdf/pdfcpu solo si PDFium demuestra una carencia concreta.

## F10 — OCR
Tesseract local, documento escaneado → texto buscable.

## F11 — Texto V2
- líneas/párrafos;
- reading order;
- reflow limitado;
- PdfPig solo si reduce complejidad.

## F12 — Profesional
- redacción real;
- formularios;
- firma criptográfica;
- compare;
- batch;
- conversiones auditadas.

## Regla de avance
Cada fase debe quedar usable y probada. Ninguna función principal puede requerir Internet. No añadir infraestructura preventiva.
