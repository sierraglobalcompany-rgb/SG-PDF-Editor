using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Reader;

internal sealed record ReaderTextSelection(
    int PageIndex,
    int StartIndex,
    int CharacterCount,
    IReadOnlyList<PdfTextRect> PdfRects);

internal static class ReaderTextSelectionRange
{
    internal static (int StartIndex, int CharacterCount)? Normalize(int firstCharIndex, int lastCharIndex)
    {
        if (firstCharIndex < 0 || lastCharIndex < 0)
            return null;

        var start = Math.Min(firstCharIndex, lastCharIndex);
        var end = Math.Max(firstCharIndex, lastCharIndex);
        return (start, checked((end - start) + 1));
    }
}
