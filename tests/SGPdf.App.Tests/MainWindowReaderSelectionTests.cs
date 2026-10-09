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
using SGPdf.App.Pdf;
using Rectangle = System.Windows.Shapes.Rectangle;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowReaderSelectionTests
{
    [Fact]
    public void OnePageDrag_CreatesSelectionAndRectOverlay()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetRects(window, (_, _, _, _) => new[]
                {
                    new PdfTextRect(20d, 40d, 60d, 52d),
                    new PdfTextRect(65d, 40d, 90d, 52d)
                });

                Assert.True((bool)Invoke(window, "BeginReaderTextSelection", 0, 3)!);
                Assert.True((bool)Invoke(window, "CompleteReaderTextSelection", 0, 7)!);

                var selection = Assert.IsType<ReaderTextSelection>(GetField(window, "_readerTextSelection"));
                Assert.Equal(0, selection.PageIndex);
                Assert.Equal(3, selection.StartIndex);
                Assert.Equal(5, selection.CharacterCount);
                Assert.Equal(2, Element<Canvas>(window, "ReaderSelectionHighlightCanvas").Children.Count);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void CrossPageDrag_DoesNotCreateSelection()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetRects(window, (_, _, _, _) => new[] { new PdfTextRect(20d, 40d, 60d, 52d) });

                Assert.True((bool)Invoke(window, "BeginReaderTextSelection", 0, 3)!);
                Assert.False((bool)Invoke(window, "CompleteReaderTextSelection", 1, 7)!);

                Assert.Null(GetField(window, "_readerTextSelection"));
                Assert.Empty(Element<Canvas>(window, "ReaderSelectionHighlightCanvas").Children);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ClickElsewhere_ClearsSelection()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetRects(window, (_, _, _, _) => new[] { new PdfTextRect(20d, 40d, 60d, 52d) });
                Invoke(window, "BeginReaderTextSelection", 0, 2);
                Invoke(window, "CompleteReaderTextSelection", 0, 5);
                Assert.NotNull(GetField(window, "_readerTextSelection"));

                Assert.False((bool)Invoke(window, "BeginReaderTextSelection", -1, -1)!);

                Assert.Null(GetField(window, "_readerTextSelection"));
                Assert.Empty(Element<Canvas>(window, "ReaderSelectionHighlightCanvas").Children);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void Zoom_ReprojectsSelectionFromPdfRects()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetRects(window, (_, _, _, _) => new[] { new PdfTextRect(20d, 40d, 60d, 52d) });
                Invoke(window, "BeginReaderTextSelection", 0, 2);
                Invoke(window, "CompleteReaderTextSelection", 0, 5);
                var canvas = Element<Canvas>(window, "ReaderSelectionHighlightCanvas");
                var before = Assert.IsType<Rectangle>(Assert.Single(canvas.Children));
                var beforeWidth = before.Width;

                var pages = (IList)GetField(window, "_readerPages")!;
                var page = Assert.IsType<ReaderPageItem>(pages[0]);
                var geometry = page.Geometry;
                page.ApplyGeometry(geometry with
                {
                    DisplayWidth = geometry.DisplayWidth * 1.5d,
                    DisplayHeight = geometry.DisplayHeight * 1.5d
                });
                Invoke(window, "UpdateReaderSelectionOverlay");

                var after = Assert.IsType<Rectangle>(Assert.Single(canvas.Children));
                Assert.True(after.Width > beforeWidth);
                Assert.Equal(5, Assert.IsType<ReaderTextSelection>(GetField(window, "_readerTextSelection")).CharacterCount);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void CtrlC_CopiesExactUnicodeSelection()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            string? copied = null;
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetRects(window, (_, _, _, _) => new[] { new PdfTextRect(20d, 40d, 60d, 52d) });
                SetField(window, "_getReaderTextRange",
                    (Func<PdfDocumentSession, int, int, int, CancellationToken, string>)((_, _, _, _, _) => "mañana útil"));
                SetField(window, "_setReaderClipboardText", (Action<string>)(text => copied = text));
                Invoke(window, "BeginReaderTextSelection", 0, 2);
                Invoke(window, "CompleteReaderTextSelection", 0, 5);

                Assert.True((bool)Invoke(window, "HandleReaderSelectionKey", Key.C, ModifierKeys.Control)!);
                Assert.Equal("mañana útil", copied);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void CtrlC_WithoutSelection_DoesNothing()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            var calls = 0;
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetField(window, "_setReaderClipboardText", (Action<string>)(_ => calls++));

                Assert.False((bool)Invoke(window, "HandleReaderSelectionKey", Key.C, ModifierKeys.Control)!);
                Assert.Equal(0, calls);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void EnterFirmar_ClearsReaderSelectionOnly()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                SetRects(window, (_, _, _, _) => new[] { new PdfTextRect(20d, 40d, 60d, 52d) });
                Invoke(window, "BeginReaderTextSelection", 0, 2);
                Invoke(window, "CompleteReaderTextSelection", 0, 5);
                var signatureState = new SignatureEditState(0, new PdfRect(0d, 0d, 300d, 400d));
                SetField(window, "_signatureEditState", signatureState);

                Element<Button>(window, "SignModeButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                PumpUntil(() => (bool)GetField(window, "_signatureModeActive")!);

                Assert.Null(GetField(window, "_readerTextSelection"));
                Assert.Empty(Element<Canvas>(window, "ReaderSelectionHighlightCanvas").Children);
                Assert.Same(signatureState, GetField(window, "_signatureEditState"));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void SelectionDrag_TakesPrecedenceOverLinkActivation()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                Assert.True((bool)Invoke(window, "BeginReaderTextSelection", 0, 4)!);
                Assert.True((bool)GetField(window, "_readerSelectionGestureActive")!);
            }
            finally { CloseClean(window); }
        });
    }

    private static void SetRects(
        MainWindow window,
        Func<int, int, int, CancellationToken, IReadOnlyList<PdfTextRect>> provider)
        => SetField(window, "_getReaderTextRangeRects",
            (Func<PdfDocumentSession, int, int, int, CancellationToken, IReadOnlyList<PdfTextRect>>)((_, page, start, count, token) =>
                provider(page, start, count, token)));

    private static async Task<bool> OpenReaderAsync(MainWindow window, string path)
    {
        EnsureLoaded(window);
        SetField(window, "_getReaderPageSizes",
            (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) => session.GetPageSizes(token)));
        SetField(window, "_renderReaderPage",
            (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => SyntheticRendered(index, dpi)));
        SetOptionalField(window, "_queueThumbnailRefreshOverride", (Action)(() => { }));
        return await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
    }

    private static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi)
    {
        var pixels = new byte[8 * 8 * 4];
        Array.Fill(pixels, (byte)55);
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 400d, 37.5d, 0d, 0d, -50d, 8, 8)
        };
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

    private static void SetOptionalField(MainWindow window, string name, object value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(window, value);
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
        if (GetField(window, "_signatureEditState") is SignatureEditState state)
            state.DiscardAll();
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
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-reader-selection-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = System.IO.Path.Combine(directory, "source.pdf");
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
