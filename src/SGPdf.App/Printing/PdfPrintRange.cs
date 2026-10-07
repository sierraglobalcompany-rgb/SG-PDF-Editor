namespace SGPdf.App.Printing;

public sealed record PdfPrintRange
{
    private PdfPrintRange(int firstPageIndex, int lastPageIndex)
    {
        FirstPageIndex = firstPageIndex;
        LastPageIndex = lastPageIndex;
    }

    public int FirstPageIndex { get; }

    public int LastPageIndex { get; }

    public int PageCount => LastPageIndex - FirstPageIndex + 1;

    public static PdfPrintRange All(int pageCount)
    {
        ValidatePageCount(pageCount);
        return new PdfPrintRange(0, pageCount - 1);
    }

    public static PdfPrintRange Current(int currentPageIndex, int pageCount)
    {
        ValidatePageCount(pageCount);
        ValidatePageIndex(currentPageIndex, pageCount, nameof(currentPageIndex));
        return new PdfPrintRange(currentPageIndex, currentPageIndex);
    }

    public static PdfPrintRange UserPages(int firstPageNumber, int lastPageNumber, int pageCount)
    {
        ValidatePageCount(pageCount);

        if (firstPageNumber < 1 || firstPageNumber > pageCount)
            throw new ArgumentOutOfRangeException(nameof(firstPageNumber));

        if (lastPageNumber < 1 || lastPageNumber > pageCount)
            throw new ArgumentOutOfRangeException(nameof(lastPageNumber));

        if (lastPageNumber < firstPageNumber)
            throw new ArgumentException("La página final no puede ser anterior a la página inicial.", nameof(lastPageNumber));

        return new PdfPrintRange(firstPageNumber - 1, lastPageNumber - 1);
    }

    private static void ValidatePageCount(int pageCount)
    {
        if (pageCount < 1)
            throw new ArgumentOutOfRangeException(nameof(pageCount));
    }

    private static void ValidatePageIndex(int pageIndex, int pageCount, string parameterName)
    {
        if (pageIndex < 0 || pageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
