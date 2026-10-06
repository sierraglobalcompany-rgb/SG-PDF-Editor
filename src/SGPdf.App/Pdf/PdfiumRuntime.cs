using System.Threading;

namespace SGPdf.App.Pdf;

internal static class PdfiumRuntime
{
    private static int _initialized;

    internal static void EnsureInitialized()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        try
        {
            PdfiumNative.FPDF_InitLibrary();
        }
        catch
        {
            Volatile.Write(ref _initialized, 0);
            throw;
        }
    }

    internal static void Shutdown()
    {
        if (Interlocked.Exchange(ref _initialized, 0) == 0)
            return;

        PdfiumNative.FPDF_DestroyLibrary();
    }
}
