using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCompareCapabilityTests
{
    [Fact]
    public void ExistingPdfium_CanSupplyMinimalSignalsForCompareMvp()
    {
        CompareProbeResult identical = ProbeWithExistingPdfium(CompareFixture.Identical);
        Assert.Equal(1, identical.LeftPageCount);
        Assert.Equal(1, identical.RightPageCount);
        Assert.False(identical.TextChanged);
        Assert.False(identical.VisualChanged);

        CompareProbeResult textChanged = ProbeWithExistingPdfium(CompareFixture.TextChanged);
        Assert.Equal(1, textChanged.LeftPageCount);
        Assert.Equal(1, textChanged.RightPageCount);
        Assert.True(textChanged.TextChanged);

        CompareProbeResult graphicChanged = ProbeWithExistingPdfium(CompareFixture.GraphicChanged);
        Assert.Equal(1, graphicChanged.LeftPageCount);
        Assert.Equal(1, graphicChanged.RightPageCount);
        Assert.False(graphicChanged.TextChanged);
        Assert.True(graphicChanged.VisualChanged);

        CompareProbeResult pageAdded = ProbeWithExistingPdfium(CompareFixture.PageAdded);
        Assert.Equal(1, pageAdded.LeftPageCount);
        Assert.Equal(2, pageAdded.RightPageCount);
    }

    private static CompareProbeResult ProbeWithExistingPdfium(CompareFixture fixture)
        => throw new NotSupportedException(
            $"F12.4 Task 1A RED: el probe PDFium para '{fixture}' todavía no está implementado.");

    private enum CompareFixture
    {
        Identical,
        TextChanged,
        GraphicChanged,
        PageAdded
    }

    private sealed record CompareProbeResult(
        int LeftPageCount,
        int RightPageCount,
        bool TextChanged,
        bool VisualChanged);
}
