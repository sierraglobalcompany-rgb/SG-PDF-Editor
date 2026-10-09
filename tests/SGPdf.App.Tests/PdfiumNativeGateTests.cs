using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfiumNativeGateTests
{
    [Fact]
    public async Task NavigationCalls_WaitForGlobalNativeSemaphore()
    {
        using var fixture = PdfFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);

        await AssertWaitsForNativeGateAsync(() => session.GetBookmarks());
        await AssertWaitsForNativeGateAsync(() => session.GetPageLinks(0));
    }

    private static async Task AssertWaitsForNativeGateAsync(Action nativeCall)
    {
        await PdfiumRuntime.NativeGate.WaitAsync();
        Task? call = null;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            call = Task.Run(() =>
            {
                started.SetResult();
                nativeCall();
            });

            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Delay(200);

            Assert.False(
                call.IsCompleted,
                "La operación de navegación atravesó PDFium mientras NativeGate seguía ocupado.");
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }

        await call!.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class PdfFixture : IDisposable
    {
        private PdfFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static PdfFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-native-gate-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");

            using var document = new PdfDocument();
            document.AddPage();
            document.Save(source);

            return new PdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
