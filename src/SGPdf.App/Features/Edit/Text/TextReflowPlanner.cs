using SGPdf.App.Features.Edit.Text.Layout;

namespace SGPdf.App.Features.Edit.Text;

internal sealed record TextReflowAssignment(TextObjectKey Key, string Text);

internal sealed record TextReflowPlan(
    bool Allowed,
    string? Reason,
    IReadOnlyList<TextReflowAssignment> Assignments)
{
    internal static TextReflowPlan Reject(string reason) =>
        new(false, reason, Array.Empty<TextReflowAssignment>());

    internal static TextReflowPlan Allow(IReadOnlyList<TextReflowAssignment> assignments) =>
        new(true, null, assignments);
}

internal static class TextReflowPlanner
{
    internal static TextReflowPlan Plan(
        TextLayoutPage page,
        TextParagraph? paragraph,
        PdfEditPreflightResult preflight,
        string text)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(preflight);

        var policy = TextReflowPolicy.Evaluate(page, paragraph, preflight);
        if (!policy.Allowed)
            return TextReflowPlan.Reject(policy.Reason ?? "El párrafo no admite reflow seguro.");

        if (string.IsNullOrWhiteSpace(text))
            return TextReflowPlan.Reject("El texto de reflow no puede quedar vacío.");

        var runs = paragraph!.Lines
            .SelectMany(line => line.Runs)
            .ToArray();
        if (runs.Length == 0)
            return TextReflowPlan.Reject("El párrafo no contiene objetos de texto editables.");

        var words = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length < runs.Length)
        {
            return TextReflowPlan.Reject(
                "El texto nuevo no tiene suficientes palabras para reemplazar todos los objetos existentes sin dejar contenido anterior.");
        }

        var assignments = new List<TextReflowAssignment>(runs.Length);
        var wordIndex = 0;

        for (var runIndex = 0; runIndex < runs.Length; runIndex++)
        {
            var run = runs[runIndex];
            var capacity = GetConservativeCapacity(run);
            var remainingRuns = runs.Length - runIndex - 1;
            var current = string.Empty;

            while (wordIndex < words.Length)
            {
                var wordsAfterCandidate = words.Length - wordIndex - 1;
                if (wordsAfterCandidate < remainingRuns)
                    break;

                var candidate = current.Length == 0
                    ? words[wordIndex]
                    : $"{current} {words[wordIndex]}";
                if (candidate.Length > capacity)
                    break;

                current = candidate;
                wordIndex++;
            }

            if (current.Length == 0)
            {
                return TextReflowPlan.Reject(
                    "El texto nuevo no cabe de forma conservadora en los objetos existentes del párrafo.");
            }

            assignments.Add(new TextReflowAssignment(run.SourceKey, current));
        }

        if (wordIndex != words.Length)
        {
            return TextReflowPlan.Reject(
                "El texto nuevo no cabe de forma conservadora en los objetos existentes del párrafo.");
        }

        return TextReflowPlan.Allow(assignments);
    }

    private static int GetConservativeCapacity(TextRun run)
    {
        var normalizedSource = string.Join(
            " ",
            run.Text.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return Math.Max(1, normalizedSource.Length);
    }
}
