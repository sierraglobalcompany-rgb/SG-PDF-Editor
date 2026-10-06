namespace SGPdf.App.Pdf;

public sealed record PdfPrintRange(int StartPageIndex, int PageCount)
{
    public static PdfPrintRange All(int totalPages)
    {
        ValidateTotalPages(totalPages);
        return new PdfPrintRange(0, totalPages);
    }

    public static PdfPrintRange Current(int totalPages, int currentPageIndex)
    {
        ValidateTotalPages(totalPages);

        if (currentPageIndex < 0 || currentPageIndex >= totalPages)
            throw new ArgumentOutOfRangeException(nameof(currentPageIndex));

        return new PdfPrintRange(currentPageIndex, 1);
    }

    public static PdfPrintRange FromUserRange(int totalPages, int pageFrom, int pageTo)
    {
        ValidateTotalPages(totalPages);

        var from = Math.Clamp(pageFrom, 1, totalPages);
        var to = Math.Clamp(pageTo, 1, totalPages);

        if (to < from)
            (from, to) = (to, from);

        return new PdfPrintRange(from - 1, to - from + 1);
    }

    private static void ValidateTotalPages(int totalPages)
    {
        if (totalPages <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalPages));
    }
}
