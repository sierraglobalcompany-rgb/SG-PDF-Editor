using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal static class OcrRasterizer
{
    internal const double Dpi = 300d;
    internal const int MaxPixelDimension = 20_000;
    internal const long MaxTotalPixels = 100_000_000L;

    internal static PdfRenderedPage Render(
        PdfDocumentSession session,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var (widthPoints, heightPoints) = session.GetPageSize(pageIndex, cancellationToken);
        var pixelWidth = Math.Ceiling(widthPoints * Dpi / 72d);
        var pixelHeight = Math.Ceiling(heightPoints * Dpi / 72d);

        if (!double.IsFinite(pixelWidth) || !double.IsFinite(pixelHeight) ||
            pixelWidth <= 0d || pixelHeight <= 0d ||
            pixelWidth > MaxPixelDimension || pixelHeight > MaxPixelDimension ||
            pixelWidth * pixelHeight > MaxTotalPixels)
        {
            throw new InvalidOperationException(
                "La página supera el límite seguro de raster OCR a 300 DPI.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return session.RenderPage(pageIndex, Dpi, cancellationToken);
    }
}
