using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace SGPdf.App.Tests;

public sealed class PdfiumOrganizeApiAvailabilityTests
{
    private static readonly string[] CriticalExports =
    {
        "FPDF_CreateNewDocument",
        "FPDF_ImportPagesByIndex",
        "FPDFPage_GetRotation",
        "FPDFPage_SetRotation",
        "FPDF_SaveAsCopy"
    };

    private static readonly string[] OptionalDetectorExports =
    {
        "FPDF_GetFormType",
        "FPDF_CountNamedDests",
        "FPDFCatalog_IsTagged",
        "FPDFDoc_GetAttachmentCount",
        "FPDF_GetPageLabel"
    };

    private readonly ITestOutputHelper _output;

    public PdfiumOrganizeApiAvailabilityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PinnedPdfium_ExportsAllF5CriticalWriterFunctions()
    {
        Assert.True(NativeLibrary.TryLoad("pdfium", out var library), "No se pudo cargar el pdfium.dll pinneado por el proyecto.");

        try
        {
            var missing = CriticalExports
                .Where(name => !NativeLibrary.TryGetExport(library, name, out _))
                .ToArray();

            Assert.True(
                missing.Length == 0,
                $"El pdfium.dll pinneado no exporta APIs críticas requeridas por F5: {string.Join(", ", missing)}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [Fact]
    public void PinnedPdfium_ReportsOptionalF5DetectorAvailability()
    {
        Assert.True(NativeLibrary.TryLoad("pdfium", out var library), "No se pudo cargar el pdfium.dll pinneado por el proyecto.");

        try
        {
            foreach (var export in OptionalDetectorExports)
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
