using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal sealed record PdfImageWatermarkPlacement(int PageIndex, PdfObjectMatrix Matrix);

internal static class PdfImageWatermarkPlanner
{
    private const double MarginPoints = 12d;
    private const double MinimumWidthPercent = 10d;
    private const double MaximumWidthPercent = 80d;
    private const int MaximumSelectedPages = 150;

    internal static IReadOnlyList<PdfImageWatermarkPlacement> Plan(
        int imageWidth,
        int imageHeight,
        double widthPercent,
        UtilityPageRange range,
        IReadOnlyList<PdfPageSize> pageSizes,
        WatermarkAnchor anchor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(imageWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(imageHeight);
        ArgumentNullException.ThrowIfNull(range);
        ArgumentNullException.ThrowIfNull(pageSizes);

        if (!double.IsFinite(widthPercent) ||
            widthPercent < MinimumWidthPercent ||
            widthPercent > MaximumWidthPercent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(widthPercent),
                "El ancho debe estar entre 10 % y 80 % de la página.");
        }

        if (anchor is not (WatermarkAnchor.Center or WatermarkAnchor.TopLeft or WatermarkAnchor.TopRight or
            WatermarkAnchor.BottomLeft or WatermarkAnchor.BottomRight))
        {
            throw new ArgumentOutOfRangeException(nameof(anchor), "La posición seleccionada no está disponible para imágenes.");
        }

        var selectedPages = range.Resolve(pageSizes.Count);
        if (selectedPages.Count > MaximumSelectedPages)
            throw new ArgumentOutOfRangeException(nameof(range), "Se pueden seleccionar hasta 150 páginas por operación.");

        var aspectRatio = imageWidth / (double)imageHeight;
        var placements = new PdfImageWatermarkPlacement[selectedPages.Count];
        for (var ordinal = 0; ordinal < selectedPages.Count; ordinal++)
        {
            var pageIndex = selectedPages[ordinal];
            var page = pageSizes[pageIndex];
            if (!double.IsFinite(page.WidthPoints) || !double.IsFinite(page.HeightPoints) ||
                page.WidthPoints <= 2d * MarginPoints || page.HeightPoints <= 2d * MarginPoints)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pageSizes),
                    $"La página {pageIndex + 1} no tiene un área útil válida para la marca de agua.");
            }

            var width = Math.Min(
                Math.Min(widthPercent / 100d * page.WidthPoints, page.WidthPoints - 2d * MarginPoints),
                (page.HeightPoints - 2d * MarginPoints) * aspectRatio);
            var height = width / aspectRatio;
            var x = anchor switch
            {
                WatermarkAnchor.TopLeft or WatermarkAnchor.BottomLeft => MarginPoints,
                WatermarkAnchor.TopRight or WatermarkAnchor.BottomRight => page.WidthPoints - MarginPoints - width,
                WatermarkAnchor.Center => (page.WidthPoints - width) / 2d,
                _ => throw new ArgumentOutOfRangeException(nameof(anchor))
            };
            var y = anchor switch
            {
                WatermarkAnchor.TopLeft or WatermarkAnchor.TopRight => page.HeightPoints - MarginPoints - height,
                WatermarkAnchor.BottomLeft or WatermarkAnchor.BottomRight => MarginPoints,
                WatermarkAnchor.Center => (page.HeightPoints - height) / 2d,
                _ => throw new ArgumentOutOfRangeException(nameof(anchor))
            };

            if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0d || height <= 0d ||
                !double.IsFinite(x) || !double.IsFinite(y))
            {
                throw new InvalidOperationException("No se pudo resolver una geometría finita para la marca de agua.");
            }

            placements[ordinal] = new PdfImageWatermarkPlacement(
                pageIndex,
                new PdfObjectMatrix(width, 0d, 0d, height, x, y));
        }

        return Array.AsReadOnly(placements);
    }
}
