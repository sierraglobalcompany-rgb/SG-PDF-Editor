using SkiaSharp;
using SGPdf.App.Features.Labels;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class LabelPdfExportIntegrationTests
{
    [Fact]
    public void ExportedA4Pdf_ReopensThroughPdfiumWithExpectedPagesSizeAndRender()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"sgpdf-pdfium-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var destination = Path.Combine(tempDirectory, "labels.pdf");

        try
        {
            var document = ZplDocumentParser.Parse(
                @"C:\labels\orders.zpl",
                "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");
            var quantity = new ZplQuantitySelection(ZplQuantityMode.Custom, 3);
            var sequence = new LabelOutputSequence(document, quantity);
            var plan = LabelLayoutPlanner.CreatePlan(
                sequence,
                new LabelLayoutSettings(LabelMediaKind.A4, labelsPerPage: 4),
                labelWidthMm: 50,
                labelHeightMm: 30);
            var png = CreatePngBytes();
            var rendered = new[]
            {
                new ZplRenderedLabel(0, 50, 30, 8, png),
                new ZplRenderedLabel(1, 50, 30, 8, png)
            };

            new LabelPdfExporter().Export(destination, plan, rendered);

            using var session = PdfDocumentSession.Open(destination);
            Assert.Equal(2, session.PageCount);

            var expectedWidthPoints = 210d * 72d / 25.4d;
            var expectedHeightPoints = 297d * 72d / 25.4d;
            for (var pageIndex = 0; pageIndex < session.PageCount; pageIndex++)
            {
                var (width, height) = session.GetPageSize(pageIndex);
                Assert.Equal(expectedWidthPoints, width, 1);
                Assert.Equal(expectedHeightPoints, height, 1);
            }

            var renderedPage = session.RenderPage(0, dpi: 72d);
            Assert.True(renderedPage.PixelWidth > 0);
            Assert.True(renderedPage.PixelHeight > 0);
            Assert.True(renderedPage.Pixels.Length > 0);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static byte[] CreatePngBytes()
    {
        using var bitmap = new SKBitmap(20, 20);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black };
        canvas.DrawRect(2, 2, 16, 16, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
