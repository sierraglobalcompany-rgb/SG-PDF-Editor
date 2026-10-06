# Auditoría de repositorios reutilizables

## Regla
Reutilizar solo cuando reduzca código/riesgo. Preferir MIT/BSD/Apache-2.0. Código GPL/AGPL se estudia como referencia funcional, pero no se copia al producto sin aprobación explícita.

## 1. bblanchon/pdfium-binaries — USAR
- Propósito: binarios PDFium actualizados para Windows.
- Valor: evita compilar Chromium/PDFium.
- Estrategia: consumir binario/paquete y mantener nuestro interop mínimo.
- Riesgo: API nativa; fijar versión y actualizar deliberadamente.

## 2. kudosscience/pdf-chisel — RECICLAR TÉCNICA/CÓDIGO SELECTIVO
- MIT.
- Ya demuestra PDFium para render, listar objetos, editar texto, reemplazar PNG/JPEG, guardar, thumbnails y undo/redo.
- Su capa nativa es C++/N-API para Electron; no adoptaremos Electron ni node-gyp.
- Portar a C# únicamente las ideas/llamadas PDFium necesarias, conservando atribución cuando se adapte código sustancial.
- Especialmente útil para: `FPDFText_SetText`, `FPDFImageObj_SetBitmap`, dirty pages y guardado diferido.

## 3. MilosKonecny/PDFiumDotNET — REFERENCIA DE VISOR
- MIT.
- Tiene WPF, vistas continuas, thumbnails, bookmarks, búsqueda y anotaciones.
- Última actividad principal observada es antigua respecto al PDFium actual; no queremos quedar atados a su binario.
- Reutilizar patrones de UI/render/caché cuando simplifiquen, no adoptar toda la librería inicialmente.

## 4. ArgusMagnus/PDFiumSharp — REFERENCIA DE INTEROP
- Permisivo/MIT, pero proyecto antiguo.
- Expone bindings de edición de PDFium útiles como referencia.
- No usar el paquete/binarios antiguos; adaptar solo firmas nativas que necesitemos.

## 5. qpdf/qpdf — USAR MÁS ADELANTE
- Apache-2.0 y activamente mantenido.
- Excelente para merge/split/cifrado/transformaciones estructurales.
- No renderiza ni edita visualmente.
- Estrategia KISS: invocar `qpdf.exe` como proceso cuando llegue Fase Organizar/Seguridad, antes de crear bindings.

## 6. UglyToad/PdfPig — RESERVA
- Apache-2.0, activo.
- Muy bueno para extracción, posiciones, imágenes y análisis de layout.
- No introducirlo mientras PDFium resuelva selección/hit testing/texto.
- Candidato para Text V2/reflow si aporta valor real.

## 7. PDFsharp — RESERVA
- MIT, activo y compatible con .NET moderno.
- Útil para generación/manipulación específica.
- No introducirlo por defecto porque duplicaría responsabilidades del motor.

## 8. Tesseract — USAR EN OCR
- Apache-2.0 y activo.
- Solo se incorpora al llegar Fase OCR.

## 9. KillerPDF — SOLO REFERENCIA FUNCIONAL
- Tiene muchas funciones similares y usa .NET 10/PDFium.
- GPL-3.0: no copiar código al producto si queremos conservar libertad de distribución/licenciamiento.
- Puede servir para comparar comportamiento y casos de uso.

## Selección inicial final
Dependencias del primer vertical slice:
1. .NET 10 / WPF.
2. PDFium actualizado (`bblanchon/pdfium-binaries`).
3. Nuestro interop mínimo C#.

Nada más salvo una dependencia pequeña que demuestre claramente reducir código.
