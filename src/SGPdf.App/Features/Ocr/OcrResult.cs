namespace SGPdf.App.Features.Ocr;

internal enum OcrResultKind
{
    Succeeded,
    NoOp
}

internal sealed record OcrResult(
    OcrResultKind Kind,
    IReadOnlyList<int> ProcessedPageIndices,
    IReadOnlyList<int> SkippedSearchablePageIndices,
    string? Error)
{
    internal static OcrResult Success(
        IReadOnlyList<int> processedPageIndices,
        IReadOnlyList<int> skippedSearchablePageIndices)
        => new(
            OcrResultKind.Succeeded,
            processedPageIndices,
            skippedSearchablePageIndices,
            null);

    internal static OcrResult NoOp(IReadOnlyList<int> skippedSearchablePageIndices)
        => new(
            OcrResultKind.NoOp,
            Array.Empty<int>(),
            skippedSearchablePageIndices,
            null);
}
