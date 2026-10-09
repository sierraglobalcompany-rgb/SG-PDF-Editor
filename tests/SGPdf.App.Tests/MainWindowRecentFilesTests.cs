using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowRecentFilesTests
{
    [Fact]
    public void ArchivoMenu_ContainsNamedRecentFilesControls()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                Assert.IsType<MenuItem>(window.FindName("RecentFilesMenuItem"));
                Assert.IsType<MenuItem>(window.FindName("ClearRecentFilesMenuItem"));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void SuccessfulPdfOpen_IsRecorded_ButFailedOpenIsNot()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create();
            using var recents = RecentFixture.Create();
            var store = new RecentPdfStore(recents.RootPath);
            var window = new MainWindow();
            try
            {
                ConfigureReader(window);
                SetField(window, "_recentPdfStore", store);

                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", fixture.SourcePath, null));
                var afterSuccess = store.Load();
                var entry = Assert.Single(afterSuccess);
                Assert.Equal(Path.GetFullPath(fixture.SourcePath), entry.FullPath);

                Assert.False(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", Path.Combine(fixture.DirectoryPath, "missing.pdf"), null));
                Assert.Equal(afterSuccess, store.Load());
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void RebuildRecentFilesMenu_ShowsStoredMissingAndUncPathsWithoutFiltering()
    {
        RunInSta(() =>
        {
            using var recents = RecentFixture.Create();
            var store = new RecentPdfStore(recents.RootPath);
            var localMissing = Path.Combine(recents.ContainerPath, "missing.pdf");
            const string uncMissing = @"\\server-that-should-not-be-contacted\share\missing.pdf";
            store.RecordSuccessfulOpen(localMissing, new DateTimeOffset(2026, 10, 8, 20, 0, 0, TimeSpan.Zero));
            store.RecordSuccessfulOpen(uncMissing, new DateTimeOffset(2026, 10, 8, 21, 0, 0, TimeSpan.Zero));
            var window = new MainWindow();
            try
            {
                SetField(window, "_recentPdfStore", store);
                Invoke(window, "RebuildRecentFilesMenu");

                var menu = Assert.IsType<MenuItem>(window.FindName("RecentFilesMenuItem"));
                var tagged = menu.Items.OfType<MenuItem>()
                    .Where(item => item.Tag is string)
                    .Select(item => (string)item.Tag!)
                    .ToArray();
                Assert.Equal(2, tagged.Length);
                Assert.Contains(uncMissing, tagged);
                Assert.Contains(Path.GetFullPath(localMissing), tagged);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ClearRecentFilesMenuItem_DeletesMetadataOnly()
    {
        RunInSta(() =>
        {
            using var recents = RecentFixture.Create();
            var source = Path.Combine(recents.ContainerPath, "keep.pdf");
            File.WriteAllText(source, "keep me");
            var store = new RecentPdfStore(recents.RootPath);
            store.RecordSuccessfulOpen(source, DateTimeOffset.UtcNow);
            var window = new MainWindow();
            try
            {
                SetField(window, "_recentPdfStore", store);
                Invoke(window, "RebuildRecentFilesMenu");
                var clear = Assert.IsType<MenuItem>(window.FindName("ClearRecentFilesMenuItem"));

                clear.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

                Assert.Empty(store.Load());
                Assert.True(File.Exists(source));
            }
            finally { CloseClean(window); }
        });
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

        internal static PdfFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-recent-ui-pdf-{Guid.NewGuid():N}");
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
                Directory.Delete(DirectoryPath, true);
        }
    }

    private sealed class RecentFixture : IDisposable
    {
        private RecentFixture(string containerPath, string rootPath)
        {
            ContainerPath = containerPath;
            RootPath = rootPath;
        }

        internal string ContainerPath { get; }
        internal string RootPath { get; }

        internal static RecentFixture Create()
        {
            var container = Path.Combine(Path.GetTempPath(), $"sgpdf-recent-ui-{Guid.NewGuid():N}");
            Directory.CreateDirectory(container);
            return new RecentFixture(container, Path.Combine(container, "store"));
        }

        public void Dispose()
        {
            if (Directory.Exists(ContainerPath))
                Directory.Delete(ContainerPath, true);
        }
    }
}
