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

        if (runs.Any(run => !IsHorizontal(run.Matrix)))
        {
            return new TextLayoutPage(
                pageWidth,
                pageHeight,
                Array.Empty<TextColumn>(),
                TextLayoutClassification.Unsupported);
        }

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
                if (!IsSameLine(line.Bounds, run.Bounds))
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

        cancellationToken.ThrowIfCancellationRequested();
        var columnDetection = DetectColumns(lines);
        if (columnDetection.IsAmbiguous)
        {
            return new TextLayoutPage(
                pageWidth,
                pageHeight,
                new[] { BuildColumn(lines) },
                TextLayoutClassification.Ambiguous);
        }

        var columns = columnDetection.Groups
            .Select(BuildColumn)
            .OrderBy(column => column.Bounds.Left)
            .ToArray();

        return new TextLayoutPage(
            pageWidth,
            pageHeight,
            columns,
            TextLayoutClassification.Simple);
    }

    private static bool TryNormalize(PdfTextObjectInfo item, out TextRun run)
    {
        run = null!;
        var text = item.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) ||
            !IsValidMatrix(item.Matrix) ||
            !IsValidBounds(item.Bounds))
        {
            return false;
        }

        run = new TextRun(
            item.Key,
            text,
            item.Matrix,
            item.Bounds,
            item.Quad,
            item.FontName,
            item.FontSize,
            item.FillColor,
            item.TextRenderMode);
        return true;
    }

    private static bool IsValidMatrix(PdfObjectMatrix matrix)
        => double.IsFinite(matrix.A) &&
           double.IsFinite(matrix.B) &&
           double.IsFinite(matrix.C) &&
           double.IsFinite(matrix.D) &&
           double.IsFinite(matrix.E) &&
           double.IsFinite(matrix.F);

    private static bool IsValidBounds(PdfObjectBounds bounds)
        => double.IsFinite(bounds.Left) &&
           double.IsFinite(bounds.Bottom) &&
           double.IsFinite(bounds.Right) &&
           double.IsFinite(bounds.Top) &&
           bounds.Right > bounds.Left &&
           bounds.Top > bounds.Bottom;

    private static bool IsHorizontal(PdfObjectMatrix matrix)
    {
        var primaryScale = Math.Max(Math.Max(Math.Abs(matrix.A), Math.Abs(matrix.D)), 1e-9d);
        return Math.Abs(matrix.B) <= primaryScale * 0.05d &&
               Math.Abs(matrix.C) <= primaryScale * 0.05d;
    }

    private static bool IsSameLine(PdfObjectBounds left, PdfObjectBounds right)
    {
        var leftHeight = left.Top - left.Bottom;
        var rightHeight = right.Top - right.Bottom;
        var minimumHeight = Math.Min(leftHeight, rightHeight);
        var maximumHeight = Math.Max(leftHeight, rightHeight);
        var overlap = Math.Min(left.Top, right.Top) - Math.Max(left.Bottom, right.Bottom);

        var verticallyCompatible = overlap >= minimumHeight * 0.50d;
        if (!verticallyCompatible)
        {
            var leftCenter = (left.Top + left.Bottom) * 0.5d;
            var rightCenter = (right.Top + right.Bottom) * 0.5d;
            verticallyCompatible = Math.Abs(leftCenter - rightCenter) <= maximumHeight * 0.35d;
        }

        if (!verticallyCompatible)
            return false;

        var horizontalGap = right.Left > left.Right
            ? right.Left - left.Right
            : left.Left > right.Right
                ? left.Left - right.Right
                : 0d;

        return horizontalGap <= maximumHeight * 8d;
    }

    private static ColumnDetection DetectColumns(IReadOnlyList<TextLine> lines)
    {
        if (lines.Count < 4)
            return ColumnDetection.Single(lines);

        var byLeft = lines
            .OrderBy(line => line.Bounds.Left)
            .ThenByDescending(line => line.Bounds.Top)
            .ToArray();

        var largestGap = 0d;
        var splitIndex = -1;
        for (var index = 1; index < byLeft.Length; index++)
        {
            var gap = byLeft[index].Bounds.Left - byLeft[index - 1].Bounds.Left;
            if (gap <= largestGap)
                continue;

            largestGap = gap;
            splitIndex = index;
        }

        if (splitIndex < 2 || byLeft.Length - splitIndex < 2)
            return ColumnDetection.Single(lines);

        var typicalHeight = Median(lines.Select(line => line.Bounds.Top - line.Bounds.Bottom));
        if (largestGap < typicalHeight * 3d)
            return ColumnDetection.Single(lines);

        var leftGroup = byLeft[..splitIndex]
            .OrderByDescending(line => line.Bounds.Top)
            .ThenBy(line => line.Bounds.Left)
            .ToArray();
        var rightGroup = byLeft[splitIndex..]
            .OrderByDescending(line => line.Bounds.Top)
            .ThenBy(line => line.Bounds.Left)
            .ToArray();

        var leftBounds = AggregateBounds(leftGroup.Select(line => line.Bounds));
        var rightBounds = AggregateBounds(rightGroup.Select(line => line.Bounds));
        var gutter = rightGroup.Min(line => line.Bounds.Left) - leftGroup.Max(line => line.Bounds.Right);
        var verticalOverlap = Math.Min(leftBounds.Top, rightBounds.Top) - Math.Max(leftBounds.Bottom, rightBounds.Bottom);
        var shorterColumnHeight = Math.Min(
            leftBounds.Top - leftBounds.Bottom,
            rightBounds.Top - rightBounds.Bottom);

        var stableGutter = gutter >= typicalHeight * 1.5d;
        var meaningfulVerticalOverlap = verticalOverlap > 0d &&
                                        verticalOverlap >= shorterColumnHeight * 0.35d;

        if (!stableGutter || !meaningfulVerticalOverlap)
            return ColumnDetection.Ambiguous(lines);

        return ColumnDetection.Two(leftGroup, rightGroup);
    }

    private static TextColumn BuildColumn(IReadOnlyList<TextLine> sourceLines)
    {
        var lines = sourceLines
            .OrderByDescending(line => line.Bounds.Top)
            .ThenBy(line => line.Bounds.Left)
            .ToArray();
        var paragraphGroups = new List<List<TextLine>>();

        foreach (var line in lines)
        {
            if (paragraphGroups.Count == 0)
            {
                paragraphGroups.Add(new List<TextLine> { line });
                continue;
            }

            var current = paragraphGroups[^1];
            if (ShouldBreakParagraph(current[^1], line))
                paragraphGroups.Add(new List<TextLine> { line });
            else
                current.Add(line);
        }

        var paragraphs = paragraphGroups
            .Select(group => new TextParagraph(
                AggregateBounds(group.Select(line => line.Bounds)),
                group.ToArray()))
            .ToArray();
        var columnBounds = AggregateBounds(lines.Select(line => line.Bounds));
        return new TextColumn(columnBounds, paragraphs);
    }

    private static bool ShouldBreakParagraph(TextLine previous, TextLine current)
    {
        var previousFontSize = Median(previous.Runs.Select(run => run.FontSize));
        var currentFontSize = Median(current.Runs.Select(run => run.FontSize));
        var smallerFont = Math.Max(Math.Min(previousFontSize, currentFontSize), 1e-9d);
        var largerFont = Math.Max(previousFontSize, currentFontSize);
        if (largerFont / smallerFont >= 1.35d)
            return true;

        var previousHeight = previous.Bounds.Top - previous.Bounds.Bottom;
        var currentHeight = current.Bounds.Top - current.Bounds.Bottom;
        var referenceHeight = Math.Max(previousHeight, currentHeight);
        var verticalGap = previous.Bounds.Bottom - current.Bounds.Top;
        if (verticalGap > referenceHeight * 3d)
            return true;

        var indentationChange = Math.Abs(previous.Bounds.Left - current.Bounds.Left);
        return indentationChange > referenceHeight * 4d;
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values
            .Where(double.IsFinite)
            .OrderBy(value => value)
            .ToArray();
        if (ordered.Length == 0)
            return 0d;

        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) * 0.5d
            : ordered[middle];
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

    private sealed record ColumnDetection(
        bool IsAmbiguous,
        IReadOnlyList<IReadOnlyList<TextLine>> Groups)
    {
        internal static ColumnDetection Single(IReadOnlyList<TextLine> lines)
            => new(false, new[] { lines });

        internal static ColumnDetection Two(
            IReadOnlyList<TextLine> left,
            IReadOnlyList<TextLine> right)
            => new(false, new[] { left, right });

        internal static ColumnDetection Ambiguous(IReadOnlyList<TextLine> lines)
            => new(true, new[] { lines });
    }
}
