using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Conversions;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfToJpgExporterTests
{
    [Fact]
    public void ExportAllPagesAsJpg_WritesValidColorJpegsAt144DpiAndPreservesSource()
    {
        using var temp = new TempDirectory();
        var source = Path.Combine(temp.Path, "colores.pdf");
        var output = Path.Combine(temp.Path, "jpg");
        File.WriteAllBytes(source, CreatePdf(
            new PdfPage("1 0 0 rg\n0 0 612 792 re f\n", 612, 792),
            new PdfPage("0 1 0 rg\n0 0 612 792 re f\n", 612, 792),
            new PdfPage("0 0 1 rg\n0 0 612 792 re f\n", 612, 792)));
        var sourceHash = HashFile(source);

        var paths = InvokeExport(source, output, CancellationToken.None);

        Assert.Equal(3, paths.Count);
        Assert.Equal("colores-pagina-001.jpg", Path.GetFileName(paths[0]));
        Assert.Equal("colores-pagina-002.jpg", Path.GetFileName(paths[1]));
        Assert.Equal("colores-pagina-003.jpg", Path.GetFileName(paths[2]));
        Assert.All(paths, path =>
        {
            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 3);
            Assert.Equal(new byte[] { 0xff, 0xd8, 0xff }, bytes[..3]);
        });

        AssertJpeg(paths[0], Colors.Red);
        AssertJpeg(paths[1], Colors.Lime);
        AssertJpeg(paths[2], Colors.Blue);
        Assert.Equal(sourceHash, HashFile(source));
    }

    [Fact]
    public void ExportAllPagesAsJpg_CollisionAndCancellationDoNotCreateOrChangeOutputs()
    {
        using var temp = new TempDirectory();
        var source = Path.Combine(temp.Path, "collisions.pdf");
        var output = Path.Combine(temp.Path, "jpg");
        File.WriteAllBytes(source, CreatePdf(
            new PdfPage("1 0 0 rg\n0 0 612 792 re f\n", 612, 792),
            new PdfPage("0 0 1 rg\n0 0 612 792 re f\n", 612, 792)));
        Directory.CreateDirectory(output);
        var sourceHash = HashFile(source);
        var existing = Path.Combine(output, "collisions-pagina-002.jpg");
        var sentinel = Encoding.UTF8.GetBytes("preexisting-jpg");
        File.WriteAllBytes(existing, sentinel);

        Assert.Throws<IOException>(() => InvokeExport(source, output, CancellationToken.None));
        Assert.False(File.Exists(Path.Combine(output, "collisions-pagina-001.jpg")));
        Assert.Equal(sentinel, File.ReadAllBytes(existing));

        File.Delete(existing);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => InvokeExport(source, output, cancellation.Token));
        Assert.Empty(Directory.GetFiles(output, "*.jpg"));
        Assert.Equal(sourceHash, HashFile(source));
    }

    [Fact]
    public void ExportAllPagesAsJpg_WhenLaterPageCannotRender_RollsBackEarlierOwnedJpg()
    {
        using var temp = new TempDirectory();
        var source = Path.Combine(temp.Path, "render-failure.pdf");
        var output = Path.Combine(temp.Path, "jpg");
        File.WriteAllBytes(source, CreatePdf(
            new PdfPage("1 0 0 rg\n0 0 612 792 re f\n", 612, 792),
            new PdfPage(string.Empty, 1_000_000_000, 1_000_000_000)));

        var error = Record.Exception(() => InvokeExport(source, output, CancellationToken.None));
        Assert.IsType<InvalidOperationException>(error);
        Assert.Contains("bitmap", error!.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Empty(Directory.Exists(output) ? Directory.GetFiles(output, "*.jpg") : Array.Empty<string>());
        Assert.Empty(Directory.Exists(output) ? Directory.GetFiles(output, "*.sgpdf.tmp") : Array.Empty<string>());
    }

    private static IReadOnlyList<string> InvokeExport(string source, string output, CancellationToken token)
    {
        return new PdfToPngExporter().ExportAllPagesAsJpg(source, output, token);
    }

    private static void AssertJpeg(string path, Color expected)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        Assert.Equal(1224, frame.PixelWidth);
        Assert.Equal(1584, frame.PixelHeight);
        Assert.InRange(frame.DpiX, 143, 145);
        Assert.InRange(frame.DpiY, 143, 145);

        BitmapSource bgra = frame.Format == PixelFormats.Bgra32
            ? frame
            : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0d);
        var pixels = new byte[checked(bgra.PixelWidth * bgra.PixelHeight * 4)];
        bgra.CopyPixels(pixels, bgra.PixelWidth * 4, 0);
        var offset = (bgra.PixelHeight / 2 * bgra.PixelWidth + bgra.PixelWidth / 2) * 4;
        var actual = Color.FromRgb(pixels[offset + 2], pixels[offset + 1], pixels[offset]);
        Assert.InRange(Math.Abs(expected.R - actual.R), 0, 28);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0, 28);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0, 28);
    }

    private static byte[] CreatePdf(params PdfPage[] pages)
    {
        if (pages.Length == 0)
            throw new ArgumentException("Se requiere al menos una página.", nameof(pages));

        var objects = new List<string> { "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n" };
        var pageIds = Enumerable.Range(0, pages.Length).Select(index => 3 + index * 2).ToArray();
        var kids = string.Join(" ", pageIds.Select(id => $"{id} 0 R"));
        objects.Add($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pages.Length} >>\nendobj\n");

        for (var index = 0; index < pages.Length; index++)
        {
            var pageId = pageIds[index];
            var contentId = pageId + 1;
            var page = pages[index];
            objects.Add($"{pageId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {page.Width} {page.Height}] /Contents {contentId} 0 R >>\nendobj\n");
            objects.Add($"{contentId} 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(page.Content)} >>\nstream\n{page.Content}endstream\nendobj\n");
        }

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n");
        writer.Flush();
        var offsets = new List<long> { 0 };
        foreach (var item in objects)
        {
            offsets.Add(stream.Position);
            writer.Write(item);
            writer.Flush();
        }

        var xref = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        for (var index = 1; index < offsets.Count; index++)
            writer.Write($"{offsets[index]:D10} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        writer.Flush();
        return stream.ToArray();
    }

    private static string HashFile(string path)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));

    private sealed record PdfPage(string Content, int Width, int Height);

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-pdf-to-jpg-{Guid.NewGuid():N}");
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
