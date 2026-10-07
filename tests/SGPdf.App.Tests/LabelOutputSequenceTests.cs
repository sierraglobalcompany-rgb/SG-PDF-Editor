using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class LabelOutputSequenceTests
{
    private static readonly ZplDocument Document = ZplDocumentParser.Parse(
        @"C:\labels\orders.zpl",
        "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");

    [Fact]
    public void FromFile_ResolvesDesignsInQuantityOrder()
    {
        var sequence = new LabelOutputSequence(
            Document,
            new ZplQuantitySelection(ZplQuantityMode.FromFile));

        Assert.Equal(5, sequence.TotalCount);
        Assert.Equal(0, sequence.GetDesignIndexAt(0));
        Assert.Equal(0, sequence.GetDesignIndexAt(1));
        Assert.Equal(1, sequence.GetDesignIndexAt(2));
        Assert.Equal(1, sequence.GetDesignIndexAt(4));
    }

    [Fact]
    public void OneEach_ResolvesOneCopyPerPrintableDesign()
    {
        var sequence = new LabelOutputSequence(
            Document,
            new ZplQuantitySelection(ZplQuantityMode.OneEach));

        Assert.Equal(2, sequence.TotalCount);
        Assert.Equal(0, sequence.GetDesignIndexAt(0));
        Assert.Equal(1, sequence.GetDesignIndexAt(1));
    }

    [Fact]
    public void Custom_ResolvesSameQuantityPerDesign()
    {
        var sequence = new LabelOutputSequence(
            Document,
            new ZplQuantitySelection(ZplQuantityMode.Custom, 7));

        Assert.Equal(14, sequence.TotalCount);
        Assert.Equal(0, sequence.GetDesignIndexAt(6));
        Assert.Equal(1, sequence.GetDesignIndexAt(7));
        Assert.Equal(1, sequence.GetDesignIndexAt(13));
    }

    [Fact]
    public void HugeCustomQuantity_UsesLongIndexesWithoutExpandingCopies()
    {
        var sequence = new LabelOutputSequence(
            Document,
            new ZplQuantitySelection(
                ZplQuantityMode.Custom,
                ZplQuantitySelection.MaxCustomQuantity));

        Assert.Equal(199_999_998L, sequence.TotalCount);
        Assert.Equal(0, sequence.GetDesignIndexAt(99_999_998L));
        Assert.Equal(1, sequence.GetDesignIndexAt(99_999_999L));
        Assert.Equal(1, sequence.GetDesignIndexAt(sequence.TotalCount - 1));
    }

    [Fact]
    public void GetDesignIndexAt_RejectsIndexesOutsideOutputRange()
    {
        var sequence = new LabelOutputSequence(
            Document,
            new ZplQuantitySelection(ZplQuantityMode.FromFile));

        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.GetDesignIndexAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.GetDesignIndexAt(sequence.TotalCount));
    }
}
