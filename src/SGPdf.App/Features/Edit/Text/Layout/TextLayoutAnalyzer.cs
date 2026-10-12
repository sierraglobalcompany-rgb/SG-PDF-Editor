using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Text.Layout;

internal static class TextLayoutAnalyzer
{
    internal static TextLayoutPage Analyze(
        double pageWidth,
        double pageHeight,
        IReadOnlyList<PdfTextObjectInfo> objects,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objects);
        cancellationToken.ThrowIfCancellationRequested();

        if (!double.IsFinite(pageWidth) || pageWidth <= 0d)
            throw new ArgumentOutOfRangeException(nameof(pageWidth));
        if (!double.IsFinite(pageHeight) || pageHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(pageHeight));

        var runs = new List<TextRun>(objects.Count);
        foreach (var item in objects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryNormalize(item, out var run))
                continue;

            runs.Add(run);
        }

        if (runs.Count == 0)
            return new TextLayoutPage(pageWidth, pageHeight, Array.Empty<TextColumn>(), TextLayoutClassification.Simple);

        runs.Sort(static (left, right) =>
        {
            var top = right.Bounds.Top.CompareTo(left.Bounds.Top);
            if (top != 0)
                return top;

            var x = left.Bounds.Left.CompareTo(right.Bounds.Left);
            return x != 0 ? x : left.SourceKey.PageObjectIndex.CompareTo(right.SourceKey.PageObjectIndex);
        });

        var lineBuilders = new List<LineBuilder>();
        foreach (var run in runs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LineBuilder? target = null;
            foreach (var line in lineBuilders)
            {
                if (!IsSameHorizontalLine(line.Bounds, run.Bounds))
                    continue;

                target = line;
                break;
            }

            if (target is null)
            {
                lineBuilders.Add(new LineBuilder(run));
                continue;
            }

            target.Add(run);
        }

        var lines = lineBuilders
            .Select(builder => builder.Build())
            .OrderByDescending(line => line.Bounds.Top)
            .ThenBy(line => line.Bounds.Left)
            .ToArray();

        var paragraphBounds = AggregateBounds(lines.Select(line => line.Bounds));
        var paragraph = new TextParagraph(paragraphBounds, lines);
        var column = new TextColumn(paragraphBounds, new[] { paragraph });

        return new TextLayoutPage(
            pageWidth,
            pageHeight,
            new[] { column },
            TextLayoutClassification.Simple);
    }

    private static bool TryNormalize(PdfTextObjectInfo item, out TextRun run)
    {
        run = null!;
        var text = item.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || !IsValidBounds(item.Bounds))
            return false;

        run = new TextRun(
            item.Key,
            text,
            item.Bounds,
            item.Quad,
            item.FontName,
            item.FontSize);
        return true;
    }

    private static bool IsValidBounds(PdfObjectBounds bounds)
        => double.IsFinite(bounds.Left) &&
           double.IsFinite(bounds.Bottom) &&
           double.IsFinite(bounds.Right) &&
           double.IsFinite(bounds.Top) &&
           bounds.Right > bounds.Left &&
           bounds.Top > bounds.Bottom;

    private static bool IsSameHorizontalLine(PdfObjectBounds left, PdfObjectBounds right)
    {
        var leftHeight = left.Top - left.Bottom;
        var rightHeight = right.Top - right.Bottom;
        var minimumHeight = Math.Min(leftHeight, rightHeight);
        var maximumHeight = Math.Max(leftHeight, rightHeight);
        var overlap = Math.Min(left.Top, right.Top) - Math.Max(left.Bottom, right.Bottom);

        if (overlap >= minimumHeight * 0.50d)
            return true;

        var leftCenter = (left.Top + left.Bottom) * 0.5d;
        var rightCenter = (right.Top + right.Bottom) * 0.5d;
        return Math.Abs(leftCenter - rightCenter) <= maximumHeight * 0.35d;
    }

    private static PdfObjectBounds AggregateBounds(IEnumerable<PdfObjectBounds> bounds)
    {
        using var enumerator = bounds.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new InvalidOperationException("No hay geometría para agrupar.");

        var first = enumerator.Current;
        var left = first.Left;
        var bottom = first.Bottom;
        var right = first.Right;
        var top = first.Top;

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            left = Math.Min(left, current.Left);
            bottom = Math.Min(bottom, current.Bottom);
            right = Math.Max(right, current.Right);
            top = Math.Max(top, current.Top);
        }

        return new PdfObjectBounds(left, bottom, right, top);
    }

    private sealed class LineBuilder
    {
        private readonly List<TextRun> _runs = new();

        internal LineBuilder(TextRun first)
        {
            _runs.Add(first);
            Bounds = first.Bounds;
        }

        internal PdfObjectBounds Bounds { get; private set; }

        internal void Add(TextRun run)
        {
            _runs.Add(run);
            Bounds = new PdfObjectBounds(
                Math.Min(Bounds.Left, run.Bounds.Left),
                Math.Min(Bounds.Bottom, run.Bounds.Bottom),
                Math.Max(Bounds.Right, run.Bounds.Right),
                Math.Max(Bounds.Top, run.Bounds.Top));
        }

        internal TextLine Build()
        {
            var orderedRuns = _runs
                .OrderBy(run => run.Bounds.Left)
                .ThenBy(run => run.SourceKey.PageObjectIndex)
                .ToArray();
            return new TextLine(Bounds, orderedRuns);
        }
    }
}
