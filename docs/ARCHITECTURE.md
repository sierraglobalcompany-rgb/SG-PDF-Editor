# Arquitectura

## Capas

### SGPdf.App
WPF + MVVM. Solo contiene presentación, comandos de UI y composición de vistas.

### SGPdf.Core
Contratos independientes de cualquier motor PDF.

Interfaces previstas:
- `IPdfDocumentService`
- `IPdfRenderService`
- `IPdfPageService`
- `IPdfAnnotationService`
- `IPdfOcrService`
- `IPdfExportService`

### SGPdf.Infrastructure
Implementaciones concretas de los contratos del Core.

Adaptadores previstos:
- PDFium: renderizado.
- qpdf: merge/split/rotate/encryption/repair.
- PoDoFo: edición y objetos PDF.
- Tesseract: OCR.

## Regla central
La UI nunca debe llamar directamente a PDFium, qpdf, PoDoFo o Tesseract.

## Modelo de edición
El documento abierto mantiene:
- ruta original;
- estado de páginas;
- operaciones pendientes;
- indicador de cambios;
- historial futuro de undo/redo.

El guardado debe soportar inicialmente `Save As` para proteger el documento original.
