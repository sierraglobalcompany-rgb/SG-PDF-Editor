using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfOcrWriterEndToEndTests
{
    [Fact]
    public void SaveAsCopy_ImageOnlySpanish_BecomesSearchableSelectableCopyable_WithoutSourceMutation()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        AssertWriterRoundTrip(
            snapshot,
            "ocr-writer-spanish.pdf",
            LinePlacement(0, new[] { "Factura", "número", "12345", "NIÑO", "Bogotá" }),
            new[] { "número", "NIÑO", "Bogotá", "12345" });
    }

    [Fact]
    public void SaveAsCopy_EnglishPlacements_RoundTrip()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        AssertWriterRoundTrip(
            snapshot,
            "ocr-writer-english.pdf",
            LinePlacement(0, new[] { "Invoice", "total", "searchable" }),
            new[] { "Invoice", "searchable" });
    }

    [Fact]
    public void SaveAsCopy_BilingualPlacements_RoundTrip()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        AssertWriterRoundTrip(
            snapshot,
            "ocr-writer-bilingual.pdf",
            LinePlacement(0, new[] { "Factura", "invoice", "NIÑO", "Bogotá" }),
            new[] { "Factura", "invoice", "NIÑO", "Bogotá" });
    }

    [Fact]
    public void SaveAsCopy_RotatedImageOnlySource_RoundTripsSearchableText()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: true);
        AssertWriterRoundTrip(
            snapshot,
            "ocr-writer-rotated.pdf",
            LinePlacement(0, new[] { "rotado", "NIÑO", "Bogotá" }),
            new[] { "rotado", "NIÑO", "Bogotá" });
    }

    [Fact]
    public void SaveAsCopy_DefaultPublicationGate_RejectsReadableButSemanticallyMisplacedOutput()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var destination = Path.Combine(snapshot.DirectoryPath, "ocr-writer-semantic-failure.pdf");
        var lineBounds = new PdfOcrRect(34d, 214d, 120d, 232d);
        var misplacedWordBounds = new PdfOcrRect(180d, 300d, 250d, 320d);
        var placement = new OcrTextLinePlacement(
            0,
            "Bogotá",
            lineBounds,
            new[] { new OcrTextWordPlacement("Bogotá", 95d, misplacedWordBounds, 0, 1) },
            0,
            1,
            1,
            1);

        var result = new PdfOcrWriter().SaveAsCopy(
            snapshot.SourcePath,
            PdfEditSourceFingerprint.Capture(snapshot.SourcePath),
            new[] { placement },
            destination,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(File.Exists(destination));
        Assert.Equal(snapshot.SourceBytesBefore, File.ReadAllBytes(snapshot.SourcePath));
        Assert.Empty(Directory.GetFiles(snapshot.DirectoryPath, "*.sgpdf-tmp.pdf", SearchOption.TopDirectoryOnly));
    }

    private static void AssertWriterRoundTrip(
        OcrInvisibleTextCapabilitySnapshot snapshot,
        string outputName,
        OcrTextLinePlacement placement,
        IReadOnlyList<string> expectedTokens)
    {
        var sourceBefore = File.ReadAllBytes(snapshot.SourcePath);
        var destination = Path.Combine(snapshot.DirectoryPath, outputName);

        var result = new PdfOcrWriter().SaveAsCopy(
            snapshot.SourcePath,
            PdfEditSourceFingerprint.Capture(snapshot.SourcePath),
            new[] { placement },
            destination,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBefore, File.ReadAllBytes(snapshot.SourcePath));
        Assert.Empty(Directory.GetFiles(snapshot.DirectoryPath, "*.sgpdf-tmp.pdf", SearchOption.TopDirectoryOnly));

        using var output = PdfDocumentSession.Open(destination);
        var copiedText = output.GetPageText(0);
        Assert.False(string.IsNullOrWhiteSpace(copiedText));

        foreach (var token in expectedTokens)
        {
            var matches = output.FindTextOnPage(0, token);
            Assert.NotEmpty(matches);
            var exact = matches.Any(match =>
                string.Equals(
                    output.GetTextRange(0, match.StartIndex, match.CharacterCount),
                    token,
                    StringComparison.Ordinal));
            Assert.True(exact, $"Expected exact OCR token '{token}' after reopen.");
            Assert.Contains(token, copiedText, StringComparison.Ordinal);
        }

        var outputRender = output.RenderPage(0, snapshot.SourceRender.Dpi);
        Assert.Equal(snapshot.SourceRender.PixelWidth, outputRender.PixelWidth);
        Assert.Equal(snapshot.SourceRender.PixelHeight, outputRender.PixelHeight);
        Assert.Equal(snapshot.SourceRender.Stride, outputRender.Stride);
        Assert.Equal(snapshot.SourceRender.Pixels, outputRender.Pixels);
    }

    private static OcrTextLinePlacement LinePlacement(int pageIndex, IReadOnlyList<string> tokens)
    {
        const double bottom = 214d;
        const double top = 232d;
        const double gap = 6d;
        var left = 34d;
        var words = new List<OcrTextWordPlacement>(tokens.Count);

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            var width = Math.Max(30d, token.Length * 9d);
            var right = left + width;
            words.Add(new OcrTextWordPlacement(
                token,
                95d,
                new PdfOcrRect(left, bottom, right, top),
                index,
                index + 1));
            left = right + gap;
        }

        var bounds = new PdfOcrRect(
            words.Min(word => word.Bounds.Left),
            bottom,
            words.Max(word => word.Bounds.Right),
            top);
        return new OcrTextLinePlacement(
            pageIndex,
            string.Join(' ', tokens),
            bounds,
            words,
            0,
            1,
            1,
            1);
    }
}
