using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace SGPdf.App.Tests;

public sealed class PdfiumTextEditApiAvailabilityTests
{
    internal static readonly string[] CriticalExports =
    {
        "FPDFPage_CountObjects",
        "FPDFPage_GetObject",
        "FPDFPageObj_GetType",
        "FPDFPageObj_GetBounds",
        "FPDFPageObj_GetMatrix",
        "FPDFPageObj_GetRotatedBounds",
        "FPDFTextObj_GetText",
        "FPDFTextObj_GetFontSize",
        "FPDFTextObj_GetFont",
        "FPDFFont_GetBaseFontName",
        "FPDFTextObj_GetTextRenderMode",
        "FPDFPageObj_GetFillColor",
        "FPDFText_SetText",
        "FPDFText_LoadFont",
        "FPDFFont_Close",
        "FPDFPageObj_CreateTextObj",
        "FPDFPageObj_SetMatrix",
        "FPDFPageObj_SetFillColor",
        "FPDFPage_RemoveObject",
        "FPDFPage_InsertObjectAtIndex",
        "FPDFPage_GenerateContent",
        "FPDF_SaveAsCopy"
    };

    internal static readonly string[] OptionalExports =
    {
        "FPDFTextObj_SetFontSize",
        "FPDFFont_GetFamilyName"
    };

    private readonly ITestOutputHelper _output;

    public PdfiumTextEditApiAvailabilityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PinnedPdfium_ExportsAllF7CriticalTextEditFunctions()
    {
        var library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();
        try
        {
            var missing = CriticalExports
                .Where(name => !NativeLibrary.TryGetExport(library, name, out _))
                .ToArray();

            Assert.True(
                missing.Length == 0,
                $"El pdfium.dll pinneado no exporta APIs core requeridas por F7: {string.Join(", ", missing)}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [Fact]
    public void PinnedPdfium_ReportsF7OptionalFontCapabilities()
    {
        var library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();
        try
        {
            foreach (var export in OptionalExports)
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