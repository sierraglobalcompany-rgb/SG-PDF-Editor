namespace SGPdf.App.Pdf;

internal readonly record struct PdfPageDeviceTransform(
    double OriginX,
    double OriginY,
    double XAxisX,
    double XAxisY,
    double YAxisX,
    double YAxisY,
    int DeviceWidth,
    int DeviceHeight)
{
    internal void Validate()
    {
        if (DeviceWidth <= 0 || DeviceHeight <= 0 ||
            !double.IsFinite(OriginX) || !double.IsFinite(OriginY) ||
            !double.IsFinite(XAxisX) || !double.IsFinite(XAxisY) ||
            !double.IsFinite(YAxisX) || !double.IsFinite(YAxisY))
        {
            throw new ArgumentException("Invalid PDF device transform.");
        }

        var determinant = (XAxisX * YAxisY) - (XAxisY * YAxisX);
        if (!double.IsFinite(determinant) || Math.Abs(determinant) < 1e-12d)
            throw new ArgumentException("PDF device transform is singular.");
    }
}
