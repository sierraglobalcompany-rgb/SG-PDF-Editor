using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfFileLockDiagnosticsTests
{
    [Fact]
    public void PdfSharpOnly_Close_AllowsImmediateDelete()
    {
        var fixture = CreatePdf();
        Directory.Delete(fixture.Directory, true);
        Assert.False(Directory.Exists(fixture.Directory));
    }

    [Fact]
    public void PdfiumSession_Dispose_AllowsImmediateDelete()
    {
        var fixture = CreatePdf();
        using (var session = PdfDocumentSession.Open(fixture.Path))
        {
            Assert.Equal(1, session.PageCount);
        }

        Directory.Delete(fixture.Directory, true);
        Assert.False(Directory.Exists(fixture.Directory));
    }

    [Fact]
    public void PdfiumRender_Dispose_AllowsImmediateDelete()
    {
        var fixture = CreatePdf();
        using (var session = PdfDocumentSession.Open(fixture.Path))
        {
            var sizes = session.GetPageSizes();
            Assert.Single(sizes);
            var rendered = session.RenderPage(0, 96d);
            Assert.True(rendered.PixelWidth > 0);
        }

        Directory.Delete(fixture.Directory, true);
        Assert.False(Directory.Exists(fixture.Directory));
    }

    private static (string Directory, string Path) CreatePdf()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-lock-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "source.pdf");
        var document = new PdfDocument();
        document.AddPage();
        document.Save(path);
        document.Close();
        return (directory, path);
    }
}
