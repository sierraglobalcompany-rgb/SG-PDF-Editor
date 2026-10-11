using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal sealed record OcrPageEligibilityResult(
    IReadOnlyList<int> CandidatePageIndices,
    IReadOnlyList<int> SkippedPageIndices);

internal sealed class OcrPageEligibilityPolicy
{
    internal OcrPageEligibilityResult Resolve(
        PdfDocumentSession session,
        OcrPageScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(scope);
        throw new NotImplementedException("Task 3 behavioral RED has not been implemented yet.");
    }
}
