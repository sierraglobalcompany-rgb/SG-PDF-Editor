using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
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
        RunInSta(async () =>
        {
            using var pdf = PdfFixture.Create();
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                ConfigureReader(window);
                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", pdf.Path, null));

                var tabs = Assert.IsType<System.Windows.Controls.TabControl>(
                    window.FindName("DocumentTabs"));
                Assert.Single(tabs.Items);
                // Instantiate the template through the real TabControl item generator,
                // not DataTemplate.LoadContent(): WPF seals templates during visual layout.
                tabs.ApplyTemplate();
                tabs.Measure(new Size(640d, 48d));
                tabs.Arrange(new Rect(0d, 0d, 640d, 48d));
                tabs.UpdateLayout();
                var tab = Assert.IsType<System.Windows.Controls.TabItem>(
                    tabs.ItemContainerGenerator.ContainerFromIndex(0));
                var icon = FindVisualDescendant<System.Windows.Controls.Image>(tab);
                Assert.NotNull(icon);
                var bitmap = Assert.IsType<BitmapImage>(icon.Source);

                // QA-V4-001: independent pack URI avoids the missing XAML BaseUri
                // of a code-built FrameworkElementFactory image.
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

    [Theory]
    [InlineData("merge")]
    [InlineData("split")]
    [InlineData("organize")]
    [InlineData("edit")]
    [InlineData("sign")]
    [InlineData("ocr")]
    [InlineData("redact")]
    [InlineData("protect")]
    [InlineData("compare")]
    [InlineData("convert")]
    [InlineData("watermark")]
    [InlineData("page-numbering")]
    public void HomePdfAction_CancelledPickerLeavesHomeWithoutOpeningPdf(string actionId)
    {
        RunInSta(async () =>
        {
            var window = new MainWindow();
            var pickerCalls = 0;
            try
            {
                RaiseLoaded(window);
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ =>
                {
                    pickerCalls++;
                    return null;
                }));

                Assert.False(await InvokeTask<bool>(window, "TryStartHomeActionAsync", actionId));
                Assert.Equal(1, pickerCalls);
                Assert.Equal("Home", GetShellKind(window));
                Assert.Empty(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void HomeImageToPdf_CancelledSelectionStagesDoNotAskForPdfOrCreateTab()
    {
        RunInSta(async () =>
        {
            var window = new MainWindow();
            var pdfPickerCalls = 0;
            var orderPickerCalls = 0;
            var destinationPickerCalls = 0;
            try
            {
                RaiseLoaded(window);
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ =>
                {
                    pdfPickerCalls++;
                    return null;
                }));
                SetField(window, "_selectImageToPdfSources", (Func<MainWindow, IReadOnlyList<string>?>)(_ => null));
                SetField(window, "_selectImageToPdfOrder", (Func<MainWindow, IReadOnlyList<string>, IReadOnlyList<string>?>)((_, _) =>
                {
                    orderPickerCalls++;
                    return null;
                }));
                SetField(window, "_selectImageToPdfDestination", (Func<MainWindow, string?>)(_ =>
                {
                    destinationPickerCalls++;
                    return null;
                }));

                Assert.True(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "image-to-pdf"));
                Assert.Equal(0, pdfPickerCalls);
                Assert.Equal(0, orderPickerCalls);
                Assert.Equal(0, destinationPickerCalls);

                SetField(window, "_selectImageToPdfSources", (Func<MainWindow, IReadOnlyList<string>?>)(_ => ["first.png", "second.jpg"]));
                Assert.True(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "image-to-pdf"));
                Assert.Equal(0, pdfPickerCalls);
                Assert.Equal(1, orderPickerCalls);
                Assert.Equal(0, destinationPickerCalls);

                SetField(window, "_selectImageToPdfOrder", (Func<MainWindow, IReadOnlyList<string>, IReadOnlyList<string>?>)((_, paths) =>
                {
                    orderPickerCalls++;
                    return paths;
                }));
                Assert.True(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "image-to-pdf"));
                Assert.Equal(0, pdfPickerCalls);
                Assert.Equal(2, orderPickerCalls);
                Assert.Equal(1, destinationPickerCalls);
                Assert.Equal("Home", GetShellKind(window));
                Assert.Empty(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
                Assert.Null(GetField(window, "_session"));
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void ImageToPdfOrderDialog_MovesSelectedImageAndNamesItsControls()
    {
        RunInSta(() =>
        {
            var dialogType = typeof(MainWindow).Assembly.GetType(
                "SGPdf.App.Features.Conversions.ImageToPdfOrderDialog",
                throwOnError: true)!;
            var dialog = (Window)Activator.CreateInstance(
                dialogType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { new[] { "first.png", "second.jpg", "third.jpeg" } },
                culture: null)!;
            try
            {
                var list = Assert.IsType<ListBox>(dialog.FindName("ImageOrderListBox"));
                var moveUp = Assert.IsType<Button>(dialog.FindName("MoveUpButton"));
                list.SelectedIndex = 2;
                Assert.True(moveUp.IsEnabled);
                Assert.Equal("Subir imagen en el orden", AutomationProperties.GetName(moveUp));

                var moveHandler = dialogType.GetMethod("MoveUp_Click", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(moveHandler);
                moveHandler!.Invoke(dialog, [moveUp, new RoutedEventArgs(Button.ClickEvent)]);

                var orderedPaths = dialogType.GetProperty("OrderedPaths", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(orderedPaths);
                Assert.Equal(
                    new[] { "first.png", "third.jpeg", "second.jpg" },
                    Assert.IsAssignableFrom<IReadOnlyList<string>>(orderedPaths!.GetValue(dialog)));
            }
            finally { dialog.Close(); }
        });
    }

    [Fact]
    public void HomeImageToPdf_CancelledSelectionPreservesAnOpenPdfSession()
    {
        RunInSta(async () =>
        {
            using var fixture = PdfFixture.Create();
            var window = new MainWindow();
            var pdfPickerCalls = 0;
            try
            {
                RaiseLoaded(window);
                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", fixture.Path, null));
                object activeDocument = window.ActiveDocument!;
                object session = GetField(window, "_session")!;
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ =>
                {
                    pdfPickerCalls++;
                    return null;
                }));
                SetField(window, "_selectImageToPdfSources", (Func<MainWindow, IReadOnlyList<string>?>)(_ => null));

                Assert.True(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "image-to-pdf"));

                Assert.Equal(0, pdfPickerCalls);
                Assert.Same(activeDocument, window.ActiveDocument);
                Assert.Same(session, GetField(window, "_session"));
                Assert.Equal(fixture.Path, Assert.IsType<PdfDocumentSession>(GetField(window, "_session")).FilePath);
                Assert.Single(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void HomeMerge_UsesExistingOrganizePlanAndAddsSecondaryPdf()
    {
        RunInSta(async () =>
        {
            using var primary = PdfFixture.Create();
            using var secondary = PdfFixture.Create();
            var window = new MainWindow();
            var primaryPickCount = 0;
            var secondaryPickCount = 0;
            try
            {
                RaiseLoaded(window);
                ConfigureReader(window);
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ =>
                {
                    primaryPickCount++;
                    return primary.Path;
                }));
                SetField(window, "_selectOrganizePdfSource", (Func<string?>)(() =>
                {
                    secondaryPickCount++;
                    return secondary.Path;
                }));

                Assert.True(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "merge"));
                Assert.Equal(1, primaryPickCount);
                Assert.Equal(1, secondaryPickCount);
                Assert.Equal("Pdf", GetShellKind(window));
                Assert.Single(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
                var organizeMode = typeof(MainWindow).GetField("_organizeModeActive", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(organizeMode);
                Assert.True((bool)organizeMode.GetValue(window)!);
                var planField = typeof(MainWindow).GetField("_organizePlan", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(planField);
                var plan = Assert.IsType<SGPdf.App.Features.Organize.OrganizePlan>(planField.GetValue(window));
                Assert.Equal(2, plan.Pages.Count);
            }
            finally
            {
                SetField(window, "_confirmDiscardOrganizeChanges", (Func<bool>)(() => true));
                CloseClean(window);
            }
        });
    }

    [Fact]
    public void HomeMerge_CancelSecondaryKeepsOpenedPrimaryAndItsUnchangedPlan()
    {
        RunInSta(async () =>
        {
            using var primary = PdfFixture.Create();
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                ConfigureReader(window);
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ => primary.Path));
                SetField(window, "_selectOrganizePdfSource", (Func<string?>)(() => null));

                Assert.False(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "merge"));
                Assert.Equal("Pdf", GetShellKind(window));
                Assert.Single(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
                var planField = typeof(MainWindow).GetField("_organizePlan", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(planField);
                var plan = Assert.IsType<SGPdf.App.Features.Organize.OrganizePlan>(planField.GetValue(window));
                Assert.Single(plan.Pages);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void HomeOrganize_OpensSelectedPdfAndReusesExistingOwnerOnce()
    {
        RunInSta(async () =>
        {
            using var pdf = PdfFixture.Create();
            var window = new MainWindow();
            var pickerCalls = 0;
            bool? reentrantRequestStarted = null;
            try
            {
                RaiseLoaded(window);
                ConfigureReader(window);
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ =>
                {
                    pickerCalls++;
                    reentrantRequestStarted = InvokeTask<bool>(window, "TryStartHomeActionAsync", "organize")
                        .GetAwaiter().GetResult();
                    return pdf.Path;
                }));

                Assert.True(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "organize"));
                Assert.False(reentrantRequestStarted);
                Assert.Equal(1, pickerCalls);
                Assert.Equal("Pdf", GetShellKind(window));
                Assert.Single(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
                var organizeMode = typeof(MainWindow).GetField("_organizeModeActive", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(organizeMode);
                Assert.True((bool)organizeMode.GetValue(window)!);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void HomeOrganize_WithActivePdfSkipsPickerAndDoesNotDuplicateTab()
    {
        RunInSta(async () =>
        {
            using var pdf = PdfFixture.Create();
            var window = new MainWindow();
            var pickerCalls = 0;
            try
            {
                RaiseLoaded(window);
                ConfigureReader(window);
                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", pdf.Path, null));
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ =>
                {
                    pickerCalls++;
                    return null;
                }));

                Assert.True(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "organize"));
                Assert.Equal(0, pickerCalls);
                Assert.Equal("Pdf", GetShellKind(window));
                Assert.Single(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
                var organizeMode = typeof(MainWindow).GetField("_organizeModeActive", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(organizeMode);
                Assert.True((bool)organizeMode.GetValue(window)!);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void HomeUnknownActionDoesNotOpenPickerOrChangeWorkspace()
    {
        RunInSta(async () =>
        {
            var window = new MainWindow();
            var pickerCalls = 0;
            try
            {
                RaiseLoaded(window);
                SetField(window, "_selectHomePdfPath", (Func<MainWindow, string?>)(_ =>
                {
                    pickerCalls++;
                    return null;
                }));

                Assert.False(await InvokeTask<bool>(window, "TryStartHomeActionAsync", "unknown-action"));
                Assert.Equal(0, pickerCalls);
                Assert.Equal("Home", GetShellKind(window));
                Assert.Empty(Assert.IsType<TabControl>(window.FindName("DocumentTabs")).Items);
            }
            finally { CloseClean(window); }
        });
    }

    [Fact]
    public void SameNamedPdfs_PreserveIndependentReaderDrawerVisibility()
    {
        RunInSta(async () =>
        {
            using var a = PdfFixture.Create();
            using var b = PdfFixture.Create();
            Assert.NotEqual(a.Path, b.Path);
            Assert.Equal(Path.GetFileName(a.Path), Path.GetFileName(b.Path));

            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                ConfigureReader(window);
                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", a.Path, null));
                var first = Assert.IsType<SGPdf.App.Shell.PdfDocumentWorkspace>(window.ActiveDocument);
                var toggle = Assert.IsType<Button>(window.FindName("ReaderNavigationToggleButton"));
                Assert.Equal(Visibility.Collapsed,
                    Assert.IsType<Border>(window.FindName("ReaderNavigationDrawer")).Visibility);

                toggle.RaiseEvent(new RoutedEventArgs(
                    System.Windows.Controls.Primitives.ButtonBase.ClickEvent, toggle));
                Assert.True(first.IsReaderNavigationOpen);
                Assert.Equal(Visibility.Visible,
                    Assert.IsType<Border>(window.FindName("ReaderNavigationDrawer")).Visibility);

                Assert.True(await InvokeTask<bool>(window, "TryOpenPdfPathAsync", b.Path, null));
                var second = Assert.IsType<SGPdf.App.Shell.PdfDocumentWorkspace>(window.ActiveDocument);
                Assert.NotSame(first, second);
                Assert.Equal(window.Title, $"SG PDF Editor — {Path.GetFileName(b.Path)}");
                Assert.False(second.IsReaderNavigationOpen);
                Assert.Equal(Visibility.Collapsed,
                    Assert.IsType<Border>(window.FindName("ReaderNavigationDrawer")).Visibility);

                await (Task)Invoke(window, "ActivateDocumentAsync", first)!;
                Assert.True(first.IsReaderNavigationOpen);
                Assert.Equal(Visibility.Visible,
                    Assert.IsType<Border>(window.FindName("ReaderNavigationDrawer")).Visibility);

                await (Task)Invoke(window, "ActivateDocumentAsync", second)!;
                Assert.False(second.IsReaderNavigationOpen);
                Assert.Equal(Visibility.Collapsed,
                    Assert.IsType<Border>(window.FindName("ReaderNavigationDrawer")).Visibility);
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

    private static T? FindVisualDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                return match;
            var nested = FindVisualDescendant<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }

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

    private static object? GetField(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window);
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
