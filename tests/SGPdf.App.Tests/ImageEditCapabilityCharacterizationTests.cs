using System.Reflection;
using System.Runtime.InteropServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditCapabilityCharacterizationTests
{
    [Fact]
    public void ConditionalExports_ArePresentOnPinnedRuntime()
    {
        var library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();
        try
        {
            foreach (var export in PdfiumImageEditApiAvailabilityTests.ConditionalExports)
                Assert.True(NativeLibrary.TryGetExport(library, export, out _), $"Expected pinned export: {export}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [Fact]
    public void ProvenConditionalExports_HaveNativeBindingsForCharacterization()
    {
        var nativeType = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfiumNative");
        Assert.NotNull(nativeType);

        foreach (var methodName in new[]
        {
            "FPDFPageObj_SetFillColor",
            "FPDFPage_InsertObjectAtIndex",
            "FPDFPageObj_GetRotatedBounds",
            "FPDFImageObj_GetRenderedBitmap"
        })
        {
            Assert.NotNull(nativeType!.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic));
        }
    }
}
