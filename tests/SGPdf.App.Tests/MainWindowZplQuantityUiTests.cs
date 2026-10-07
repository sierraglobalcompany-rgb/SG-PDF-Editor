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

public sealed class MainWindowZplQuantityUiTests
{
    [Fact]
    public void QuantityControls_DefaultToFileAndUpdateTotalWithoutRerenderingPreview()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var properties = Assert.IsType<StackPanel>(window.FindName("LabelPropertiesPanel"));
                Assert.Equal(Visibility.Collapsed, properties.Visibility);

                var document = CreateTwoDesignDocument();
                Commit(window, document, CreateRenderedLabels(document.Designs.Count));

                var image = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var originalPreview = image.Source;
                var fromFile = Assert.IsType<RadioButton>(window.FindName("QuantityFromFileRadio"));
                var oneEach = Assert.IsType<RadioButton>(window.FindName("QuantityOneEachRadio"));
                var custom = Assert.IsType<RadioButton>(window.FindName("QuantityCustomRadio"));
                var customText = Assert.IsType<TextBox>(window.FindName("CustomQuantityTextBox"));
                var output = Assert.IsType<TextBlock>(window.FindName("OutputQuantityText"));

                Assert.Equal(Visibility.Visible, properties.Visibility);
                Assert.True(fromFile.IsChecked);
                Assert.False(oneEach.IsChecked);
                Assert.False(custom.IsChecked);
                Assert.False(customText.IsEnabled);
                Assert.Contains("5", output.Text, StringComparison.Ordinal);

                oneEach.IsChecked = true;

                Assert.Contains("2", output.Text, StringComparison.Ordinal);
                Assert.Same(originalPreview, image.Source);

                custom.IsChecked = true;
                Assert.True(customText.IsEnabled);
                customText.Text = "7";

                Assert.Contains("14", output.Text, StringComparison.Ordinal);
                Assert.Same(originalPreview, image.Source);

                var selection = Assert.IsType<ZplQuantitySelection>(typeof(MainWindow)
                    .GetField("_zplQuantitySelection", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window));
                Assert.Equal(ZplQuantityMode.Custom, selection.Mode);
                Assert.Equal(7, selection.CustomQuantity);
            }
            finally
            {
                window.Close();
            }
        });
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

    private static IReadOnlyList<ZplRenderedLabel> CreateRenderedLabels(int count)
    {
        var labels = new List<ZplRenderedLabel>(count);
        for (var index = 0; index < count; index++)
        {
            labels.Add(new ZplRenderedLabel(
                index,
                102d,
                152d,
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
