namespace SGPdf.App.Pdf;

public sealed record PdfPrintLayout(double X, double Y, double Width, double Height)
{
    public static PdfPrintLayout Fit(
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight)
    {
        if (sourceWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceWidth));
        if (sourceHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceHeight));
        if (targetWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetWidth));
        if (targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetHeight));

        var scale = Math.Min(targetWidth / sourceWidth, targetHeight / sourceHeight);
        var width = sourceWidth * scale;
        var height = sourceHeight * scale;
        var x = (targetWidth - width) / 2.0;
        var y = (targetHeight - height) / 2.0;

        return new PdfPrintLayout(x, y, width, height);
    }
}
