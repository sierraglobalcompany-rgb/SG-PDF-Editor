using System.Reflection;
using System.Text;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfOcrPreservationTests
{
    private const string WriterTypeName = "SGPdf.App.Pdf.PdfOcrWriter";
    private static readonly byte[] AttachmentPayload = Encoding.ASCII.GetBytes("ATTACHMENT-PAYLOAD-F10");

    [Fact]
    public void SaveAsCopy_HybridCorpusPreservesDocumentStructuresAndSkippedText()
    {
        using var fixture = HybridCorpusFixture.Create();
        var sourceBefore = File.ReadAllBytes(fixture.SourcePath);
        var destination = fixture.Output("ocr-preserved.pdf");

        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        Assert.Equal(2, source.PageCount);
        var sourceScanRender = source.RenderPage(0, 72d);
        var skippedTextBefore = source.GetPageText(1);
        Assert.Contains("SEARCHABLE-SKIP", skippedTextBefore, StringComparison.Ordinal);

        var result = Save(
            CreateProductionWriter(),
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            Placements(0, "OCR-F10"),
            destination,
            CancellationToken.None);

        Assert.True(Succeeded(result), Error(result));
        Assert.Equal(sourceBefore, File.ReadAllBytes(fixture.SourcePath));
        Assert.True(File.Exists(destination));

        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(2, output.PageCount);
        Assert.Contains("OCR-F10", output.GetPageText(0), StringComparison.Ordinal);
        Assert.Equal(skippedTextBefore, output.GetPageText(1));

        var outputScanRender = output.RenderPage(0, 72d);
        Assert.Equal(sourceScanRender.PixelWidth, outputScanRender.PixelWidth);
        Assert.Equal(sourceScanRender.PixelHeight, outputScanRender.PixelHeight);
        Assert.Equal(sourceScanRender.Stride, outputScanRender.Stride);
        Assert.Equal(sourceScanRender.Pixels, outputScanRender.Pixels);

        var savedBytes = File.ReadAllBytes(destination);
        var savedText = Encoding.Latin1.GetString(savedBytes);

        Assert.Contains("/AcroForm", savedText, StringComparison.Ordinal);
        Assert.Contains("/FT /Tx", savedText, StringComparison.Ordinal);
        Assert.Contains("/Outlines", savedText, StringComparison.Ordinal);
        Assert.Contains("F10 Bookmark", savedText, StringComparison.Ordinal);
        Assert.Contains("/Dests", savedText, StringComparison.Ordinal);
        Assert.Contains("F10Dest", savedText, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Link", savedText, StringComparison.Ordinal);
        Assert.Contains("/StructTreeRoot", savedText, StringComparison.Ordinal);
        Assert.Contains("/MarkInfo", savedText, StringComparison.Ordinal);
        Assert.Contains("/PageLabels", savedText, StringComparison.Ordinal);
        Assert.Contains("/EmbeddedFiles", savedText, StringComparison.Ordinal);
        Assert.Contains("fixture.bin", savedText, StringComparison.Ordinal);
        Assert.True(ContainsSequence(savedBytes, AttachmentPayload), "Embedded attachment payload changed or disappeared.");
        Assert.Contains("/Title (F10 Preservation Corpus)", savedText, StringComparison.Ordinal);
        Assert.Contains("/Author (SG PDF)", savedText, StringComparison.Ordinal);
        Assert.Contains("/Rotate 90", savedText, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Text", savedText, StringComparison.Ordinal);
        Assert.Contains("SUPPORTED-ANNOTATION-F10", savedText, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Polygon", savedText, StringComparison.Ordinal);
        Assert.Contains("UNSUPPORTED-ANNOTATION-F10", savedText, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Image", savedText, StringComparison.Ordinal);
    }

    private static object CreateProductionWriter()
    {
        var type = RequireType(WriterTypeName);
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        Assert.NotNull(constructor);
        return constructor!.Invoke(null);
    }

    private static OcrTextLinePlacement[] Placements(int pageIndex, string text)
    {
        var bounds = new PdfOcrRect(50d, 80d, 145d, 105d);
        var word = new OcrTextWordPlacement(text, 95d, bounds, 0, 1);
        return
        [
            new OcrTextLinePlacement(
                pageIndex,
                text,
                bounds,
                [word],
                0,
                1,
                1,
                1)
        ];
    }

    private static object Save(
        object writer,
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        IReadOnlyList<OcrTextLinePlacement> placements,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var method = writer.GetType().GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(
            writer,
            [sourcePath, fingerprint, placements, destinationPath, cancellationToken]);
        Assert.NotNull(result);
        return result!;
    }

    private static bool Succeeded(object result)
    {
        var property = result.GetType().GetProperty("Succeeded");
        Assert.NotNull(property);
        return Assert.IsType<bool>(property!.GetValue(result));
    }

    private static string? Error(object result)
    {
        var property = result.GetType().GetProperty("Error");
        Assert.NotNull(property);
        return property!.GetValue(result) as string;
    }

    private static Type RequireType(string typeName)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(typeName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static bool ContainsSequence(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || needle.Length > haystack.Length)
            return false;

        for (var start = 0; start <= haystack.Length - needle.Length; start++)
        {
            var matches = true;
            for (var index = 0; index < needle.Length; index++)
            {
                if (haystack[start + index] == needle[index])
                    continue;
                matches = false;
                break;
            }

            if (matches)
                return true;
        }

        return false;
    }

    private sealed class HybridCorpusFixture : IDisposable
    {
        private HybridCorpusFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static HybridCorpusFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f10-preservation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "hybrid-source.pdf");
            WriteHybridPdf(source);
            return new HybridCorpusFixture(directory, source);
        }

        internal string Output(string fileName) => Path.Combine(DirectoryPath, fileName);

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }

        private static void WriteHybridPdf(string path)
        {
            const string scanContent = "q 120 0 0 120 60 80 cm /Im0 Do Q";
            const string searchableContent = "BT /F1 18 Tf 60 300 Td (SEARCHABLE-SKIP) Tj ET";
            const string imageHex = "FF000000FF000000FFFFFFFF>";
            var attachment = Encoding.ASCII.GetString(AttachmentPayload);

            var objects = new Dictionary<int, string>
            {
                [1] = "<< /Type /Catalog /Pages 2 0 R /AcroForm 7 0 R /Outlines 9 0 R /Names << /Dests 12 0 R /EmbeddedFiles 13 0 R >> /StructTreeRoot 15 0 R /PageLabels 16 0 R /MarkInfo << /Marked true >> >>",
                [2] = "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                [3] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 500] /Rotate 90 /Resources << /XObject << /Im0 23 0 R >> >> /Contents 5 0 R /Annots [8 0 R 17 0 R 18 0 R 21 0 R] /StructParents 0 >>",
                [4] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 500] /Resources << /Font << /F1 11 0 R >> >> /Contents 6 0 R >>",
                [5] = Stream(scanContent),
                [6] = Stream(searchableContent),
                [7] = "<< /Fields [8 0 R] /NeedAppearances false /DA (/Helv 0 Tf 0 g) /DR << /Font << /Helv 11 0 R >> >> >>",
                [8] = "<< /Type /Annot /Subtype /Widget /FT /Tx /T (F10Field) /V (KeepMe) /Rect [30 30 120 55] /P 3 0 R >>",
                [9] = "<< /Type /Outlines /First 10 0 R /Last 10 0 R /Count 1 >>",
                [10] = "<< /Title (F10 Bookmark) /Parent 9 0 R /Dest [3 0 R /Fit] >>",
                [11] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                [12] = "<< /Names [(F10Dest) [4 0 R /Fit]] >>",
                [13] = "<< /Names [(fixture.bin) 14 0 R] >>",
                [14] = "<< /Type /Filespec /F (fixture.bin) /UF (fixture.bin) /EF << /F 19 0 R >> >>",
                [15] = "<< /Type /StructTreeRoot /K [] /ParentTree 22 0 R >>",
                [16] = "<< /Nums [0 << /S /r >> 1 << /S /D /St 5 >>] >>",
                [17] = "<< /Type /Annot /Subtype /Text /Rect [150 60 170 80] /Contents (SUPPORTED-ANNOTATION-F10) >>",
                [18] = "<< /Type /Annot /Subtype /Polygon /Rect [190 180 290 250] /Vertices [200 200 240 240 280 200] /Contents (UNSUPPORTED-ANNOTATION-F10) >>",
                [19] = $"<< /Type /EmbeddedFile /Length {AttachmentPayload.Length} >>\nstream\n{attachment}\nendstream",
                [20] = "<< /Title (F10 Preservation Corpus) /Author (SG PDF) >>",
                [21] = "<< /Type /Annot /Subtype /Link /Rect [40 400 180 430] /Border [0 0 0] /Dest [4 0 R /Fit] >>",
                [22] = "<< /Nums [] >>",
                [23] = $"<< /Type /XObject /Subtype /Image /Width 2 /Height 2 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /ASCIIHexDecode /Length {imageHex.Length} >>\nstream\n{imageHex}\nendstream"
            };

            var builder = new StringBuilder("%PDF-1.7\n%SGPDF-F10-PRESERVATION\n");
            var offsets = new long[24];
            for (var number = 1; number <= 23; number++)
            {
                offsets[number] = Encoding.ASCII.GetByteCount(builder.ToString());
                builder.Append(number).Append(" 0 obj\n")
                    .Append(objects[number]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
            builder.Append("xref\n0 24\n")
                .Append("0000000000 65535 f \n");
            for (var number = 1; number <= 23; number++)
                builder.Append(offsets[number].ToString("D10")).Append(" 00000 n \n");

            builder.Append("trailer\n<< /Size 24 /Root 1 0 R /Info 20 0 R >>\n")
                .Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

            File.WriteAllBytes(path, Encoding.ASCII.GetBytes(builder.ToString()));
        }

        private static string Stream(string data) =>
            $"<< /Length {Encoding.ASCII.GetByteCount(data)} >>\nstream\n{data}\nendstream";
    }
}
