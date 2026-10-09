using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfPasswordTests
{
    [Fact]
    public void ProtectedPdf_OpenWithoutPassword_ThrowsTypedPasswordError()
    {
        using var fixture = ProtectedPdfFixture.Create();

        var error = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(fixture.Path));

        Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, error.Error);
        Assert.Equal(4u, error.PdfiumErrorCode);
    }

    [Fact]
    public void ProtectedPdf_OpenWrongPassword_ThrowsTypedPasswordError()
    {
        using var fixture = ProtectedPdfFixture.Create();

        var error = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(fixture.Path, "wrong-password"));

        Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, error.Error);
        Assert.Equal(4u, error.PdfiumErrorCode);
    }

    [Fact]
    public void ProtectedPdf_OpenCorrectPassword_SucceedsAndRenders()
    {
        using var fixture = ProtectedPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.Path, "secret");

        Assert.Equal(1, session.PageCount);
        var rendered = session.RenderPage(0, 72d);
        Assert.Equal(0, rendered.PageIndex);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
    }

    [Fact]
    public void InvalidPdf_StillClassifiesAsOtherPdfiumError()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = System.IO.Path.Combine(root, "invalid.pdf");
        File.WriteAllText(path, "not a pdf");
        try
        {
            var error = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(path));
            Assert.Equal(PdfDocumentOpenError.OtherPdfiumError, error.Error);
            Assert.NotEqual(4u, error.PdfiumErrorCode);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void PasswordException_DoesNotExposePasswordValue()
    {
        using var fixture = ProtectedPdfFixture.Create();
        const string password = "dont-leak-this-value";

        var error = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(fixture.Path, password));

        Assert.DoesNotContain(password, error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(password, error.ToString(), StringComparison.Ordinal);
    }

    private sealed class ProtectedPdfFixture : IDisposable
    {
        private ProtectedPdfFixture(string root, string path)
        {
            Root = root;
            Path = path;
        }

        internal string Root { get; }
        internal string Path { get; }

        internal static ProtectedPdfFixture Create()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-protected-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = System.IO.Path.Combine(root, "protected.pdf");
            using var document = new PdfDocument();
            document.AddPage();
            document.SecuritySettings.UserPassword = "secret";
            document.SecuritySettings.OwnerPassword = "owner";
            document.Save(path);
            return new ProtectedPdfFixture(root, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
