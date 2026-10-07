namespace SGPdf.App.Features.Labels;

public sealed record ZplRenderedLabel(
    int DesignIndex,
    double WidthMm,
    double HeightMm,
    int Dpmm,
    byte[] PngBytes);
