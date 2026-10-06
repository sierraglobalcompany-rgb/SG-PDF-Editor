using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App.Pdf;

public static class PdfPrintService
{
    public static bool ShowPrintDialogAndPrint(PdfDocumentSession session, int currentPageIndex)
    {
        ArgumentNullException.ThrowIfNull(session);

        var totalPages = session.PageCount;
        if (totalPages <= 0)
            throw new InvalidOperationException("El documento no tiene páginas para imprimir.");
        if (currentPageIndex < 0 || currentPageIndex >= totalPages)
            throw new ArgumentOutOfRangeException(nameof(currentPageIndex));

        var dialog = new PrintDialog
        {
            MinPage = 1,
            MaxPage = (uint)totalPages,
            PageRange = new PageRange(1, totalPages),
            PageRangeSelection = PageRangeSelection.AllPages,
            UserPageRangeEnabled = true,
            CurrentPageEnabled = true,
            SelectedPagesEnabled = false
        };

        if (dialog.ShowDialog() != true)
            return false;

        var range = dialog.PageRangeSelection switch
        {
            PageRangeSelection.CurrentPage => PdfPrintRange.Current(totalPages, currentPageIndex),
            PageRangeSelection.UserPages => PdfPrintRange.FromUserRange(
                totalPages,
                dialog.PageRange.PageFrom,
                dialog.PageRange.PageTo),
            _ => PdfPrintRange.All(totalPages)
        };

        var printableWidth = dialog.PrintableAreaWidth;
        var printableHeight = dialog.PrintableAreaHeight;
        if (printableWidth <= 0 || printableHeight <= 0)
            throw new InvalidOperationException("El controlador de impresión no informó un área imprimible válida.");

        var paginator = new PdfDocumentPaginator(
            session,
            range,
            new Size(printableWidth, printableHeight));

        dialog.PrintDocument(
            paginator,
            $"SG PDF Editor - {Path.GetFileName(session.FilePath)}");

        return true;
    }
}
