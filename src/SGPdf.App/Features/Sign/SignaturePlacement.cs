namespace SGPdf.App.Features.Sign;

public readonly record struct PdfRect(double Left, double Bottom, double Width, double Height)
{
    public double Right => Left + Width;
    public double Top => Bottom + Height;
}

internal readonly record struct SignatureDeviceRect(double X, double Y, double Width, double Height);

public sealed record SignaturePlacement(
    Guid Id,
    int PageIndex,
    PdfRect Bounds,
    SignatureAsset Asset);
