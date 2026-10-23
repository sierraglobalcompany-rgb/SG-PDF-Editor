using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Conversions;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageToPdfExporterTests
{
    [Fact]
    public void Export_WritesOrderedA4PagesAndNormalizesExifOrientation()
    {
        using var temp = new TempDirectory();
        var normalJpeg = Path.Combine(temp.Path, "normal-landscape.jpg");
        var portraitPng = Path.Combine(temp.Path, "portrait.png");
        var rotatedJpeg = Path.Combine(temp.Path, "rotated-exif-6.jpg");
        var output = Path.Combine(temp.Path, "ordered.pdf");

        WriteJpeg(normalJpeg, 80, 40, (_, _) => Colors.LimeGreen, orientation: 1);
        WritePng(portraitPng, 40, 80, (_, _) => Colors.OrangeRed);
        WriteJpeg(rotatedJpeg, 80, 40, (x, _) => x < 40 ? Colors.Red : Colors.Blue, orientation: 6);

        var inputs = new[] { normalJpeg, portraitPng, rotatedJpeg };
        var inputHashes = inputs.ToDictionary(path => path, HashFile, StringComparer.OrdinalIgnoreCase);

        new ImageToPdfExporter().Export([portraitPng, rotatedJpeg, normalJpeg], output, CancellationToken.None);

        Assert.True(File.Exists(output));
        using (var pdf = PdfReader.Open(output, PdfDocumentOpenMode.Import))
        {
            Assert.Equal(3, pdf.Pages.Count);
            Assert.InRange(pdf.Pages[0].Width.Point, 594.7, 595.9);
            Assert.InRange(pdf.Pages[0].Height.Point, 841.2, 842.5);
            Assert.InRange(pdf.Pages[1].Width.Point, 594.7, 595.9);
            Assert.InRange(pdf.Pages[1].Height.Point, 841.2, 842.5);
            Assert.InRange(pdf.Pages[2].Width.Point, 841.2, 842.5);
            Assert.InRange(pdf.Pages[2].Height.Point, 594.7, 595.9);
        }

        using (var session = PdfDocumentSession.Open(output))
        {
            Assert.Equal(3, session.PageCount);
            AssertColorNear(Colors.OrangeRed, Sample(session.RenderPage(0, 72), 0.5, 0.5), 18);

            var rotated = session.RenderPage(1, 72);
            AssertColorNear(Colors.Red, Sample(rotated, 0.5, 0.30), 40);
            AssertColorNear(Colors.Blue, Sample(rotated, 0.5, 0.70), 40);

            AssertColorNear(Colors.LimeGreen, Sample(session.RenderPage(2, 72), 0.5, 0.5), 20);
        }

        foreach (var (path, expectedHash) in inputHashes)
            Assert.Equal(expectedHash, HashFile(path));
        Assert.Empty(Directory.GetFiles(temp.Path, "*.sgpdf.tmp"));
    }

    [Fact]
    public void Export_CompositesTransparentPngOverWhite()
    {
        using var temp = new TempDirectory();
        var source = Path.Combine(temp.Path, "transparent.png");
        var output = Path.Combine(temp.Path, "white-background.pdf");
        WritePng(source, 40, 80, (_, y) => y < 40 ? Colors.Red : Color.FromArgb(0, 0, 0, 0));

        new ImageToPdfExporter().Export([source], output, CancellationToken.None);

        using var session = PdfDocumentSession.Open(output);
        var rendered = session.RenderPage(0, 72);
        AssertColorNear(Colors.Red, Sample(rendered, 0.5, 0.30), 20);
        AssertColorNear(Colors.White, Sample(rendered, 0.5, 0.70), 12);
    }

    [Fact]
    public void Export_RejectsEmptyCorruptCancelledAndExistingDestinationWithoutPublishing()
    {
        using var temp = new TempDirectory();
        var valid = Path.Combine(temp.Path, "valid.png");
        var corrupt = Path.Combine(temp.Path, "corrupt.png");
        var output = Path.Combine(temp.Path, "new.pdf");
        var existing = Path.Combine(temp.Path, "existing.pdf");
        WritePng(valid, 20, 30, (_, _) => Colors.CornflowerBlue);
        File.WriteAllBytes(corrupt, [1, 2, 3, 4, 5]);

        Assert.Throws<ArgumentException>(() => new ImageToPdfExporter().Export([], output, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => new ImageToPdfExporter().Export([corrupt], output, CancellationToken.None));
        Assert.False(File.Exists(output));

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => new ImageToPdfExporter().Export([valid], output, cancellation.Token));
        }
        Assert.False(File.Exists(output));

        var sentinel = new byte[] { 9, 8, 7, 6 };
        File.WriteAllBytes(existing, sentinel);
        Assert.Throws<IOException>(() => new ImageToPdfExporter().Export([valid], existing, CancellationToken.None));
        Assert.Equal(sentinel, File.ReadAllBytes(existing));
        Assert.False(File.Exists(output));
        Assert.Empty(Directory.GetFiles(temp.Path, "*.sgpdf.tmp"));
    }

    private static void WritePng(string path, int width, int height, Func<int, int, Color> colorAt)
    {
        var bitmap = CreateBitmap(width, height, colorAt);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void WriteJpeg(
        string path,
        int width,
        int height,
        Func<int, int, Color> colorAt,
        ushort orientation)
    {
        var metadata = new BitmapMetadata("jpg");
        metadata.SetQuery("/app1/ifd/{ushort=274}", orientation);
        var encoder = new JpegBitmapEncoder { QualityLevel = 100 };
        encoder.Frames.Add(BitmapFrame.Create(CreateBitmap(width, height, colorAt), null, metadata, null));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static BitmapSource CreateBitmap(int width, int height, Func<int, int, Color> colorAt)
    {
        var stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var color = colorAt(x, y);
            var offset = y * stride + x * 4;
            pixels[offset] = color.B;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.R;
            pixels[offset + 3] = color.A;
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static Color Sample(PdfRenderedPage page, double xRatio, double yRatio)
    {
        var x = Math.Clamp((int)(page.PixelWidth * xRatio), 0, page.PixelWidth - 1);
        var y = Math.Clamp((int)(page.PixelHeight * yRatio), 0, page.PixelHeight - 1);
        var offset = checked(y * page.Stride + x * 4);
        return Color.FromRgb(page.Pixels[offset + 2], page.Pixels[offset + 1], page.Pixels[offset]);
    }

    private static void AssertColorNear(Color expected, Color actual, int tolerance)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0, tolerance);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0, tolerance);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0, tolerance);
    }

    private static string HashFile(string path)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-image-to-pdf-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
