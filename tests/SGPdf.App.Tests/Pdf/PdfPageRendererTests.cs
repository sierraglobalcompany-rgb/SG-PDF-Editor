using System.Reflection;
using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests.Pdf;

public sealed class PdfPageRendererTests
{
    [Fact]
    public void Render_returns_expected_dimensions_and_pixels()
    {
        var rendererType = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfPageRenderer");
        Assert.NotNull(rendererType);

        var renderMethod = rendererType.GetMethod(
            "Render",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(PdfDocumentSession), typeof(int), typeof(int), typeof(int)],
            modifiers: null);
        Assert.NotNull(renderMethod);

        var path = CreateSinglePagePdf();
        try
        {
            using var session = PdfDocumentSession.Open(path);
            var result = renderMethod.Invoke(null, [session, 0, 200, 300]);

            Assert.NotNull(result);

            var resultType = result.GetType();
            Assert.Equal(200, (int)resultType.GetProperty("Width")!.GetValue(result)!);
            Assert.Equal(300, (int)resultType.GetProperty("Height")!.GetValue(result)!);

            var stride = (int)resultType.GetProperty("Stride")!.GetValue(result)!;
            var pixels = (byte[])resultType.GetProperty("Pixels")!.GetValue(result)!;

            Assert.True(stride >= 200 * 4);
            Assert.Equal(stride * 300, pixels.Length);
            Assert.Contains((byte)0, pixels);
            Assert.Contains((byte)255, pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateSinglePagePdf()
    {
        const string content = "0 0 0 rg 20 20 50 50 re f\n";
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] /Contents 4 0 R >>\nendobj\n",
            $"4 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream\nendobj\n"
        };

        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(obj);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 5\n");
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");

        builder.Append("trailer\n<< /Size 5 /Root 1 0 R >>\n");
        builder.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-render-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, builder.ToString(), Encoding.ASCII);
        return path;
    }
}
