using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderWindowLockDiagnosticsTests
{
    [Fact]
    public void MainWindow_WithSyntheticRenderer_CloseAndDispose_AllowsImmediateDelete()
    {
        RunInSta(async () =>
        {
            var fixture = CreatePdf();
            var window = new MainWindow();
            SetField(window, "_getReaderPageSizes",
                (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) => session.GetPageSizes(token)));
            SetField(window, "_renderReaderPage",
                (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => SyntheticRendered(index, dpi)));

            try
            {
                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", fixture.Path, null));
            }
            finally
            {
                window.Close();
                if (GetField(window, "_session") is PdfDocumentSession session)
                {
                    session.Dispose();
                    SetField(window, "_session", null!);
                }
            }

            File.Delete(fixture.Path);
            Directory.Delete(fixture.Directory);
            Assert.False(File.Exists(fixture.Path));
        });
    }

    private static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi)
    {
        var pixels = new byte[8 * 8 * 4];
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 8, 8)
        };
    }

    private static (string Directory, string Path) CreatePdf()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-window-lock-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "source.pdf");
        var document = new PdfDocument();
        document.AddPage();
        document.AddPage();
        document.Save(path);
        document.Close();
        return (directory, path);
    }

    private static object? GetField(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window);
    }

    private static void SetField(MainWindow window, string name, object value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, value);
    }

    private static object? Invoke(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == name && item.GetParameters().Length == args.Length);
        return method.Invoke(window, args);
    }

    private static async Task<T> InvokeTask<T>(MainWindow window, string name, params object?[] args)
        => await (Task<T>)Invoke(window, name, args)!;

    private static void RunInSta(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { AwaitWithDispatcher(action()); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void AwaitWithDispatcher(Task task)
    {
        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            var dispatcher = Dispatcher.CurrentDispatcher;
            task.ContinueWith(
                _ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
            Dispatcher.PushFrame(frame);
        }
        task.GetAwaiter().GetResult();
    }
}
