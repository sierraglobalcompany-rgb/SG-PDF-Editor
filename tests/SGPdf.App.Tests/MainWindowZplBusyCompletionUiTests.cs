using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowZplBusyCompletionUiTests
{
    [Fact]
    public void EndingBusyState_RefreshesValidThermalLabelActions()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var export = Assert.IsType<Button>(window.FindName("ExportLabelPdfButton"));
                var print = Assert.IsType<Button>(window.FindName("PrintLabelButton"));
                var document = ZplDocumentParser.Parse(
                    @"C:\labels\orders.zpl",
                    "^XA^FO10,10^FDOne^FS^XZ");
                var rendered = new[]
                {
                    new ZplRenderedLabel(0, 50, 30, 8, CreatePngBytes(Colors.Black))
                };

                Invoke(window, "SetBusy", true);
                Commit(window, document, rendered);

                Assert.False(export.IsEnabled);
                Assert.False(print.IsEnabled);

                Invoke(window, "SetBusy", false);

                Assert.True(export.IsEnabled);
                Assert.True(print.IsEnabled);
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

    private static object? Invoke(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == args.Length);
        return method.Invoke(window, args);
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
