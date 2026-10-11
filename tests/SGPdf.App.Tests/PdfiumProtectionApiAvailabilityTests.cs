using System.Runtime.InteropServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfiumNativeGateCollection.Name)]
public sealed class PdfiumProtectionApiAvailabilityTests
{
    [Fact]
    public void PinnedPdfium_CanConsumeProtectionButExposesNoCreationSetter()
    {
        PdfiumRuntime.EnsureInitialized();

        var module = GetModuleHandleW("pdfium.dll");
        Assert.NotEqual(IntPtr.Zero, module);

        AssertExport(module, "FPDF_LoadDocument");
        AssertExport(module, "FPDF_SaveAsCopy");
        AssertExport(module, "FPDF_GetDocPermissions");
        AssertExport(module, "FPDF_GetSecurityHandlerRevision");

        AssertNoExport(module, "FPDF_SetPassword");
        AssertNoExport(module, "FPDF_SetDocPermissions");
        AssertNoExport(module, "FPDF_SetEncryption");
        AssertNoExport(module, "FPDF_SetEncrypt");
    }

    private static void AssertExport(IntPtr module, string name)
        => Assert.True(
            NativeLibrary.TryGetExport(module, name, out _),
            $"Pinned PDFium is missing expected public export '{name}'.");

    private static void AssertNoExport(IntPtr module, string name)
        => Assert.False(
            NativeLibrary.TryGetExport(module, name, out _),
            $"Pinned PDFium unexpectedly exposes '{name}'; reevaluate the protection-engine decision.");

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern IntPtr GetModuleHandleW(string lpModuleName);
}
