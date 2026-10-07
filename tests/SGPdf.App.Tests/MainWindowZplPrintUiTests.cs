using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowZplPrintUiTests
{
    [Fact]
    public void PrintButton_IsDisabledWithoutLoadedZpl()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var button = Assert.IsType<Button>(window.FindName("PrintLabelButton"));
                Assert.False(button.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void PrintButton_IsEnabledOnlyForValidThermalLayout()
    {
        RunInSta(() =>
        {
            var window = LoadedWindow();
            try
            {
                var button = Assert.IsType<Button>(window.FindName("PrintLabelButton"));
                Assert.True(button.IsEnabled);

                SetField(window, "_labelLayoutPlan", null);
                Invoke(window, "UpdateLabelPropertiesUi");
                Assert.False(button.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void A4OrLetterLayout_DisablesThermalPrintButton()
    {
        RunInSta(() =>
        {
            var window = LoadedWindow();
            try
            {
                var button = Assert.IsType<Button>(window.FindName("PrintLabelButton"));
                var media = Assert.IsType<ComboBox>(window.FindName("SheetMediaComboBox"));
                var apply = Assert.IsType<Button>(window.FindName("ApplySheetLayoutButton"));

                media.SelectedIndex = 1;
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.False(button.IsEnabled);

                media.SelectedIndex = 2;
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.False(button.IsEnabled);

                media.SelectedIndex = 0;
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.True(button.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Click_UsesCurrentPlanRenderedLabelsAndRequestedDpi()
    {
        RunInSta(() =>
        {
            var window = LoadedWindow();
            try
            {
                var button = Assert.IsType<Button>(window.FindName("PrintLabelButton"));
                var expectedPlan = GetField<LabelLayoutPlan>(window, "_labelLayoutPlan");
                var expectedLabels = GetField<IReadOnlyList<ZplRenderedLabel>>(window, "_renderedZplLabels");

                Window? capturedOwner = null;
                LabelLayoutPlan? capturedPlan = null;
                IReadOnlyList<ZplRenderedLabel>? capturedLabels = null;
                var capturedDpi = 0;
                var calls = 0;
                SetPrintSeam(window, (owner, plan, labels, dpi) =>
                {
                    calls++;
                    capturedOwner = owner;
                    capturedPlan = plan;
                    capturedLabels = labels;
                    capturedDpi = dpi;
                    return false;
                });

                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Equal(1, calls);
                Assert.Same(window, capturedOwner);
                Assert.Same(expectedPlan, capturedPlan);
                Assert.Same(expectedLabels, capturedLabels);
                Assert.Equal(203, capturedDpi);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void WorkflowCancel_DoesNotChangeWorkspaceOrReportSubmittedJob()
    {
        RunInSta(() =>
        {
            var window = LoadedWindow();
            try
            {
                var button = Assert.IsType<Button>(window.FindName("PrintLabelButton"));
                var image = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var beforeDocument = GetField<ZplDocument>(window, "_zplDocument");
                var beforeLabels = GetField<IReadOnlyList<ZplRenderedLabel>>(window, "_renderedZplLabels");
                var beforeQuantity = GetField<ZplQuantitySelection>(window, "_zplQuantitySelection");
                var beforeOptions = GetField<ZplRenderOptions>(window, "_zplRenderOptions");
                var beforeSelected = GetField<int>(window, "_selectedZplDesignIndex");
                var beforeImage = image.Source;

                SetPrintSeam(window, (_, _, _, _) => false);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Same(beforeDocument, GetField<ZplDocument>(window, "_zplDocument"));
                Assert.Same(beforeLabels, GetField<IReadOnlyList<ZplRenderedLabel>>(window, "_renderedZplLabels"));
                Assert.Same(beforeQuantity, GetField<ZplQuantitySelection>(window, "_zplQuantitySelection"));
                Assert.Equal(beforeOptions, GetField<ZplRenderOptions>(window, "_zplRenderOptions"));
                Assert.Equal(beforeSelected, GetField<int>(window, "_selectedZplDesignIndex"));
                Assert.Same(beforeImage, image.Source);
                var status = Assert.IsType<TextBlock>(window.FindName("StatusText"));
                Assert.DoesNotContain("enviado", status.Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void WorkflowSuccess_ReportsSubmittedJobWithoutRerenderingLabelize()
    {
        RunInSta(() =>
        {
            var window = LoadedWindow();
            try
            {
                var renderCalls = 0;
                SetRenderer(window, (source, options, _) =>
                {
                    renderCalls++;
                    return Task.FromResult(CreateRenderedLabels(source.Designs.Count, options.WidthMm, options.HeightMm));
                });
                SetPrintSeam(window, (_, _, _, _) => true);

                var button = Assert.IsType<Button>(window.FindName("PrintLabelButton"));
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var status = Assert.IsType<TextBlock>(window.FindName("StatusText"));
                Assert.Equal("Trabajo de impresión enviado.", status.Text);
                Assert.Equal(0, renderCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static MainWindow LoadedWindow()
    {
        var window = new MainWindow();
        var document = ZplDocumentParser.Parse(
            @"C:\labels\orders.zpl",
            "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");
        Commit(window, document, CreateRenderedLabels(document.Designs.Count, 50, 30));
        return window;
    }

    private static void Commit(MainWindow window, ZplDocument document, IReadOnlyList<ZplRenderedLabel> rendered)
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

    private static void SetPrintSeam(
        MainWindow window,
        Func<Window, LabelLayoutPlan, IReadOnlyList<ZplRenderedLabel>, int, bool> print)
        => SetField(window, "_printThermalLabels", print);

    private static void SetRenderer(
        MainWindow window,
        Func<ZplDocument, ZplRenderOptions, CancellationToken, Task<IReadOnlyList<ZplRenderedLabel>>> renderer)
        => SetField(window, "_renderZplAsync", renderer);

    private static T GetField<T>(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<T>(field.GetValue(window));
    }

    private static void SetField(MainWindow window, string name, object? value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, value);
    }

    private static void Invoke(MainWindow window, string name)
    {
        var method = typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(window, null);
    }

    private static IReadOnlyList<ZplRenderedLabel> CreateRenderedLabels(int count, double widthMm, double heightMm)
    {
        var labels = new List<ZplRenderedLabel>(count);
        for (var index = 0; index < count; index++)
        {
            labels.Add(new ZplRenderedLabel(
                index,
                widthMm,
                heightMm,
                8,
                CreatePngBytes(index == 0 ? Colors.Black : Colors.DarkGray)));
        }

        return labels;
    }

    private static byte[] CreatePngBytes(Color color)
    {
        var pixels = new byte[] { color.B, color.G, color.R, color.A };
        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
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
}
