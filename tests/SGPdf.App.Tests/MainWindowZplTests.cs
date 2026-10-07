using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowZplTests
{
    [Fact]
    public void CommitLoadedZpl_SwitchesWorkspaceToLoadedLabelPlaceholder()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var document = ZplDocumentParser.Parse(
                    @"C:\labels\orders.zpl",
                    "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");

                var commit = typeof(MainWindow).GetMethod(
                    "CommitLoadedZpl",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(commit);

                commit.Invoke(window, [document]);

                var activeDocument = typeof(MainWindow)
                    .GetField("_zplDocument", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window);
                Assert.Same(document, activeDocument);

                var navigationBar = Assert.IsType<Border>(window.FindName("NavigationBar"));
                var pdfImage = Assert.IsType<System.Windows.Controls.Image>(window.FindName("PdfImage"));
                var emptyState = Assert.IsType<TextBlock>(window.FindName("EmptyStateText"));
                var status = Assert.IsType<TextBlock>(window.FindName("StatusText"));

                Assert.Equal(Visibility.Collapsed, navigationBar.Visibility);
                Assert.Equal(Visibility.Collapsed, pdfImage.Visibility);
                Assert.Equal("Archivo ZPL cargado\n2 diseño(s) detectado(s)", emptyState.Text);
                Assert.Equal(Visibility.Visible, emptyState.Visibility);
                Assert.Contains("orders.zpl", window.Title, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("2 diseño", status.Text, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("5", status.Text, StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetBusy_DisablesBothOpenCommands()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var openPdf = Assert.IsType<MenuItem>(window.FindName("OpenPdfMenuItem"));
                var openZpl = Assert.IsType<MenuItem>(window.FindName("OpenZplMenuItem"));
                var setBusy = typeof(MainWindow).GetMethod(
                    "SetBusy",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(setBusy);

                setBusy.Invoke(window, [true]);
                Assert.False(openPdf.IsEnabled);
                Assert.False(openZpl.IsEnabled);

                setBusy.Invoke(window, [false]);
                Assert.True(openPdf.IsEnabled);
                Assert.True(openZpl.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });
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
