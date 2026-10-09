namespace SGPdf.App.Pdf;

public readonly record struct PdfTextRect(
    double Left,
    double Bottom,
    double Right,
    double Top);

public sealed record PdfTextMatch(
    int PageIndex,
    int StartIndex,
    int CharacterCount,
    IReadOnlyList<PdfTextRect> Rects);
