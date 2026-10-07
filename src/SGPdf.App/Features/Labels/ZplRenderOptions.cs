namespace SGPdf.App.Features.Labels;

public sealed record ZplRenderOptions
{
    private static readonly int[] SupportedDpmm = [6, 8, 12, 24];

    public static ZplRenderOptions Default { get; } = new(102d, 152d, 8);

    public ZplRenderOptions(double widthMm, double heightMm, int dpmm)
    {
        if (!double.IsFinite(widthMm) || widthMm <= 0d)
            throw new ArgumentOutOfRangeException(nameof(widthMm), "Label width must be positive and finite.");
        if (!double.IsFinite(heightMm) || heightMm <= 0d)
            throw new ArgumentOutOfRangeException(nameof(heightMm), "Label height must be positive and finite.");
        if (!SupportedDpmm.Contains(dpmm))
            throw new ArgumentOutOfRangeException(nameof(dpmm), "Labelize supports 6, 8, 12, or 24 dpmm.");

        WidthMm = widthMm;
        HeightMm = heightMm;
        Dpmm = dpmm;
    }

    public double WidthMm { get; }

    public double HeightMm { get; }

    public int Dpmm { get; }
}
