using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfRenderTests
{
    [Fact]
    public void RenderPage_RasterizesLetterPageAt96Dpi()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-render-{Guid.NewGuid():N}.pdf");

        try
        {
            File.WriteAllBytes(path, CreatePdf("0 0 0 rg\n72 720 144 36 re f\n"));

            using var session = PdfDocumentSession.Open(path);
            var rendered = session.RenderPage(0, 96d);

            Assert.Equal(0, rendered.PageIndex);
            Assert.Equal(816, rendered.PixelWidth);
            Assert.Equal(1056, rendered.PixelHeight);
            Assert.Equal(816 * 4, rendered.Stride);
            Assert.Equal(rendered.Stride * rendered.PixelHeight, rendered.Pixels.Length);
            Assert.Contains(rendered.Pixels, value => value < 250);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MultiPageDocument_ReportsCountAndRendersLastPage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-multipage-{Guid.NewGuid():N}.pdf");

        try
        {
            File.WriteAllBytes(path, CreatePdf(
                "0 0 0 rg\n72 720 144 36 re f\n",
                "0 0 0 rg\n72 600 72 72 re f\n",
                "0 0 0 rg\n300 400 100 100 re f\n"));

            using var session = PdfDocumentSession.Open(path);
            Assert.Equal(3, session.PageCount);

            var rendered = session.RenderPage(2, 96d);
            Assert.Equal(2, rendered.PageIndex);
            Assert.Equal(816, rendered.PixelWidth);
            Assert.Equal(1056, rendered.PixelHeight);
            Assert.Contains(rendered.Pixels, value => value < 250);
            Assert.Throws<ArgumentOutOfRangeException>(() => session.RenderPage(3, 96d));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void InvalidPdf_OpenThrowsControlledInvalidOperationException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-invalid-{Guid.NewGuid():N}.pdf");

        try
        {
            File.WriteAllText(path, "not a pdf", Encoding.ASCII);

            var error = Assert.ThrowsAny<InvalidOperationException>(() => PdfDocumentSession.Open(path));
            Assert.Contains("PDFium no pudo abrir el documento", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetPageSize_PreCanceledToken_ThrowsOperationCanceledException()
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetPageSize",
            new[] { typeof(int), typeof(CancellationToken) });
        Assert.NotNull(method);

        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-size-cancel-{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(path, CreatePdf("0 0 0 rg\n72 720 144 36 re f\n"));
            using var session = PdfDocumentSession.Open(path);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method!.Invoke(session, new object[] { 0, cancellation.Token }));

            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RenderPage_PreCanceledToken_ThrowsOperationCanceledException()
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "RenderPage",
            new[] { typeof(int), typeof(double), typeof(CancellationToken) });
        Assert.NotNull(method);

        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-render-cancel-{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(path, CreatePdf("0 0 0 rg\n72 720 144 36 re f\n"));
            using var session = PdfDocumentSession.Open(path);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method!.Invoke(session, new object[] { 0, 96d, cancellation.Token }));

            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetPageSizes_MixedPortraitLandscape_ReturnsIndexOrderedPointSizes()
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetPageSizes",
            new[] { typeof(CancellationToken) });
        Assert.NotNull(method);

        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-page-sizes-{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(path, CreatePdfWithPageSizes((612d, 792d), (792d, 612d)));
            using var session = PdfDocumentSession.Open(path);

            var result = method!.Invoke(session, new object[] { CancellationToken.None });
            var sizes = Assert.IsAssignableFrom<IEnumerable>(result).Cast<object>().ToArray();

            Assert.Equal(2, sizes.Length);
            Assert.Equal(612d, GetDoubleProperty(sizes[0], "WidthPoints"), 6);
            Assert.Equal(792d, GetDoubleProperty(sizes[0], "HeightPoints"), 6);
            Assert.Equal(792d, GetDoubleProperty(sizes[1], "WidthPoints"), 6);
            Assert.Equal(612d, GetDoubleProperty(sizes[1], "HeightPoints"), 6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetPageSizes_PreCanceledToken_ThrowsWithoutPartialResult()
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetPageSizes",
            new[] { typeof(CancellationToken) });
        Assert.NotNull(method);

        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-page-sizes-cancel-{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(path, CreatePdfWithPageSizes((612d, 792d), (792d, 612d)));
            using var session = PdfDocumentSession.Open(path);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method!.Invoke(session, new object[] { cancellation.Token }));

            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static double GetDoubleProperty(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (double)property!.GetValue(target)!;
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

    private static byte[] CreatePdfWithPageSizes(params (double Width, double Height)[] pageSizes)
    {
        if (pageSizes.Length == 0)
            throw new ArgumentException("Se requiere al menos una página.", nameof(pageSizes));

        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
        };

        var pageIds = Enumerable.Range(0, pageSizes.Length)
            .Select(index => 3 + index * 2)
            .ToArray();
        var kids = string.Join(" ", pageIds.Select(id => $"{id} 0 R"));
        objects.Add($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageSizes.Length} >>\nendobj\n");

        for (var index = 0; index < pageSizes.Length; index++)
        {
            var pageId = pageIds[index];
            var contentId = pageId + 1;
            var width = pageSizes[index].Width.ToString(CultureInfo.InvariantCulture);
            var height = pageSizes[index].Height.ToString(CultureInfo.InvariantCulture);
            const string content = "";

            objects.Add($"{pageId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Contents {contentId} 0 R >>\nendobj\n");
            objects.Add($"{contentId} 0 obj\n<< /Length 0 >>\nstream\n{content}endstream\nendobj\n");
        }

        return BuildPdf(objects);
    }

    private static byte[] BuildPdf(IReadOnlyList<string> objects)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long>();
        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, obj);
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");

        foreach (var offset in offsets)
            WriteAscii(stream, $"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
