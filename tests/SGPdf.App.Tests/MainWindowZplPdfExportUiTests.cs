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

public sealed class MainWindowZplPdfExportUiTests
{
    [Fact]
    public void ExportButton_UsesCurrentValidPlanAndIsDisabledWhenLayoutBecomesInvalid()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var exportButton = Assert.IsType<Button>(window.FindName("ExportLabelPdfButton"));
                Assert.False(exportButton.IsEnabled);

                var document = ZplDocumentParser.Parse(
                    @"C:\labels\orders.zpl",
                    "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");
                var rendered = CreateRenderedLabels(document.Designs.Count, 50, 30);
                Commit(window, document, rendered);

                Assert.True(exportButton.IsEnabled);

                var destination = Path.Combine(Path.GetTempPath(), $"sgpdf-ui-export-{Guid.NewGuid():N}.pdf");
                SetField(window, "_selectLabelPdfDestination", (Func<string?>)(() => destination));

                var calls = 0;
                string? capturedPath = null;
                LabelLayoutPlan? capturedPlan = null;
                IReadOnlyList<ZplRenderedLabel>? capturedRendered = null;
                SetField(
                    window,
                    "_exportLabelPdf",
                    (Action<string, LabelLayoutPlan, IReadOnlyList<ZplRenderedLabel>>)((path, plan, labels) =>
                    {
                        calls++;
                        capturedPath = path;
                        capturedPlan = plan;
                        capturedRendered = labels;
                    }));

                exportButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Equal(1, calls);
                Assert.Equal(destination, capturedPath);
                Assert.Same(GetLayoutPlan(window), capturedPlan);
                Assert.Same(GetRenderedLabels(window), capturedRendered);
                var status = Assert.IsType<TextBlock>(window.FindName("StatusText"));
                Assert.Contains("PDF", status.Text, StringComparison.OrdinalIgnoreCase);

                var media = Assert.IsType<ComboBox>(window.FindName("SheetMediaComboBox"));
                var layout = Assert.IsType<ComboBox>(window.FindName("SheetLayoutComboBox"));
                var apply = Assert.IsType<Button>(window.FindName("ApplySheetLayoutButton"));
                media.SelectedIndex = 1; // A4
                layout.SelectedIndex = 3; // 4 labels at 50x30 still valid, switch to 102x152 first.

                SetRenderOptions(window, new ZplRenderOptions(102, 152, 8));
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Null(GetLayoutPlan(window));
                Assert.False(exportButton.IsEnabled);
                Assert.Equal(1, calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static void SetField(MainWindow window, string name, object value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, value);
    }

    private static void SetRenderOptions(MainWindow window, ZplRenderOptions options)
        => SetField(window, "_zplRenderOptions", options);

    private static LabelLayoutPlan? GetLayoutPlan(MainWindow window)
        => (LabelLayoutPlan?)typeof(MainWindow)
            .GetField("_labelLayoutPlan", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(window);

    private static IReadOnlyList<ZplRenderedLabel> GetRenderedLabels(MainWindow window)
        => Assert.IsAssignableFrom<IReadOnlyList<ZplRenderedLabel>>(typeof(MainWindow)
            .GetField("_renderedZplLabels", BindingFlags.Instance | BindingFlags.NonPublic)
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

    private static IReadOnlyList<ZplRenderedLabel> CreateRenderedLabels(
        int count,
        double widthMm,
        double heightMm)
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
