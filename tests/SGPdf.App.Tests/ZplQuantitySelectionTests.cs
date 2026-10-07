using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ZplQuantitySelectionTests
{
    private static readonly ZplDocument Document = ZplDocumentParser.Parse(
        @"C:\labels\orders.zpl",
        "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");

    [Fact]
    public void FromFile_UsesEachDesignQuantityAndTotal()
    {
        var selection = new ZplQuantitySelection(ZplQuantityMode.FromFile);

        Assert.Equal(2, selection.GetQuantity(Document.Designs[0]));
        Assert.Equal(3, selection.GetQuantity(Document.Designs[1]));
        Assert.Equal(5, selection.GetTotalQuantity(Document));
    }

    [Fact]
    public void OneEach_UsesOnePerPrintableDesign()
    {
        var selection = new ZplQuantitySelection(ZplQuantityMode.OneEach);

        Assert.Equal(1, selection.GetQuantity(Document.Designs[0]));
        Assert.Equal(1, selection.GetQuantity(Document.Designs[1]));
        Assert.Equal(2, selection.GetTotalQuantity(Document));
    }

    [Fact]
    public void Custom_UsesSameQuantityPerPrintableDesign()
    {
        var selection = new ZplQuantitySelection(ZplQuantityMode.Custom, customQuantity: 7);

        Assert.Equal(7, selection.GetQuantity(Document.Designs[0]));
        Assert.Equal(7, selection.GetQuantity(Document.Designs[1]));
        Assert.Equal(14, selection.GetTotalQuantity(Document));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000000)]
    public void Custom_RejectsUnsupportedQuantity(long quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ZplQuantitySelection(ZplQuantityMode.Custom, quantity));
    }
}
