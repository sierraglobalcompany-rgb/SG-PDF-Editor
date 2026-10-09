using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using SGPdf.App.Features.Organize;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOrganizeTests
{
    [Fact]
    public void OrganizeSurface_IsPresentDisabledWithoutPdf_AndVirtualized()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var mode = OrganizeWindowTestHost.Element<Button>(window, "OrganizeModeButton");
                var surface = OrganizeWindowTestHost.Element<Grid>(window, "OrganizeSurface");
                var list = OrganizeWindowTestHost.Element<ListBox>(window, "OrganizePageList");

                Assert.False(mode.IsEnabled);
                Assert.Equal(Visibility.Collapsed, surface.Visibility);
                Assert.True(VirtualizingPanel.GetIsVirtualizing(list));
                Assert.Equal(VirtualizationMode.Recycling, VirtualizingPanel.GetVirtualizationMode(list));
                Assert.Equal(ScrollUnit.Pixel, VirtualizingPanel.GetScrollUnit(list));
                Assert.True(ScrollViewer.GetCanContentScroll(list));

                foreach (var name in new[]
                {
                    "OrganizeRotateLeftButton", "OrganizeRotateRightButton", "OrganizeDeleteButton",
                    "OrganizeDuplicateButton", "OrganizeInsertButton", "OrganizeMergeButton",
                    "OrganizeExtractButton", "OrganizeSplitButton", "OrganizeSaveAsButton"
                })
                {
                    Assert.False(OrganizeWindowTestHost.Element<Button>(window, name).IsEnabled);
                }
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void EnterAndLeaveOrganize_HidesOtherSurfaces_DiscardsPlan_AndRestoresReader()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);

                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<Grid>(window, "OrganizeSurface").Visibility);
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<Grid>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<ScrollViewer>(window, "PdfScrollViewer").Visibility);
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<Canvas>(window, "SignatureOverlayCanvas").Visibility);
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<Canvas>(window, "LabelSheetCanvas").Visibility);
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                Assert.Equal(4, OrganizeWindowTestHost.GetOrganizeItems(window).Count);

                OrganizeWindowTestHost.Invoke(window, "LeaveOrganizeMode");

                Assert.Null(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                Assert.Empty(OrganizeWindowTestHost.GetOrganizeItems(window));
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<Grid>(window, "OrganizeSurface").Visibility);
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<Grid>(window, "ReaderContinuousSurface").Visibility);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtySignature_CancelBlocksOrganizeTransition()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var state = new SignatureEditState(0, new PdfRect(0d, 0d, 300d, 400d));
                state.AddCentered(new SignatureAsset(1, 1, 4, new byte[4], "test"));
                OrganizeWindowTestHost.SetField(window, "_signatureEditState", state);
                OrganizeWindowTestHost.SetField(window, "_signatureModeActive", true);
                OrganizeWindowTestHost.SetField(window, "_resolvePendingSignatureDecision",
                    (Func<SignatureGuardReason, PendingSignatureDecision>)(_ => PendingSignatureDecision.Cancel));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_signatureModeActive")!);
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<Grid>(window, "OrganizeSurface").Visibility);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SignedCurrentSource_IsControlledBlock()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var result = new OrganizePreflightResult(new[]
                {
                    new OrganizeFinding(
                        OrganizeFindingKind.CryptographicSignature,
                        OrganizeFindingSeverity.Block,
                        "signed",
                        OrganizePreservationStatus.Unknown)
                });
                OrganizeWindowTestHost.SetField(window, "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => result));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                Assert.Contains("no se puede organizar", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void PasswordOpenedCurrentSource_IsControlledBlock()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizePdfFixtureFactory.CreateProtected();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path, "secret"));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                Assert.Contains("no se puede organizar", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }
}

internal sealed class OrganizeWindowPdfFixture : IDisposable
{
    private OrganizeWindowPdfFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    internal static OrganizeWindowPdfFixture Create(int pageCount)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-organize-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "source.pdf");
        using var document = new PdfDocument();
        for (var index = 0; index < pageCount; index++)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(300d + (index % 3) * 10d);
            page.Height = XUnit.FromPoint(400d + (index % 2) * 20d);
        }
        document.Save(path);
        return new OrganizeWindowPdfFixture(directory, path);
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal static class OrganizeWindowTestHost
{
    internal static async Task<bool> OpenReaderAsync(MainWindow window, string path, string? password = null)
    {
        EnsureLoaded(window);
        SetField(window, "_renderReaderPage",
            (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) => SyntheticRendered(index, dpi, 31)));
        SetField(window, "_thumbnailRefreshQueued", true);
        try
        {
            return await InvokeTask<bool>(window, "TryOpenPdfPathAsync", path, password);
        }
        finally
        {
            SetField(window, "_thumbnailRefreshQueued", false);
        }
    }

    internal static PdfRenderedPage SyntheticRendered(int pageIndex, double dpi, byte marker)
    {
        var pixels = new byte[8 * 8 * 4];
        Array.Fill(pixels, marker);
        return new PdfRenderedPage(pageIndex, 8, 8, 32, dpi, pixels)
        {
            DeviceTransform = new PdfPageDeviceTransform(marker, 400d, 1d, 0d, 0d, -1d, 8, 8)
        };
    }

    internal static byte ReadFirstPixelByte(System.Windows.Media.Imaging.BitmapSource bitmap)
    {
        var pixels = new byte[Math.Max(4, bitmap.PixelWidth * bitmap.PixelHeight * 4)];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        return pixels[0];
    }

    internal static void EnsureLoaded(MainWindow window)
        => window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, window));

    internal static IReadOnlyList<object> GetOrganizeItems(MainWindow window)
        => ((IEnumerable)GetField(window, "_organizePages")!).Cast<object>().ToArray();

    internal static T Element<T>(MainWindow window, string name) where T : class
        => Assert.IsType<T>(window.FindName(name));

    internal static object? GetField(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window);
    }

    internal static T GetProperty<T>(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return (T)property.GetValue(instance)!;
    }

    internal static void SetField(MainWindow window, string name, object? value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, value);
    }

    internal static object? Invoke(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == name && item.GetParameters().Length == args.Length);
        return method.Invoke(window, args);
    }

    internal static void InvokeOn(object instance, string name, params object?[] args)
    {
        var method = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(item => item.Name == name && item.GetParameters().Length == args.Length);
        method.Invoke(instance, args);
    }

    internal static async Task InvokeTask(MainWindow window, string name, params object?[] args)
        => await (Task)Invoke(window, name, args)!;

    internal static async Task<T> InvokeTask<T>(MainWindow window, string name, params object?[] args)
        => await (Task<T>)Invoke(window, name, args)!;

    internal static void RunInSta(Action action)
        => RunInSta(() => { action(); return Task.CompletedTask; });

    internal static void RunInSta(Func<Task> action)
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA test timed out.");
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal static void PumpUntil(Func<bool> predicate, int timeoutMilliseconds = 3000)
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

    internal static void CloseClean(MainWindow window)
    {
        try
        {
            SetField(window, "_signatureEditState", null);
            window.Close();
        }
        catch
        {
        }
    }

    private static void AwaitWithDispatcher(Task task)
    {
        if (task.IsCompleted)
        {
            task.GetAwaiter().GetResult();
            return;
        }

        var frame = new DispatcherFrame();
        task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        task.GetAwaiter().GetResult();
    }
}
