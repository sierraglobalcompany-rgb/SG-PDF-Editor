using System.Reflection;
using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfOrganizeSessionTests
{
    [Fact]
    public void NormalPdf_Open_SetsOpenedWithPasswordFalse()
    {
        using var fixture = RotationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.Path);

        Assert.False(ReadOpenedWithPassword(session));
    }

    [Fact]
    public void GetPageRotation_PreRotatedPages_ReturnsZeroToThree()
    {
        using var fixture = RotationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var rotations = Enumerable.Range(0, 4)
            .Select(index => InvokeGetPageRotation(session, index, CancellationToken.None))
            .ToArray();

        Assert.Equal(new[] { 0, 1, 2, 3 }, rotations);
    }

    [Fact]
    public void GetPageRotation_InvalidIndex_Throws()
    {
        using var fixture = RotationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var error = Assert.Throws<TargetInvocationException>(
            () => InvokeGetPageRotationRaw(session, 4, CancellationToken.None));

        Assert.IsType<ArgumentOutOfRangeException>(error.InnerException);
    }

    [Fact]
    public void GetPageRotation_PreCanceled_ThrowsOperationCanceledException()
    {
        using var fixture = RotationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var error = Assert.Throws<TargetInvocationException>(
            () => InvokeGetPageRotationRaw(session, 0, cancellation.Token));

        Assert.IsType<OperationCanceledException>(error.InnerException);
    }

    private static bool ReadOpenedWithPassword(PdfDocumentSession session)
    {
        var property = typeof(PdfDocumentSession).GetProperty(
            "OpenedWithPassword",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
        return Assert.IsType<bool>(property.GetValue(session));
    }

    private static int InvokeGetPageRotation(
        PdfDocumentSession session,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        return Assert.IsType<int>(InvokeGetPageRotationRaw(session, pageIndex, cancellationToken));
    }

    private static object? InvokeGetPageRotationRaw(
        PdfDocumentSession session,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetPageRotation",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(int), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);
        Assert.Equal(typeof(int), method.ReturnType);
        return method.Invoke(session, new object[] { pageIndex, cancellationToken });
    }

    private sealed class RotationPdfFixture : IDisposable
    {
        private RotationPdfFixture(string root, string path)
        {
            Root = root;
            Path = path;
        }

        internal string Root { get; }
        internal string Path { get; }

        internal static RotationPdfFixture Create()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-organize-rotation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = System.IO.Path.Combine(root, "rotations.pdf");
            File.WriteAllBytes(path, BuildPdf());
            return new RotationPdfFixture(root, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }

        private static byte[] BuildPdf()
        {
            var objects = new[]
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Kids [3 0 R 5 0 R 7 0 R 9 0 R] /Count 4 >>\nendobj\n",
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] /Rotate 0 /Contents 4 0 R >>\nendobj\n",
                "4 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
                "5 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] /Rotate 90 /Contents 6 0 R >>\nendobj\n",
                "6 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
                "7 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] /Rotate 180 /Contents 8 0 R >>\nendobj\n",
                "8 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
                "9 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] /Rotate 270 /Contents 10 0 R >>\nendobj\n",
                "10 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n"
            };

            using var stream = new MemoryStream();
            WriteLatin1(stream, "%PDF-1.7\n");
            var offsets = new long[objects.Length + 1];
            for (var index = 0; index < objects.Length; index++)
            {
                offsets[index + 1] = stream.Position;
                WriteLatin1(stream, objects[index]);
            }

            var xrefOffset = stream.Position;
            WriteLatin1(stream, $"xref\n0 {objects.Length + 1}\n");
            WriteLatin1(stream, "0000000000 65535 f \n");
            for (var index = 1; index < offsets.Length; index++)
                WriteLatin1(stream, $"{offsets[index]:D10} 00000 n \n");
            WriteLatin1(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
            return stream.ToArray();
        }

        private static void WriteLatin1(Stream stream, string value)
        {
            var bytes = Encoding.Latin1.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
