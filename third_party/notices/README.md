# Third-party notices

Este directorio contiene avisos/atribuciones adicionales requeridos por dependencias que entren al producto.

## Estado actual

El runtime actual referencia `bblanchon.PDFium.Win32` / PDFium. Sus textos de licencia están en `third_party/licenses/`.

## Cuando entre una nueva dependencia

Antes del merge se debe:

1. registrar versión/licencia en `third_party/manifest.json`;
2. revisar dependencias transitivas;
3. copiar LICENSE/NOTICE/credits requeridos;
4. registrar hash/content hash cuando sea posible;
5. verificar que el runtime siga funcionando offline.

BinaryKits.Zpl, PDFsharp, Tesseract, PdfPig, qpdf/pdfcpu están documentados como candidatos/futuros y no deben generar notices runtime hasta que realmente se incorporen al dependency graph.
