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

public sealed class MainWindowZplTests
{
    [Fact]
    public void CommitLoadedZpl_ShowsFirstRenderedPreviewAndLabelNavigation()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                var rendered = CreateRenderedLabels(document.Designs.Count);
                var commit = GetRenderedCommitMethod();

                commit.Invoke(window, [document, rendered]);

                var activeDocument = typeof(MainWindow)
                    .GetField("_zplDocument", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window);
                var selectedIndex = typeof(MainWindow)
                    .GetField("_selectedZplDesignIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window);
                Assert.Same(document, activeDocument);
                Assert.Equal(0, Assert.IsType<int>(selectedIndex));

                var pdfNavigation = Assert.IsType<Border>(window.FindName("NavigationBar"));
                var labelNavigation = Assert.IsType<Border>(window.FindName("LabelNavigationBar"));
                var pdfImage = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var emptyState = Assert.IsType<TextBlock>(window.FindName("EmptyStateText"));
                var labelCount = Assert.IsType<TextBlock>(window.FindName("LabelCountText"));
                var previous = Assert.IsType<Button>(window.FindName("PreviousLabelButton"));
                var next = Assert.IsType<Button>(window.FindName("NextLabelButton"));
                var status = Assert.IsType<TextBlock>(window.FindName("StatusText"));

                Assert.Equal(Visibility.Collapsed, pdfNavigation.Visibility);
                Assert.Equal(Visibility.Visible, labelNavigation.Visibility);
                Assert.Equal(Visibility.Visible, pdfImage.Visibility);
                Assert.NotNull(pdfImage.Source);
                Assert.Equal(Visibility.Collapsed, emptyState.Visibility);
                Assert.Equal("Etiqueta 1 de 2", labelCount.Text);
                Assert.False(previous.IsEnabled);
                Assert.True(next.IsEnabled);
                Assert.Contains("orders.zpl", window.Title, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("etiqueta 1 de 2", status.Text, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("5", status.Text, StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LabelNavigation_ChangesPreviewIndexAndRespectsBounds()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                var rendered = CreateRenderedLabels(document.Designs.Count);
                GetRenderedCommitMethod().Invoke(window, [document, rendered]);

                var image = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var labelCount = Assert.IsType<TextBlock>(window.FindName("LabelCountText"));
                var previous = Assert.IsType<Button>(window.FindName("PreviousLabelButton"));
                var next = Assert.IsType<Button>(window.FindName("NextLabelButton"));
                var firstSource = image.Source;

                next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var selectedIndex = Assert.IsType<int>(typeof(MainWindow)
                    .GetField("_selectedZplDesignIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window));
                Assert.Equal(1, selectedIndex);
                Assert.Equal("Etiqueta 2 de 2", labelCount.Text);
                Assert.NotSame(firstSource, image.Source);
                Assert.True(previous.IsEnabled);
                Assert.False(next.IsEnabled);

                next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                selectedIndex = Assert.IsType<int>(typeof(MainWindow)
                    .GetField("_selectedZplDesignIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window));
                Assert.Equal(1, selectedIndex);

                previous.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                previous.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                selectedIndex = Assert.IsType<int>(typeof(MainWindow)
                    .GetField("_selectedZplDesignIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window));
                Assert.Equal(0, selectedIndex);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetBusy_DisablesOpenCommandsAndLabelNavigation()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = CreateTwoDesignDocument();
                GetRenderedCommitMethod().Invoke(window, [document, CreateRenderedLabels(document.Designs.Count)]);

                var openPdf = Assert.IsType<MenuItem>(window.FindName("OpenPdfMenuItem"));
                var openZpl = Assert.IsType<MenuItem>(window.FindName("OpenZplMenuItem"));
                var previous = Assert.IsType<Button>(window.FindName("PreviousLabelButton"));
                var next = Assert.IsType<Button>(window.FindName("NextLabelButton"));
                var setBusy = typeof(MainWindow).GetMethod(
                    "SetBusy",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(setBusy);

                setBusy.Invoke(window, [true]);
                Assert.False(openPdf.IsEnabled);
                Assert.False(openZpl.IsEnabled);
                Assert.False(previous.IsEnabled);
                Assert.False(next.IsEnabled);

                setBusy.Invoke(window, [false]);
                Assert.True(openPdf.IsEnabled);
                Assert.True(openZpl.IsEnabled);
                Assert.False(previous.IsEnabled);
                Assert.True(next.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static MethodInfo GetRenderedCommitMethod()
    {
        var method = typeof(MainWindow).GetMethod(
            "CommitLoadedZpl",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ZplDocument), typeof(IReadOnlyList<ZplRenderedLabel>)],
            modifiers: null);
        Assert.NotNull(method);
        return method;
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
