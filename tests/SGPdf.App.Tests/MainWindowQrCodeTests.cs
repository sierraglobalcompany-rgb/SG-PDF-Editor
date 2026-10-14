using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowQrCodeTests
{
    [Fact]
    public void QrUi_ExposesMinimalDialogAndMainWindowHandler()
    {
        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;

        Type? dialogType = appAssembly.GetType("SGPdf.App.QrCodeDialog", throwOnError: false);
        Assert.NotNull(dialogType);
        Assert.NotNull(dialogType!.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public));

        Type windowType = appAssembly.GetType("SGPdf.App.MainWindow", throwOnError: true)!;
        Assert.NotNull(windowType.GetMethod("QrCode_Click", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(windowType.GetMethod("TryRunQrCodeGeneratorAsync", BindingFlags.Instance | BindingFlags.NonPublic));
    }
}
