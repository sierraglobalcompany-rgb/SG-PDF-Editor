using System.Collections;
using System.Collections.Concurrent;
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
using SGPdf.App.Features.Reader;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowReaderThumbnailTests
{
    [Fact]
    public void PagesTab_IsVirtualizedAndBookmarksTabExists()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var tabs = Element<TabControl>(window, "ReaderNavigationTabs");
                var pages = Element<TabItem>(window, "ReaderPagesTab");
                var bookmarks = Element<TabItem>(window, "ReaderBookmarksTab");
                var list = Element<ListBox>(window, "ReaderThumbnailList");
                var tree = Element<TreeView>(window, "ReaderBookmarksTree");

                Assert.Same(pages, tabs.Items[0]);
                Assert.Same(bookmarks, tabs.Items[1]);
                Assert.NotNull(tree);
                Assert.True(VirtualizingPanel.GetIsVirtualizing(list));
                Assert.Equal(VirtualizationMode.Recycling, VirtualizingPanel.GetVirtualizationMode(list));
                Assert.Equal(ScrollUnit.Pixel, VirtualizingPanel.GetScrollUnit(list));
                Assert.True(ScrollViewer.GetCanContentScroll(list));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ThumbnailRequests_AreLazyForRealizedWindowOnly()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(40);
            var window = new MainWindow();
            var calls = new ConcurrentQueue<int>();
            try
            {
                SetField(window, "_getThumbnailRealizedRange", (Func<(int First, int Last)?>)(() => (10, 12)));
                SetField(window, "_renderReaderThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) =>
                    {
                        calls.Enqueue(index);
                        return SyntheticRendered(index, dpi, 21);
                    }));

                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                var requested = calls.Distinct().OrderBy(value => value).ToArray();
                Assert.NotEmpty(requested);
                Assert.Contains(10, requested);
                Assert.Contains(11, requested);
                Assert.Contains(12, requested);
                Assert.All(requested, index => Assert.InRange(index, 9, 13));
                Assert.True(requested.Length <= 5);
                Assert.Equal(40, GetThumbnailItems(window).Count);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ThumbnailClick_NavigatesExactlyOnce()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(6);
            var window = new MainWindow();
            try
            {
                SetField(window, "_getThumbnailRealizedRange", (Func<(int First, int Last)?>)(() => (0, 2)));
                SetField(window, "_renderReaderThumbnail", DefaultThumbnailRenderer());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                var beforeVersion = GetSchedulerVersion(window, "_readerRenderScheduler");
                var list = Element<ListBox>(window, "ReaderThumbnailList");
                list.SelectedIndex = 3;
                PumpUntil(() => GetNavigation(window)?.CurrentPageIndex == 3);
                PumpUntil(() => GetSchedulerVersion(window, "_readerRenderScheduler") > beforeVersion);

                Assert.Equal(3, GetNavigation(window)!.CurrentPageIndex);
                Assert.Equal(beforeVersion + 1, GetSchedulerVersion(window, "_readerRenderScheduler"));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ReaderScroll_UpdatesSelectedThumbnailWithoutRecursiveNavigation()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(6);
            var window = new MainWindow();
            try
            {
                SetField(window, "_getThumbnailRealizedRange", (Func<(int First, int Last)?>)(() => (0, 2)));
                SetField(window, "_renderReaderThumbnail", DefaultThumbnailRenderer());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                var beforeVersion = GetSchedulerVersion(window, "_readerRenderScheduler");
                Invoke(window, "ScrollReaderToPage", 4);

                Assert.Equal(4, Element<ListBox>(window, "ReaderThumbnailList").SelectedIndex);
                Assert.Equal(4, GetNavigation(window)!.CurrentPageIndex);
                Assert.Equal(beforeVersion, GetSchedulerVersion(window, "_readerRenderScheduler"));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ThumbnailRenderFailure_LeavesMainReaderUsable()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                SetField(window, "_getThumbnailRealizedRange", (Func<(int First, int Last)?>)(() => (0, 0)));
                SetField(window, "_renderReaderThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, _, _) =>
                        throw new InvalidOperationException($"thumbnail {index} failed")));

                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                var thumbnails = GetThumbnailItems(window);
                Assert.True(GetProperty<bool>(thumbnails[0], "HasError"));
                Assert.NotNull(GetSession(window));
                Assert.Equal(Visibility.Visible, Element<Grid>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal("Ready", GetProperty(GetReaderItems(window)[0], "RenderState")?.ToString());
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void RapidThumbnailScroll_DropsStalePublication()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(8);
            var window = new MainWindow();
            var firstStarted = new ManualResetEventSlim(false);
            var releaseFirst = new ManualResetEventSlim(false);
            var range = (First: 0, Last: 0);
            var call = 0;
            try
            {
                SetField(window, "_getThumbnailRealizedRange", (Func<(int First, int Last)?>)(() => range));
                SetField(window, "_renderReaderThumbnail", DefaultThumbnailRenderer());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                ReleaseAllThumbnails(window);

                SetField(window, "_renderReaderThumbnail",
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

                var first = InvokeTask(window, "RefreshThumbnailRenderWindowAsync");
                Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(3)));
                range = (4, 4);
                var second = InvokeTask(window, "RefreshThumbnailRenderWindowAsync");
                await second;
                releaseFirst.Set();
                await first;

                var thumbnails = GetThumbnailItems(window);
                Assert.Null(GetProperty(thumbnails[0], "Bitmap"));
                var currentBitmap = GetProperty<BitmapSource?>(thumbnails[4], "Bitmap");
                Assert.NotNull(currentBitmap);
                Assert.Equal((byte)2, ReadFirstPixelByte(currentBitmap!));
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

    private static Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage> DefaultThumbnailRenderer()
        => (_, index, dpi, _) => SyntheticRendered(index, dpi, 44);

    private static async Task<bool> OpenReaderAsync(MainWindow window, string path)
    {
        SetField(window, "_getReaderPageSizes",
            (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) => session.GetPageSizes(token)));
        SetField(window, "_renderReaderPage",
            (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => SyntheticRendered(index, dpi, 33)));
        return await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
    }

    private static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi, byte marker)
    {
        var pixels = new byte[8 * 8 * 4];
        Array.Fill(pixels, marker);
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(marker, 300d, 1d, 0d, 0d, -1d, 8, 8)
        };
    }

    private static byte ReadFirstPixelByte(BitmapSource bitmap)
    {
        var pixels = new byte[Math.Max(4, bitmap.PixelWidth * bitmap.PixelHeight * 4)];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        return pixels[0];
    }

    private static void ReleaseAllThumbnails(MainWindow window)
    {
        foreach (var item in GetThumbnailItems(window))
            InvokeOn(item, "ReleaseBitmap");
    }

    private static IReadOnlyList<object> GetThumbnailItems(MainWindow window)
        => ((IEnumerable)GetField(window, "_readerThumbnails")!).Cast<object>().ToArray();

    private static IReadOnlyList<object> GetReaderItems(MainWindow window)
        => ((IEnumerable)GetField(window, "_readerPages")!).Cast<object>().ToArray();

    private static PdfDocumentSession? GetSession(MainWindow window)
        => (PdfDocumentSession?)GetField(window, "_session");

    private static PageNavigationState? GetNavigation(MainWindow window)
        => (PageNavigationState?)GetField(window, "_navigation");

    private static long GetSchedulerVersion(MainWindow window, string fieldName)
    {
        var scheduler = GetField(window, fieldName)!;
        var field = scheduler.GetType().GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (long)field.GetValue(scheduler)!;
    }

    private static T Element<T>(MainWindow window, string name) where T : class
        => Assert.IsType<T>(window.FindName(name));

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
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
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

    private static void CloseClean(MainWindow window)
    {
        window.Close();
        if (GetOptionalField(window, "_session") is PdfDocumentSession session)
        {
            session.Dispose();
            SetField(window, "_session", null!);
        }
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
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-reader-thumbs-{Guid.NewGuid():N}");
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
