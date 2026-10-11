using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal enum PageNumberAnchor
{
    HeaderLeft,
    HeaderCenter,
    HeaderRight,
    FooterLeft,
    FooterCenter,
    FooterRight
}

internal sealed class PageNumberingPlanner
{
    private const double MarginPoints = 36d;

    internal IReadOnlyList<TextOverlayPlacement> Plan(
        PageNumberingSettings settings,
        UtilityPageRange range,
        IReadOnlyList<PdfPageSize> pageSizes,
        PageNumberAnchor anchor,
        double fontSize,
        double opacity,
        TextOverlayLayer layer,
        double offsetXPoints,
        double offsetYPoints)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(range);
        ArgumentNullException.ThrowIfNull(pageSizes);

        ValidateOverlayStyle(fontSize, opacity);
        ValidateFinite(offsetXPoints, nameof(offsetXPoints));
        ValidateFinite(offsetYPoints, nameof(offsetYPoints));

        var selectedPages = range.Resolve(pageSizes.Count);
        var placements = new TextOverlayPlacement[selectedPages.Count];

        for (var ordinal = 0; ordinal < selectedPages.Count; ordinal++)
        {
            var pageIndex = selectedPages[ordinal];
            var pageSize = pageSizes[pageIndex];
            ValidatePageSize(pageSize, pageIndex);

            var number = settings.NumberAt(ordinal);
            var text = PageNumberFormatter.Format(number, settings);
            var (anchorX, anchorY) = ResolveAnchor(pageSize, anchor);

            placements[ordinal] = new TextOverlayPlacement(
                pageIndex,
                text,
                fontSize,
                opacity,
                layer,
                new PdfObjectMatrix(
                    1d,
                    0d,
                    0d,
                    1d,
                    anchorX + offsetXPoints,
                    anchorY + offsetYPoints),
                settings.FillColor);
        }

        return Array.AsReadOnly(placements);
    }

    private static (double X, double Y) ResolveAnchor(PdfPageSize pageSize, PageNumberAnchor anchor)
    {
        var left = MarginPoints;
        var centerX = pageSize.WidthPoints / 2d;
        var right = pageSize.WidthPoints - MarginPoints;
        var footer = MarginPoints;
        var header = pageSize.HeightPoints - MarginPoints;

        return anchor switch
        {
            PageNumberAnchor.HeaderLeft => (left, header),
            PageNumberAnchor.HeaderCenter => (centerX, header),
            PageNumberAnchor.HeaderRight => (right, header),
            PageNumberAnchor.FooterLeft => (left, footer),
            PageNumberAnchor.FooterCenter => (centerX, footer),
            PageNumberAnchor.FooterRight => (right, footer),
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
