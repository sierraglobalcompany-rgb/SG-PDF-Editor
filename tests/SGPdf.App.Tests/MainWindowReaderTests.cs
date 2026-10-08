using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Features.Labels;
using SGPdf.App.Features.Reader;
using SGPdf.App.Features.Sign;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;
using SGPdf.App.Printing;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowReaderTests
{
    [Fact]
    public void LeerMode_ShowsContinuousSurfaceAndHidesSinglePageEditSurface()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                Assert.Equal(Visibility.Visible, Element<FrameworkElement>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(Visibility.Collapsed, Element<ScrollViewer>(window, "PdfScrollViewer").Visibility);
                Assert.Equal(Visibility.Collapsed, Element<Canvas>(window, "SignatureOverlayCanvas").Visibility);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ContinuousSurface_UsesRecyclingVirtualization()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var list = Element<ListBox>(window, "ReaderPageList");
                Assert.True(VirtualizingPanel.GetIsVirtualizing(list));
                Assert.Equal(VirtualizationMode.Recycling, VirtualizingPanel.GetVirtualizationMode(list));
                Assert.Equal(ScrollUnit.Pixel, VirtualizingPanel.GetScrollUnit(list));
                Assert.IsNotType<ScrollViewer>(list.Parent);
                Assert.NotNull(window.FindName("ReaderContinuousSurface"));
            }
            finally { window.Close(); }
        });
    }

    [Fact]
    public void OpenPdf_PublishesSessionAfterPageMetricsWithoutEagerAllPageRender()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(8);
            var window = new MainWindow();
            var metricCalls = 0;
            var renderCalls = 0;
            try
            {
                SetField(window, "_getReaderPageSizes",
                    (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) =>
                    {
                        metricCalls++;
                        return session.GetPageSizes(token);
                    }));
                SetField(window, "_renderReaderPage",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((session, index, dpi, token) =>
                    {
                        renderCalls++;
                        return session.RenderPage(index, dpi, token);
                    }));

                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", fixture.SourcePath, null));

                Assert.Equal(1, metricCalls);
                Assert.InRange(renderCalls, 1, 3);
                Assert.Equal(8, GetReaderItems(window).Count);
                Assert.Equal(Path.GetFullPath(fixture.SourcePath), GetSession(window)!.FilePath);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ScrollCenter_UpdatesNavigationPageNumberAndPrintCurrentSemantics()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(5);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var list = Element<ListBox>(window, "ReaderPageList");
                list.Height = 280d;
                list.Measure(new Size(800d, 280d));
                list.Arrange(new Rect(0d, 0d, 800d, 280d));

                var geometry = GetGeometry(window);
                SetField(window, "_readerVerticalOffset", geometry[2].Top);
                Invoke(window, "UpdateReaderCurrentPageFromViewport");

                var navigation = GetNavigation(window)!;
                Assert.Equal(2, navigation.CurrentPageIndex);
                Assert.Equal("3", Element<TextBox>(window, "PageNumberTextBox").Text);
                var current = PdfPrintRange.Current(navigation.CurrentPageIndex, navigation.PageCount);
                Assert.Equal(2, current.FirstPageIndex);
                Assert.Equal(2, current.LastPageIndex);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void PreviousNextAndPageNumber_ScrollWithoutRecreatingSession()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(5);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var session = GetSession(window);

                await InvokeTask(window, "NavigateAsync", (Func<PageNavigationState, PageNavigationState>)(state => state.Next()));
                Assert.Equal(1, GetNavigation(window)!.CurrentPageIndex);
                Assert.Same(session, GetSession(window));

                await InvokeTask(window, "NavigateAsync", (Func<PageNavigationState, PageNavigationState>)(state => state.Previous()));
                Assert.Equal(0, GetNavigation(window)!.CurrentPageIndex);
                Assert.Same(session, GetSession(window));

                Invoke(window, "ScrollReaderToPage", 3);
                Assert.Equal(3, GetNavigation(window)!.CurrentPageIndex);
                Assert.Equal("4", Element<TextBox>(window, "PageNumberTextBox").Text);
                Assert.Same(session, GetSession(window));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void Zoom_RebuildsGeometryKeepsCurrentPageAnchorAndReleasesStaleBitmaps()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(5);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                Invoke(window, "ScrollReaderToPage", 2);
                PublishAllReaderItems(window, 33);

                SetField(window, "_zoom", new PdfZoomState().FitWidth());
                Invoke(window, "RebuildReaderGeometry", true);

                Assert.Equal(2, GetNavigation(window)!.CurrentPageIndex);
                Assert.All(GetReaderItems(window), item => Assert.Null(GetProperty(item, "Bitmap")));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void StaleReaderGeneration_DoesNotPublishBitmapOrTransform()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(3);
            var window = new MainWindow();
            var firstStarted = new ManualResetEventSlim(false);
            var releaseFirst = new ManualResetEventSlim(false);
            var call = 0;
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                Invoke(window, "RebuildReaderGeometry", true);

                SetField(window, "_renderReaderPage",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) =>
                    {
                        var current = Interlocked.Increment(ref call);
                        if (current == 1)
                        {
                            firstStarted.Set();
                            releaseFirst.Wait(TimeSpan.FromSeconds(5));
                            return SyntheticRendered(index, dpi, 1);
                        }
                        return SyntheticRendered(index, dpi, 2);
                    }));

                var first = InvokeTask(window, "RefreshReaderRenderWindowAsync");
                Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(3)));
                var second = InvokeTask(window, "RefreshReaderRenderWindowAsync");
                await second;
                releaseFirst.Set();
                await first;

                var transform = GetProperty<PdfPageDeviceTransform?>(GetReaderItems(window)[0], "DeviceTransform");
                Assert.NotNull(transform);
                Assert.Equal(2d, transform.Value.OriginX);
            }
            finally
            {
                releaseFirst.Set();
                firstStarted.Dispose();
                releaseFirst.Dispose();
                CloseClean(window);
            }
        });
    }

    [Fact]
    public void PerPageRenderFailure_IsolatedAndSessionRemainsActive()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                var render = (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((session, index, dpi, token) =>
                {
                    if (index == 0)
                        throw new InvalidOperationException("forced page failure");
                    return session.RenderPage(index, dpi, token);
                });
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath, render));

                var items = GetReaderItems(window);
                Assert.Equal("Error", GetProperty(items[0], "RenderState")?.ToString());
                Assert.Equal("Ready", GetProperty(items[1], "RenderState")?.ToString());
                Assert.NotNull(GetSession(window));
                Assert.Equal(0, GetNavigation(window)!.CurrentPageIndex);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ReaderRetention_ReleasesBitmapOutsideVisiblePlusNeighborWindow()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(8);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var list = Element<ListBox>(window, "ReaderPageList");
                list.Height = 280d;
                list.Measure(new Size(800d, 280d));
                list.Arrange(new Rect(0d, 0d, 800d, 280d));
                PublishAllReaderItems(window, 55);

                var geometry = GetGeometry(window);
                SetField(window, "_readerVerticalOffset", geometry[4].Top);
                SetField(window, "_navigation", new PageNavigationState(8, 4));
                await InvokeTask(window, "RefreshReaderRenderWindowAsync");

                var items = GetReaderItems(window);
                Assert.Null(GetProperty(items[0], "Bitmap"));
                Assert.Null(GetProperty(items[1], "Bitmap"));
                Assert.Null(GetProperty(items[2], "Bitmap"));
                Assert.NotNull(GetProperty(items[4], "Bitmap"));
                Assert.Null(GetProperty(items[6], "Bitmap"));
                Assert.Null(GetProperty(items[7], "Bitmap"));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void EnterFirmar_UsesCurrentReaderPageAndExistingSinglePageSurface()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                Invoke(window, "ScrollReaderToPage", 2);

                Click(window, "SignModeButton");
                PumpUntil(() => (bool)(GetField(window, "_signatureModeActive") ?? false));

                Assert.Equal(2, GetNavigation(window)!.CurrentPageIndex);
                Assert.Equal(Visibility.Collapsed, Element<FrameworkElement>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(Visibility.Visible, Element<ScrollViewer>(window, "PdfScrollViewer").Visibility);
                Assert.Equal(Visibility.Visible, Element<Image>(window, "PdfImage").Visibility);
                Assert.NotNull(Element<Image>(window, "PdfImage").Source);
                Assert.Equal(Visibility.Visible, Element<Canvas>(window, "SignatureOverlayCanvas").Visibility);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ReturnLeer_PreservesCurrentPageAndExistingSignatureDirtyGuard()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                Invoke(window, "ScrollReaderToPage", 2);
                Click(window, "SignModeButton");
                PumpUntil(() => (bool)(GetField(window, "_signatureModeActive") ?? false));
                Invoke(window, "AddSignatureAsset", CreateSignatureAsset());

                SetField(window, "_resolvePendingSignatureDecision",
                    (Func<SignatureGuardReason, PendingSignatureDecision>)(_ => PendingSignatureDecision.Cancel));
                Click(window, "ReadModeButton");
                Assert.True((bool)GetField(window, "_signatureModeActive")!);
                Assert.Equal(Visibility.Collapsed, Element<FrameworkElement>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(2, GetNavigation(window)!.CurrentPageIndex);
                Assert.True(((SignatureEditState)GetField(window, "_signatureEditState")!).IsDirty);

                SetField(window, "_resolvePendingSignatureDecision",
                    (Func<SignatureGuardReason, PendingSignatureDecision>)(_ => PendingSignatureDecision.Discard));
                Click(window, "ReadModeButton");
                Assert.False((bool)GetField(window, "_signatureModeActive")!);
                Assert.Equal(Visibility.Visible, Element<FrameworkElement>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(Visibility.Collapsed, Element<Canvas>(window, "SignatureOverlayCanvas").Visibility);
                Assert.Equal(2, GetNavigation(window)!.CurrentPageIndex);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void PdfToZpl_HidesBothPdfReaderSurfacesAndKeepsExistingZplBehavior()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var document = ZplDocumentParser.Parse(@"C:\labels\orders.zpl", "^XA^FO10,10^FDOne^FS^PQ1^XZ");
                IReadOnlyList<ZplRenderedLabel> rendered =
                [new ZplRenderedLabel(0, 102d, 152d, 8, CreatePngBytes())];

                Invoke(window, "CommitLoadedZpl", document, rendered);

                Assert.Equal(Visibility.Collapsed, Element<FrameworkElement>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(Visibility.Collapsed, Element<Canvas>(window, "SignatureOverlayCanvas").Visibility);
                Assert.Null(GetSession(window));
                Assert.Equal(Visibility.Visible, Element<Border>(window, "LabelNavigationBar").Visibility);
                Assert.Equal(Visibility.Visible, Element<Image>(window, "PdfImage").Visibility);
                Assert.NotNull(Element<Image>(window, "PdfImage").Source);
            }
            finally { CloseClean(window); }
        });
    }

    private static async Task<bool> OpenReaderAsync(
        MainWindow window,
        string path,
        Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>? render = null)
    {
        SetField(window, "_getReaderPageSizes",
            (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) => session.GetPageSizes(token)));
        SetField(window, "_renderReaderPage", render ??
            (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((session, index, dpi, token) => session.RenderPage(index, dpi, token)));
        return await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
    }

    private static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi, int marker)
    {
        var pixels = new byte[8 * 8 * 4];
        Array.Fill(pixels, (byte)marker);
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(marker, 300d, 1d, 0d, 0d, -1d, 8, 8)
        };
    }

    private static void PublishAllReaderItems(MainWindow window, byte marker)
    {
        foreach (var item in GetReaderItems(window))
        {
            var pageIndex = GetProperty<int>(item, "PageIndex");
            var geometry = GetProperty<ReaderPageGeometry>(item, "Geometry");
            var rendered = SyntheticRendered(pageIndex, geometry.ResolvedDpi, marker);
            var bitmap = CreateBitmap(8, 8, marker);
            InvokeOn(item, "Publish", rendered, bitmap);
        }
    }

    private static IReadOnlyList<object> GetReaderItems(MainWindow window)
        => ((IEnumerable)GetField(window, "_readerPages")!).Cast<object>().ToArray();

    private static IReadOnlyList<ReaderPageGeometry> GetGeometry(MainWindow window)
        => (IReadOnlyList<ReaderPageGeometry>)GetField(window, "_readerGeometry")!;

    private static PdfDocumentSession? GetSession(MainWindow window)
        => (PdfDocumentSession?)GetField(window, "_session");

    private static PageNavigationState? GetNavigation(MainWindow window)
        => (PageNavigationState?)GetField(window, "_navigation");

    private static SignatureAsset CreateSignatureAsset()
    {
        var pixels = new byte[80 * 40 * 4];
        for (var index = 3; index < pixels.Length; index += 4)
            pixels[index] = 255;
        pixels[3] = 0;
        return new SignatureAsset(80, 40, 320, pixels, "reader-test.png");
    }

    private static BitmapSource CreateBitmap(int width, int height, byte marker)
    {
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, marker);
        var bitmap = BitmapSource.Create(width, height, 96d, 96d, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte[] CreatePngBytes()
    {
        var bitmap = CreateBitmap(1, 1, 40);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static T Element<T>(MainWindow window, string name) where T : class
        => Assert.IsType<T>(window.FindName(name));

    private static void Click(MainWindow window, string name)
        => Element<Button>(window, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

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

    private static object? GetProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return property.GetValue(instance);
    }

    private static T GetProperty<T>(object instance, string name)
        => (T)GetProperty(instance, name)!;

    private static object? Invoke(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == name && item.GetParameters().Length == args.Length);
        return method.Invoke(window, args);
    }

    private static object? InvokeOn(object instance, string name, params object?[] args)
    {
        var method = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(item => item.Name == name && item.GetParameters().Length == args.Length);
        return method.Invoke(instance, args);
    }

    private static async Task InvokeTask(MainWindow window, string name, params object?[] args)
        => await (Task)Invoke(window, name, args)!;

    private static async Task<T> InvokeTask<T>(MainWindow window, string name, params object?[] args)
        => await (Task<T>)Invoke(window, name, args)!;

    private static void PumpUntil(Func<bool> predicate, int timeoutMilliseconds = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("WPF condition did not become true in time.");
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            Thread.Sleep(5);
        }
    }

    private static void RunInSta(Action action)
        => RunInSta(() => { action(); return Task.CompletedTask; });

    private static void RunInSta(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                AwaitWithDispatcher(action());
            }
            catch (Exception ex)
            {
                failure = ex;
            }
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

    private static void CloseClean(MainWindow window)
    {
        if (GetOptionalField(window, "_signatureEditState") is SignatureEditState state)
            state.DiscardAll();
        window.Close();
    }

    private static object? GetOptionalField(MainWindow window, string name)
        => typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window);

    private sealed class PdfFixture : IDisposable
    {
        private PdfFixture(string directory, string sourcePath)
        {
            DirectoryPath = directory;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static PdfFixture Create(int pageCount)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-reader-ui-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            var document = new PdfDocument();
            for (var index = 0; index < pageCount; index++)
            {
                var page = document.AddPage();
                page.Width = PdfSharp.Drawing.XUnit.FromPoint(index % 2 == 0 ? 300d : 400d);
                page.Height = PdfSharp.Drawing.XUnit.FromPoint(index % 2 == 0 ? 400d : 300d);
            }
            document.Save(source);
            document.Close();
            return new PdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
    }
}
