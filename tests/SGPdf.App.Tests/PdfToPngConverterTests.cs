using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkiaSharp;
using SGPdf.App.Features.Conversions;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfToPngConverterTests
{
    [Fact]
    public void Convert_RendersEveryPageAt150DpiWithDeterministicNames()
    {
        string root = CreateTempDirectory();
        try
        {
            string source = Path.Combine(root, "source.pdf");
            string output = Path.Combine(root, "png");
            CreateTwoPagePdf(source);

            IReadOnlyList<string> paths = new PdfToPngConverter().Convert(source, output, CancellationToken.None);

            Assert.Equal(2, paths.Count);
            Assert.Equal("page-0001.png", Path.GetFileName(paths[0]));
            Assert.Equal("page-0002.png", Path.GetFileName(paths[1]));

            using SKBitmap? first = SKBitmap.Decode(paths[0]);
            using SKBitmap? second = SKBitmap.Decode(paths[1]);
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(300, first.Width);
            Assert.Equal(150, first.Height);
            Assert.Equal(150, second.Width);
            Assert.Equal(300, second.Height);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Convert_RefusesExistingOutputBeforeWritingAnyPage()
    {
        string root = CreateTempDirectory();
        try
        {
            string source = Path.Combine(root, "source.pdf");
            string output = Path.Combine(root, "png");
            CreateTwoPagePdf(source);
            Directory.CreateDirectory(output);
            string existing = Path.Combine(output, "page-0002.png");
            File.WriteAllText(existing, "keep");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
                new PdfToPngConverter().Convert(source, output, CancellationToken.None));

            Assert.Contains("page-0002.png", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(Path.Combine(output, "page-0001.png")));
            Assert.Equal("keep", File.ReadAllText(existing));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void CreateTwoPagePdf(string path)
    {
        using var document = new PdfDocument();
        PdfPage first = document.AddPage();
        first.Width = XUnit.FromPoint(144);
        first.Height = XUnit.FromPoint(72);
        using (XGraphics graphics = XGraphics.FromPdfPage(first))
            graphics.DrawRectangle(XBrushes.Black, 12, 12, 40, 20);

        PdfPage second = document.AddPage();
        second.Width = XUnit.FromPoint(72);
        second.Height = XUnit.FromPoint(144);
        using (XGraphics graphics = XGraphics.FromPdfPage(second))
            graphics.DrawRectangle(XBrushes.Black, 8, 8, 20, 40);

        document.Save(path);
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "sgpdf-png-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
