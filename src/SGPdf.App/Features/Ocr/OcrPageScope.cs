namespace SGPdf.App.Features.Ocr;

internal enum OcrPageScopeKind
{
    Automatic,
    CurrentPage,
    Range
}

internal sealed record OcrPageScope
{
    private OcrPageScope(OcrPageScopeKind kind, int? startPageIndex, int? endPageIndex)
    {
        Kind = kind;
        StartPageIndex = startPageIndex;
        EndPageIndex = endPageIndex;
    }

    internal OcrPageScopeKind Kind { get; }
    internal int? StartPageIndex { get; }
    internal int? EndPageIndex { get; }

    public static OcrPageScope Automatic { get; } =
        new(OcrPageScopeKind.Automatic, null, null);

    public static OcrPageScope Current(int pageIndex)
    {
        if (pageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        return new OcrPageScope(OcrPageScopeKind.CurrentPage, pageIndex, pageIndex);
    }

    public static OcrPageScope Range(int startPageIndex, int endPageIndex)
    {
        if (startPageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startPageIndex));
        if (endPageIndex < startPageIndex)
            throw new ArgumentOutOfRangeException(nameof(endPageIndex));
        return new OcrPageScope(OcrPageScopeKind.Range, startPageIndex, endPageIndex);
    }
}
