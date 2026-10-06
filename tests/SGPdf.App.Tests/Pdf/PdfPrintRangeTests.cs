using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests.Pdf;

public sealed class PdfPrintRangeTests
{
    [Theory]
    [InlineData(10, 2, 4, 1, 3)]
    [InlineData(10, 0, 99, 0, 10)]
    [InlineData(1, 1, 1, 0, 1)]
    public void FromUserRange_converts_one_based_input_and_clamps(
        int totalPages,
        int pageFrom,
        int pageTo,
        int expectedStart,
        int expectedCount)
    {
        var rangeType = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfPrintRange");
        Assert.NotNull(rangeType);

        var method = rangeType.GetMethod(
            "FromUserRange",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(int), typeof(int), typeof(int)],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(null, [totalPages, pageFrom, pageTo]);
        Assert.NotNull(result);

        Assert.Equal(expectedStart, (int)rangeType.GetProperty("StartPageIndex")!.GetValue(result)!);
        Assert.Equal(expectedCount, (int)rangeType.GetProperty("PageCount")!.GetValue(result)!);
    }
}
