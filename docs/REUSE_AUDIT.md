# Auditoría de repositorios reutilizables

## Regla
Reutilizar solo cuando reduzca código/riesgo. Runtime offline. Preferir MIT/BSD/Apache-2.0. GPL/AGPL solo como referencia funcional salvo aprobación expresa.

## 1. bblanchon/pdfium-binaries — USAR
- PDFium actualizado para Windows.
- Evita compilar Chromium/PDFium.
- Interop mínimo propio.

## 2. GOODBOY008/labelize — USAR PARA ETIQUETAS
- MIT.
- Activo en 2026; publica binario Windows x64.
- CLI local: ZPL/EPL → PNG/PDF.
- No necesita Internet, servidor ni impresora.
- Soporta comandos relevantes para archivos reales: `^XA/^XZ`, `^LH`, `^CI`, `^FO/^FT`, `^A`, `^FD/^FS`, `^FB`, `^FR`, `^FH`, `^BC`, `^BQ`, `^GF/^GFA`, `^GB`, `^PQ` y otros.
- `^PQ` produce múltiples copias; nuestra app extraerá la cantidad y renderizará preview con 1 copia para permitir layouts personalizados.
- Tiene golden tests comparados contra renders de Labelary.
- Estrategia KISS: distribuir `labelize.exe` y llamarlo como proceso; NO integrar Rust ni levantar HTTP.

### Riesgos Labelize
- No es un firmware Zebra completo: algunos comandos ZPL pueden faltar o diferir.
- Por eso se mantiene un corpus de archivos reales Mercado Libre.
- Si encuentra comando no soportado, debemos informar; nunca ocultar el warning.
- Antes de fijar una nueva versión, comparar visualmente corpus de referencia.

## 3. BinaryKits/BinaryKits.Zpl — FALLBACK/REFERENCIA
- MIT y .NET nativo.
- Tiene viewer local, QR, Code128, gráficos y texto.
- Encaja bien con C#, pero la cobertura/fidelidad para ZPL real debe probarse contra nuestro corpus.
- No usar junto con Labelize de inicio; queda como alternativa si Labelize presenta un bloqueo serio.

## 4. empira/PDFsharp — USAR SOLO PARA PDF DE ETIQUETAS
- MIT, activamente mantenido.
- Versión estable 6.2.x soporta .NET 10.
- Muy simple para crear páginas nuevas y colocar PNGs.
- Responsabilidad: composición/export de hojas de etiquetas.
- PDFium sigue siendo lector/editor principal.

## 5. kudosscience/pdf-chisel — RECICLAR TÉCNICA/CÓDIGO SELECTIVO
- MIT.
- Demuestra PDFium para render, objetos, texto, imágenes, guardado, thumbnails y undo/redo.
- No adoptar Electron/Node/N-API.

## 6. MilosKonecny/PDFiumDotNET — REFERENCIA DE VISOR
- MIT.
- WPF, continuo, thumbnails, bookmarks, búsqueda y anotaciones.
- Reutilizar patrones; no quedar atados a binario antiguo.

## 7. ArgusMagnus/PDFiumSharp — REFERENCIA DE INTEROP
- Proyecto antiguo; útil para firmas P/Invoke de edición.
- No usar paquete/binarios viejos.

## 8. qpdf/qpdf — MÁS ADELANTE
- Apache-2.0.
- Merge/split/cifrado/estructura.
- Preferir CLI local.

## 9. UglyToad/PdfPig — RESERVA
- Apache-2.0.
- Layout/extracción avanzada si PDFium no basta.

## 10. Tesseract — OCR
- Apache-2.0.

## 11. KillerPDF — SOLO REFERENCIA
- GPL-3.0; no copiar código al producto.

## Dependencias aprobadas por fase
### Base PDF
- PDFium binaries.

### Etiquetas
- Labelize CLI local.
- PDFsharp Core estable.

### OCR futuro
- Tesseract.

Nada más hasta que una función concreta lo justifique.

## Prohibido para la solución
- Labelary API en runtime.
- APIs ZPL de pago.
- SaaS obligatorio.
- dependencias que requieran login/Internet para renderizar o imprimir.
