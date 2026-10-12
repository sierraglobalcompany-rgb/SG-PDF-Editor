using SGPdf.App.Features.Edit.Text.Layout;

namespace SGPdf.App.Features.Edit.Text;

internal sealed record TextReflowDecision(bool Allowed, string? Reason)
{
    internal static TextReflowDecision Allow { get; } = new(true, null);

    internal static TextReflowDecision Reject(string reason) => new(false, reason);
}

internal static class TextReflowPolicy
{
    private const int FillRenderMode = 0;

    internal static TextReflowDecision Evaluate(
        TextLayoutPage page,
        TextParagraph? paragraph,
        PdfEditPreflightResult preflight)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(preflight);

        if (!preflight.CanProceed)
            return TextReflowDecision.Reject("El preflight general bloquea la edición de este PDF.");

        if (page.Classification != TextLayoutClassification.Simple)
            return TextReflowDecision.Reject("El layout no es suficientemente simple y seguro para reflow.");

        if (page.Columns.Count != 1)
            return TextReflowDecision.Reject("El reflow solo se permite en una sola columna.");

        if (paragraph is null || !page.Columns[0].Paragraphs.Contains(paragraph))
            return TextReflowDecision.Reject("No hay un párrafo seguro seleccionado para reflow.");

        var runs = paragraph.Lines
            .SelectMany(line => line.Runs)
            .ToArray();
        if (runs.Length == 0)
            return TextReflowDecision.Reject("El párrafo no contiene texto editable.");

        if (runs.Any(run => run.TextRenderMode != FillRenderMode))
            return TextReflowDecision.Reject("El párrafo contiene texto de solo lectura y no admite reflow.");

        if (HasOverlappingRuns(runs))
            return TextReflowDecision.Reject("El párrafo contiene fragmentos solapados y no es seguro para reflow.");

        if (!HasHomogeneousStyle(runs))
            return TextReflowDecision.Reject("El párrafo no tiene un estilo suficientemente homogéneo para reflow.");

        return TextReflowDecision.Allow;
    }

    private static bool HasOverlappingRuns(IReadOnlyList<TextRun> runs)
    {
        for (var leftIndex = 0; leftIndex < runs.Count; leftIndex++)
        {
            var left = runs[leftIndex].Bounds;
            for (var rightIndex = leftIndex + 1; rightIndex < runs.Count; rightIndex++)
            {
                var right = runs[rightIndex].Bounds;
                var horizontalOverlap = Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left);
                var verticalOverlap = Math.Min(left.Top, right.Top) - Math.Max(left.Bottom, right.Bottom);
                if (horizontalOverlap > 0d && verticalOverlap > 0d)
                    return true;
            }
        }

        return false;
    }

    private static bool HasHomogeneousStyle(IReadOnlyList<TextRun> runs)
    {
        var first = runs[0];
        foreach (var run in runs.Skip(1))
        {
            if (!string.Equals(first.FontName, run.FontName, StringComparison.OrdinalIgnoreCase))
                return false;

            var tolerance = Math.Max(0.25d, Math.Abs(first.FontSize) * 0.03d);
            if (!double.IsFinite(run.FontSize) || Math.Abs(first.FontSize - run.FontSize) > tolerance)
                return false;

            if (first.FillColor != run.FillColor)
                return false;
        }

        return double.IsFinite(first.FontSize) && first.FontSize > 0d;
    }
}
