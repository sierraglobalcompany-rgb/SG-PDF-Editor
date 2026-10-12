using System.Globalization;
using System.Reflection;
using System.Text;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrRasterizerTests
{
    [Fact]
    public void Render_UsesFixed300Dpi()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-ocr-raster-{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(path, CreatePdf(72d, 72d));
            using var session = PdfDocumentSession.Open(path);

            var rendered = InvokeRender(session, 0);

            Assert.Equal(300d, rendered.Dpi);
            Assert.Equal(300, rendered.PixelWidth);
            Assert.Equal(300, rendered.PixelHeight);
            Assert.Equal(rendered.Stride * rendered.PixelHeight, rendered.Pixels.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Render_RejectsDimensionAbove20000PixelsBeforeAllocation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-ocr-raster-wide-{Guid.NewGuid():N}.pdf");
        try
        {
            // 4,800.25 pt at 300 DPI rounds up to 20,002 px.
            File.WriteAllBytes(path, CreatePdf(4800.25d, 72d));
            using var session = PdfDocumentSession.Open(path);

            var error = Assert.Throws<TargetInvocationException>(() => InvokeRenderViaReflection(session, 0));
            var inner = Assert.IsType<InvalidOperationException>(error.InnerException);
            Assert.Contains("límite", inner.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("OCR", inner.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Render_RejectsMoreThan100MillionPixelsBeforeAllocation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-ocr-raster-area-{Guid.NewGuid():N}.pdf");
        try
        {
            // 2,880 x 2,160 pt at 300 DPI => 12,000 x 9,000 = 108,000,000 px.
            File.WriteAllBytes(path, CreatePdf(2880d, 2160d));
            using var session = PdfDocumentSession.Open(path);

            var error = Assert.Throws<TargetInvocationException>(() => InvokeRenderViaReflection(session, 0));
            var inner = Assert.IsType<InvalidOperationException>(error.InnerException);
            Assert.Contains("límite", inner.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("OCR", inner.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static PdfRenderedPage InvokeRender(PdfDocumentSession session, int pageIndex)
    {
        return Assert.IsType<PdfRenderedPage>(InvokeRenderViaReflection(session, pageIndex));
    }

    private static object? InvokeRenderViaReflection(PdfDocumentSession session, int pageIndex)
    {
        var type = typeof(OcrRequest).Assembly.GetType("SGPdf.App.Features.Ocr.OcrRasterizer");
        Assert.NotNull(type);

        var method = type!.GetMethod(
            "Render",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(PdfDocumentSession), typeof(int), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);

        return method!.Invoke(null, new object[] { session, pageIndex, CancellationToken.None });
    }

    private static byte[] CreatePdf(double widthPoints, double heightPoints)
    {
        var width = widthPoints.ToString("0.########", CultureInfo.InvariantCulture);
        var height = heightPoints.ToString("0.########", CultureInfo.InvariantCulture);
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Contents 4 0 R >>\nendobj\n",
            "4 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n"
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
