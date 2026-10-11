using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal enum WatermarkAnchor
{
    BottomLeft,
    BottomCenter,
    BottomRight,
    MiddleLeft,
    Center,
    MiddleRight,
    TopLeft,
    TopCenter,
    TopRight
}

internal sealed class WatermarkPlanner
{
    private const double MarginPoints = 36d;

    internal IReadOnlyList<TextOverlayPlacement> Plan(
        TextOverlaySettings settings,
        UtilityPageRange range,
        IReadOnlyList<PdfPageSize> pageSizes,
        WatermarkAnchor anchor,
        double offsetXPoints,
        double offsetYPoints,
        double rotationDegrees)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(range);
        ArgumentNullException.ThrowIfNull(pageSizes);

        ValidateOverlayStyle(settings.FontSize, settings.Opacity);
        ValidateFinite(offsetXPoints, nameof(offsetXPoints));
        ValidateFinite(offsetYPoints, nameof(offsetYPoints));
        ValidateFinite(rotationDegrees, nameof(rotationDegrees));

        var selectedPages = range.Resolve(pageSizes.Count);
        var radians = rotationDegrees * Math.PI / 180d;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        var placements = new TextOverlayPlacement[selectedPages.Count];

        for (var ordinal = 0; ordinal < selectedPages.Count; ordinal++)
        {
            var pageIndex = selectedPages[ordinal];
            var pageSize = pageSizes[pageIndex];
            ValidatePageSize(pageSize, pageIndex);

            var (anchorX, anchorY) = ResolveAnchor(pageSize, anchor);
            placements[ordinal] = new TextOverlayPlacement(
                pageIndex,
                settings.Text,
                settings.FontSize,
                settings.Opacity,
                settings.Layer,
                new PdfObjectMatrix(
                    cosine,
                    sine,
                    -sine,
                    cosine,
                    anchorX + offsetXPoints,
                    anchorY + offsetYPoints),
                settings.FillColor);
        }

        return Array.AsReadOnly(placements);
    }

    private static (double X, double Y) ResolveAnchor(PdfPageSize pageSize, WatermarkAnchor anchor)
    {
        var left = MarginPoints;
        var centerX = pageSize.WidthPoints / 2d;
        var right = pageSize.WidthPoints - MarginPoints;
        var bottom = MarginPoints;
        var middleY = pageSize.HeightPoints / 2d;
        var top = pageSize.HeightPoints - MarginPoints;

        return anchor switch
        {
            WatermarkAnchor.BottomLeft => (left, bottom),
            WatermarkAnchor.BottomCenter => (centerX, bottom),
            WatermarkAnchor.BottomRight => (right, bottom),
            WatermarkAnchor.MiddleLeft => (left, middleY),
            WatermarkAnchor.Center => (centerX, middleY),
            WatermarkAnchor.MiddleRight => (right, middleY),
            WatermarkAnchor.TopLeft => (left, top),
            WatermarkAnchor.TopCenter => (centerX, top),
            WatermarkAnchor.TopRight => (right, top),
            _ => throw new ArgumentOutOfRangeException(nameof(anchor))
        };
    }

    private static void ValidateOverlayStyle(double fontSize, double opacity)
    {
        if (!double.IsFinite(fontSize) || fontSize <= 0d)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be a positive finite number.");
        if (!double.IsFinite(opacity) || opacity < 0d || opacity > 1d)
            throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between zero and one.");
    }

    private static void ValidatePageSize(PdfPageSize pageSize, int pageIndex)
    {
        if (!double.IsFinite(pageSize.WidthPoints) || pageSize.WidthPoints <= 0d ||
            !double.IsFinite(pageSize.HeightPoints) || pageSize.HeightPoints <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"Page {pageIndex + 1} must have positive finite dimensions.");
        }
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
    }
}
