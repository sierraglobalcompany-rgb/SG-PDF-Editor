namespace SGPdf.App.Features.Comments;

internal enum CommentSubtype
{
    Text,
    Square,
    Circle,
    Highlight,
    Underline,
    Strikeout,
    Ink
}

internal enum CommentOperationKind
{
    Create,
    Update,
    Move,
    Resize,
    EditNoteText,
    Delete
}

internal readonly record struct CommentKey(Guid Value)
{
    internal static CommentKey New() => new(Guid.NewGuid());
}

internal readonly record struct CommentPoint
{
    internal CommentPoint(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
            throw new ArgumentException("Las coordenadas del comentario deben ser finitas.");

        X = x;
        Y = y;
    }

    internal double X { get; }
    internal double Y { get; }
}

internal readonly record struct CommentRect
{
    internal CommentRect(double left, double bottom, double right, double top)
    {
        if (!double.IsFinite(left) || !double.IsFinite(bottom) ||
            !double.IsFinite(right) || !double.IsFinite(top))
        {
            throw new ArgumentException("La geometría del comentario debe ser finita.");
        }

        if (right <= left || top <= bottom)
            throw new ArgumentException("El rectángulo del comentario debe tener ancho y alto positivos.");

        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    internal double Left { get; }
    internal double Bottom { get; }
    internal double Right { get; }
    internal double Top { get; }
}

internal readonly record struct CommentColor(byte Red, byte Green, byte Blue, byte Alpha);

internal readonly record struct CommentQuad(
    CommentPoint P1,
    CommentPoint P2,
    CommentPoint P3,
    CommentPoint P4);

internal sealed record CommentStroke
{
    internal CommentStroke(IReadOnlyList<CommentPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
            throw new ArgumentException("Un trazo Ink requiere al menos dos puntos.", nameof(points));

        Points = Array.AsReadOnly(points.ToArray());
    }

    internal IReadOnlyList<CommentPoint> Points { get; }
}
