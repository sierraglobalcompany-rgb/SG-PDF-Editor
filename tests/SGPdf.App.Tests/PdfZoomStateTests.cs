using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfZoomStateTests
{
    [Fact]
    public void DefaultState_IsManualAt100Percent()
    {
        var state = new PdfZoomState();

        Assert.Equal(PdfZoomMode.Manual, state.Mode);
        Assert.Equal(100, state.ManualPercent);
        Assert.Equal(96d, state.ResolveDpi(612d, 792d, 1000d, 1000d, 48d), 6);
    }

    [Fact]
    public void ZoomInAndOut_UseCommercialStylePresetsAndBounds()
    {
        var state = new PdfZoomState();

        state = state.ZoomIn(96d);
        Assert.Equal(125, state.ManualPercent);

        state = state.ZoomOut(120d);
        Assert.Equal(100, state.ManualPercent);

        state = state.ZoomOut(96d);
        Assert.Equal(75, state.ManualPercent);

        state = state.ZoomOut(72d);
        Assert.Equal(50, state.ManualPercent);

        state = state.ZoomOut(48d);
        Assert.Equal(25, state.ManualPercent);

        state = state.ZoomOut(24d);
        Assert.Equal(25, state.ManualPercent);

        state = state.ZoomIn(384d);
        Assert.Equal(400, state.ManualPercent);
    }

    [Fact]
    public void FitBelowMinimumPreset_ZoomOutDoesNotIncreaseMagnification()
    {
        var state = new PdfZoomState().FitPage();

        var candidate = state.ZoomOut(9.6d);

        Assert.Same(state, candidate);
    }

    [Fact]
    public void FitWidth_UsesAvailableViewportWidth()
    {
        var state = new PdfZoomState().FitWidth();

        var dpi = state.ResolveDpi(612d, 792d, 864d, 900d, 48d);

        Assert.Equal(PdfZoomMode.FitWidth, state.Mode);
        Assert.Equal(96d, dpi, 6);
    }

    [Fact]
    public void FitPage_UsesTheMoreRestrictiveDimension()
    {
        var state = new PdfZoomState().FitPage();

        var dpi = state.ResolveDpi(612d, 792d, 864d, 900d, 48d);

        Assert.Equal(PdfZoomMode.FitPage, state.Mode);
        Assert.Equal(852d * 72d / 792d, dpi, 6);
    }
}
