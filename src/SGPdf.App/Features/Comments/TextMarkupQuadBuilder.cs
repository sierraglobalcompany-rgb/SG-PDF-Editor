using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Comments;

internal static class TextMarkupQuadBuilder
{
    internal static IReadOnlyList<CommentQuad> BuildQuads(IReadOnlyList<PdfTextRect> rects)
    {
        var validated = Validate(rects);
        return validated
            .Select(rect => new CommentQuad(
                new CommentPoint(rect.Left, rect.Top),
                new CommentPoint(rect.Right, rect.Top),
                new CommentPoint(rect.Left, rect.Bottom),
                new CommentPoint(rect.Right, rect.Bottom)))
            .ToArray();
    }

    internal static CommentRect GetBounds(IReadOnlyList<PdfTextRect> rects)
    {
        var validated = Validate(rects);
        return new CommentRect(
            validated.Min(rect => rect.Left),
            validated.Min(rect => rect.Bottom),
            validated.Max(rect => rect.Right),
            validated.Max(rect => rect.Top));
    }

    private static IReadOnlyList<PdfTextRect> Validate(IReadOnlyList<PdfTextRect> rects)
    {
        ArgumentNullException.ThrowIfNull(rects);
        if (rects.Count == 0)
            throw new ArgumentException("La selección de texto no contiene geometría.", nameof(rects));

        foreach (var rect in rects)
        {
            if (!double.IsFinite(rect.Left) ||
                !double.IsFinite(rect.Bottom) ||
                !double.IsFinite(rect.Right) ||
                !double.IsFinite(rect.Top) ||
                rect.Right <= rect.Left ||
                rect.Top <= rect.Bottom)
            {
                throw new ArgumentException("La selección de texto contiene geometría PDF inválida.", nameof(rects));
            }
        }

        return rects;
    }
}
