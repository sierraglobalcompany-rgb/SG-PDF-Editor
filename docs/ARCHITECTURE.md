# Arquitectura KISS

## Principio
Una aplicación Windows local no necesita arquitectura distribuida. Empezamos con lo mínimo que resuelva el trabajo real: PDF + etiquetas térmicas. Separamos componentes solo cuando el código lo exige.

## Estructura inicial
```text
src/SGPdf.App/
  App.xaml
  MainWindow.xaml
  Pdf/
    PdfiumNative.cs
    PdfDocumentSession.cs
    PdfPageRenderer.cs
  Features/
    Reader/
    Labels/
      LabelFileDetector.cs
      ZplJobParser.cs
      LabelizeCli.cs
      LabelLayout.cs
      LabelPdfExporter.cs
    Sign/
    Edit/
    Organize/
  Models/
  Utilities/
  ThirdPartyNotices/

tests/SGPdf.App.Tests/
```

Solo dos proyectos: aplicación + pruebas.

## Tecnología
- C# / .NET 10 / WPF.
- PDFium: PDF.
- Labelize `labelize.exe`: ZPL/EPL local.
- PDFsharp Core 6.2.x: creación de PDFs de etiquetas.
- Sin DI container.
- MVVM pragmático; code-behind permitido para drag/resize/hit testing y UI simple.

## PDF
`PdfDocumentSession` posee el handle PDFium y cubre abrir/cerrar/render/objetos/guardar según se necesite.

No existe una jerarquía `IPdf*Service` mientras no haya una segunda implementación real.

## Etiquetas
### `LabelFileDetector`
Determina si un `.txt/.prn/.zpl` contiene ZPL verificando bloques `^XA ... ^XZ`. No intenta comprender todo ZPL.

### `ZplJobParser`
Parser mínimo nuestro, exclusivamente para:
- separar cada bloque de etiqueta;
- leer `^PQ`;
- mantener el ZPL original;
- producir una copia temporal de preview con cantidad 1.

No renderiza texto, códigos ni gráficos.

### `LabelizeCli`
Invoca `labelize.exe` local con `UseShellExecute=false`, sin red y con archivos temporales controlados. Devuelve PNG/PDF o error. No se levanta servidor HTTP local porque no hace falta.

El ejecutable se distribuye con la aplicación junto a su licencia MIT y avisos de terceros.

### `LabelLayout`
Pura lógica .NET:
- cantidad de etiquetas;
- columnas/filas;
- márgenes;
- gaps;
- tamaño de hoja;
- rotación automática;
- escala uniforme.

Debe ser testeable sin WPF ni Labelize.

### `LabelPdfExporter`
PDFsharp crea un PDF nuevo y coloca los PNG renderizados en las posiciones calculadas por `LabelLayout`.

PDFsharp no se usa como lector/editor principal; su responsabilidad inicial termina en composición de PDFs nuevos.

## Política de cantidades
Cada diseño conserva:
- `OriginalZpl`;
- `RequestedCopies` desde `^PQ` o 1 si no existe;
- `SelectedCopies` elegido por el usuario;
- imagen de preview.

Esto evita que `^PQ28` se confunda con 28 diseños distintos.

## Impresión
Dos caminos:
1. generar PDF y abrir/imprimir;
2. imprimir directamente el layout mediante Windows cuando simplifique UX.

La impresora nunca necesita interpretar ZPL. Recibe el resultado gráfico/PDF por su driver de Windows.

## Red
En runtime, Reader/Labels/Sign/Edit no hacen llamadas HTTP. Debe existir una prueba/manual QA con adaptador de red deshabilitado.

## Dependencias bajo demanda
- qpdf: Organizar/Seguridad si simplifica merge/split/cifrado.
- Tesseract: OCR.
- PdfPig: solo análisis de texto avanzado si PDFium no basta.
- ZXing.Net: solo si añadimos generador/editor explícito de códigos de barras, no para render ZPL inicial.

## Undo / Redo
Se introduce únicamente cuando aparece la primera edición destructiva/reversible.

## Guardado
Durante MVP:
- `Guardar como` por defecto;
- no sobrescribir original automáticamente;
- archivos temporales se eliminan después del trabajo cuando sea seguro.

## Rendimiento
### PDF
- páginas visibles + vecinas;
- caché limitada;
- thumbnails a baja resolución.

### ZPL
- renderizar previews en background;
- caché por hash del bloque ZPL + tamaño + dpmm;
- no volver a renderizar diseños idénticos;
- exportar lotes secuencialmente o con concurrencia limitada para no disparar memoria.
