using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowPdfToPngTests
{
    [Fact]
    public void PdfToPngUi_ExposesMinimalMainWindowHandlers()
    {
        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;
        Type windowType = appAssembly.GetType("SGPdf.App.MainWindow", throwOnError: true)!;

        MethodInfo? click = windowType.GetMethod(
            "ExportPdfToPng_Click",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo? runner = windowType.GetMethod(
            "TryExportPdfToPngAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(click);
        Assert.NotNull(runner);
        Assert.Equal(typeof(Task<bool>), runner!.ReturnType);
    }
}
