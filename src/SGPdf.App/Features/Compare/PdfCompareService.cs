using System.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Compare;

internal static class PdfCompareService
{
    internal static PdfCompareResult Compare(string leftPath, string rightPath)
    {
        using var left = PdfDocumentSession.Open(leftPath);
        using var right = PdfDocumentSession.Open(rightPath);

        int comparablePageCount = Math.Min(left.PageCount, right.PageCount);
        bool textChanged = !string.Equals(
            ExtractText(left, comparablePageCount),
            ExtractText(right, comparablePageCount),
            StringComparison.Ordinal);
        bool visualChanged = !textChanged && HasVisualDifference(left, right, comparablePageCount);

        return new PdfCompareResult(
            left.PageCount,
            right.PageCount,
            textChanged,
            visualChanged);
    }

    private static string ExtractText(PdfDocumentSession session, int pageCount)
    {
        var builder = new StringBuilder();
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            foreach (var textObject in session.GetTextObjects(pageIndex))
                builder.Append(textObject.Text).Append('\u001f');

            builder.Append('\u001e');
        }

        return builder.ToString();
    }

    private static bool HasVisualDifference(
        PdfDocumentSession left,
        PdfDocumentSession right,
        int pageCount)
    {
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            PdfRenderedPage leftPage = left.RenderPage(pageIndex, 72d);
            PdfRenderedPage rightPage = right.RenderPage(pageIndex, 72d);
            if (leftPage.PixelWidth != rightPage.PixelWidth ||
                leftPage.PixelHeight != rightPage.PixelHeight ||
                leftPage.Stride != rightPage.Stride ||
                !leftPage.Pixels.AsSpan().SequenceEqual(rightPage.Pixels))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed record PdfCompareResult(
    int LeftPageCount,
    int RightPageCount,
    bool TextChanged,
    bool VisualChanged);
