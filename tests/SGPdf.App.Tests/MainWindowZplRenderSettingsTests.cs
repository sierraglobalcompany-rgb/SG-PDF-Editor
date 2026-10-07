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

public sealed class MainWindowZplRenderSettingsTests
{
    [Fact]
    public void PresetAndDpmmChanges_RerenderAndPreserveSelectedDesign()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                Commit(window, document, CreateRenderedLabels(document.Designs.Count, ZplRenderOptions.Default));

                var next = Assert.IsType<Button>(window.FindName("NextLabelButton"));
                next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var size = Assert.IsType<ComboBox>(window.FindName("LabelSizeComboBox"));
                var dpmm = Assert.IsType<ComboBox>(window.FindName("LabelDpmmComboBox"));
                var info = Assert.IsType<TextBlock>(window.FindName("RenderSettingsText"));
                Assert.Equal(0, size.SelectedIndex);
                Assert.Equal(1, dpmm.SelectedIndex);
                Assert.Contains("102", info.Text, StringComparison.Ordinal);
                Assert.Contains("152", info.Text, StringComparison.Ordinal);
                Assert.Contains("8 dpmm", info.Text, StringComparison.OrdinalIgnoreCase);

                ZplRenderOptions? captured = null;
                SetRenderer(window, (source, options, _) =>
                {
                    captured = options;
                    return Task.FromResult(CreateRenderedLabels(source.Designs.Count, options));
                });

                var image = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var previousPreview = image.Source;
                size.SelectedIndex = 2;

                Assert.NotNull(captured);
                Assert.Equal(100d, captured.WidthMm);
                Assert.Equal(100d, captured.HeightMm);
                Assert.Equal(8, captured.Dpmm);
                Assert.Equal(1, GetSelectedDesignIndex(window));
                Assert.NotSame(previousPreview, image.Source);

                dpmm.SelectedIndex = 2;

                Assert.Equal(100d, captured.WidthMm);
                Assert.Equal(100d, captured.HeightMm);
                Assert.Equal(12, captured.Dpmm);
                Assert.Equal(1, GetSelectedDesignIndex(window));
                Assert.Contains("12 dpmm", info.Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void CustomSize_AppliesOnlyWhenRequested()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                Commit(window, document, CreateRenderedLabels(document.Designs.Count, ZplRenderOptions.Default));

                var size = Assert.IsType<ComboBox>(window.FindName("LabelSizeComboBox"));
                var customPanel = Assert.IsType<StackPanel>(window.FindName("CustomSizePanel"));
                var width = Assert.IsType<TextBox>(window.FindName("CustomWidthTextBox"));
                var height = Assert.IsType<TextBox>(window.FindName("CustomHeightTextBox"));
                var apply = Assert.IsType<Button>(window.FindName("ApplyCustomSizeButton"));

                var calls = 0;
                ZplRenderOptions? captured = null;
                SetRenderer(window, (source, options, _) =>
                {
                    calls++;
                    captured = options;
                    return Task.FromResult(CreateRenderedLabels(source.Designs.Count, options));
                });

                size.SelectedIndex = 3;
                Assert.Equal(Visibility.Visible, customPanel.Visibility);
                Assert.Equal(0, calls);

                width.Text = "80";
                height.Text = "50";
                Assert.Equal(0, calls);

                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Equal(1, calls);
                Assert.NotNull(captured);
                Assert.Equal(80d, captured.WidthMm);
                Assert.Equal(50d, captured.HeightMm);
                Assert.Equal(8, captured.Dpmm);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void FailedRerender_KeepsLastValidPreviewAndSettings()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                Commit(window, document, CreateRenderedLabels(document.Designs.Count, ZplRenderOptions.Default));

                var image = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var previousPreview = image.Source;
                var size = Assert.IsType<ComboBox>(window.FindName("LabelSizeComboBox"));
                var status = Assert.IsType<TextBlock>(window.FindName("StatusText"));

                SetRenderer(window, (_, _, _) =>
                    Task.FromException<IReadOnlyList<ZplRenderedLabel>>(new InvalidOperationException("synthetic failure")));

                size.SelectedIndex = 2;

                Assert.Same(previousPreview, image.Source);
                var options = GetRenderOptions(window);
                Assert.Equal(102d, options.WidthMm);
                Assert.Equal(152d, options.HeightMm);
                Assert.Equal(8, options.Dpmm);
                Assert.Equal(0, size.SelectedIndex);
                Assert.Contains("no se pudo", status.Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static void SetRenderer(
        MainWindow window,
        Func<ZplDocument, ZplRenderOptions, CancellationToken, Task<IReadOnlyList<ZplRenderedLabel>>> renderer)
    {
        var field = typeof(MainWindow).GetField(
            "_renderZplAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, renderer);
    }

    private static ZplRenderOptions GetRenderOptions(MainWindow window)
        => Assert.IsType<ZplRenderOptions>(typeof(MainWindow)
            .GetField("_zplRenderOptions", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(window));

    private static int GetSelectedDesignIndex(MainWindow window)
        => Assert.IsType<int>(typeof(MainWindow)
            .GetField("_selectedZplDesignIndex", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(window));

    private static void Commit(
        MainWindow window,
        ZplDocument document,
        IReadOnlyList<ZplRenderedLabel> rendered)
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

    private static ZplDocument CreateTwoDesignDocument()
        => ZplDocumentParser.Parse(
            @"C:\labels\orders.zpl",
            "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");

    private static IReadOnlyList<ZplRenderedLabel> CreateRenderedLabels(
        int count,
        ZplRenderOptions options)
    {
        var labels = new List<ZplRenderedLabel>(count);
        for (var index = 0; index < count; index++)
        {
            labels.Add(new ZplRenderedLabel(
                index,
                options.WidthMm,
                options.HeightMm,
                options.Dpmm,
                CreatePngBytes(index == 0 ? Colors.Black : Colors.DarkGray)));
        }

        return labels;
    }

    private static byte[] CreatePngBytes(Color color)
    {
        var pixels = new byte[] { color.B, color.G, color.R, color.A };
        var bitmap = BitmapSource.Create(
            1,
            1,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            4);
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
