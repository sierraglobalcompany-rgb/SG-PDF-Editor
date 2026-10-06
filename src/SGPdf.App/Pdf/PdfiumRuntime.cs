using System.Threading;

namespace SGPdf.App.Pdf;

internal static class PdfiumRuntime
{
    private static int _initialized;

    internal static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        PdfiumNative.FPDF_InitLibrary();
    }

    internal static void Shutdown()
    {
        if (Interlocked.Exchange(ref _initialized, 0) == 0)
            return;

        PdfiumNative.FPDF_DestroyLibrary();
    }
}
