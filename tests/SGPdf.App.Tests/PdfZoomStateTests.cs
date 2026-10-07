using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfZoomStateTests
{
    private static Type ZoomType
    {
        get
        {
            var type = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfZoomState");
            Assert.NotNull(type);
            return type!;
        }
    }

    [Fact]
    public void DefaultState_IsManualAt100Percent()
    {
        dynamic state = Activator.CreateInstance(ZoomType)!;

        Assert.Equal("Manual", state.Mode.ToString());
        Assert.Equal(100, (int)state.ManualPercent);
        Assert.Equal(96d, (double)state.ResolveDpi(612d, 792d, 1000d, 1000d, 48d), 6);
    }

    [Fact]
    public void ZoomInAndOut_UseCommercialStylePresetsAndBounds()
    {
        dynamic state = Activator.CreateInstance(ZoomType)!;

        state = state.ZoomIn(96d);
        Assert.Equal(125, (int)state.ManualPercent);

        state = state.ZoomOut(120d);
        Assert.Equal(100, (int)state.ManualPercent);

        state = state.ZoomOut(24d);
        Assert.Equal(25, (int)state.ManualPercent);

        state = state.ZoomIn(384d);
        Assert.Equal(400, (int)state.ManualPercent);
    }

    [Fact]
    public void FitWidth_UsesAvailableViewportWidth()
    {
        dynamic state = Activator.CreateInstance(ZoomType)!;
        state = state.FitWidth();

        var dpi = (double)state.ResolveDpi(612d, 792d, 864d, 900d, 48d);

        Assert.Equal("FitWidth", state.Mode.ToString());
        Assert.Equal(96d, dpi, 6);
    }

    [Fact]
    public void FitPage_UsesTheMoreRestrictiveDimension()
    {
        dynamic state = Activator.CreateInstance(ZoomType)!;
        state = state.FitPage();

        var dpi = (double)state.ResolveDpi(612d, 792d, 864d, 900d, 48d);

        Assert.Equal("FitPage", state.Mode.ToString());
        Assert.Equal(852d * 72d / 792d, dpi, 6);
    }
}
