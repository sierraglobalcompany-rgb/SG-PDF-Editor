namespace SGPdf.App.Features.Comments;

internal sealed record CommentState
{
    internal CommentState(
        CommentKey key,
        int pageIndex,
        CommentSubtype subtype,
        CommentRect rect,
        CommentColor color,
        IReadOnlyList<CommentQuad>? quadPoints,
        string? contents,
        IReadOnlyList<CommentStroke>? inkStrokes,
        double? borderWidth,
        bool isNew,
        bool deleted)
    {
        if (key.Value == Guid.Empty)
            throw new ArgumentException("La identidad managed del comentario no puede estar vacía.", nameof(key));
        if (pageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (borderWidth is not null && (!double.IsFinite(borderWidth.Value) || borderWidth.Value <= 0d))
            throw new ArgumentOutOfRangeException(nameof(borderWidth));

        Key = key;
        PageIndex = pageIndex;
        Subtype = subtype;
        Rect = rect;
        Color = color;
        QuadPoints = Array.AsReadOnly((quadPoints ?? Array.Empty<CommentQuad>()).ToArray());
        Contents = contents;
        InkStrokes = Array.AsReadOnly((inkStrokes ?? Array.Empty<CommentStroke>()).ToArray());
        BorderWidth = borderWidth;
        IsNew = isNew;
        Deleted = deleted;
    }

    internal CommentKey Key { get; init; }
    internal int PageIndex { get; init; }
    internal CommentSubtype Subtype { get; init; }
    internal CommentRect Rect { get; init; }
    internal CommentColor Color { get; init; }
    internal IReadOnlyList<CommentQuad> QuadPoints { get; init; }
    internal string? Contents { get; init; }
    internal IReadOnlyList<CommentStroke> InkStrokes { get; init; }
    internal double? BorderWidth { get; init; }
    internal bool IsNew { get; init; }
    internal bool Deleted { get; init; }

    internal bool SemanticallyEquals(CommentState other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Key == other.Key
            && PageIndex == other.PageIndex
            && Subtype == other.Subtype
            && Rect == other.Rect
            && Color == other.Color
            && string.Equals(Contents, other.Contents, StringComparison.Ordinal)
            && Nullable.Equals(BorderWidth, other.BorderWidth)
            && IsNew == other.IsNew
            && Deleted == other.Deleted
            && QuadPoints.SequenceEqual(other.QuadPoints)
            && InkStrokesEqual(InkStrokes, other.InkStrokes);
    }

    private static bool InkStrokesEqual(
        IReadOnlyList<CommentStroke> left,
        IReadOnlyList<CommentStroke> right)
    {
        if (left.Count != right.Count)
            return false;

        for (var index = 0; index < left.Count; index++)
        {
            if (!left[index].Points.SequenceEqual(right[index].Points))
                return false;
        }

        return true;
    }
}

internal sealed record CommentCandidate(
    CommentOperationKind Kind,
    CommentKey Key,
    CommentState? ExpectedState,
    CommentState? NextState);

internal sealed record CommentMutation(
    CommentOperationKind Kind,
    CommentKey Key,
    CommentState? Before,
    CommentState? After);
