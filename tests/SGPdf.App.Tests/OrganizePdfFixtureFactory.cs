using System.Text;
using PdfSharp.Pdf;

namespace SGPdf.App.Tests;

internal sealed class OrganizePdfFixture : IDisposable
{
    internal OrganizePdfFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, true);
    }
}

internal static class OrganizePdfFixtureFactory
{
    internal static OrganizePdfFixture CreatePlain()
    {
        return CreateRaw("plain.pdf", BuildPdf(new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 5 0 R >>\nendobj\n",
            "5 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n"
        }, "<< /Size 6 /Root 1 0 R >>"));
    }

    internal static OrganizePdfFixture CreateNavigation()
    {
        return CreateRaw("navigation.pdf", BuildPdf(new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Outlines 8 0 R /PageMode /UseOutlines >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R 6 0 R] /Count 2 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 5 0 R /Annots [10 0 R] >>\nendobj\n",
            "5 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
            "6 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 7 0 R >>\nendobj\n",
            "7 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
            "8 0 obj\n<< /Type /Outlines /First 9 0 R /Last 9 0 R /Count 1 >>\nendobj\n",
            "9 0 obj\n<< /Title (Root) /Parent 8 0 R /Dest [4 0 R /Fit] >>\nendobj\n",
            "10 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 30 120 60] /Border [0 0 0] /Dest [6 0 R /Fit] >>\nendobj\n"
        }, "<< /Size 11 /Root 1 0 R >>"));
    }

    internal static OrganizePdfFixture CreateMetadata()
    {
        return CreateRaw("metadata.pdf", BuildPdf(new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 5 0 R >>\nendobj\n",
            "5 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
            "6 0 obj\n<< /Title (SG PDF metadata fixture) /Author (SG PDF tests) >>\nendobj\n"
        }, "<< /Size 7 /Root 1 0 R /Info 6 0 R >>"));
    }

    internal static OrganizePdfFixture CreateAcroForm()
    {
        return CreateRaw("form.pdf", BuildPdf(new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 6 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 5 0 R /Annots [7 0 R] >>\nendobj\n",
            "5 0 obj\n<< /Length 0 >>\nstream\nendstream\nendobj\n",
            "6 0 obj\n<< /Fields [7 0 R] >>\nendobj\n",
            "7 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /Rect [20 20 120 45] /P 4 0 R >>\nendobj\n"
        }, "<< /Size 8 /Root 1 0 R >>"));
    }

    internal static OrganizePdfFixture CreateProtected()
    {
        var directory = NewDirectory();
        var path = System.IO.Path.Combine(directory, "protected.pdf");
        using var document = new PdfDocument();
        document.AddPage();
        document.SecuritySettings.UserPassword = "secret";
        document.SecuritySettings.OwnerPassword = "owner";
        document.Save(path);
        return new OrganizePdfFixture(directory, path);
    }

    private static OrganizePdfFixture CreateRaw(string fileName, byte[] bytes)
    {
        var directory = NewDirectory();
        var path = System.IO.Path.Combine(directory, fileName);
        File.WriteAllBytes(path, bytes);
        return new OrganizePdfFixture(directory, path);
    }

    private static string NewDirectory()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-organize-preflight-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static byte[] BuildPdf(IReadOnlyList<string> objects, string trailer)
    {
        using var stream = new MemoryStream();
        WriteLatin1(stream, "%PDF-1.7\n");
        var offsets = new long[objects.Count + 1];
        for (var index = 0; index < objects.Count; index++)
        {
            offsets[index + 1] = stream.Position;
            WriteLatin1(stream, objects[index]);
        }

        var xrefOffset = stream.Position;
        WriteLatin1(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteLatin1(stream, "0000000000 65535 f \n");
        for (var index = 1; index < offsets.Length; index++)
            WriteLatin1(stream, $"{offsets[index]:D10} 00000 n \n");
        WriteLatin1(stream, $"trailer\n{trailer}\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void WriteLatin1(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
