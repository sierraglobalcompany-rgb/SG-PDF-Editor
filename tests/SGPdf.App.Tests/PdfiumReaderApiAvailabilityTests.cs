using System.Runtime.InteropServices;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfiumReaderApiAvailabilityTests
{
    private static readonly string[] RequiredExports =
    {
        "FPDF_GetPageSizeByIndexF",
        "FPDFText_LoadPage",
        "FPDFText_ClosePage",
        "FPDFText_FindStart",
        "FPDFText_FindNext",
        "FPDFText_FindPrev",
        "FPDFText_FindClose",
        "FPDFText_GetSchResultIndex",
        "FPDFText_GetSchCount",
        "FPDFText_GetCharIndexAtPos",
        "FPDFText_GetText",
        "FPDFText_CountRects",
        "FPDFText_GetRect",
        "FPDFBookmark_GetFirstChild",
        "FPDFBookmark_GetNextSibling",
        "FPDFBookmark_GetTitle",
        "FPDFBookmark_GetDest",
        "FPDFDest_GetDestPageIndex",
        "FPDFLink_Enumerate",
        "FPDFLink_GetAnnotRect",
        "FPDFLink_GetDest",
        "FPDFLink_GetAction",
        "FPDFAction_GetType",
        "FPDFAction_GetDest",
        "FPDFAction_GetURIPath"
    };

    [Fact]
    public void PinnedPdfium_ExportsAllF4RequiredStableFunctions()
    {
        var library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();

        try
        {
            var missing = RequiredExports
                .Where(name => !NativeLibrary.TryGetExport(library, name, out _))
                .ToArray();

            Assert.True(
                missing.Length == 0,
                $"El pdfium.dll pinneado no exporta APIs requeridas por F4: {string.Join(", ", missing)}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }
}
