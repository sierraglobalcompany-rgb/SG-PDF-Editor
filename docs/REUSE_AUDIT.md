# Auditoría de repositorios reutilizables

## Regla

Reutilizar solo cuando reduzca código o riesgo. Runtime offline. Preferir MIT/BSD/Apache-2.0. Ninguna dependencia entra por popularidad: debe resolver una necesidad concreta de la fase activa.

## 1. bblanchon/pdfium-binaries — USAR

- distribución actual de PDFium para Windows;
- evita compilar Chromium/PDFium;
- licencia del repositorio de distribución: MIT;
- PDFium subyacente usa licencia permisiva BSD-style;
- integrar mediante P/Invoke mínimo propio;
- versión fijada explícitamente y auditada antes de actualizar.

## 2. BinaryKits/BinaryKits.Zpl — CANDIDATO PREFERENTE

- MIT;
- .NET in-process;
- viewer local;
- usa SkiaSharp/HarfBuzz y ZXing.Net;
- puede generar bitmap y PDF mediante Skia;
- soporta Code128, QR, DataMatrix y múltiples elementos;
- soporte conocido de `^DF`, `^XF` y `^CI28`;
- barcodes rasterizados con nearest-neighbor.

**Estado:** no incorporarlo todavía al runtime principal hasta pasar Gate ZPL-A con corpus real/sintético. Si la fidelidad es suficiente, será el motor ZPL elegido por KISS.

## 3. GOODBOY008/labelize — FALLBACK CONDICIONADO

- offline;
- ZPL/EPL;
- CLI/librería Rust;
- buena cobertura y pruebas de fidelidad;
- puede generar PNG/PDF.

### Riesgos

- proceso/binario adicional si se usa CLI;
- I/O/temporales y packaging extra;
- posible fricción EDR/antivirus;
- `ZplGSCustom.ttf` tiene procedencia que no queremos asumir para release estable.

**Regla:** usarlo solo si BinaryKits no logra la fidelidad necesaria. No distribuir en release estable mientras el activo dudoso no quede reemplazado/eliminado de forma verificable.

## 4. empira/PDFsharp — USAR SOLO DONDE SIMPLIFIQUE COMPOSICIÓN

- MIT;
- activamente mantenido;
- útil para crear páginas nuevas y componer etiquetas;
- `XPdfForm` es candidato para colocar PDFs de etiquetas en grids;
- `XImage.Interpolate=false` cuando se inserte bitmap que no debe suavizarse.

No usar como visor/editor principal.

## 5. kudosscience/pdf-chisel — REFERENCIA TÉCNICA

- MIT;
- demuestra edición de texto/imágenes con PDFium, render, guardado, thumbnails y undo/redo;
- no adoptar Electron/Node/N-API;
- reutilizar patrones/técnicas permisivas con atribución cuando corresponda.

## 6. MilosKonecny/PDFiumDotNET — REFERENCIA DE VISOR

- MIT;
- patrones WPF para continuo, thumbnails, bookmarks, búsqueda/anotaciones;
- no quedar atados a binarios antiguos.

## 7. ArgusMagnus/PDFiumSharp y wrappers antiguos — REFERENCIA

Útiles para estudiar firmas P/Invoke, no para introducir paquetes desactualizados como dependencia central.

## 8. qpdf/qpdf — RESERVA, NO NÚCLEO

- Apache-2.0;
- fuerte en estructura/cifrado/merge/split;
- evaluar solo si PDFium falla en una necesidad concreta.

## 9. pdfcpu/pdfcpu — RESERVA, NO NÚCLEO

- Apache-2.0;
- muchas operaciones estructurales/utilidades;
- competiría con qpdf solo cuando exista una carencia real de PDFium;
- no integrar ambos preventivamente.

## 10. UglyToad/PdfPig — RESERVA

- Apache-2.0;
- layout/reading order/texto avanzado;
- solo Texto V2 si PDFium no basta.

## 11. Tesseract — OCR FUTURO

- Apache-2.0;
- solo F10 OCR.

## 12. ZXing.Net — TESTS / SOPORTE ZPL

- Apache-2.0;
- útil para validar automáticamente que Code128/QR siguen decodificando tras render/composición;
- ya puede llegar transitivamente con BinaryKits; registrar licencia/notices reales si entra.

## 13. Repos/licencias rechazadas para el núcleo

### MuPDF
AGPL/comercial. No encaja con aplicación cerrada/gratuita sin obligaciones adicionales.

### iText
AGPL/comercial. Fuera del runtime.

### Ghostscript
AGPL/comercial. Fuera del runtime.

### KillerPDF
GPL-3.0. Solo referencia funcional, no copiar código al producto.

### Labelary API
Online. Puede servir como referencia manual con datos sintéticos durante investigación, nunca como dependencia de producción ni con datos reales.

## Dependencias por fase

### A0/F0
- PDFium únicamente como runtime adicional ya presente.

### F1
- evaluar BinaryKits y Labelize en harness; no congelar ambos en el producto.

### F2
- motor ZPL ganador;
- PDFsharp si XPdfForm/layout reduce código;
- ZXing.Net para validación/tests cuando corresponda.

### F10
- Tesseract.

Nada más hasta que una función concreta lo justifique.

## Regla de admisión

Antes de añadir cualquier dependencia responder:

1. ¿Resuelve una necesidad actual?
2. ¿Reduce código/riesgo frente al stack existente?
3. ¿Funciona offline?
4. ¿No requiere key/cuenta/pago?
5. ¿La licencia permite nuestra distribución?
6. ¿Las dependencias transitivas también son aceptables?
7. ¿Está mantenida?
8. ¿Podemos fijar versión/checksum?
9. ¿Podemos retirarla si deja de servir?

Si no hay una respuesta clara y favorable, no entra.
