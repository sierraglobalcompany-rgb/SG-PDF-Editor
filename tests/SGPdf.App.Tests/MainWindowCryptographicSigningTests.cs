using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowCryptographicSigningTests
{
    [Fact]
    public void CryptographicSigningCommand_IsSingleFocusedCommand_DisabledWithoutPdf_AndNoOps()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var command = OrganizeWindowTestHost.Element<MenuItem>(window, "CryptographicSignMenuItem");
                var status = OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText");
                string statusBefore = status.Text;

                Assert.Equal("Firmar digitalmente", command.Header);
                Assert.False(command.IsEnabled);

                OrganizeWindowTestHost.Invoke(
                    window,
                    "CryptographicSign_Click",
                    command,
                    new RoutedEventArgs(MenuItem.ClickEvent, command));

                Assert.Equal(statusBefore, status.Text);

                var center = Assert.IsType<Grid>(OrganizeWindowTestHost.Element<ScrollViewer>(window, "PdfScrollViewer").Parent);
                var root = Assert.IsType<Grid>(center.Parent);
                Assert.Equal(3, root.ColumnDefinitions.Count);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }
}
