using System.Text;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfSharpFontGateCollection.Name)]
public sealed class PdfCompareCapabilityTests
{
    [Fact]
    public void ExistingPdfium_CanSupplyMinimalSignalsForCompareMvp()
    {
        CompareProbeResult identical = ProbeWithExistingPdfium(CompareFixture.Identical);
        Assert.Equal(1, identical.LeftPageCount);
        Assert.Equal(1, identical.RightPageCount);
        Assert.False(identical.TextChanged);
        Assert.False(identical.VisualChanged);

        CompareProbeResult textChanged = ProbeWithExistingPdfium(CompareFixture.TextChanged);
        Assert.Equal(1, textChanged.LeftPageCount);
        Assert.Equal(1, textChanged.RightPageCount);
        Assert.True(textChanged.TextChanged);

        CompareProbeResult graphicChanged = ProbeWithExistingPdfium(CompareFixture.GraphicChanged);
        Assert.Equal(1, graphicChanged.LeftPageCount);
        Assert.Equal(1, graphicChanged.RightPageCount);
        Assert.False(graphicChanged.TextChanged);
        Assert.True(graphicChanged.VisualChanged);

        CompareProbeResult pageAdded = ProbeWithExistingPdfium(CompareFixture.PageAdded);
        Assert.Equal(1, pageAdded.LeftPageCount);
        Assert.Equal(2, pageAdded.RightPageCount);
    }

    private static CompareProbeResult ProbeWithExistingPdfium(CompareFixture fixture)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-4-compare-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string leftPath = Path.Combine(directory, "left.pdf");
            string rightPath = Path.Combine(directory, "right.pdf");
            CreateFixturePair(fixture, leftPath, rightPath);

            using var left = PdfDocumentSession.Open(leftPath);
            using var right = PdfDocumentSession.Open(rightPath);

            bool textChanged = !string.Equals(
                ExtractPdfiumText(left),
                ExtractPdfiumText(right),
                StringComparison.Ordinal);
            bool visualChanged = HasPdfiumVisualDifference(left, right);

            return new CompareProbeResult(
                left.PageCount,
                right.PageCount,
                textChanged,
                visualChanged);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string ExtractPdfiumText(PdfDocumentSession session)
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

    private static bool HasPdfiumVisualDifference(
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

    private static void CreateFixturePair(
        CompareFixture fixture,
        string leftPath,
        string rightPath)
    {
        switch (fixture)
        {
            case CompareFixture.Identical:
                CreatePdf(leftPath, "BASELINE", graphicX: 40d, pageCount: 1);
                CreatePdf(rightPath, "BASELINE", graphicX: 40d, pageCount: 1);
                break;
            case CompareFixture.TextChanged:
                CreatePdf(leftPath, "BASELINE", graphicX: 40d, pageCount: 1);
                CreatePdf(rightPath, "CHANGED", graphicX: 40d, pageCount: 1);
                break;
            case CompareFixture.GraphicChanged:
                CreatePdf(leftPath, "BASELINE", graphicX: 40d, pageCount: 1);
                CreatePdf(rightPath, "BASELINE", graphicX: 160d, pageCount: 1);
                break;
            case CompareFixture.PageAdded:
                CreatePdf(leftPath, "BASELINE", graphicX: 40d, pageCount: 1);
                CreatePdf(rightPath, "BASELINE", graphicX: 40d, pageCount: 2);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fixture));
        }
    }

    private static void CreatePdf(
        string path,
        string firstPageText,
        double graphicX,
        int pageCount)
    {
        using var document = new PdfDocument();
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            PdfPage page = document.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(400d);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(300d);

            string text = pageIndex == 0 ? firstPageText : "SECOND PAGE";
            AddHelveticaResources(page, document);
            AddPageContent(page, document, text, graphicX);
        }

        document.Save(path);
    }

    private static void AddHelveticaResources(PdfPage page, PdfDocument document)
    {
        var resources = new PdfDictionary(document);
        var fonts = new PdfDictionary(document);
        var font = new PdfDictionary(document);
        font.Elements.SetName("/Type", "/Font");
        font.Elements.SetName("/Subtype", "/Type1");
        font.Elements.SetName("/BaseFont", "/Helvetica");
        font.Elements.SetName("/Encoding", "/WinAnsiEncoding");
        document.Internals.AddObject(font);
        fonts.Elements["/F1"] = font.Reference;
        resources.Elements["/Font"] = fonts;
        page.Elements["/Resources"] = resources;
    }

    private static void AddPageContent(
        PdfPage page,
        PdfDocument document,
        string text,
        double graphicX)
    {
        string contentText = FormattableString.Invariant(
            $"q 0 0 0 rg {graphicX:0.###} 60 80 40 re f Q\nBT /F1 20 Tf 40 220 Td ({text}) Tj ET\n");
        var content = new PdfDictionary(document);
        content.CreateStream(Encoding.ASCII.GetBytes(contentText));
        document.Internals.AddObject(content);
        page.Elements["/Contents"] = content.Reference;
    }

    private enum CompareFixture
    {
        Identical,
        TextChanged,
        GraphicChanged,
        PageAdded
    }

    private sealed record CompareProbeResult(
        int LeftPageCount,
        int RightPageCount,
        bool TextChanged,
        bool VisualChanged);
}
