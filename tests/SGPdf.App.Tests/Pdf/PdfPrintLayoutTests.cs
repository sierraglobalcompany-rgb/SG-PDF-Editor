using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests.Pdf;

public sealed class PdfPrintLayoutTests
{
    [Theory]
    [InlineData(600, 800, 600, 800, 0, 0, 600, 800)]
    [InlineData(600, 800, 1200, 800, 300, 0, 600, 800)]
    [InlineData(600, 800, 600, 1200, 0, 200, 600, 800)]
    [InlineData(1200, 800, 600, 800, 0, 200, 600, 400)]
    public void Fit_preserves_aspect_ratio_and_centers(
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight,
        double expectedX,
        double expectedY,
        double expectedWidth,
        double expectedHeight)
    {
        var layoutType = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfPrintLayout");
        Assert.NotNull(layoutType);

        var method = layoutType.GetMethod(
            "Fit",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(double), typeof(double), typeof(double), typeof(double)],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(null, [sourceWidth, sourceHeight, targetWidth, targetHeight]);
        Assert.NotNull(result);

        Assert.Equal(expectedX, (double)layoutType.GetProperty("X")!.GetValue(result)!, 6);
        Assert.Equal(expectedY, (double)layoutType.GetProperty("Y")!.GetValue(result)!, 6);
        Assert.Equal(expectedWidth, (double)layoutType.GetProperty("Width")!.GetValue(result)!, 6);
        Assert.Equal(expectedHeight, (double)layoutType.GetProperty("Height")!.GetValue(result)!, 6);
    }
}
