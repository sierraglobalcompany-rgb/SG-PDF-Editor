using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Reader;

internal static class ReaderLayoutPlanner
{
    internal static IReadOnlyList<ReaderPageGeometry> Build(
        IReadOnlyList<PdfPageSize> pages,
        PdfZoomState zoom,
        double viewportWidth,
        double viewportHeight,
        double horizontalPadding = 48d,
        double pageGap = 16d)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(zoom);

        if (pages.Count == 0)
            throw new ArgumentException("El lector requiere al menos una página.", nameof(pages));

        ValidatePositiveFinite(viewportWidth, nameof(viewportWidth));
        ValidatePositiveFinite(viewportHeight, nameof(viewportHeight));
        ValidateNonNegativeFinite(horizontalPadding, nameof(horizontalPadding));
        ValidateNonNegativeFinite(pageGap, nameof(pageGap));

        var result = new ReaderPageGeometry[pages.Count];
        var top = 0d;

        for (var index = 0; index < pages.Count; index++)
        {
            var page = pages[index];
            ValidatePositiveFinite(page.WidthPoints, nameof(page.WidthPoints));
            ValidatePositiveFinite(page.HeightPoints, nameof(page.HeightPoints));

            var dpi = zoom.ResolveDpi(
                page.WidthPoints,
                page.HeightPoints,
                viewportWidth,
                viewportHeight,
                horizontalPadding);
            var displayWidth = page.WidthPoints * dpi / 72d;
            var displayHeight = page.HeightPoints * dpi / 72d;
            var bottom = top + displayHeight;

            result[index] = new ReaderPageGeometry(
                index,
                page.WidthPoints,
                page.HeightPoints,
                displayWidth,
                displayHeight,
                top,
                bottom,
                dpi);

            top = bottom + pageGap;
        }

        return result;
    }

    internal static int FindCurrentPageIndex(
        IReadOnlyList<ReaderPageGeometry> pages,
        double verticalOffset,
        double viewportHeight)
    {
        ValidateLayoutInput(pages, verticalOffset, viewportHeight);

        var center = verticalOffset + viewportHeight / 2d;
        var bestIndex = 0;
        var bestDistance = DistanceToPage(center, pages[0]);

        for (var index = 1; index < pages.Count; index++)
        {
            var distance = DistanceToPage(center, pages[index]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    internal static ReaderRenderWindow GetRenderWindow(
        IReadOnlyList<ReaderPageGeometry> pages,
        double verticalOffset,
        double viewportHeight)
    {
        ValidateLayoutInput(pages, verticalOffset, viewportHeight);

        var viewportBottom = verticalOffset + viewportHeight;
        var visible = new List<int>();

        for (var index = 0; index < pages.Count; index++)
        {
            var page = pages[index];
            if (page.Bottom > verticalOffset && page.Top < viewportBottom)
                visible.Add(index);
        }

        if (visible.Count == 0)
            visible.Add(FindCurrentPageIndex(pages, verticalOffset, viewportHeight));

        var firstVisible = visible[0];
        var lastVisible = visible[^1];
        var firstRetained = Math.Max(0, firstVisible - 1);
        var lastRetained = Math.Min(pages.Count - 1, lastVisible + 1);
        var priority = new List<int>(visible.Count + 2);
        priority.AddRange(visible);

        if (firstRetained < firstVisible)
            priority.Add(firstRetained);
        if (lastRetained > lastVisible)
            priority.Add(lastRetained);

        return new ReaderRenderWindow(
            firstVisible,
            lastVisible,
            firstRetained,
            lastRetained,
            priority);
    }

    private static void ValidateLayoutInput(
        IReadOnlyList<ReaderPageGeometry> pages,
        double verticalOffset,
        double viewportHeight)
    {
        ArgumentNullException.ThrowIfNull(pages);
        if (pages.Count == 0)
            throw new ArgumentException("El lector requiere al menos una página.", nameof(pages));
        if (!double.IsFinite(verticalOffset) || verticalOffset < 0d)
            throw new ArgumentOutOfRangeException(nameof(verticalOffset));
        ValidatePositiveFinite(viewportHeight, nameof(viewportHeight));
    }

    private static double DistanceToPage(double y, ReaderPageGeometry page)
    {
        if (y < page.Top)
            return page.Top - y;
        if (y > page.Bottom)
            return y - page.Bottom;
        return 0d;
    }

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidateNonNegativeFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
