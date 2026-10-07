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

public sealed class MainWindowZplLayoutUiTests
{
    [Fact]
    public void SheetControls_ApplyA4LayoutAndNavigateWithoutRerenderingLabelize()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                Commit(window, document, CreateRenderedLabels(document.Designs.Count, 50, 30));

                var labelMode = Assert.IsType<RadioButton>(window.FindName("PreviewLabelRadio"));
                var sheetMode = Assert.IsType<RadioButton>(window.FindName("PreviewSheetRadio"));
                var media = Assert.IsType<ComboBox>(window.FindName("SheetMediaComboBox"));
                var layout = Assert.IsType<ComboBox>(window.FindName("SheetLayoutComboBox"));
                var marginH = Assert.IsType<TextBox>(window.FindName("SheetHorizontalMarginTextBox"));
                var marginV = Assert.IsType<TextBox>(window.FindName("SheetVerticalMarginTextBox"));
                var gapH = Assert.IsType<TextBox>(window.FindName("SheetHorizontalGapTextBox"));
                var gapV = Assert.IsType<TextBox>(window.FindName("SheetVerticalGapTextBox"));
                var apply = Assert.IsType<Button>(window.FindName("ApplySheetLayoutButton"));
                var canvas = Assert.IsType<Canvas>(window.FindName("LabelSheetCanvas"));
                var image = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var count = Assert.IsType<TextBlock>(window.FindName("LabelCountText"));
                var next = Assert.IsType<Button>(window.FindName("NextLabelButton"));

                Assert.True(labelMode.IsChecked);
                Assert.False(sheetMode.IsChecked);
                Assert.Equal(0, media.SelectedIndex);
                Assert.Equal(0, layout.SelectedIndex);
                Assert.Equal(Visibility.Collapsed, canvas.Visibility);

                var renderCalls = 0;
                SetRenderer(window, (source, options, _) =>
                {
                    renderCalls++;
                    return Task.FromResult(CreateRenderedLabels(
                        source.Designs.Count,
                        options.WidthMm,
                        options.HeightMm));
                });

                media.SelectedIndex = 1; // A4
                layout.SelectedIndex = 3; // 4 per page
                marginH.Text = "5";
                marginV.Text = "5";
                gapH.Text = "2";
                gapV.Text = "3";
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var plan = GetLayoutPlan(window);
                Assert.NotNull(plan);
                Assert.Equal(210d, plan.PageWidthMm, 3);
                Assert.Equal(297d, plan.PageHeightMm, 3);
                Assert.Equal(4, plan.LabelsPerPage);
                Assert.Equal(2, plan.PageCount);
                Assert.Equal(0, renderCalls);

                sheetMode.IsChecked = true;

                Assert.Equal(Visibility.Collapsed, image.Visibility);
                Assert.Equal(Visibility.Visible, canvas.Visibility);
                Assert.Equal(4, canvas.Children.Count);
                Assert.Contains("Hoja 1 de 2", count.Text, StringComparison.Ordinal);

                next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Contains("Hoja 2 de 2", count.Text, StringComparison.Ordinal);
                Assert.Single(canvas.Children);
                Assert.Equal(0, renderCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void QuantityChange_ReplansSheetWithoutCallingLabelize()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                Commit(window, document, CreateRenderedLabels(document.Designs.Count, 50, 30));

                var media = Assert.IsType<ComboBox>(window.FindName("SheetMediaComboBox"));
                var layout = Assert.IsType<ComboBox>(window.FindName("SheetLayoutComboBox"));
                var apply = Assert.IsType<Button>(window.FindName("ApplySheetLayoutButton"));
                var custom = Assert.IsType<RadioButton>(window.FindName("QuantityCustomRadio"));
                var customText = Assert.IsType<TextBox>(window.FindName("CustomQuantityTextBox"));

                media.SelectedIndex = 1;
                layout.SelectedIndex = 3;
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var renderCalls = 0;
                SetRenderer(window, (source, options, _) =>
                {
                    renderCalls++;
                    return Task.FromResult(CreateRenderedLabels(
                        source.Designs.Count,
                        options.WidthMm,
                        options.HeightMm));
                });

                custom.IsChecked = true;
                customText.Text = "7";

                var plan = GetLayoutPlan(window);
                Assert.NotNull(plan);
                Assert.Equal(14, plan.TotalLabels);
                Assert.Equal(4, plan.PageCount);
                Assert.Equal(0, renderCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void InvalidPhysicalLayout_ShowsReasonAndDisablesSheetPreviewWithoutRerendering()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                Commit(window, document, CreateRenderedLabels(document.Designs.Count, 102, 152));

                var media = Assert.IsType<ComboBox>(window.FindName("SheetMediaComboBox"));
                var layout = Assert.IsType<ComboBox>(window.FindName("SheetLayoutComboBox"));
                var apply = Assert.IsType<Button>(window.FindName("ApplySheetLayoutButton"));
                var sheetMode = Assert.IsType<RadioButton>(window.FindName("PreviewSheetRadio"));
                var validation = Assert.IsType<TextBlock>(window.FindName("LayoutValidationText"));

                var renderCalls = 0;
                SetRenderer(window, (source, options, _) =>
                {
                    renderCalls++;
                    return Task.FromResult(CreateRenderedLabels(
                        source.Designs.Count,
                        options.WidthMm,
                        options.HeightMm));
                });

                media.SelectedIndex = 1; // A4
                layout.SelectedIndex = 3; // 4 labels cannot fit at 102×152 mm
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Null(GetLayoutPlan(window));
                Assert.Contains("caben", validation.Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(sheetMode.IsEnabled);
                Assert.Equal(0, renderCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static LabelLayoutPlan? GetLayoutPlan(MainWindow window)
        => (LabelLayoutPlan?)typeof(MainWindow)
            .GetField("_labelLayoutPlan", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(window);

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
