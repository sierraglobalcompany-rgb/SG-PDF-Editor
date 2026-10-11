using System.Reflection;
using System.Runtime.InteropServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfiumOcrApiAvailabilityTests
{
    [Fact]
    public void PinnedRuntime_exports_text_render_mode_setter_and_managed_invisible_mode_is_exact()
    {
        PdfiumRuntime.EnsureInitialized();

        Assert.True(
            NativeLibrary.TryLoad(
                "pdfium",
                typeof(PdfiumNative).Assembly,
                DllImportSearchPath.SafeDirectories,
                out var libraryHandle),
            "The pinned PDFium native library could not be loaded from the application runtime.");

        try
        {
            Assert.True(
                NativeLibrary.TryGetExport(libraryHandle, "FPDFTextObj_SetTextRenderMode", out var export),
                "The pinned PDFium runtime does not export FPDFTextObj_SetTextRenderMode.");
            Assert.NotEqual(IntPtr.Zero, export);
        }
        finally
        {
            NativeLibrary.Free(libraryHandle);
        }

        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var invisibleMode = typeof(PdfiumNative).GetField("FPDF_TEXTRENDERMODE_INVISIBLE", flags);
        Assert.NotNull(invisibleMode);
        Assert.True(invisibleMode!.IsLiteral);
        Assert.Equal(3, Assert.IsType<int>(invisibleMode.GetRawConstantValue()));

        var setter = typeof(PdfiumNative).GetMethod("FPDFTextObj_SetTextRenderMode", flags);
        Assert.NotNull(setter);
        Assert.Equal(typeof(int), setter!.ReturnType);
        Assert.Equal(new[] { typeof(IntPtr), typeof(int) }, setter.GetParameters().Select(parameter => parameter.ParameterType));
    }
}
