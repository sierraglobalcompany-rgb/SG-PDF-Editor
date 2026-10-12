using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal static class OcrCoordinateMapper
{
    internal static PdfOcrRect Map(PdfRenderedPage renderedPage, OcrPixelRect bounds)
    {
        ArgumentNullException.ThrowIfNull(renderedPage);

        var transform = renderedPage.DeviceTransform
            ?? throw new InvalidOperationException("La página renderizada no contiene la transformación de coordenadas PDF requerida para OCR.");
        transform.Validate();

        var right = (long)bounds.Left + bounds.Width;
        var bottom = (long)bounds.Top + bounds.Height;
        if (right > transform.DeviceWidth || bottom > transform.DeviceHeight)
            throw new ArgumentOutOfRangeException(nameof(bounds), "El rectángulo OCR está fuera del bitmap renderizado.");

        var topLeft = MapPoint(transform, bounds.Left, bounds.Top);
        var topRight = MapPoint(transform, right, bounds.Top);
        var bottomLeft = MapPoint(transform, bounds.Left, bottom);
        var bottomRight = MapPoint(transform, right, bottom);

        var leftPdf = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        var rightPdf = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        var bottomPdf = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        var topPdf = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));

        if (!double.IsFinite(leftPdf) || !double.IsFinite(bottomPdf) ||
            !double.IsFinite(rightPdf) || !double.IsFinite(topPdf) ||
            rightPdf <= leftPdf || topPdf <= bottomPdf)
        {
            throw new InvalidOperationException("La transformación OCR produjo un rectángulo PDF inválido.");
        }

        return new PdfOcrRect(leftPdf, bottomPdf, rightPdf, topPdf);
    }

    private static (double X, double Y) MapPoint(
        PdfPageDeviceTransform transform,
        long deviceX,
        long deviceY)
    {
        var pdfX = transform.OriginX +
                   (deviceX * transform.XAxisX) +
                   (deviceY * transform.YAxisX);
        var pdfY = transform.OriginY +
                   (deviceX * transform.XAxisY) +
                   (deviceY * transform.YAxisY);

        if (!double.IsFinite(pdfX) || !double.IsFinite(pdfY))
            throw new InvalidOperationException("La transformación OCR produjo coordenadas PDF no finitas.");

        return (pdfX, pdfY);
    }
}
