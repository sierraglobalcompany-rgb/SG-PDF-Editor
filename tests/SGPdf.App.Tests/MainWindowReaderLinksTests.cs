using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowReaderLinksTests
{
    [Fact]
    public void BookmarksTree_PopulatesAndInternalBookmarkNavigates()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                SetNavigationSeams(window,
                    new[] { new PdfBookmarkNode("Capítulo", 1, Array.Empty<PdfBookmarkNode>()) },
                    Array.Empty<PdfPageLink>());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                var tree = Element<TreeView>(window, "ReaderBookmarksTree");
                PumpUntil(() => tree.Items.Count == 1);
                var bookmark = Assert.IsType<PdfBookmarkNode>(tree.Items[0]);

                Assert.True((bool)Invoke(window, "TryActivateReaderBookmark", bookmark)!);
                Assert.Equal(1, CurrentPageIndex(window));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void InternalLink_NavigatesWithoutExternalConfirmation()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            var confirmations = 0;
            try
            {
                SetNavigationSeams(window, Array.Empty<PdfBookmarkNode>(), Array.Empty<PdfPageLink>());
                SetField(window, "_confirmExternalUri", (Func<Window, Uri, bool>)((_, _) => { confirmations++; return true; }));
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var link = new PdfPageLink(0, new PdfTextRect(20, 20, 80, 40), PdfLinkActionKind.InternalGoto, 1, null);

                Assert.True((bool)Invoke(window, "TryActivateReaderLink", link)!);
                Assert.Equal(1, CurrentPageIndex(window));
                Assert.Equal(0, confirmations);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void HttpUri_RequiresExplicitConfirmationBeforeBrowserOpen()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            Uri? confirmed = null;
            Uri? opened = null;
            try
            {
                SetNavigationSeams(window, Array.Empty<PdfBookmarkNode>(), Array.Empty<PdfPageLink>());
                SetField(window, "_confirmExternalUri", (Func<Window, Uri, bool>)((_, uri) => { confirmed = uri; return true; }));
                SetField(window, "_openExternalUri", (Action<Uri>)(uri => opened = uri));
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var link = new PdfPageLink(0, new PdfTextRect(20, 20, 80, 40), PdfLinkActionKind.Uri, null, "https://example.invalid/path");

                Assert.True((bool)Invoke(window, "TryActivateReaderLink", link)!);
                Assert.Equal("https://example.invalid/path", confirmed?.AbsoluteUri);
                Assert.Equal(confirmed, opened);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void UriConfirmationCancel_PerformsNoExternalAction()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            var opens = 0;
            try
            {
                SetNavigationSeams(window, Array.Empty<PdfBookmarkNode>(), Array.Empty<PdfPageLink>());
                SetField(window, "_confirmExternalUri", (Func<Window, Uri, bool>)((_, _) => false));
                SetField(window, "_openExternalUri", (Action<Uri>)(_ => opens++));
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var link = new PdfPageLink(0, new PdfTextRect(20, 20, 80, 40), PdfLinkActionKind.Uri, null, "http://example.invalid/");

                Assert.True((bool)Invoke(window, "TryActivateReaderLink", link)!);
                Assert.Equal(0, opens);
            }
            finally { CloseClean(window); }
        });
    }

    [Theory]
    [InlineData("mailto:user@example.invalid")]
    [InlineData("file:///C:/Windows/notepad.exe")]
    [InlineData("ftp://example.invalid/file")]
    public void NonHttpSchemes_NeverConfirmOrOpen(string target)
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            var confirmations = 0;
            var opens = 0;
            try
            {
                SetNavigationSeams(window, Array.Empty<PdfBookmarkNode>(), Array.Empty<PdfPageLink>());
                SetField(window, "_confirmExternalUri", (Func<Window, Uri, bool>)((_, _) => { confirmations++; return true; }));
                SetField(window, "_openExternalUri", (Action<Uri>)(_ => opens++));
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var link = new PdfPageLink(0, new PdfTextRect(20, 20, 80, 40), PdfLinkActionKind.Uri, null, target);

                Assert.False((bool)Invoke(window, "TryActivateReaderLink", link)!);
                Assert.Equal(0, confirmations);
                Assert.Equal(0, opens);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void UnsupportedAction_IsNoOp()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                SetNavigationSeams(window, Array.Empty<PdfBookmarkNode>(), Array.Empty<PdfPageLink>());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var link = new PdfPageLink(0, new PdfTextRect(20, 20, 80, 40), PdfLinkActionKind.Unsupported, 1, "https://example.invalid/");

                Assert.False((bool)Invoke(window, "TryActivateReaderLink", link)!);
                Assert.Equal(0, CurrentPageIndex(window));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void SelectionGesture_SuppressesLinkActivation()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                SetNavigationSeams(window, Array.Empty<PdfBookmarkNode>(), Array.Empty<PdfPageLink>());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetField(window, "_readerSelectionGestureActive", true);
                var link = new PdfPageLink(0, new PdfTextRect(20, 20, 80, 40), PdfLinkActionKind.InternalGoto, 1, null);

                Assert.False((bool)Invoke(window, "TryActivateReaderLink", link)!);
                Assert.Equal(0, CurrentPageIndex(window));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ReaderLinkOverlay_ExistsWithoutReplacingSelectionOverlay()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                SetNavigationSeams(window, Array.Empty<PdfBookmarkNode>(), new[]
                {
                    new PdfPageLink(0, new PdfTextRect(20, 20, 80, 40), PdfLinkActionKind.InternalGoto, 0, null)
                });
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                Assert.NotNull(Element<Canvas>(window, "ReaderLinkOverlayCanvas"));
                Assert.NotNull(Element<Canvas>(window, "ReaderSelectionHighlightCanvas"));
            }
            finally { CloseClean(window); }
        });
    }

    private static void SetNavigationSeams(
        MainWindow window,
        IReadOnlyList<PdfBookmarkNode> bookmarks,
        IReadOnlyList<PdfPageLink> links)
    {
        SetField(window, "_getReaderBookmarks",
            (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfBookmarkNode>>)((_, _) => bookmarks));
        SetField(window, "_getReaderPageLinks",
            (Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfPageLink>>)((_, _, _) => links));
    }

    private static async Task<bool> OpenReaderAsync(MainWindow window, string path)
    {
        EnsureLoaded(window);
        SetField(window, "_getReaderPageSizes",
            (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) => session.GetPageSizes(token)));
        SetField(window, "_renderReaderPage",
            (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => SyntheticRendered(index, dpi)));
        return await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
    }

    private static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi)
    {
        var pixels = new byte[8 * 8 * 4];
        Array.Fill(pixels, (byte)77);
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 400d, 37.5d, 0d, 0d, -50d, 8, 8)
        };
    }

    private static int CurrentPageIndex(MainWindow window)
    {
        var navigation = GetField(window, "_navigation");
        Assert.NotNull(navigation);
        var property = navigation.GetType().GetProperty("CurrentPageIndex", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return (int)property.GetValue(navigation)!;
    }

    private static T Element<T>(MainWindow window, string name) where T : class
        => Assert.IsType<T>(window.FindName(name));

    private static void EnsureLoaded(MainWindow window)
        => window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, window));

    private static object? GetField(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window);
    }

    private static void SetField(MainWindow window, string name, object? value)
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
        if (GetField(window, "_session") is PdfDocumentSession session)
        {
            session.Dispose();
            SetField(window, "_session", null);
        }
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

        internal static PdfFixture Create(int pageCount)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-reader-links-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            var document = new PdfDocument();
            for (var index = 0; index < pageCount; index++)
            {
                var page = document.AddPage();
                page.Width = PdfSharp.Drawing.XUnit.FromPoint(300d);
                page.Height = PdfSharp.Drawing.XUnit.FromPoint(400d);
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
