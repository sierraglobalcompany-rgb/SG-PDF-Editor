using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace SGPdf.App.Tests;

public sealed class PdfiumImageEditApiAvailabilityTests
{
    private static readonly string[] CriticalExports =
    {
        "FPDFPage_CountObjects",
        "FPDFPage_GetObject",
        "FPDFPageObj_GetType",
        "FPDFPageObj_GetBounds",
        "FPDFPageObj_GetMatrix",
        "FPDFImageObj_GetImageMetadata",
        "FPDFImageObj_GetBitmap",
        "FPDFBitmap_GetWidth",
        "FPDFBitmap_GetHeight",
        "FPDFBitmap_GetFormat",
        "FPDFPageObj_SetMatrix",
        "FPDFPage_RemoveObject",
        "FPDFPage_GenerateContent",
        "FPDF_SaveAsCopy"
    };

    private static readonly string[] ConditionalExports =
    {
        "FPDFPageObj_SetFillColor",
        "FPDFPage_InsertObjectAtIndex",
        "FPDFPageObj_GetRotatedBounds",
        "FPDFImageObj_GetRenderedBitmap"
    };

    private readonly ITestOutputHelper _output;

    public PdfiumImageEditApiAvailabilityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PinnedPdfium_ExportsAllF6CoreImageEditFunctions()
    {
        Assert.True(NativeLibrary.TryLoad("pdfium", out var library), "No se pudo cargar el pdfium.dll pinneado por el proyecto.");

        try
        {
            var missing = CriticalExports
                .Where(name => !NativeLibrary.TryGetExport(library, name, out _))
                .ToArray();

            Assert.True(
                missing.Length == 0,
                $"El pdfium.dll pinneado no exporta APIs core requeridas por F6: {string.Join(", ", missing)}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [Fact]
    public void PinnedPdfium_ReportsConditionalF6ImageEditFunctions()
    {
        Assert.True(NativeLibrary.TryLoad("pdfium", out var library), "No se pudo cargar el pdfium.dll pinneado por el proyecto.");

        try
        {
            foreach (var export in ConditionalExports)
            {
                var available = NativeLibrary.TryGetExport(library, export, out _);
                _output.WriteLine($"{export}: {(available ? "available" : "missing")}");
            }
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }
}
