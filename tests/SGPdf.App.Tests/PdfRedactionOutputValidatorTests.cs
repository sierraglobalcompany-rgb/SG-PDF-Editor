using System.Text;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfRedactionOutputValidatorTests
{
    [Fact]
    public void Validate_OutputWithoutBlackRedactionPath_FailsClosed()
    {
        using var fixture = ValidatorFixture.Create();

        PdfTextObjectInfo target;
        using (var source = PdfDocumentSession.Open(fixture.SourcePath))
            target = Assert.Single(source.GetTextObjects(0));

        var workspace = RedactionWorkspace.Create(fixture.SourcePath);
        workspace.Stage(new RedactionPlan(0, new[] { new RedactionTarget(target) }));

        var error = Record.Exception(() =>
            new PdfRedactionOutputValidator().Validate(fixture.OutputPath, workspace));

        var invalid = Assert.IsType<InvalidOperationException>(error);
        Assert.Contains("marca", invalid.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ValidatorFixture : IDisposable
    {
        private ValidatorFixture(string directoryPath, string sourcePath, string outputPath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
            OutputPath = outputPath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }
        internal string OutputPath { get; }

        internal static ValidatorFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f12-validator-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            var output = Path.Combine(directory, "output-without-mark.pdf");
            Write(source, "BT /F1 14 Tf 100 500 Td (CASA) Tj ET", includeFont: true);
            Write(output, string.Empty, includeFont: false);
            return new ValidatorFixture(directory, source, output);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }

        private static void Write(string path, string content, bool includeFont)
        {
            var stream = $"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream";
            var page = includeFont
                ? "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>"
                : "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> /Contents 4 0 R >>";
            var objects = new Dictionary<int, string>
            {
                [1] = "<< /Type /Catalog /Pages 2 0 R >>",
                [2] = "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                [3] = page,
                [4] = stream
            };
            if (includeFont)
                objects[5] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

            var maxObject = includeFont ? 5 : 4;
            var builder = new StringBuilder("%PDF-1.7\n%SGPDF-F12-VALIDATOR\n");
            var offsets = new long[maxObject + 1];
            for (var number = 1; number <= maxObject; number++)
            {
                offsets[number] = Encoding.Latin1.GetByteCount(builder.ToString());
                builder.Append(number).Append(" 0 obj\n").Append(objects[number]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.Latin1.GetByteCount(builder.ToString());
            builder.Append("xref\n0 ").Append(maxObject + 1).Append("\n0000000000 65535 f \n");
            for (var number = 1; number <= maxObject; number++)
                builder.Append(offsets[number].ToString("D10")).Append(" 00000 n \n");
            builder.Append("trailer\n<< /Size ").Append(maxObject + 1).Append(" /Root 1 0 R >>\n")
                .Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");
            File.WriteAllBytes(path, Encoding.Latin1.GetBytes(builder.ToString()));
        }
    }
}
