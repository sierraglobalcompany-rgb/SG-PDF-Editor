using System.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal sealed record OcrPageEligibilityResult(
    IReadOnlyList<int> CandidatePageIndices,
    IReadOnlyList<int> SkippedPageIndices);

internal sealed class OcrPageEligibilityPolicy
{
    private const int MeaningfulTextThreshold = 20;

    internal OcrPageEligibilityResult Resolve(
        PdfDocumentSession session,
        OcrPageScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(scope);

        return ResolveCore(
            session.PageCount,
            pageIndex => session.GetPageText(pageIndex, cancellationToken),
            scope,
            cancellationToken);
    }

    private static OcrPageEligibilityResult ResolveCore(
        int pageCount,
        Func<int, string> pageTextProvider,
        OcrPageScope scope,
        CancellationToken cancellationToken)
    {
        if (pageCount < 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        ArgumentNullException.ThrowIfNull(pageTextProvider);
        ArgumentNullException.ThrowIfNull(scope);

        var (startPageIndex, endPageIndex) = ResolveBounds(pageCount, scope);
        if (startPageIndex > endPageIndex)
        {
            return new OcrPageEligibilityResult(
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        var candidates = new List<int>();
        var skipped = new List<int>();
        for (var pageIndex = startPageIndex; pageIndex <= endPageIndex; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageText = pageTextProvider(pageIndex) ?? string.Empty;
            if (CountMeaningfulCharacters(pageText) >= MeaningfulTextThreshold)
                skipped.Add(pageIndex);
            else
                candidates.Add(pageIndex);
        }

        return new OcrPageEligibilityResult(candidates, skipped);
    }

    private static (int StartPageIndex, int EndPageIndex) ResolveBounds(
        int pageCount,
        OcrPageScope scope)
    {
        if (scope.Kind == OcrPageScopeKind.Automatic)
            return pageCount == 0 ? (0, -1) : (0, pageCount - 1);

        if (pageCount == 0)
            throw new ArgumentOutOfRangeException(nameof(scope), "El documento no contiene páginas.");

        var startPageIndex = scope.StartPageIndex
            ?? throw new InvalidOperationException("El alcance OCR no contiene una página inicial.");
        var endPageIndex = scope.EndPageIndex
            ?? throw new InvalidOperationException("El alcance OCR no contiene una página final.");

        if (startPageIndex < 0 || startPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(scope), "La página inicial está fuera del documento.");
        if (endPageIndex < startPageIndex || endPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(scope), "La página final está fuera del documento.");

        return (startPageIndex, endPageIndex);
    }

    private static int CountMeaningfulCharacters(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var count = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
                count++;
        }

        return count;
    }
}
