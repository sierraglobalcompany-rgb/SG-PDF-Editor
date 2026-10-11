using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal sealed record OcrTextWordPlacement(
    string Text,
    double Confidence,
    PdfOcrRect Bounds,
    int SourceOrder,
    int Word);

internal sealed record OcrTextLinePlacement(
    int PageIndex,
    string Text,
    PdfOcrRect Bounds,
    IReadOnlyList<OcrTextWordPlacement> Words,
    int SourceOrder,
    int Block,
    int Paragraph,
    int Line);

internal static class OcrTextLayerPlanner
{
    internal static IReadOnlyList<OcrTextLinePlacement> Build(
        IReadOnlyList<OcrWord> words,
        PdfRenderedPage renderedPage)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(renderedPage);

        if (words.Count == 0)
            return Array.Empty<OcrTextLinePlacement>();

        var groups = new List<LineGroup>();
        var groupIndexes = new Dictionary<(int Block, int Paragraph, int Line), int>();

        for (var sourceOrder = 0; sourceOrder < words.Count; sourceOrder++)
        {
            var word = words[sourceOrder]
                ?? throw new ArgumentException("La lista OCR no puede contener palabras nulas.", nameof(words));
            var key = (word.Block, word.Paragraph, word.Line);

            if (!groupIndexes.TryGetValue(key, out var groupIndex))
            {
                groupIndex = groups.Count;
                groupIndexes.Add(key, groupIndex);
                groups.Add(new LineGroup(
                    word.Block,
                    word.Paragraph,
                    word.Line,
                    sourceOrder));
            }

            var mappedBounds = OcrCoordinateMapper.Map(renderedPage, word.Bounds);
            groups[groupIndex].Words.Add(new OcrTextWordPlacement(
                word.Text,
                word.Confidence,
                mappedBounds,
                sourceOrder,
                word.Word));
        }

        var placements = new OcrTextLinePlacement[groups.Count];
        for (var index = 0; index < groups.Count; index++)
        {
            var group = groups[index];
            var placedWords = group.Words.ToArray();
            placements[index] = new OcrTextLinePlacement(
                renderedPage.PageIndex,
                string.Join(" ", placedWords.Select(word => word.Text)),
                Union(placedWords),
                placedWords,
                index,
                group.Block,
                group.Paragraph,
                group.Line);
        }

        return placements;
    }

    private static PdfOcrRect Union(IReadOnlyList<OcrTextWordPlacement> words)
    {
        var first = words[0].Bounds;
        var left = first.Left;
        var bottom = first.Bottom;
        var right = first.Right;
        var top = first.Top;

        for (var index = 1; index < words.Count; index++)
        {
            var bounds = words[index].Bounds;
            left = Math.Min(left, bounds.Left);
            bottom = Math.Min(bottom, bounds.Bottom);
            right = Math.Max(right, bounds.Right);
            top = Math.Max(top, bounds.Top);
        }

        return new PdfOcrRect(left, bottom, right, top);
    }

    private sealed class LineGroup
    {
        internal LineGroup(int block, int paragraph, int line, int firstSourceOrder)
        {
            Block = block;
            Paragraph = paragraph;
            Line = line;
            FirstSourceOrder = firstSourceOrder;
        }

        internal int Block { get; }
        internal int Paragraph { get; }
        internal int Line { get; }
        internal int FirstSourceOrder { get; }
        internal List<OcrTextWordPlacement> Words { get; } = new();
    }
}
