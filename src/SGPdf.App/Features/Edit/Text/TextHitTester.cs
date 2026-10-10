using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Text;

internal static class TextHitTester
{
    private const double GeometryTolerance = 1e-9d;

    internal static TextObjectKey? HitTest(
        IReadOnlyList<PdfTextObjectInfo> texts,
        PdfPoint point)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
            return null;

        PdfTextObjectInfo? selected = null;
        foreach (var text in texts)
        {
            if (!Contains(text, point))
                continue;

            if (selected is null || text.Key.PageObjectIndex > selected.Key.PageObjectIndex)
                selected = text;
        }

        return selected?.Key;
    }

    private static bool Contains(PdfTextObjectInfo text, PdfPoint point)
    {
        var bounds = text.Bounds;
        if (point.X < bounds.Left - GeometryTolerance ||
            point.X > bounds.Right + GeometryTolerance ||
            point.Y < bounds.Bottom - GeometryTolerance ||
            point.Y > bounds.Top + GeometryTolerance)
        {
            return false;
        }

        var quad = text.Quad;
        var points = new[]
        {
            new PdfPoint(quad.X1, quad.Y1),
            new PdfPoint(quad.X2, quad.Y2),
            new PdfPoint(quad.X3, quad.Y3),
            new PdfPoint(quad.X4, quad.Y4)
        };

        if (points.Any(candidate => !double.IsFinite(candidate.X) || !double.IsFinite(candidate.Y)))
            return false;

        var twiceArea = 0d;
        for (var index = 0; index < points.Length; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Length];
            twiceArea += (current.X * next.Y) - (next.X * current.Y);
        }

        if (!double.IsFinite(twiceArea) || Math.Abs(twiceArea) < GeometryTolerance)
            return false;

        var hasPositive = false;
        var hasNegative = false;
        for (var index = 0; index < points.Length; index++)
        {
            var start = points[index];
            var end = points[(index + 1) % points.Length];
            var cross = ((end.X - start.X) * (point.Y - start.Y)) -
                        ((end.Y - start.Y) * (point.X - start.X));

            if (!double.IsFinite(cross))
                return false;

            if (cross > GeometryTolerance)
                hasPositive = true;
            else if (cross < -GeometryTolerance)
                hasNegative = true;

            if (hasPositive && hasNegative)
                return false;
        }

        return true;
    }
}
