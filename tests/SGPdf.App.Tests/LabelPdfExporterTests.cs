using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed partial class LabelPdfExporterTests
{
    [Fact]
    public void Export_A4Plan_WritesExactPhysicalPageSizeAndPageCount()
    {
        var document = CreateDocument();
        var plan = LabelLayoutPlanner.CreatePlan(
            new LabelOutputSequence(
                document,
                new ZplQuantitySelection(ZplQuantityMode.Custom, 3)),
            new LabelLayoutSettings(LabelMediaKind.A4, labelsPerPage: 4),
            labelWidthMm: 50,
            labelHeightMm: 30);
        var rendered = CreateRenderedLabels(document.Designs.Count, 50, 30);
        var path = CreateTempPdfPath();

        try
        {
            new LabelPdfExporter().Export(path, plan, rendered);

            Assert.True(File.Exists(path));
            var pdfText = ReadPdfAsLatin1(path);
            Assert.Equal(2, PageObjectRegex().Matches(pdfText).Count);

            var mediaBoxes = MediaBoxRegex().Matches(pdfText);
            Assert.NotEmpty(mediaBoxes);
            Assert.All(mediaBoxes.Cast<Match>(), match =>
            {
                Assert.Equal(MmToPoints(210), double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 2);
                Assert.Equal(MmToPoints(297), double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), 2);
            });

            var directory = Path.GetDirectoryName(path)!;
            var fileName = Path.GetFileName(path);
            Assert.Empty(Directory.GetFiles(directory, $".{fileName}.*.sgpdf.tmp"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Export_CustomPageAndRotation_WritesRequestedMediaBox()
    {
        var document = CreateDocument();
        var plan = LabelLayoutPlanner.CreatePlan(
            new LabelOutputSequence(
                document,
                new ZplQuantitySelection(ZplQuantityMode.OneEach)),
            new LabelLayoutSettings(
                LabelMediaKind.Custom,
                labelsPerPage: 2,
                rows: 1,
                columns: 2,
                rotation: LabelRotation.Degrees90,
                customPageWidthMm: 180,
                customPageHeightMm: 90),
            labelWidthMm: 30,
            labelHeightMm: 50);
        var rendered = CreateRenderedLabels(document.Designs.Count, 30, 50);
        var path = CreateTempPdfPath();

        try
        {
            new LabelPdfExporter().Export(path, plan, rendered);

            var pdfText = ReadPdfAsLatin1(path);
            var mediaBox = Assert.Single(MediaBoxRegex().Matches(pdfText).Cast<Match>());
            Assert.Equal(MmToPoints(180), double.Parse(mediaBox.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 2);
            Assert.Equal(MmToPoints(90), double.Parse(mediaBox.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), 2);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Export_WhenImageIsInvalid_PreservesExistingDestinationAndCleansTemporaryFile()
    {
        var document = CreateDocument();
        var plan = LabelLayoutPlanner.CreatePlan(
            new LabelOutputSequence(
                document,
                new ZplQuantitySelection(ZplQuantityMode.OneEach)),
            new LabelLayoutSettings(LabelMediaKind.Thermal),
            labelWidthMm: 50,
            labelHeightMm: 30);
        var invalid = new[]
        {
            new ZplRenderedLabel(0, 50, 30, 8, [0x01, 0x02, 0x03]),
            new ZplRenderedLabel(1, 50, 30, 8, [0x04, 0x05, 0x06])
        };
        var path = CreateTempPdfPath();
        var originalBytes = Encoding.UTF8.GetBytes("ORIGINAL_DESTINATION");
        File.WriteAllBytes(path, originalBytes);

        try
        {
            Assert.ThrowsAny<Exception>(() =>
                new LabelPdfExporter().Export(path, plan, invalid));

            Assert.Equal(originalBytes, File.ReadAllBytes(path));
            var directory = Path.GetDirectoryName(path)!;
            var fileName = Path.GetFileName(path);
            Assert.Empty(Directory.GetFiles(directory, $".{fileName}.*.sgpdf.tmp"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Export_RejectsRenderedLabelCountMismatchWithoutTouchingDestination()
    {
        var document = CreateDocument();
        var plan = LabelLayoutPlanner.CreatePlan(
            new LabelOutputSequence(
                document,
                new ZplQuantitySelection(ZplQuantityMode.OneEach)),
            new LabelLayoutSettings(LabelMediaKind.Thermal),
            labelWidthMm: 50,
            labelHeightMm: 30);
        var path = CreateTempPdfPath();
        var originalBytes = Encoding.UTF8.GetBytes("KEEP_ME");
        File.WriteAllBytes(path, originalBytes);

        try
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new LabelPdfExporter().Export(path, plan, CreateRenderedLabels(1, 50, 30)));
            Assert.Contains("design", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(originalBytes, File.ReadAllBytes(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static ZplDocument CreateDocument()
        => ZplDocumentParser.Parse(
            @"C:\labels\orders.zpl",
            "^XA^FO10,10^FDOne^FS^XZ\n^XA^FO10,10^FDTwo^FS^XZ");

    private static IReadOnlyList<ZplRenderedLabel> CreateRenderedLabels(
        int count,
        double widthMm,
        double heightMm)
    {
        var result = new List<ZplRenderedLabel>(count);
        for (var index = 0; index < count; index++)
        {
            result.Add(new ZplRenderedLabel(
                index,
                widthMm,
                heightMm,
                8,
                CreatePngBytes(index == 0 ? Colors.Black : Colors.DarkGray)));
        }

        return result;
    }

    private static byte[] CreatePngBytes(Color color)
    {
        var pixels = new byte[] { color.B, color.G, color.R, color.A };
        var bitmap = BitmapSource.Create(
            1,
            1,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static string CreateTempPdfPath()
        => Path.Combine(
            Path.GetTempPath(),
            $"sgpdf-export-test-{Guid.NewGuid():N}.pdf");

    private static string ReadPdfAsLatin1(string path)
        => Encoding.Latin1.GetString(File.ReadAllBytes(path));

    private static double MmToPoints(double mm)
        => mm * 72d / 25.4d;

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    [GeneratedRegex(@"/Type\s*/Page(?!s)\b")]
    private static partial Regex PageObjectRegex();

    [GeneratedRegex(@"/MediaBox\s*\[\s*0(?:\.0+)?\s+0(?:\.0+)?\s+([0-9.]+)\s+([0-9.]+)\s*\]")]
    private static partial Regex MediaBoxRegex();
}
