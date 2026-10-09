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
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowReaderShortcutTests
{
    [Fact]
    public void CtrlOpenAndPrint_DispatchExistingActions()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(3);
            var window = new MainWindow();
            var openCalls = 0;
            var printCalls = 0;
            try
            {
                SetField(window, "_recentPdfStore", new RecentPdfStore(Path.Combine(fixture.DirectoryPath, "recents")));
                SetField(window, "_openPdfShortcutAction", (Action)(() => openCalls++));
                SetField(window, "_printPdfShortcutAction", (Action)(() => printCalls++));

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.O, ModifierKeys.Control, null));
                Assert.Equal(1, openCalls);

                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.P, ModifierKeys.Control, null));
                Assert.Equal(1, printCalls);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void DirtySignature_CtrlOpenCancelWinsBeforeOpenAction()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            var openCalls = 0;
            try
            {
                var state = new SignatureEditState(0, new PdfRect(0d, 0d, 300d, 400d));
                state.AddCentered(CreateSignatureAsset());
                SetField(window, "_signatureEditState", state);
                SetField(window, "_signatureModeActive", true);
                SetField(window, "_resolvePendingSignatureDecision",
                    (Func<SignatureGuardReason, PendingSignatureDecision>)(_ => PendingSignatureDecision.Cancel));
                SetField(window, "_openPdfShortcutAction", (Action)(() => openCalls++));

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.O, ModifierKeys.Control, null));

                Assert.Equal(0, openCalls);
                Assert.True(state.IsDirty);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void HomeEnd_GoToFirstAndLastPageInContinuousReader()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(5);
            var window = new MainWindow();
            try
            {
                SetField(window, "_recentPdfStore", new RecentPdfStore(Path.Combine(fixture.DirectoryPath, "recents")));
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                Invoke(window, "ScrollReaderToPage", 2);

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.End, ModifierKeys.None, null));
                Assert.Equal(4, GetNavigation(window)!.CurrentPageIndex);

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.Home, ModifierKeys.None, null));
                Assert.Equal(0, GetNavigation(window)!.CurrentPageIndex);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void PageUpPageDown_MoveExactlyOneViewportHeight()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(8);
            var window = new MainWindow();
            try
            {
                SetField(window, "_recentPdfStore", new RecentPdfStore(Path.Combine(fixture.DirectoryPath, "recents")));
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));
                var list = Assert.IsType<ListBox>(window.FindName("ReaderPageList"));
                list.Height = 280d;
                list.Measure(new Size(800d, 280d));
                list.Arrange(new Rect(0d, 0d, 800d, 280d));
                var geometry = GetGeometry(window);
                var start = geometry[2].Top;
                SetField(window, "_readerVerticalOffset", start);

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.PageDown, ModifierKeys.None, null));
                Assert.Equal(start + 280d, (double)GetField(window, "_readerVerticalOffset")!, 3);

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.PageUp, ModifierKeys.None, null));
                Assert.Equal(start, (double)GetField(window, "_readerVerticalOffset")!, 3);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ZoomShortcuts_UseExistingZoomStateAndActualSize()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                SetField(window, "_recentPdfStore", new RecentPdfStore(Path.Combine(fixture.DirectoryPath, "recents")));
                Assert.True(await OpenReaderAsync(window, fixture.SourcePath));

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.OemPlus, ModifierKeys.Control, null));
                PumpUntil(() => ((PdfZoomState)GetField(window, "_zoom")!).ManualPercent == 125);

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.OemMinus, ModifierKeys.Control, null));
                PumpUntil(() => ((PdfZoomState)GetField(window, "_zoom")!).ManualPercent == 100);

                Assert.True(InvokeBool(window, "TryHandleReaderShortcut", Key.D0, ModifierKeys.Control, null));
                Assert.Equal(100, ((PdfZoomState)GetField(window, "_zoom")!).ManualPercent);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void EditableControls_AreNotClaimedByGlobalReaderShortcuts()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                Assert.False(InvokeBool(window, "TryHandleReaderShortcut", Key.Home, ModifierKeys.None, new TextBox()));
                Assert.False(InvokeBool(window, "TryHandleReaderShortcut", Key.End, ModifierKeys.None, new PasswordBox()));
                Assert.False(InvokeBool(window, "TryHandleReaderShortcut", Key.PageDown, ModifierKeys.None, new ComboBox { IsEditable = true }));
                Assert.False(InvokeBool(window, "TryHandleReaderShortcut", Key.O, ModifierKeys.Control, new TextBox()));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void SearchAndCopyHandlers_DoNotOverrideEditableTextBox()
    {
        RunInSta(async () =>
        {
            var window = new MainWindow();
            try
            {
                var editor = new TextBox();
                Assert.False(await InvokeTask<bool>(window, "HandleReaderSearchKeyAsync", Key.F, ModifierKeys.Control, editor));
                Assert.False(InvokeBool(window, "HandleReaderSelectionKey", Key.C, ModifierKeys.Control, editor));
            }
            finally { CloseClean(window); }
        });
    }

    private static async Task<bool> OpenReaderAsync(MainWindow window, string path)
    {
        SetField(window, "_renderReaderPage",
            (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => SyntheticRendered(index, dpi)));
        return await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, null);
    }

    private static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi)
    {
        var pixels = new byte[8 * 8 * 4];
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 8, 8)
        };
    }

    private static SignatureAsset CreateSignatureAsset()
    {
        var pixels = new byte[80 * 40 * 4];
        for (var index = 3; index < pixels.Length; index += 4)
            pixels[index] = 255;
        pixels[3] = 0;
        return new SignatureAsset(80, 40, 320, pixels, "shortcut-signature.png");
    }

    private static IReadOnlyList<ReaderPageGeometry> GetGeometry(MainWindow window)
        => (IReadOnlyList<ReaderPageGeometry>)GetField(window, "_readerGeometry")!;

    private static SGPdf.App.Navigation.PageNavigationState? GetNavigation(MainWindow window)
        => (SGPdf.App.Navigation.PageNavigationState?)GetField(window, "_navigation");

    private static bool InvokeBool(MainWindow window, string name, params object?[] args)
        => (bool)Invoke(window, name, args)!;

    private static object? Invoke(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == name && item.GetParameters().Length == args.Length);
        return method.Invoke(window, args);
    }

    private static async Task<T> InvokeTask<T>(MainWindow window, string name, params object?[] args)
        => await (Task<T>)Invoke(window, name, args)!;

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
            task.ContinueWith(_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)), TaskScheduler.Default);
            Dispatcher.PushFrame(frame);
        }
        task.GetAwaiter().GetResult();
    }

    private static void CloseClean(MainWindow window)
    {
        if (typeof(MainWindow).GetField("_signatureEditState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window) is SignatureEditState state)
            state.DiscardAll();
        window.Close();
        if (typeof(MainWindow).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window) is PdfDocumentSession session)
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
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-shortcuts-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            using var document = new PdfDocument();
            for (var index = 0; index < pageCount; index++)
                document.AddPage();
            document.Save(source);
            return new PdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
    }
}
