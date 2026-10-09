using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Features.Reader;
using SGPdf.App.Features.Sign;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowReaderSearchTests
{
    [Fact]
    public void CtrlF_OpensAndFocusesFindBar()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                Assert.True(await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control));

                Assert.Equal(Visibility.Visible, Element<Border>(window, "ReaderFindBar").Visibility);
                Assert.Same(Element<TextBox>(window, "ReaderFindTextBox"), FocusManager.GetFocusedElement(window));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void EnterNext_ShiftEnterPrevious_NavigatesWithinQueryCursor()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                SetFinder(window, (page, _, _) => page == 0
                    ? new[] { Match(0, 2), Match(0, 9) }
                    : Array.Empty<PdfTextMatch>());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control);
                Element<TextBox>(window, "ReaderFindTextBox").Text = "needle";

                Assert.True(await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.Enter, ModifierKeys.None));
                Assert.Equal(0, GetCursor(window)!.MatchIndexWithinPage);
                Assert.True(await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.Enter, ModifierKeys.None));
                Assert.Equal(1, GetCursor(window)!.MatchIndexWithinPage);
                Assert.True(await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.Enter, ModifierKeys.Shift));
                Assert.Equal(0, GetCursor(window)!.MatchIndexWithinPage);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void Escape_ClosesFindBar()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control);

                Assert.True(await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.Escape, ModifierKeys.None));

                Assert.Equal(Visibility.Collapsed, Element<Border>(window, "ReaderFindBar").Visibility);
                Assert.Empty(Element<Canvas>(window, "ReaderSearchHighlightCanvas").Children);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ActiveMatch_NavigatesAndHighlightsOnlyActiveResult()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                SetFinder(window, (page, _, _) => page switch
                {
                    1 => new[] { Match(1, 5, 2) },
                    2 => new[] { Match(2, 8, 1) },
                    _ => Array.Empty<PdfTextMatch>()
                });
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control);
                Element<TextBox>(window, "ReaderFindTextBox").Text = "needle";

                await InvokeTask(window, "RunReaderSearchAsync", ReaderSearchDirection.Forward);
                Assert.Equal(1, GetNavigation(window)!.CurrentPageIndex);
                Assert.Equal(2, Element<Canvas>(window, "ReaderSearchHighlightCanvas").Children.Count);

                await InvokeTask(window, "RunReaderSearchAsync", ReaderSearchDirection.Forward);
                Assert.Equal(2, GetNavigation(window)!.CurrentPageIndex);
                Assert.Single(Element<Canvas>(window, "ReaderSearchHighlightCanvas").Children);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void NoResults_ShowsControlledState()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                SetFinder(window, (_, _, _) => Array.Empty<PdfTextMatch>());
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control);
                Element<TextBox>(window, "ReaderFindTextBox").Text = "missing";

                await InvokeTask(window, "RunReaderSearchAsync", ReaderSearchDirection.Forward);

                Assert.Equal("Sin resultados", Element<TextBlock>(window, "ReaderFindStatusText").Text);
                Assert.Empty(Element<Canvas>(window, "ReaderSearchHighlightCanvas").Children);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void QueryGeneration_RejectsStaleResult()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(3);
            var window = new MainWindow();
            using var firstStarted = new ManualResetEventSlim(false);
            using var releaseFirst = new ManualResetEventSlim(false);
            try
            {
                SetFinder(window, (page, query, _) =>
                {
                    if (query == "first")
                    {
                        firstStarted.Set();
                        releaseFirst.Wait(TimeSpan.FromSeconds(5));
                        return page == 2 ? new[] { Match(2, 7) } : Array.Empty<PdfTextMatch>();
                    }

                    return query == "second" && page == 1
                        ? new[] { Match(1, 3) }
                        : Array.Empty<PdfTextMatch>();
                });
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control);
                var box = Element<TextBox>(window, "ReaderFindTextBox");
                box.Text = "first";

                var stale = (Task)Invoke(window, "RunReaderSearchAsync", ReaderSearchDirection.Forward)!;
                Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(3)));
                box.Text = "second";
                await InvokeTask(window, "RunReaderSearchAsync", ReaderSearchDirection.Forward);
                releaseFirst.Set();
                await stale;

                Assert.Equal("second", GetCursor(window)!.Query);
                Assert.Equal(1, GetNavigation(window)!.CurrentPageIndex);
            }
            finally
            {
                releaseFirst.Set();
                CloseClean(window);
            }
        });
    }

    [Fact]
    public void EnterFirmar_HidesReaderSearchUiWithoutChangingSignatureState()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control);
                var state = new SignatureEditState(0, new PdfRect(0d, 0d, 300d, 400d));
                SetField(window, "_signatureEditState", state);

                Element<Button>(window, "SignModeButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                PumpUntil(() => (bool)GetField(window, "_signatureModeActive")!);

                Assert.Equal(Visibility.Collapsed, Element<Border>(window, "ReaderFindBar").Visibility);
                Assert.Same(state, GetField(window, "_signatureEditState"));
            }
            finally { CloseClean(window); }
        });
    }

    private static PdfTextMatch Match(int pageIndex, int startIndex, int rectCount = 1)
        => new(
            pageIndex,
            startIndex,
            6,
            Enumerable.Range(0, rectCount)
                .Select(index => new PdfTextRect(20d + (index * 12d), 40d, 30d + (index * 12d), 52d))
                .ToArray());

    private static void SetFinder(
        MainWindow window,
        Func<int, string, CancellationToken, IReadOnlyList<PdfTextMatch>> finder)
        => SetField(window, "_findReaderTextOnPage",
            (Func<PdfDocumentSession, int, string, CancellationToken, IReadOnlyList<PdfTextMatch>>)((_, page, query, token) =>
                finder(page, query, token)));

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
        Array.Fill(pixels, (byte)33);
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 400d, 37.5d, 0d, 0d, -50d, 8, 8)
        };
    }

    private static ReaderSearchCursor? GetCursor(MainWindow window)
        => (ReaderSearchCursor?)GetField(window, "_readerSearchCursor");

    private static PageNavigationState? GetNavigation(MainWindow window)
        => (PageNavigationState?)GetField(window, "_navigation");

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
        if (GetField(window, "_signatureEditState") is SignatureEditState state)
            state.DiscardAll();
        window.Close();
        if (GetField(window, "_session") is PdfDocumentSession session)
        {
            session.Dispose();
            SetField(window, "_session", null!);
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
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-reader-search-{Guid.NewGuid():N}");
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
