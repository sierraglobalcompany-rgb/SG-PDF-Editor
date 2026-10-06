# Arquitectura KISS

## Principio
Una aplicación de escritorio no necesita arquitectura distribuida. Empezamos con lo mínimo que permita abrir, modificar y guardar PDFs de forma estable. Separaremos componentes solo cuando el código lo exija.

## Estructura objetivo inicial

```text
src/SGPdf.App/
  App.xaml
  MainWindow.xaml
  Pdf/
    PdfiumNative.cs
    PdfDocumentSession.cs
    PdfPageRenderer.cs
    PdfObjectInspector.cs
    PdfSave.cs
  Features/
    Reader/
    Sign/
    Edit/
    Organize/
  Models/
  Utilities/

tests/SGPdf.App.Tests/
```

Solo dos proyectos: aplicación + pruebas.

## Tecnología
- C# / .NET 10 / WPF.
- PDFium como motor principal.
- Binario PDFium actualizado desde `bblanchon/pdfium-binaries`.
- Interop directo y pequeño: exponer únicamente las llamadas PDFium que usamos.
- Sin contenedor de DI.
- MVVM pragmático: ViewModels donde ayuden; code-behind permitido para interacción puramente visual (drag, resize, hit testing).

## Motor PDF
`PdfDocumentSession` representa un documento abierto y es dueño del handle nativo.

Responsabilidades iniciales:
- abrir/cerrar;
- número y tamaño de páginas;
- renderizar;
- listar objetos de página;
- insertar firma/imagen;
- editar texto simple;
- reemplazar imagen;
- guardar copia.

No existe una jerarquía de `IPdf*Service` hasta que tengamos una segunda implementación real que justifique interfaces.

## Dependencias bajo demanda
- `qpdf`: se añade en Organizar/Seguridad si simplifica merge, split o cifrado. Preferir invocarlo como proceso CLI antes que crear bindings propios.
- `Tesseract`: solo en OCR.
- `PdfPig`: solo si PDFium no basta para análisis/reflujo de texto.
- `PDFsharp`: solo si una operación concreta resulta claramente más sencilla que con PDFium.

## Undo / Redo
Una única abstracción pequeña cuando llegue edición:

```csharp
public interface IUndoableAction
{
    void Do();
    void Undo();
}
```

No construir un framework de comandos antes de necesitar la primera operación editable.

## Guardado
Durante MVP:
- `Guardar como` es la acción segura por defecto.
- no sobrescribir el original automáticamente;
- mantener estado `IsDirty`;
- generar contenido PDF solo al guardar cuando PDFium lo requiera.

## Rendimiento
- renderizar solo páginas visibles + vecinas;
- miniaturas en baja resolución;
- caché limitada por memoria;
- cancelar render obsoleto al cambiar rápido de página/zoom;
- no precargar todo el documento.
