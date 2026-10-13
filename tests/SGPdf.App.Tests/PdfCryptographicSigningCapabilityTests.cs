using System.Runtime.InteropServices;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCryptographicSigningCapabilityTests
{
    [Fact]
    public void CapabilityGate_PinnedRuntimeSupportsPdfSharpPkcsAndPdfiumSignatureInspection()
    {
        string pdfiumPath = Path.Combine(AppContext.BaseDirectory, "pdfium.dll");
        Assert.True(File.Exists(pdfiumPath), $"Pinned PDFium runtime not found at '{pdfiumPath}'.");

        IntPtr library = NativeLibrary.Load(pdfiumPath);
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
        Assert.NotNull(Type.GetType("PdfSharp.Pdf.Signatures.IDigitalSigner, PdfSharp"));
        Assert.NotNull(Type.GetType("System.Security.Cryptography.Pkcs.SignedCms, System.Security.Cryptography.Pkcs"));
    }

    private static void AssertExport(IntPtr library, string exportName)
    {
        Assert.True(
            NativeLibrary.TryGetExport(library, exportName, out _),
            $"Pinned PDFium runtime does not export '{exportName}'.");
    }
}
