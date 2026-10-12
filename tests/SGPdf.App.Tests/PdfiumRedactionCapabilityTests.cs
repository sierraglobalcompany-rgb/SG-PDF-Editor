using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfiumRedactionCapabilityTests
{
    [Fact]
    public void CreateNewRect_PinnedPdfiumRuntime_CreatesNativeRectangle()
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();

        IntPtr rectangle = IntPtr.Zero;
        try
        {
            rectangle = PdfiumNative.FPDFPageObj_CreateNewRect(10f, 20f, 30f, 40f);

            Assert.NotEqual(IntPtr.Zero, rectangle);
        }
        finally
        {
            if (rectangle != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(rectangle);
            PdfiumRuntime.NativeGate.Release();
        }
    }
}
