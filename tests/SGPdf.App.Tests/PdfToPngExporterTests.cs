using System.Text;
using SGPdf.App.Features.Conversions;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfToPngExporterTests
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    [Fact]
    public void ExportAllPages_WritesOneDeterministicallyNamedPngPerPage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"sgpdf-pdf-to-png-{Guid.NewGuid():N}");
        var source = Path.Combine(root, "pedido.pdf");
        var output = Path.Combine(root, "png");

        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(source, CreatePdf(
                "0 0 0 rg\n72 720 144 36 re f\n",
                "0 0 0 rg\n72 600 72 72 re f\n"));

            var result = new PdfToPngExporter().ExportAllPages(source, output);

            Assert.Equal(2, result.Count);
            Assert.Equal("pedido-pagina-001.png", Path.GetFileName(result[0]));
            Assert.Equal("pedido-pagina-002.png", Path.GetFileName(result[1]));
            Assert.All(result, path =>
            {
                Assert.True(File.Exists(path));
                Assert.True(File.ReadAllBytes(path).AsSpan().StartsWith(PngSignature));
            });
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ExportAllPages_WhenAnyTargetExists_FailsBeforeWritingAnything()
    {
        var root = Path.Combine(Path.GetTempPath(), $"sgpdf-pdf-to-png-collision-{Guid.NewGuid():N}");
        var source = Path.Combine(root, "pedido.pdf");
        var output = Path.Combine(root, "png");
        var existing = Path.Combine(output, "pedido-pagina-002.png");
        var sentinel = Encoding.UTF8.GetBytes("existing-file");

        try
        {
            Directory.CreateDirectory(output);
            File.WriteAllBytes(source, CreatePdf("", ""));
            File.WriteAllBytes(existing, sentinel);

            var error = Assert.Throws<IOException>(() =>
                new PdfToPngExporter().ExportAllPages(source, output));

            Assert.Contains("ya existe", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(Path.Combine(output, "pedido-pagina-001.png")));
            Assert.Equal(sentinel, File.ReadAllBytes(existing));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExportAllPages_BlankOutputDirectory_IsRejected(string outputDirectory)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new PdfToPngExporter().ExportAllPages("source.pdf", outputDirectory));

        Assert.Equal("outputDirectory", error.ParamName);
    }

    private static byte[] CreatePdf(params string[] pageContents)
    {
        if (pageContents.Length == 0)
            throw new ArgumentException("Se requiere al menos una página.", nameof(pageContents));

        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
        };

        var pageIds = Enumerable.Range(0, pageContents.Length)
            .Select(index => 3 + index * 2)
            .ToArray();
        var kids = string.Join(" ", pageIds.Select(id => $"{id} 0 R"));
        objects.Add($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageContents.Length} >>\nendobj\n");

        for (var index = 0; index < pageContents.Length; index++)
        {
            var pageId = pageIds[index];
            var contentId = pageId + 1;
            var content = pageContents[index];
            objects.Add($"{pageId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents {contentId} 0 R >>\nendobj\n");
            objects.Add($"{contentId} 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream\nendobj\n");
        }

        return BuildPdf(objects);
    }

    private static byte[] BuildPdf(IReadOnlyList<string> objects)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
        {
            NewLine = "\n"
        };

        writer.Write("%PDF-1.4\n");
        writer.Flush();
        var offsets = new List<long> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            writer.Write(obj);
            writer.Flush();
        }

        var xref = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        for (var index = 1; index < offsets.Count; index++)
            writer.Write($"{offsets[index]:D10} 00000 n \n");

        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        writer.Flush();
        return stream.ToArray();
    }
}
