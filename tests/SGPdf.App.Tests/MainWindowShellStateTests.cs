using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Features.Labels;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowShellStateTests
{
    [Fact]
    public void Startup_ShowsHome_AndWiresHomeIntentHandlers()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                Assert.Equal("Home", GetShellKind(window));
                var dashboard = GetHomeDashboard(window);
                Assert.Equal(Visibility.Visible, dashboard.Visibility);

                foreach (var eventName in new[] { "OpenPdfRequested", "OpenZplRequested", "RecentPdfRequested", "ToolRequested" })
                {
                    var backing = dashboard.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.NotNull(backing);
                    Assert.NotNull(backing.GetValue(dashboard));
                }
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void DocumentTabHeader_ResolvesApprovedPdfIconFromAbsolutePackResource()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                var tabs = Assert.IsType<System.Windows.Controls.TabControl>(
                    window.FindName("DocumentTabs"));
                var header = Assert.IsType<System.Windows.Controls.StackPanel>(
                    tabs.ItemTemplate!.LoadContent());
                var icon = Assert.IsType<System.Windows.Controls.Image>(header.Children[0]);
                var bitmap = Assert.IsType<BitmapImage>(icon.Source);

                // QA-V4-001: relative BitmapImage URIs in a code-built DataTemplate
                // have no guaranteed XAML BaseUri; icon was invisible in real Windows QA.
                Assert.True(bitmap.UriSource.IsAbsoluteUri,
                    "Document tab icon must resolve independently of XAML BaseUri.");
                Assert.Equal("pack", bitmap.UriSource.Scheme);
                Assert.Contains("/SGPdf.App;component/Assets/SGPdf.Document.png",
                    bitmap.UriSource.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
                Assert.True(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0,
                    "The approved PDF icon must decode to visible pixels.");
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void SuccessfulPdfOpen_SwitchesHomeToPdf()
    {
        RunInSta(async () =>
        {
            using var pdf = PdfFixture.Create();
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                ConfigureReader(window);

                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", pdf.Path, null));

                Assert.Equal("Pdf", GetShellKind(window));
                Assert.Equal(Visibility.Collapsed, GetHomeDashboard(window).Visibility);
                // Runtime regression: actual PDF selection and header strip track the same owner.
                var tabs = Assert.IsType<TabControl>(window.FindName("DocumentTabs"));
                Assert.Single(tabs.Items);
                Assert.Same(window.ActiveDocument, tabs.SelectedItem);
                Assert.NotNull(window.ActiveDocument);
                Assert.Equal(Visibility.Visible, tabs.Visibility);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void SuccessfulZplCommit_SwitchesToZpl_AndFailedPdfOpenPreservesZpl()
    {
        RunInSta(async () =>
        {
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                var document = ZplDocumentParser.Parse(
                    @"C:\labels\orders.zpl",
                    "^XA^FO10,10^FDOne^FS^XZ");
                CommitZpl(window, document, [CreateRenderedLabel()]);

                Assert.Equal("Zpl", GetShellKind(window));
                Assert.Equal(Visibility.Collapsed, GetHomeDashboard(window).Visibility);

                var missing = Path.Combine(Path.GetTempPath(), $"sgpdf-missing-{Guid.NewGuid():N}.pdf");
                Assert.False(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", missing, null));

                Assert.Equal("Zpl", GetShellKind(window));
                Assert.Equal(Visibility.Collapsed, GetHomeDashboard(window).Visibility);
            }
            finally { CloseClean(window); }
        });
    }

    private static void RaiseLoaded(MainWindow window)
        => window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    private static string GetShellKind(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("_shellWorkspaceKind", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window)?.ToString() ?? string.Empty;
    }

    private static FrameworkElement GetHomeDashboard(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("_homeDashboard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<FrameworkElement>(field.GetValue(window));
    }

    private static void ConfigureReader(MainWindow window)
    {
        SetField(window, "_renderReaderPage",
            (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => SyntheticRendered(index, dpi)));
    }

    private static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi)
    {
        var pixels = new byte[8 * 8 * 4];
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 8, 8)
        };
    }

    private static void CommitZpl(MainWindow window, ZplDocument document, IReadOnlyList<ZplRenderedLabel> rendered)
    {
        var method = typeof(MainWindow).GetMethod(
            "CommitLoadedZpl",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ZplDocument), typeof(IReadOnlyList<ZplRenderedLabel>)],
            modifiers: null);
        Assert.NotNull(method);
        method.Invoke(window, [document, rendered]);
    }

    private static ZplRenderedLabel CreateRenderedLabel()
        => new(0, 50, 30, 8, CreatePngBytes());

    private static byte[] CreatePngBytes()
    {
        var bitmap = BitmapSource.Create(
            1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { Colors.Black.B, Colors.Black.G, Colors.Black.R, Colors.Black.A }, 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
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

    private static void RunInSta(Action action)
        => RunInSta(() => { action(); return Task.CompletedTask; });

    private static void RunInSta(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                AwaitWithDispatcher(action());
            }
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
            task.ContinueWith(_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)), TaskScheduler.Default);
            Dispatcher.PushFrame(frame);
        }
        task.GetAwaiter().GetResult();
    }

    private static void CloseClean(MainWindow window)
    {
        window.Close();
        var field = typeof(MainWindow).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(window) is PdfDocumentSession session)
        {
            session.Dispose();
            field.SetValue(window, null);
        }
    }

    private sealed class PdfFixture : IDisposable
    {
        private PdfFixture(string root, string path) { Root = root; Path = path; }
        internal string Root { get; }
        internal string Path { get; }

        internal static PdfFixture Create()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-shell-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = System.IO.Path.Combine(root, "sample.pdf");
            using var document = new PdfDocument();
            document.AddPage();
            document.Save(path);
            return new PdfFixture(root, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
