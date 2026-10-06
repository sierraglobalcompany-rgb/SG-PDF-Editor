using System.Threading;

namespace SGPdf.App.Pdf;

internal static class PdfiumRuntime
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static int _initialized;

    internal static void EnsureInitialized()
    {
        RunExclusive(static () => { });
    }

    internal static void RunExclusive(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Gate.Wait();
        try
        {
            EnsureInitializedUnsafe();
            action();
        }
        finally
        {
            Gate.Release();
        }
    }

    internal static T RunExclusive<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Gate.Wait();
        try
        {
            EnsureInitializedUnsafe();
            return action();
        }
        finally
        {
            Gate.Release();
        }
    }

    internal static void Shutdown()
    {
        Gate.Wait();
        try
        {
            if (Interlocked.Exchange(ref _initialized, 0) == 0)
                return;

            PdfiumNative.FPDF_DestroyLibrary();
        }
        finally
        {
            Gate.Release();
        }
    }

    private static void EnsureInitializedUnsafe()
    {
        if (Volatile.Read(ref _initialized) != 0)
            return;

        PdfiumNative.FPDF_InitLibrary();
        Volatile.Write(ref _initialized, 1);
    }
}
