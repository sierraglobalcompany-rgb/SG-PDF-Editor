using System.Text;

namespace SGPdf.App.Tests;

internal static class PdfEditWriterTextFixtureFactory
{
    internal static TextEditPdfFixture CreateImageThenText()
    {
        const string pageContent =
            "q\n80 0 0 50 40 80 cm\n/Im1 Do\nQ\n" +
            "BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 72 300 Tm\n(CASA 123) Tj\nET\n";
        const string imageData = "FF000000FF000000FFFFFFFF>\n";

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> /XObject << /Im1 6 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            Stream(pageContent),
            Stream(
                imageData,
                "/Type /XObject /Subtype /Image /Width 2 /Height 2 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /ASCIIHexDecode")
        };

        var directory = Path.Combine(
            Path.GetTempPath(),
            $"sgpdf-f7-writer-mixed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "source.pdf");
        Write(path, objects);
        return new TextEditPdfFixture(directory, path);
    }

    private static string Stream(string content, string dictionaryPrefix = "")
    {
        var prefix = string.IsNullOrWhiteSpace(dictionaryPrefix) ? string.Empty : dictionaryPrefix + " ";
        return $"<< {prefix}/Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream";
    }

    private static void Write(string path, IReadOnlyList<string> objects)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
        {
            NewLine = "\n"
        };

        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
            writer.Flush();
        }

        var xrefOffset = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();
    }
}
