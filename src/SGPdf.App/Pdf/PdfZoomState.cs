namespace SGPdf.App.Pdf;

public enum PdfZoomMode
{
    Manual,
    FitPage,
    FitWidth
}

public sealed class PdfZoomState
{
    private const double BaseDpi = 96d;
    private const double MaxDpi = BaseDpi * 4d;

    private static readonly int[] Presets =
    [
        25,
        50,
        75,
        100,
        125,
        150,
        200,
        300,
        400
    ];

    public PdfZoomState()
        : this(PdfZoomMode.Manual, 100)
    {
    }

    private PdfZoomState(PdfZoomMode mode, int manualPercent)
    {
        Mode = mode;
        ManualPercent = manualPercent;
    }

    public PdfZoomMode Mode { get; }

    public int ManualPercent { get; }

    public PdfZoomState ActualSize()
    {
        return new PdfZoomState(PdfZoomMode.Manual, 100);
    }

    public PdfZoomState FitPage()
    {
        return new PdfZoomState(PdfZoomMode.FitPage, ManualPercent);
    }

    public PdfZoomState FitWidth()
    {
        return new PdfZoomState(PdfZoomMode.FitWidth, ManualPercent);
    }

    public PdfZoomState ZoomIn(double currentDpi)
    {
        var currentPercent = ToPercent(currentDpi);
        foreach (var preset in Presets)
        {
            if (preset > currentPercent + 0.01d)
                return new PdfZoomState(PdfZoomMode.Manual, preset);
        }

        return new PdfZoomState(PdfZoomMode.Manual, Presets[^1]);
    }

    public PdfZoomState ZoomOut(double currentDpi)
    {
        var currentPercent = ToPercent(currentDpi);
        for (var index = Presets.Length - 1; index >= 0; index--)
        {
            if (Presets[index] < currentPercent - 0.01d)
                return new PdfZoomState(PdfZoomMode.Manual, Presets[index]);
        }

        return new PdfZoomState(PdfZoomMode.Manual, Presets[0]);
    }

    public double ResolveDpi(
        double pageWidthPoints,
        double pageHeightPoints,
        double viewportWidthPixels,
        double viewportHeightPixels,
        double paddingPixels = 48d)
    {
        if (Mode == PdfZoomMode.Manual)
            return BaseDpi * ManualPercent / 100d;

        ValidatePositiveFinite(pageWidthPoints, nameof(pageWidthPoints));
        ValidatePositiveFinite(pageHeightPoints, nameof(pageHeightPoints));
        ValidatePositiveFinite(viewportWidthPixels, nameof(viewportWidthPixels));
        ValidatePositiveFinite(viewportHeightPixels, nameof(viewportHeightPixels));

        if (!double.IsFinite(paddingPixels) || paddingPixels < 0d)
            throw new ArgumentOutOfRangeException(nameof(paddingPixels));

        var availableWidth = Math.Max(1d, viewportWidthPixels - paddingPixels);
        var availableHeight = Math.Max(1d, viewportHeightPixels - paddingPixels);
        var widthDpi = availableWidth * 72d / pageWidthPoints;

        if (Mode == PdfZoomMode.FitWidth)
            return Math.Min(widthDpi, MaxDpi);

        var heightDpi = availableHeight * 72d / pageHeightPoints;
        return Math.Min(Math.Min(widthDpi, heightDpi), MaxDpi);
    }

    public static int PercentFromDpi(double dpi)
    {
        return (int)Math.Round(ToPercent(dpi), MidpointRounding.AwayFromZero);
    }

    private static double ToPercent(double dpi)
    {
        ValidatePositiveFinite(dpi, nameof(dpi));
        return dpi / BaseDpi * 100d;
    }

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
