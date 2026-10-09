using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfNavigationTests
{
    [Fact]
    public void GetBookmarks_ReturnsHierarchyTitleDestinationAndUnsupportedNode()
    {
        using var fixture = NavigationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var bookmarks = session.GetBookmarks();

        Assert.Equal(2, bookmarks.Count);
        Assert.Equal("Root", bookmarks[0].Title);
        Assert.Equal(0, bookmarks[0].DestinationPageIndex);
        var child = Assert.Single(bookmarks[0].Children);
        Assert.Equal("Child", child.Title);
        Assert.Equal(1, child.DestinationPageIndex);
        Assert.Equal("Unsupported", bookmarks[1].Title);
        Assert.Null(bookmarks[1].DestinationPageIndex);
    }

    [Fact]
    public void GetPageLinks_ReturnsInternalDestinationAndRectangle()
    {
        using var fixture = NavigationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var links = session.GetPageLinks(0);
        var link = Assert.Single(links, item => item.ActionKind == PdfLinkActionKind.InternalGoto);

        Assert.Equal(0, link.PageIndex);
        Assert.Equal(1, link.DestinationPageIndex);
        Assert.Null(link.Uri);
        Assert.Equal(20d, link.Rect.Left, 3);
        Assert.Equal(30d, link.Rect.Bottom, 3);
        Assert.Equal(120d, link.Rect.Right, 3);
        Assert.Equal(60d, link.Rect.Top, 3);
    }

    [Fact]
    public void GetPageLinks_ExtractsUriWithoutLaunchingAnything()
    {
        using var fixture = NavigationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var links = session.GetPageLinks(0);
        var link = Assert.Single(links, item => item.ActionKind == PdfLinkActionKind.Uri);

        Assert.Equal("https://example.invalid/path", link.Uri);
        Assert.Null(link.DestinationPageIndex);
    }

    [Fact]
    public void GetPageLinks_ClassifiesLaunchAndJavaScriptAsUnsupported()
    {
        using var fixture = NavigationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var unsupported = session.GetPageLinks(0)
            .Where(item => item.ActionKind == PdfLinkActionKind.Unsupported)
            .ToArray();

        Assert.Equal(2, unsupported.Length);
        Assert.All(unsupported, item =>
        {
            Assert.Null(item.DestinationPageIndex);
            Assert.Null(item.Uri);
        });
    }

    [Fact]
    public void NavigationOperations_PreCanceledTokenAbort()
    {
        using var fixture = NavigationPdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => session.GetBookmarks(cancellation.Token));
        Assert.Throws<OperationCanceledException>(() => session.GetPageLinks(0, cancellation.Token));
    }

    private sealed class NavigationPdfFixture : IDisposable
    {
        private NavigationPdfFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static NavigationPdfFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-navigation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "navigation.pdf");
            File.WriteAllBytes(source, BuildPdf());
            return new NavigationPdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }

        private static byte[] BuildPdf()
        {
            var objects = new[]
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Outlines 8 0 R /PageMode /UseOutlines >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Kids [4 0 R 6 0 R] /Count 2 >>\nendobj\n",
                "3 0 obj\n<< >>\nendobj\n",
                "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 5 0 R /Annots [12 0 R 13 0 R 14 0 R 15 0 R] >>\nendobj\n",
                "5 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
                "6 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 7 0 R >>\nendobj\n",
                "7 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
                "8 0 obj\n<< /Type /Outlines /First 9 0 R /Last 11 0 R /Count 3 >>\nendobj\n",
                "9 0 obj\n<< /Title (Root) /Parent 8 0 R /First 10 0 R /Last 10 0 R /Count 1 /Next 11 0 R /Dest [4 0 R /Fit] >>\nendobj\n",
                "10 0 obj\n<< /Title (Child) /Parent 9 0 R /Dest [6 0 R /Fit] >>\nendobj\n",
                "11 0 obj\n<< /Title (Unsupported) /Parent 8 0 R /Prev 9 0 R /A << /S /Launch /F (local.txt) >> >>\nendobj\n",
                "12 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 30 120 60] /Border [0 0 0] /Dest [6 0 R /Fit] >>\nendobj\n",
                "13 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 80 180 105] /Border [0 0 0] /A << /S /URI /URI (https://example.invalid/path) >> >>\nendobj\n",
                "14 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 120 160 145] /Border [0 0 0] /A << /S /Launch /F (local.txt) >> >>\nendobj\n",
                "15 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 160 160 185] /Border [0 0 0] /A << /S /JavaScript /JS (app.alert\\(test\\)) >> >>\nendobj\n"
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
