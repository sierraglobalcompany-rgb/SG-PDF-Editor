using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfTextTests
{
    [Fact]
    public void UnicodeText_ExtractsAccentsAndEnye()
    {
        using var fixture = TextPdfFixture.Create("Árbol niño pingüino");
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var text = session.GetTextRange(0, 0, "Árbol niño pingüino".Length);

        Assert.Equal("Árbol niño pingüino", text);
    }

    [Fact]
    public void FindTextOnPage_IsCaseInsensitiveAndNonWholeWordByDefault()
    {
        using var fixture = TextPdfFixture.Create("alpha y ALPHABET parcial");
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var matches = session.FindTextOnPage(0, "ALPHA");

        Assert.Equal(2, matches.Count);
        Assert.All(matches, match => Assert.Equal(5, match.CharacterCount));
    }

    [Fact]
    public void FindTextOnPage_ReturnsStartCountAndRects()
    {
        using var fixture = TextPdfFixture.Create("uno needle dos needle tres");
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var matches = session.FindTextOnPage(0, "needle");

        Assert.Equal(2, matches.Count);
        Assert.Equal(4, matches[0].StartIndex);
        Assert.Equal(6, matches[0].CharacterCount);
        Assert.Equal(15, matches[1].StartIndex);
        Assert.Equal(6, matches[1].CharacterCount);
        Assert.All(matches, match =>
        {
            Assert.NotEmpty(match.Rects);
            Assert.All(match.Rects, rect =>
            {
                Assert.True(double.IsFinite(rect.Left));
                Assert.True(double.IsFinite(rect.Bottom));
                Assert.True(double.IsFinite(rect.Right));
                Assert.True(double.IsFinite(rect.Top));
                Assert.True(rect.Right > rect.Left);
                Assert.True(rect.Top > rect.Bottom);
            });
        });
    }

    [Fact]
    public void ImageOnlyPage_ReturnsNoTextMatches()
    {
        using var fixture = TextPdfFixture.Create((string?)null);
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        Assert.Empty(session.FindTextOnPage(0, "anything"));
    }

    [Fact]
    public void GetTextRange_ReturnsUnicodeWithoutTrailingGarbage()
    {
        const string expected = "mañana útil";
        using var fixture = TextPdfFixture.Create(expected);
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var text = session.GetTextRange(0, 0, expected.Length);

        Assert.Equal(expected, text);
        Assert.DoesNotContain('\0', text);
    }

    [Fact]
    public void GetCharacterIndexAtPoint_OutsideTextReturnsMinusOne()
    {
        using var fixture = TextPdfFixture.Create("texto visible");
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        var index = session.GetCharacterIndexAtPoint(0, 10d, 10d, 0.5d, 0.5d);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void TextOperations_PreCanceledToken_AbortBeforePublication()
    {
        using var fixture = TextPdfFixture.Create("cancelación");
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            session.FindTextOnPage(0, "can", cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            session.GetTextRange(0, 0, 3, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            session.GetTextRangeRects(0, 0, 3, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            session.GetCharacterIndexAtPoint(0, 72d, 720d, 1d, 1d, cancellation.Token));
    }

    private sealed class TextPdfFixture : IDisposable
    {
        private TextPdfFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static TextPdfFixture Create(params string?[] pageTexts)
        {
            if (pageTexts.Length == 0)
                throw new ArgumentException("At least one page is required.", nameof(pageTexts));

            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-text-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            File.WriteAllBytes(source, BuildPdf(pageTexts));
            return new TextPdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }

        private static byte[] BuildPdf(IReadOnlyList<string?> pageTexts)
        {
            var objects = new List<byte[]>();
            objects.Add(Latin1("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"));

            var pageObjectIds = Enumerable.Range(0, pageTexts.Count)
                .Select(index => 4 + (index * 2))
                .ToArray();
            var kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
            objects.Add(Latin1($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageTexts.Count} >>\nendobj\n"));
            objects.Add(Latin1("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n"));

            for (var index = 0; index < pageTexts.Count; index++)
            {
                var pageId = pageObjectIds[index];
                var contentId = pageId + 1;
                var text = pageTexts[index];
                var content = text is null
                    ? Array.Empty<byte>()
                    : Latin1($"BT\n/F1 18 Tf\n72 720 Td\n({EscapePdfString(text)}) Tj\nET\n");

                objects.Add(Latin1(
                    $"{pageId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>\nendobj\n"));

                using var contentObject = new MemoryStream();
                WriteLatin1(contentObject, $"{contentId} 0 obj\n<< /Length {content.Length} >>\nstream\n");
                contentObject.Write(content, 0, content.Length);
                WriteLatin1(contentObject, "endstream\nendobj\n");
                objects.Add(contentObject.ToArray());
            }

            using var stream = new MemoryStream();
            WriteLatin1(stream, "%PDF-1.4\n");
            var offsets = new List<long>();
            foreach (var obj in objects)
            {
                offsets.Add(stream.Position);
                stream.Write(obj, 0, obj.Length);
            }

            var xrefOffset = stream.Position;
            WriteLatin1(stream, $"xref\n0 {objects.Count + 1}\n");
            WriteLatin1(stream, "0000000000 65535 f \n");
            foreach (var offset in offsets)
                WriteLatin1(stream, $"{offset:D10} 00000 n \n");
            WriteLatin1(stream,
                $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
            return stream.ToArray();
        }

        private static string EscapePdfString(string value)
            => value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);

        private static byte[] Latin1(string value) => Encoding.Latin1.GetBytes(value);

        private static void WriteLatin1(Stream stream, string value)
        {
            var bytes = Latin1(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
