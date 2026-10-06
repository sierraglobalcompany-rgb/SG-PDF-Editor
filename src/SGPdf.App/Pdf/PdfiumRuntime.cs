using System.Threading;

namespace SGPdf.App.Pdf;

internal static class PdfiumRuntime
{
    internal static readonly SemaphoreSlim NativeGate = new(1, 1);
    private static bool _initialized;

    internal static void EnsureInitialized()
    {
        NativeGate.Wait();
        try
        {
            if (_initialized)
                return;

            PdfiumNative.FPDF_InitLibrary();
            _initialized = true;
        }
        finally
        {
            NativeGate.Release();
        }
    }

    internal static void Shutdown()
    {
        NativeGate.Wait();
        try
        {
            if (!_initialized)
                return;

            PdfiumNative.FPDF_DestroyLibrary();
            _initialized = false;
        }
        finally
        {
            NativeGate.Release();
        }
    }
}
