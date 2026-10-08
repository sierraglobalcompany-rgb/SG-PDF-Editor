using System.IO;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignatureInkRendererTests
{
    [Fact]
    public void StrokeStyle_UsesFrozenColorsAndWidths()
    {
        Assert.Equal(Color.FromRgb(0x00, 0x00, 0x00), SignatureStrokeStyle.GetColor(SignatureInkColor.Black));
        Assert.Equal(Color.FromRgb(0x19, 0x41, 0x96), SignatureStrokeStyle.GetColor(SignatureInkColor.Blue));
        Assert.Equal(2d, SignatureStrokeStyle.GetWidthDip(SignatureInkWidth.Thin));
        Assert.Equal(3.5d, SignatureStrokeStyle.GetWidthDip(SignatureInkWidth.Medium));
        Assert.Equal(5d, SignatureStrokeStyle.GetWidthDip(SignatureInkWidth.Thick));
    }

    [Fact]
    public void HasUsefulInk_RejectsEmptyOnePointAndTooSmallBounds()
    {
        Assert.False(SignatureInkRenderer.HasUsefulInk(new StrokeCollection()));
        Assert.False(SignatureInkRenderer.HasUsefulInk(new StrokeCollection { CreateStroke([(10, 10)], SignatureInkColor.Black, SignatureInkWidth.Medium) }));
        Assert.False(SignatureInkRenderer.HasUsefulInk(new StrokeCollection { CreateStroke([(10, 10), (10.25, 10.25)], SignatureInkColor.Black, SignatureInkWidth.Thin) }));
    }

    [Fact]
    public void HasUsefulInk_AcceptsNormalSignatureStroke()
    {
        var strokes = new StrokeCollection { CreateStroke([(10, 20), (50, 45), (90, 22)], SignatureInkColor.Black, SignatureInkWidth.Medium) };
        Assert.True(SignatureInkRenderer.HasUsefulInk(strokes));
    }

    [Fact]
    public void Render_BlackStroke_HasTransparentBackgroundAndVisibleBlackInk()
    {
        var asset = SignatureInkRenderer.Render(new StrokeCollection { CreateStroke([(20, 20), (80, 55), (140, 20)], SignatureInkColor.Black, SignatureInkWidth.Medium) });
        var pixels = asset.BgraPixels.Span;
        Assert.Contains((byte)0, AlphaValues(asset));
        Assert.True(FindInkPixel(pixels, isBlue: false));
    }

    [Fact]
    public void Render_BlueStroke_Uses194196WithAlpha()
    {
        var asset = SignatureInkRenderer.Render(new StrokeCollection { CreateStroke([(20, 20), (80, 55), (140, 20)], SignatureInkColor.Blue, SignatureInkWidth.Medium) });
        Assert.True(FindInkPixel(asset.BgraPixels.Span, isBlue: true));
    }

    [Fact]
    public void Render_CropsAroundInkAndIncludesBoundedPadding()
    {
        var asset = SignatureInkRenderer.Render(new StrokeCollection { CreateStroke([(100, 100), (150, 130), (200, 100)], SignatureInkColor.Black, SignatureInkWidth.Medium) });
        Assert.InRange(asset.PixelWidth, 150, 450);
        Assert.InRange(asset.PixelHeight, 80, 250);
        Assert.Contains((byte)0, AlphaValues(asset));
    }

    [Fact]
    public void Render_MixedAttributes_PreserveEachStrokeColorAndRelativeWidth()
    {
        var strokes = new StrokeCollection
        {
            CreateStroke([(10, 20), (80, 20)], SignatureInkColor.Black, SignatureInkWidth.Thin),
            CreateStroke([(10, 50), (80, 50)], SignatureInkColor.Blue, SignatureInkWidth.Thick)
        };
        var asset = SignatureInkRenderer.Render(strokes);
        var pixels = asset.BgraPixels.Span;
        Assert.True(FindInkPixel(pixels, isBlue: false));
        Assert.True(FindInkPixel(pixels, isBlue: true));
    }

    [Fact]
    public void Render_ThinMediumThick_HaveIncreasingVisibleCoverage()
    {
        var points = new[] { (10d, 20d), (55d, 28d), (100d, 20d) };
        var thin = CountVisible(SignatureInkRenderer.Render(new StrokeCollection { CreateStroke(points, SignatureInkColor.Black, SignatureInkWidth.Thin) }));
        var medium = CountVisible(SignatureInkRenderer.Render(new StrokeCollection { CreateStroke(points, SignatureInkColor.Black, SignatureInkWidth.Medium) }));
        var thick = CountVisible(SignatureInkRenderer.Render(new StrokeCollection { CreateStroke(points, SignatureInkColor.Black, SignatureInkWidth.Thick) }));
        Assert.True(thin < medium);
        Assert.True(medium < thick);
    }

    [Fact]
    public void Render_DoesNotMutateSourceStrokesOrDrawingAttributes()
    {
        var stroke = CreateStroke([(10, 10), (80, 40)], SignatureInkColor.Blue, SignatureInkWidth.Medium);
        var beforePoints = stroke.StylusPoints.Select(p => (p.X, p.Y)).ToArray();
        var beforeColor = stroke.DrawingAttributes.Color;
        var beforeWidth = stroke.DrawingAttributes.Width;
        SignatureInkRenderer.Render(new StrokeCollection { stroke });
        Assert.Equal(beforePoints, stroke.StylusPoints.Select(p => (p.X, p.Y)).ToArray());
        Assert.Equal(beforeColor, stroke.DrawingAttributes.Color);
        Assert.Equal(beforeWidth, stroke.DrawingAttributes.Width);
    }

    [Fact]
    public void Render_RejectsGeometryBeyondTwentyMillionPixelsBeforeOutputAllocation()
    {
        var huge = CreateStroke([(0, 0), (10000, 10000)], SignatureInkColor.Black, SignatureInkWidth.Medium);
        Assert.ThrowsAny<Exception>(() => SignatureInkRenderer.Render(new StrokeCollection { huge }));
    }

    [Fact]
    public void EquivalentStrokeGeometry_ProducesStableDimensionsBoundsAndInkColors()
    {
        var first = SignatureInkRenderer.Render(new StrokeCollection { CreateStroke([(10, 20), (60, 55), (120, 20)], SignatureInkColor.Blue, SignatureInkWidth.Medium) });
        var second = SignatureInkRenderer.Render(new StrokeCollection { CreateStroke([(10, 20), (60, 55), (120, 20)], SignatureInkColor.Blue, SignatureInkWidth.Medium) });
        Assert.Equal(first.PixelWidth, second.PixelWidth);
        Assert.Equal(first.PixelHeight, second.PixelHeight);
        Assert.Equal(first.Stride, second.Stride);
        Assert.Equal(CountVisible(first), CountVisible(second));
        Assert.True(FindInkPixel(first.BgraPixels.Span, isBlue: true));
        Assert.True(FindInkPixel(second.BgraPixels.Span, isBlue: true));
    }

    private static Stroke CreateStroke((double X, double Y)[] points, SignatureInkColor color, SignatureInkWidth width)
    {
        var stylusPoints = new StylusPointCollection(points.Select(p => new StylusPoint(p.X, p.Y)));
        return new Stroke(stylusPoints, SignatureStrokeStyle.CreateDrawingAttributes(color, width));
    }

    private static byte[] AlphaValues(SignatureAsset asset)
    {
        var span = asset.BgraPixels.Span;
        var alpha = new byte[asset.PixelWidth * asset.PixelHeight];
        for (var row = 0; row < asset.PixelHeight; row++)
        for (var x = 0; x < asset.PixelWidth; x++)
            alpha[(row * asset.PixelWidth) + x] = span[(row * asset.Stride) + (x * 4) + 3];
        return alpha;
    }

    private static int CountVisible(SignatureAsset asset)
        => AlphaValues(asset).Count(a => a > 16);

    private static bool FindInkPixel(ReadOnlySpan<byte> pixels, bool isBlue)
    {
        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            var b = pixels[i];
            var g = pixels[i + 1];
            var r = pixels[i + 2];
            var a = pixels[i + 3];
            if (a < 128)
                continue;
            if (isBlue && Math.Abs(r - 0x19) <= 3 && Math.Abs(g - 0x41) <= 3 && Math.Abs(b - 0x96) <= 3)
                return true;
            if (!isBlue && r <= 3 && g <= 3 && b <= 3)
                return true;
        }
        return false;
    }
}
