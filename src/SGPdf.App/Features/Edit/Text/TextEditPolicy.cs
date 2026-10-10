using System.Text;

namespace SGPdf.App.Features.Edit.Text;

internal static class TextEditPolicy
{
    private const int FillRenderMode = 0;
    private const double MaxFontSize = 1000d;

    internal static TextEditPolicyResult Evaluate(
        PdfTextObjectInfo source,
        string text,
        double fontSize,
        PdfTextFillColor fillColor)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.TextRenderMode != FillRenderMode)
        {
            return new TextEditPolicyResult(
                Candidate: null,
                Error: "El modo de renderizado de este texto es de solo lectura en Texto V1.",
                IsReadOnly: true);
        }

        if (string.IsNullOrWhiteSpace(text))
            return Invalid("El texto no puede quedar vacío ni contener solo espacios.");

        if (!double.IsFinite(fontSize) || fontSize <= 0d || fontSize > MaxFontSize)
            return Invalid("El tamaño de fuente debe ser finito, mayor que 0 y menor o igual a 1000 pt.");

        var sourceRunes = source.Text
            .EnumerateRunes()
            .Where(static rune => !Rune.IsWhiteSpace(rune))
            .Select(static rune => rune.Value)
            .ToHashSet();

        var candidateRunes = text.EnumerateRunes().ToArray();
        var hasNewNonWhitespaceRune = candidateRunes.Any(rune =>
            !Rune.IsWhiteSpace(rune) && !sourceRunes.Contains(rune.Value));

        // FPDFTextObj_SetFontSize sigue siendo capability opcional/no promovida.
        // Hasta demostrar un setter seguro, cualquier cambio de tamaño recrea el objeto.
        var sizeChanged = Math.Abs(fontSize - source.FontSize) > 1e-9d;
        var strategy = hasNewNonWhitespaceRune || sizeChanged
            ? TextFontStrategy.FallbackTtf
            : TextFontStrategy.OriginalFont;

        if (strategy == TextFontStrategy.FallbackTtf)
        {
            foreach (var rune in candidateRunes)
            {
                if (!FallbackFontAsset.SupportsRune(rune))
                    return Invalid($"La fuente fallback no cubre U+{rune.Value:X4}.");
            }
        }

        var expected = new TextEditState(
            source,
            source.Text,
            source.FontSize,
            source.FillColor,
            TextFontStrategy.OriginalFont);

        return new TextEditPolicyResult(
            new TextEditCandidate(
                source.Key,
                expected,
                text,
                fontSize,
                fillColor,
                strategy),
            Error: null,
            IsReadOnly: false);
    }

    private static TextEditPolicyResult Invalid(string error) =>
        new(Candidate: null, Error: error, IsReadOnly: false);
}
