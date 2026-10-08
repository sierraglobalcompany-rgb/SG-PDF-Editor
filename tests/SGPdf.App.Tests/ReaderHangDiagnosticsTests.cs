using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderHangDiagnosticsTests
{
    [Fact]
    public void OpenWithSyntheticRenderer_WithoutClose_Completes()
    {
        RunInSta(async () =>
        {
            var path = CreatePdf();
            var window = new MainWindow();
            SetField(window, "_getReaderPageSizes",
                (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((_, _) =>
                    [new PdfPageSize(300d, 400d), new PdfPageSize(300d, 400d)]));
            SetField(window, "_renderReaderPage",
                (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => Synthetic(index, dpi)));

            var opened = await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
            Assert.True(opened);
        });
    }

    [Fact]
    public void OpenWithRealRenderer_WithoutClose_Completes()
    {
        RunInSta(async () =>
        {
            var path = CreatePdf();
            var window = new MainWindow();
            SetField(window, "_getReaderPageSizes",
                (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) => session.GetPageSizes(token)));
            SetField(window, "_renderReaderPage",
                (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((session, index, dpi, token) => session.RenderPage(index, dpi, token)));

            var opened = await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
            Assert.True(opened);
        });
    }

    [Fact]
    public void OpenWithSyntheticRenderer_ThenClose_Completes()
    {
        RunInSta(async () =>
        {
            var path = CreatePdf();
            var window = new MainWindow();
            SetField(window, "_getReaderPageSizes",
                (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((_, _) =>
                    [new PdfPageSize(300d, 400d), new PdfPageSize(300d, 400d)]));
            SetField(window, "_renderReaderPage",
                (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => Synthetic(index, dpi)));

            var opened = await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
            Assert.True(opened);
            window.Close();
        });
    }

    private static string CreatePdf()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-reader-hang-{Guid.NewGuid():N}.pdf");
        var document = new PdfDocument();
        for (var index = 0; index < 2; index++)
        {
            var page = document.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(300d);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(400d);
        }
        document.Save(path);
        document.Close();
        return path;
    }

    private static PdfRenderedPage Synthetic(int pageIndex, double dpi)
    {
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, new byte[8 * 8 * 4])
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 400d, 1d, 0d, 0d, -1d, 8, 8)
        };
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
            task.ContinueWith(
                _ => Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => frame.Continue = false)),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
            Dispatcher.PushFrame(frame);
        }
        task.GetAwaiter().GetResult();
    }
}
