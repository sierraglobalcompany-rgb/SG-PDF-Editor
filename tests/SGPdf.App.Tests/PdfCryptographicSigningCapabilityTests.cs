using System.Runtime.InteropServices;
using System.Text.Json;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCryptographicSigningCapabilityTests
{
    [Fact]
    public void CapabilityGate_PinnedRuntimeSupportsPdfSharpPkcsAndPdfiumSignatureInspection()
    {
        string? pdfiumPath = Directory
            .EnumerateFiles(AppContext.BaseDirectory, "pdfium.dll", SearchOption.AllDirectories)
            .FirstOrDefault(path => path.Contains("win-x64", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(pdfiumPath), "Pinned x64 PDFium runtime was not found in the test output.");

        IntPtr library = NativeLibrary.Load(pdfiumPath!);
        try
        {
            AssertExport(library, "FPDF_GetSignatureObject");
            AssertExport(library, "FPDFSignatureObj_GetContents");
            AssertExport(library, "FPDFSignatureObj_GetByteRange");
            AssertExport(library, "FPDFSignatureObj_GetSubFilter");
        }
        finally
        {
            NativeLibrary.Free(library);
        }

        Assert.NotNull(Type.GetType("PdfSharp.Pdf.Signatures.DigitalSignatureHandler, PdfSharp"));
        Assert.NotNull(Type.GetType("PdfSharp.Pdf.Signatures.IDigitalSigner, PdfSharp.System"));

        Type? signedCmsType = Type.GetType("System.Security.Cryptography.Pkcs.SignedCms, System.Security.Cryptography.Pkcs");
        Assert.NotNull(signedCmsType);
        Assert.Equal(10, signedCmsType!.Assembly.GetName().Version?.Major);

        string repositoryRoot = FindRepositoryRoot();
        string projectText = File.ReadAllText(Path.Combine(repositoryRoot, "src", "SGPdf.App", "SGPdf.App.csproj"));
        Assert.Contains(
            "<PackageReference Include=\"System.Security.Cryptography.Pkcs\" Version=\"10.0.12\" />",
            projectText,
            StringComparison.Ordinal);

        using JsonDocument lockFile = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(repositoryRoot, "src", "SGPdf.App", "packages.lock.json")));
        JsonElement target = lockFile.RootElement.GetProperty("dependencies").EnumerateObject().Single().Value;
        JsonElement pkcs = target.GetProperty("System.Security.Cryptography.Pkcs");
        Assert.Equal("Direct", pkcs.GetProperty("type").GetString());
        Assert.Equal("[10.0.12, )", pkcs.GetProperty("requested").GetString());
        Assert.Equal("10.0.12", pkcs.GetProperty("resolved").GetString());
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SGPdf.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing SGPdf.slnx was not found.");
    }

    private static void AssertExport(IntPtr library, string exportName)
    {
        Assert.True(
            NativeLibrary.TryGetExport(library, exportName, out _),
            $"Pinned PDFium runtime does not export '{exportName}'.");
    }
}
