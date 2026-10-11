using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Comments;

internal static class CommentHitTester
{
    internal static PdfCommentInfo? HitTest(
        IReadOnlyList<PdfCommentInfo> comments,
        CommentPoint point,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(comments);
        if (!double.IsFinite(tolerance) || tolerance < 0d)
            throw new ArgumentOutOfRangeException(nameof(tolerance));

        return comments
            .Where(comment => comment is not null)
            .OrderByDescending(comment => comment.AnnotationIndex)
            .FirstOrDefault(comment => Contains(comment, point, tolerance));
    }

    internal static PdfCommentInfo? HitTestDevice(
        IReadOnlyList<PdfCommentInfo> comments,
        double deviceX,
        double deviceY,
        PdfPageDeviceTransform transform,
        double deviceTolerance)
    {
        ArgumentNullException.ThrowIfNull(comments);
        if (!double.IsFinite(deviceX) || !double.IsFinite(deviceY))
            throw new ArgumentOutOfRangeException(nameof(deviceX));
        if (!double.IsFinite(deviceTolerance) || deviceTolerance < 0d)
            throw new ArgumentOutOfRangeException(nameof(deviceTolerance));

        transform.Validate();
        var pdfPoint = new CommentPoint(
            transform.OriginX + (deviceX * transform.XAxisX) + (deviceY * transform.YAxisX),
            transform.OriginY + (deviceX * transform.XAxisY) + (deviceY * transform.YAxisY));

        var xScale = Math.Sqrt((transform.XAxisX * transform.XAxisX) + (transform.XAxisY * transform.XAxisY));
        var yScale = Math.Sqrt((transform.YAxisX * transform.YAxisX) + (transform.YAxisY * transform.YAxisY));
        var pdfTolerance = deviceTolerance * Math.Max(xScale, yScale);
        return HitTest(comments, pdfPoint, pdfTolerance);
    }

    private static bool Contains(PdfCommentInfo comment, CommentPoint point, double tolerance)
    {
        return comment.Subtype switch
        {
            CommentSubtype.Highlight or CommentSubtype.Underline or CommentSubtype.Strikeout
                => HitQuads(comment.Quads, point, tolerance),
            CommentSubtype.Ink
                => HitInk(comment.Strokes, comment.BorderWidth, point, tolerance),
            CommentSubtype.Circle
                => comment.Rect.HasValue && HitEllipse(comment.Rect.Value, point, tolerance),
            CommentSubtype.Text or CommentSubtype.Square
                => comment.Rect.HasValue && HitRect(comment.Rect.Value, point, tolerance),
            _ => comment.Rect.HasValue && HitRect(comment.Rect.Value, point, tolerance)
        };
    }

    private static bool HitQuads(
        IReadOnlyList<CommentQuad> quads,
        CommentPoint point,
        double tolerance)
    {
        foreach (var quad in quads)
        {
            var polygon = new[] { quad.P1, quad.P2, quad.P4, quad.P3 };
            if (PointInPolygon(polygon, point))
                return true;

            if (tolerance > 0d)
            {
                for (var index = 0; index < polygon.Length; index++)
                {
                    var next = (index + 1) % polygon.Length;
                    if (DistanceToSegment(point, polygon[index], polygon[next]) <= tolerance)
                        return true;
                }
            }
        }

        return false;
    }

    private static bool HitInk(
        IReadOnlyList<CommentStroke> strokes,
        double? borderWidth,
        CommentPoint point,
        double tolerance)
    {
        var effectiveTolerance = tolerance + Math.Max(0d, borderWidth ?? 0d) / 2d;
        foreach (var stroke in strokes)
        {
            for (var index = 0; index < stroke.Points.Count - 1; index++)
            {
                if (DistanceToSegment(point, stroke.Points[index], stroke.Points[index + 1]) <= effectiveTolerance)
                    return true;
            }
        }

        return false;
    }

    private static bool HitRect(CommentRect rect, CommentPoint point, double tolerance) =>
        point.X >= rect.Left - tolerance &&
        point.X <= rect.Right + tolerance &&
        point.Y >= rect.Bottom - tolerance &&
        point.Y <= rect.Top + tolerance;

    private static bool HitEllipse(CommentRect rect, CommentPoint point, double tolerance)
    {
        var radiusX = ((rect.Right - rect.Left) / 2d) + tolerance;
        var radiusY = ((rect.Top - rect.Bottom) / 2d) + tolerance;
        if (radiusX <= 0d || radiusY <= 0d)
            return false;

        var centerX = (rect.Left + rect.Right) / 2d;
        var centerY = (rect.Bottom + rect.Top) / 2d;
        var normalizedX = (point.X - centerX) / radiusX;
        var normalizedY = (point.Y - centerY) / radiusY;
        return (normalizedX * normalizedX) + (normalizedY * normalizedY) <= 1d;
    }

    private static bool PointInPolygon(IReadOnlyList<CommentPoint> polygon, CommentPoint point)
    {
        var inside = false;
        for (var current = 0; current < polygon.Count; current++)
        {
            var previous = current == 0 ? polygon.Count - 1 : current - 1;
            var a = polygon[current];
            var b = polygon[previous];

            var crosses = (a.Y > point.Y) != (b.Y > point.Y);
            if (!crosses)
                continue;

            var denominator = b.Y - a.Y;
            if (Math.Abs(denominator) < 1e-12d)
                continue;

            var xAtY = ((b.X - a.X) * (point.Y - a.Y) / denominator) + a.X;
            if (point.X < xAtY)
                inside = !inside;
        }

        return inside;
    }

    private static double DistanceToSegment(CommentPoint point, CommentPoint start, CommentPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared <= 1e-24d)
            return Distance(point, start);

        var projection = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;
        var t = Math.Clamp(projection, 0d, 1d);
        var closest = new CommentPoint(start.X + (t * dx), start.Y + (t * dy));
        return Distance(point, closest);
    }

    private static double Distance(CommentPoint left, CommentPoint right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
