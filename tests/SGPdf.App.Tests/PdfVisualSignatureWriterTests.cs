using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfVisualSignatureWriterTests
{
    [Fact]
    public void SaveAsCopy_PreservesAlphaOrientationPositionAndPageCount()
    {
        using var fixture = PdfFixture.CreateBluePage();
        var destination = fixture.PathFor("signed.pdf");
        var asset = CreateAsymmetricAsset();
        var placement = new SignaturePlacement(
            Guid.NewGuid(),
            0,
            new PdfRect(72d, 72d, 72d, 72d),
            asset);

        new PdfVisualSignatureWriter().SaveAsCopy(
            fixture.SourcePath,
            destination,
            new[] { placement });

        using var session = PdfDocumentSession.Open(destination);
        Assert.Equal(1, session.PageCount);
        var rendered = session.RenderPage(0, dpi: 300d);

        // Asset quadrants are intentionally asymmetric. These samples prove
        // orientation and alpha after PDFium save/reopen rather than file creation only.
        var opaque = SamplePdfPoint(rendered, pageHeightPoints: 300d, x: 81d, y: 135d);
        var transparent = SamplePdfPoint(rendered, 300d, x: 135d, y: 135d);
        var semi = SamplePdfPoint(rendered, 300d, x: 81d, y: 81d);
        var bottomRight = SamplePdfPoint(rendered, 300d, x: 135d, y: 81d);

        Assert.True(IsNear(opaque, b: 0, g: 0, r: 0, tolerance: 35));
        Assert.True(IsNear(transparent, b: 255, g: 0, r: 0, tolerance: 35));
        Assert.InRange(semi.B, 90, 180);
        Assert.InRange(semi.R, 90, 180);
        Assert.True(bottomRight.R > 180 && bottomRight.G < 80 && bottomRight.B < 80);
    }

    [Fact]
    public void SaveAsCopy_SourceEqualsDestination_IsRejectedBeforeWrite()
    {
        using var fixture = PdfFixture.CreateBluePage();
        var placement = ValidPlacement();

        Assert.Throws<ArgumentException>(() =>
            new PdfVisualSignatureWriter().SaveAsCopy(
                fixture.SourcePath,
                fixture.SourcePath,
                new[] { placement }));
    }

    [Fact]
    public void SaveAsCopy_EmptyPlacements_IsRejectedWithoutOutput()
    {
        using var fixture = PdfFixture.CreateBluePage();
        var destination = fixture.PathFor("empty.pdf");

        Assert.Throws<ArgumentException>(() =>
            new PdfVisualSignatureWriter().SaveAsCopy(
                fixture.SourcePath,
                destination,
                Array.Empty<SignaturePlacement>()));
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void SaveAsCopy_InvalidPageOrBounds_AreRejected()
    {
        using var fixture = PdfFixture.CreateBluePage();
        var writer = new PdfVisualSignatureWriter();
        var asset = CreateAsymmetricAsset();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            writer.SaveAsCopy(
                fixture.SourcePath,
                fixture.PathFor("bad-page.pdf"),
                new[] { new SignaturePlacement(Guid.NewGuid(), 4, new PdfRect(10, 10, 20, 20), asset) }));

        Assert.Throws<ArgumentException>(() =>
            writer.SaveAsCopy(
                fixture.SourcePath,
                fixture.PathFor("bad-bounds.pdf"),
                new[] { new SignaturePlacement(Guid.NewGuid(), 0, new PdfRect(double.NaN, 10, 20, 20), asset) }));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            writer.SaveAsCopy(
                fixture.SourcePath,
                fixture.PathFor("outside.pdf"),
                new[] { new SignaturePlacement(Guid.NewGuid(), 0, new PdfRect(290, 290, 20, 20), asset) }));
    }

    [Fact]
    public void SaveAsCopy_ForcedNativeFailure_PreservesExistingDestinationAndCleansTemp()
    {
        using var fixture = PdfFixture.CreateBluePage();
        var destination = fixture.PathFor("existing.pdf");
        File.WriteAllText(destination, "KEEP");
        var writer = new PdfVisualSignatureWriter(
            saveAsCopyOverride: static (_, _, _) => 0,
            validateOutputOverride: null);

        Assert.Throws<InvalidOperationException>(() =>
            writer.SaveAsCopy(fixture.SourcePath, destination, new[] { ValidPlacement() }));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void SaveAsCopy_ForcedValidationFailure_PreservesExistingDestinationAndCleansTemp()
    {
        using var fixture = PdfFixture.CreateBluePage();
        var destination = fixture.PathFor("existing-validation.pdf");
        File.WriteAllText(destination, "KEEP");
        var writer = new PdfVisualSignatureWriter(
            saveAsCopyOverride: null,
            validateOutputOverride: static (_, _, _) => throw new InvalidDataException("forced validation failure"));

        Assert.Throws<InvalidDataException>(() =>
            writer.SaveAsCopy(fixture.SourcePath, destination, new[] { ValidPlacement() }));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void GetCryptographicSignatureCount_UnsignedSyntheticPdf_ReturnsZero()
    {
        using var fixture = PdfFixture.CreateBluePage();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        Assert.Equal(0, session.GetCryptographicSignatureCount());
    }

    private static SignaturePlacement ValidPlacement()
        => new(
            Guid.NewGuid(),
            0,
            new PdfRect(72d, 72d, 72d, 72d),
            CreateAsymmetricAsset());

    private static SignatureAsset CreateAsymmetricAsset()
    {
        const int width = 4;
        const int height = 4;
        const int stride = width * 4;
        var pixels = new byte[stride * height];

        // BGRA rows are top-to-bottom for our managed asset.
        FillQuadrant(pixels, stride, x0: 0, y0: 0, b: 0, g: 0, r: 0, a: 255);       // top-left black
        FillQuadrant(pixels, stride, x0: 2, y0: 0, b: 0, g: 0, r: 0, a: 0);         // top-right transparent
        FillQuadrant(pixels, stride, x0: 0, y0: 2, b: 0, g: 0, r: 255, a: 128);     // bottom-left half red
        FillQuadrant(pixels, stride, x0: 2, y0: 2, b: 0, g: 0, r: 255, a: 255);     // bottom-right red

        return new SignatureAsset(width, height, stride, pixels, "asymmetric.png");
    }

    private static void FillQuadrant(byte[] pixels, int stride, int x0, int y0, byte b, byte g, byte r, byte a)
    {
        for (var y = y0; y < y0 + 2; y++)
        {
            for (var x = x0; x < x0 + 2; x++)
            {
                var offset = (y * stride) + (x * 4);
                pixels[offset] = b;
                pixels[offset + 1] = g;
                pixels[offset + 2] = r;
                pixels[offset + 3] = a;
            }
        }
    }

    private static (byte B, byte G, byte R) SamplePdfPoint(
        PdfRenderedPage rendered,
        double pageHeightPoints,
        double x,
        double y)
    {
        var px = Math.Clamp((int)Math.Round(x * rendered.Dpi / 72d), 0, rendered.PixelWidth - 1);
        var py = Math.Clamp((int)Math.Round((pageHeightPoints - y) * rendered.Dpi / 72d), 0, rendered.PixelHeight - 1);
        var offset = (py * rendered.Stride) + (px * 4);
        return (rendered.Pixels[offset], rendered.Pixels[offset + 1], rendered.Pixels[offset + 2]);
    }

    private static bool IsNear((byte B, byte G, byte R) actual, int b, int g, int r, int tolerance)
        => Math.Abs(actual.B - b) <= tolerance &&
           Math.Abs(actual.G - g) <= tolerance &&
           Math.Abs(actual.R - r) <= tolerance;

    private static void AssertNoTempResidue(string destination)
    {
        var directory = Path.GetDirectoryName(destination)!;
        var fileName = Path.GetFileName(destination);
        Assert.Empty(Directory.GetFiles(directory, $".{fileName}.*.sgpdf.tmp"));
    }

    private sealed class PdfFixture : IDisposable
    {
        private PdfFixture(string directory, string sourcePath)
        {
            DirectoryPath = directory;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }
        internal string PathFor(string fileName) => Path.Combine(DirectoryPath, fileName);

        internal static PdfFixture CreateBluePage()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-writer-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");

            var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(300d);
            page.Height = XUnit.FromPoint(300d);
            using (var graphics = XGraphics.FromPdfPage(page))
            {
                graphics.DrawRectangle(XBrushes.Blue, 0d, 0d, 300d, 300d);
            }
            document.Save(source);
            document.Close();
            return new PdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
