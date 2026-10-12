using System.Text;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Text.Layout;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfTextLayoutIntegrationTests
{
    [Fact]
    public void AnalyzeTextLayout_PdfiumObjectsOutOfContentOrder_UsesGeometryAndPreservesSpanishText()
    {
        using var fixture = TextLayoutCorpusFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.LayoutPath);

        var sourceObjects = session.GetTextObjects(0);
        Assert.Equal(2, sourceObjects.Count);
        Assert.Equal("Segunda linea", sourceObjects[0].Text);
        Assert.Equal("¡Hola, señor! ¿Qué tal?", sourceObjects[1].Text);

        var layout = session.AnalyzeTextLayout(0);

        Assert.Equal(TextLayoutClassification.Simple, layout.Classification);
        var lines = Assert.Single(Assert.Single(layout.Columns).Paragraphs).Lines;
        Assert.Equal(
            new[] { "¡Hola, señor! ¿Qué tal?", "Segunda linea" },
            lines.Select(line => line.Text));
    }

    [Fact]
    public void AnalyzeTextLayout_RotatedPdfiumText_IsUnsupported()
    {
        using var fixture = TextLayoutCorpusFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.LayoutPath);

        var layout = session.AnalyzeTextLayout(1);

        Assert.Equal(TextLayoutClassification.Unsupported, layout.Classification);
    }

    [Fact]
    public void AnalyzeTextLayout_F10InvisibleOcrLayer_IsConsumableWithoutSecondTextEngine()
    {
        using var fixture = TextLayoutCorpusFixture.Create();
        var destination = fixture.Output("ocr-layout.pdf");
        var bounds = new PdfOcrRect(50d, 80d, 150d, 105d);
        var placements = new[]
        {
            new OcrTextLinePlacement(
                0,
                "OCR español",
                bounds,
                [
                    new OcrTextWordPlacement(
                        "OCR",
                        95d,
                        new PdfOcrRect(50d, 80d, 80d, 105d),
                        0,
                        1),
                    new OcrTextWordPlacement(
                        "español",
                        95d,
                        new PdfOcrRect(85d, 80d, 150d, 105d),
                        1,
                        1)
                ],
                0,
                1,
                1,
                1)
        };

        var writer = new PdfOcrWriter();
        var write = writer.SaveAsCopy(
            fixture.ScanPath,
            PdfEditSourceFingerprint.Capture(fixture.ScanPath),
            placements,
            destination,
            CancellationToken.None);

        Assert.True(write.Succeeded, write.Error);
        using var output = PdfDocumentSession.Open(destination);
        var layout = output.AnalyzeTextLayout(0);

        Assert.Equal(TextLayoutClassification.Simple, layout.Classification);
        var line = Assert.Single(Assert.Single(Assert.Single(layout.Columns).Paragraphs).Lines);
        Assert.Equal("OCR español", line.Text);
        Assert.All(line.Runs, run => Assert.Equal(3, output.GetTextObjects(0).Single(item => item.Key == run.SourceKey).TextRenderMode));
    }

    private sealed class TextLayoutCorpusFixture : IDisposable
    {
        private TextLayoutCorpusFixture(string directoryPath, string layoutPath, string scanPath)
        {
            DirectoryPath = directoryPath;
            LayoutPath = layoutPath;
            ScanPath = scanPath;
        }

        internal string DirectoryPath { get; }
        internal string LayoutPath { get; }
        internal string ScanPath { get; }

        internal static TextLayoutCorpusFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f11-layout-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var layout = Path.Combine(directory, "layout.pdf");
            var scan = Path.Combine(directory, "scan.pdf");
            WriteLayoutPdf(layout);
            WriteScanPdf(scan);
            return new TextLayoutCorpusFixture(directory, layout, scan);
        }

        internal string Output(string fileName) => Path.Combine(DirectoryPath, fileName);

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }

        private static void WriteLayoutPdf(string path)
        {
            const string pageOne =
                "BT /F1 14 Tf 50 650 Td (Segunda linea) Tj ET\n" +
                "BT /F1 14 Tf 50 700 Td (\\241Hola, se\\361or! \\277Qu\\351 tal?) Tj ET";
            const string pageTwo =
                "BT /F1 14 Tf 0 1 -1 0 100 500 Tm (Rotado) Tj ET";

            var objects = new Dictionary<int, string>
            {
                [1] = "<< /Type /Catalog /Pages 2 0 R >>",
                [2] = "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                [3] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 7 0 R >> >> /Contents 5 0 R >>",
                [4] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 7 0 R >> >> /Contents 6 0 R >>",
                [5] = Stream(pageOne),
                [6] = Stream(pageTwo),
                [7] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
            };
            WritePdf(path, objects, 7, rootObject: 1);
        }

        private static void WriteScanPdf(string path)
        {
            const string scanContent = "q 120 0 0 120 60 80 cm /Im0 Do Q";
            const string imageHex = "FF000000FF000000FFFFFFFF>";
            var objects = new Dictionary<int, string>
            {
                [1] = "<< /Type /Catalog /Pages 2 0 R >>",
                [2] = "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                [3] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 500] /Resources << /XObject << /Im0 5 0 R >> >> /Contents 4 0 R >>",
                [4] = Stream(scanContent),
                [5] = $"<< /Type /XObject /Subtype /Image /Width 2 /Height 2 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /ASCIIHexDecode /Length {imageHex.Length} >>\nstream\n{imageHex}\nendstream"
            };
            WritePdf(path, objects, 5, rootObject: 1);
        }

        private static void WritePdf(
            string path,
            IReadOnlyDictionary<int, string> objects,
            int maxObject,
            int rootObject)
        {
            var builder = new StringBuilder("%PDF-1.7\n%SGPDF-F11-LAYOUT\n");
            var offsets = new long[maxObject + 1];
            for (var number = 1; number <= maxObject; number++)
            {
                offsets[number] = Encoding.Latin1.GetByteCount(builder.ToString());
                builder.Append(number).Append(" 0 obj\n")
                    .Append(objects[number]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.Latin1.GetByteCount(builder.ToString());
            builder.Append("xref\n0 ").Append(maxObject + 1).Append("\n")
                .Append("0000000000 65535 f \n");
            for (var number = 1; number <= maxObject; number++)
                builder.Append(offsets[number].ToString("D10")).Append(" 00000 n \n");

            builder.Append("trailer\n<< /Size ").Append(maxObject + 1)
                .Append(" /Root ").Append(rootObject).Append(" 0 R >>\n")
                .Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

            File.WriteAllBytes(path, Encoding.Latin1.GetBytes(builder.ToString()));
        }

        private static string Stream(string data) =>
            $"<< /Length {Encoding.Latin1.GetByteCount(data)} >>\nstream\n{data}\nendstream";
    }
}
