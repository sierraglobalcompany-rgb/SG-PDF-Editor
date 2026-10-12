using System.Text;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterRedactionTaggedTests
{
    private const string SemanticSecret = "SECRET_SEMANTIC_F12";

    [Fact]
    public void SaveAsCopy_RedactionOfTaggedActualText_RemovesSemanticSecret()
    {
        using var fixture = TaggedActualTextFixture.Create();
        var destination = Path.Combine(fixture.DirectoryPath, "redacted-tagged.pdf");

        PdfTextObjectInfo target;
        using (var source = PdfDocumentSession.Open(fixture.Path))
        {
            Assert.True(source.IsOrganizeTagged());
            var preflight = new PdfEditPreflightInspector((_, _) => 0).Inspect(source);
            Assert.Contains(preflight.Findings, finding => finding.Kind == PdfEditFindingKind.TaggedStructure);
            target = Assert.Single(source.GetTextObjects(0));
            Assert.Equal("CASA", target.Text);
        }

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(0, new[] { new RedactionTarget(target) }));
        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);

        new PdfEditWriter().SaveAsCopy(
            images,
            text,
            redaction,
            destination,
            warningsConfirmed: true);

        Assert.True(File.Exists(destination));
        using (var saved = PdfDocumentSession.Open(destination))
            Assert.Empty(saved.GetTextObjects(0));

        var raw = Encoding.Latin1.GetString(File.ReadAllBytes(destination));
        Assert.DoesNotContain(SemanticSecret, raw, StringComparison.Ordinal);
    }

    private sealed class TaggedActualTextFixture : IDisposable
    {
        private TaggedActualTextFixture(string directoryPath, string path)
        {
            DirectoryPath = directoryPath;
            Path = path;
        }

        internal string DirectoryPath { get; }
        internal string Path { get; }

        internal static TaggedActualTextFixture Create()
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"sgpdf-f12-tagged-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, "tagged.pdf");
            Write(path);
            return new TaggedActualTextFixture(directory, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }

        private static void Write(string path)
        {
            const string content = "/P << /MCID 0 >> BDC BT /F1 14 Tf 100 500 Td (CASA) Tj ET EMC";
            var stream = $"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream";
            var objects = new Dictionary<int, string>
            {
                [1] = "<< /Type /Catalog /Pages 2 0 R /StructTreeRoot 6 0 R /MarkInfo << /Marked true >> >>",
                [2] = "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                [3] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /StructParents 0 /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
                [4] = stream,
                [5] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                [6] = "<< /Type /StructTreeRoot /K [7 0 R] /ParentTree 8 0 R /ParentTreeNextKey 1 >>",
                [7] = $"<< /Type /StructElem /S /P /P 6 0 R /Pg 3 0 R /K 0 /ActualText ({SemanticSecret}) >>",
                [8] = "<< /Nums [0 [7 0 R]] >>"
            };

            var builder = new StringBuilder("%PDF-1.7\n%SGPDF-F12-TAGGED\n");
            var offsets = new long[9];
            for (var number = 1; number <= 8; number++)
            {
                offsets[number] = Encoding.Latin1.GetByteCount(builder.ToString());
                builder.Append(number).Append(" 0 obj\n")
                    .Append(objects[number]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.Latin1.GetByteCount(builder.ToString());
            builder.Append("xref\n0 9\n")
                .Append("0000000000 65535 f \n");
            for (var number = 1; number <= 8; number++)
                builder.Append(offsets[number].ToString("D10")).Append(" 00000 n \n");

            builder.Append("trailer\n<< /Size 9 /Root 1 0 R >>\n")
                .Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");
            File.WriteAllBytes(path, Encoding.Latin1.GetBytes(builder.ToString()));
        }
    }
}
