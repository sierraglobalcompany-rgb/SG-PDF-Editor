# Arquitectura KISS

> Fuente completa: `MASTER_CONTEXT.md`. Plan de ejecución: `MASTER_PLAN.md`.

## Principio

Una aplicación Windows local no necesita arquitectura distribuida. Se usa la menor cantidad de piezas capaz de resolver correctamente PDF + etiquetas térmicas.

## Estructura inicial

```text
src/SGPdf.App/
  Pdf/
    PdfiumNative.cs
    PdfiumRuntime.cs
    PdfDocumentSession.cs
    PdfRenderScheduler.cs
    PdfPageRenderer.cs
    PdfPrintService.cs
  Features/
    Reader/
    Labels/
    Sign/
    Edit/
    Organize/
    Comments/
  Utilities/

tests/SGPdf.App.Tests/
```

Solo dos proyectos mientras sea suficiente: aplicación + pruebas.

No crear `Core`, `Infrastructure`, `Domain`, DI container, buses o plugin framework preventivamente.

## Stack

- C# / .NET 10 / WPF.
- PDFium: motor PDF principal.
- BinaryKits.Zpl: candidato preferente para ZPL, sujeto a Gate ZPL-A.
- Labelize: fallback condicionado.
- PDFsharp: composición puntual de PDFs de etiquetas.
- Tesseract/PdfPig/qpdf/pdfcpu: únicamente bajo demanda en fases posteriores.

## PDFium

Se usa interop mínimo propio. `PdfDocumentSession` contiene el estado/handles necesarios de un documento, pero la exclusión nativa es global porque PDFium no es thread-safe.

```text
UI → background task → PdfRenderScheduler → SemaphoreSlim global → PDFium
```

Prioridad: página visible > acción explícita > vecinas > thumbnails.

Para renders cancelables se puede usar progressive render con `IFSDK_PAUSE`; en cancelación se cierra el render y se liberan bitmaps/handles managed y nativos.

## PDF

### Leer

Render bajo demanda, caché limitada, zoom y navegación. No renderizar todo el documento al abrir.

### Firmar

Firma visual PNG como objeto/overlay editable; insertar y guardar como copia. Firma criptográfica es otro proyecto futuro.

### Editar

Menús contextuales. Imágenes: extraer/reemplazar/mover/resize/rotar. Texto V1: edición conservadora; si una fuente/subset no cubre nuevos code points, crear nuevo objeto con una TTF redistribuible auditada.

### Organizar

PDFium primero. Operaciones intra-documento mediante APIs de mover/rotar/eliminar; importación entre documentos mediante APIs de import pages. Antes se ejecuta preflight para firmas, formularios, bookmarks y otras estructuras que puedan no preservarse.

## Etiquetas ZPL

No implementar un intérprete completo propio. El código nuestro solo gestiona:

- archivos y diseños;
- cantidades `^PQ`;
- selección de cantidad;
- layout;
- cache;
- composición;
- impresión;
- warnings/errores.

### Gate ZPL-A

Comparar BinaryKits.Zpl y Labelize usando corpus real privado + fixtures sintéticos. Si BinaryKits tiene fidelidad suficiente, gana por KISS y Labelize sale del runtime.

### Pipeline preferido si gana BinaryKits

```text
ZPL → BinaryKits bitmap → preview WPF
ZPL → BinaryKits PDF → PDFsharp XPdfForm → hoja final → Windows Print
```

No duplicar renders por `^PQ`: `^PQ28` = un diseño + 28 copias.

## Impresión térmica

El medio PDF conserva tamaño físico exacto. Se consultan capacidades del driver Windows y no se escala un barcode silenciosamente para acomodar márgenes. Si un driver rompe la escala, el fallback primero será raster a resolución nativa mediante el driver, no RAW genérico.

## Undo/Redo

Se introduce con la primera edición reversible mediante comandos simples (`Execute/Undo`), sin framework adicional.

## Guardado

MVP: `Guardar como` por defecto. Reabrir y verificar salidas modificadas importantes.

## Red y privacidad

Reader/Labels/Sign/Edit no hacen HTTP en runtime. Los archivos reales Mercado Libre/clientes viven fuera del repo público.

## Dependencias

Toda dependencia nueva pasa por licencia, mantenimiento, offline, redistribución y necesidad actual antes de entrar. `third_party/manifest.json` registra el inventario aprobado/planificado.
