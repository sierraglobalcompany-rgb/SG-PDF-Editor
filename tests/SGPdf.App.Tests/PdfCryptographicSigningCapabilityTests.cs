using System.Runtime.InteropServices;
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
    }

    private static void AssertExport(IntPtr library, string exportName)
    {
        Assert.True(
            NativeLibrary.TryGetExport(library, exportName, out _),
            $"Pinned PDFium runtime does not export '{exportName}'.");
    }
}
