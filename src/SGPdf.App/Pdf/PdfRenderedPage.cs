namespace SGPdf.App.Pdf;

public sealed record PdfRenderedPage(
    int PageIndex,
    int PixelWidth,
    int PixelHeight,
    int Stride,
    double Dpi,
    byte[] Pixels);
