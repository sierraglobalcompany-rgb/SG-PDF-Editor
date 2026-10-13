using System.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Compare;

internal static class PdfCompareService
{
    internal static PdfCompareResult Compare(string leftPath, string rightPath)
    {
        using var left = PdfDocumentSession.Open(leftPath);
        using var right = PdfDocumentSession.Open(rightPath);

        bool textChanged = !string.Equals(
            ExtractText(left),
            ExtractText(right),
            StringComparison.Ordinal);
        bool visualChanged = HasVisualDifference(left, right);

        return new PdfCompareResult(
            left.PageCount,
            right.PageCount,
            textChanged,
            visualChanged);
    }

    private static string ExtractText(PdfDocumentSession session)
    {
        var builder = new StringBuilder();
        for (int pageIndex = 0; pageIndex < session.PageCount; pageIndex++)
        {
            foreach (var textObject in session.GetTextObjects(pageIndex))
                builder.Append(textObject.Text).Append('\u001f');

            builder.Append('\u001e');
        }

        return builder.ToString();
    }

    private static bool HasVisualDifference(
        PdfDocumentSession left,
        PdfDocumentSession right)
    {
        if (left.PageCount != right.PageCount)
            return true;

        for (int pageIndex = 0; pageIndex < left.PageCount; pageIndex++)
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
