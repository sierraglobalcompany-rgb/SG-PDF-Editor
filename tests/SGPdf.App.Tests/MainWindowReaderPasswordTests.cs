using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PdfSharp.Pdf;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowReaderPasswordTests
{
    [Fact]
    public void PasswordWrongOrCancel_PreservesExistingWorkspace()
    {
        RunInSta(async () =>
        {
            using var existing = PdfFixture.Create(false);
            using var protectedPdf = PdfFixture.Create(true);
            var window = new MainWindow();
            try
            {
                ConfigureReader(window);
                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", existing.Path, null));
                var priorSession = GetSession(window);
                var priorNavigation = GetNavigation(window);
                var priorTitle = window.Title;
                var prompts = new Queue<string?>(new string?[] { "wrong", null });
                SetField(window, "_requestPdfPassword",
                    (Func<Window, string, string?>)((_, _) => prompts.Dequeue()));

                Assert.False(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", protectedPdf.Path, null));

                Assert.Same(priorSession, GetSession(window));
                Assert.Same(priorNavigation, GetNavigation(window));
                Assert.Equal(priorTitle, window.Title);
                Assert.Empty(prompts);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void PasswordRetryCorrect_PublishesCandidateOnce()
    {
        RunInSta(async () =>
        {
            using var existing = PdfFixture.Create(false);
            using var protectedPdf = PdfFixture.Create(true);
            var window = new MainWindow();
            var metricCalls = 0;
            try
            {
                ConfigureReader(window);
                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", existing.Path, null));
                var oldSession = GetSession(window);
                SetField(window, "_getReaderPageSizes",
                    (Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>>)((session, token) =>
                    {
                        metricCalls++;
                        return session.GetPageSizes(token);
                    }));
                var prompts = new Queue<string?>(new string?[] { "wrong", "secret" });
                SetField(window, "_requestPdfPassword",
                    (Func<Window, string, string?>)((_, _) => prompts.Dequeue()));

                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", protectedPdf.Path, null));

                Assert.Equal(1, metricCalls);
                Assert.NotSame(oldSession, GetSession(window));
                Assert.Equal(System.IO.Path.GetFullPath(protectedPdf.Path), GetSession(window)!.FilePath);
                Assert.Empty(prompts);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void PasswordValue_IsNotStoredInStatusTitleOrPersistentState()
    {
        RunInSta(async () =>
        {
            using var protectedPdf = PdfFixture.Create(true);
            var window = new MainWindow();
            const string password = "secret";
            try
            {
                ConfigureReader(window);
                SetField(window, "_requestPdfPassword",
                    (Func<Window, string, string?>)((_, _) => password));

                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", protectedPdf.Path, null));

                Assert.DoesNotContain(password, window.Title, StringComparison.Ordinal);
                Assert.DoesNotContain(password, GetStatusText(window), StringComparison.Ordinal);
                var session = GetSession(window)!;
                var stringFields = typeof(PdfDocumentSession)
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => field.FieldType == typeof(string))
                    .Select(field => (string?)field.GetValue(session))
                    .Where(value => value is not null)
                    .ToArray();
                Assert.DoesNotContain(stringFields, value => string.Equals(value, password, StringComparison.Ordinal));
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

    private static string GetStatusText(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("StatusText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field?.GetValue(window) is System.Windows.Controls.TextBlock text)
            return text.Text;
        var property = typeof(MainWindow).GetProperty("StatusText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return property?.GetValue(window)?.ToString() ?? string.Empty;
    }

    private static PdfDocumentSession? GetSession(MainWindow window)
        => (PdfDocumentSession?)GetField(window, "_session");

    private static PageNavigationState? GetNavigation(MainWindow window)
        => (PageNavigationState?)GetField(window, "_navigation");

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
        if (GetField(window, "_session") is PdfDocumentSession session)
        {
            session.Dispose();
            SetField(window, "_session", null!);
        }
    }

    private sealed class PdfFixture : IDisposable
    {
        private PdfFixture(string root, string path)
        {
            Root = root;
            Path = path;
        }

        internal string Root { get; }
        internal string Path { get; }

        internal static PdfFixture Create(bool passwordProtected)
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-password-ui-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = System.IO.Path.Combine(root, passwordProtected ? "protected.pdf" : "existing.pdf");
            using var document = new PdfDocument();
            document.AddPage();
            if (passwordProtected)
            {
                document.SecuritySettings.UserPassword = "secret";
                document.SecuritySettings.OwnerPassword = "owner";
            }
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
