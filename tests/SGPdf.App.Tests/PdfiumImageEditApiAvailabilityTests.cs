using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace SGPdf.App.Tests;

public sealed class PdfiumImageEditApiAvailabilityTests
{
    internal static readonly string[] CriticalExports =
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

    internal static readonly string[] ConditionalExports =
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
        var library = LoadPinnedPdfium();
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
        var library = LoadPinnedPdfium();
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

    internal static IntPtr LoadPinnedPdfium()
    {
        var direct = Path.Combine(AppContext.BaseDirectory, "pdfium.dll");
        if (File.Exists(direct) && NativeLibrary.TryLoad(direct, out var directLibrary))
            return directLibrary;

        var x64 = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            "win-x64",
            "native",
            "pdfium.dll");
        Assert.True(File.Exists(x64), $"No se encontró el pdfium.dll x64 pinneado: {x64}");
        Assert.True(NativeLibrary.TryLoad(x64, out var library), $"No se pudo cargar el pdfium.dll x64 pinneado: {x64}");
        return library;
    }
}
