using System.Globalization;
using System.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

public sealed class PdfRenderTests
{
    [Fact]
    public void RenderPage_RasterizesLetterPageAt96Dpi()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-render-{Guid.NewGuid():N}.pdf");

        try
        {
            File.WriteAllBytes(path, CreateSinglePagePdf());

            using var session = PdfDocumentSession.Open(path);
            dynamic dynamicSession = session;
            dynamic rendered = dynamicSession.RenderPage(0, 96d);

            Assert.Equal(0, (int)rendered.PageIndex);
            Assert.Equal(816, (int)rendered.PixelWidth);
            Assert.Equal(1056, (int)rendered.PixelHeight);
            Assert.Equal(816 * 4, (int)rendered.Stride);

            byte[] pixels = rendered.Pixels;
            Assert.Equal(rendered.Stride * rendered.PixelHeight, pixels.Length);
            Assert.Contains(pixels, value => value < 250);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static byte[] CreateSinglePagePdf()
    {
        const string pageContent = "0 0 0 rg\n72 720 144 36 re f\n";
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n",
            $"4 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(pageContent)} >>\nstream\n{pageContent}endstream\nendobj\n"
        };

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long>();
        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, obj);
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Length + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");

        foreach (var offset in offsets)
            WriteAscii(stream, $"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");

        WriteAscii(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
