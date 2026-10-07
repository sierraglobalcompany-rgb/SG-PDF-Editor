using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Sign;

internal static class SignatureCoordinateMapper
{
    internal static PdfRect DeviceRectToPdfRect(
        SignatureDeviceRect rect,
        PdfPageDeviceTransform transform)
    {
        ValidateDeviceRect(rect);
        transform.Validate();

        var p1 = DeviceToPdf(rect.X, rect.Y, transform);
        var p2 = DeviceToPdf(rect.X + rect.Width, rect.Y, transform);
        var p3 = DeviceToPdf(rect.X, rect.Y + rect.Height, transform);
        var p4 = DeviceToPdf(rect.X + rect.Width, rect.Y + rect.Height, transform);

        return BoundsFromPoints(p1, p2, p3, p4);
    }

    internal static SignatureDeviceRect PdfRectToDeviceRect(
        PdfRect rect,
        PdfPageDeviceTransform transform)
    {
        ValidatePdfRect(rect);
        transform.Validate();

        var p1 = PdfToDevice(rect.Left, rect.Bottom, transform);
        var p2 = PdfToDevice(rect.Right, rect.Bottom, transform);
        var p3 = PdfToDevice(rect.Left, rect.Top, transform);
        var p4 = PdfToDevice(rect.Right, rect.Top, transform);

        var minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
        var maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
        var minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        var maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

        return new SignatureDeviceRect(minX, minY, maxX - minX, maxY - minY);
    }

    internal static PdfRect GetVisiblePdfBounds(PdfPageDeviceTransform transform)
    {
        transform.Validate();
        return DeviceRectToPdfRect(
            new SignatureDeviceRect(0d, 0d, transform.DeviceWidth, transform.DeviceHeight),
            transform);
    }

    private static (double X, double Y) DeviceToPdf(
        double deviceX,
        double deviceY,
        PdfPageDeviceTransform transform)
        => (
            transform.OriginX + (deviceX * transform.XAxisX) + (deviceY * transform.YAxisX),
            transform.OriginY + (deviceX * transform.XAxisY) + (deviceY * transform.YAxisY));

    private static (double X, double Y) PdfToDevice(
        double pdfX,
        double pdfY,
        PdfPageDeviceTransform transform)
    {
        var determinant = (transform.XAxisX * transform.YAxisY) -
                          (transform.XAxisY * transform.YAxisX);
        if (Math.Abs(determinant) < 1e-12d)
            throw new ArgumentException("PDF device transform is singular.", nameof(transform));

        var dx = pdfX - transform.OriginX;
        var dy = pdfY - transform.OriginY;
        var x = ((dx * transform.YAxisY) - (dy * transform.YAxisX)) / determinant;
        var y = ((transform.XAxisX * dy) - (transform.XAxisY * dx)) / determinant;
        return (x, y);
    }

    private static PdfRect BoundsFromPoints(params (double X, double Y)[] points)
    {
        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        return new PdfRect(minX, minY, maxX - minX, maxY - minY);
    }

    private static void ValidateDeviceRect(SignatureDeviceRect rect)
    {
        if (!double.IsFinite(rect.X) || !double.IsFinite(rect.Y) ||
            !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height) ||
            rect.Width <= 0d || rect.Height <= 0d)
        {
            throw new ArgumentException("Invalid device rectangle.", nameof(rect));
        }
    }

    private static void ValidatePdfRect(PdfRect rect)
    {
        if (!double.IsFinite(rect.Left) || !double.IsFinite(rect.Bottom) ||
            !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height) ||
            rect.Width <= 0d || rect.Height <= 0d)
        {
            throw new ArgumentException("Invalid PDF rectangle.", nameof(rect));
        }
    }
}
