using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignatureImageProcessorTests
{
    private static readonly SignaturePaperColor WhitePaper = new(255, 255, 255);

    [Fact]
    public void AutomaticSettings_AreFrozenAndRangesValidate()
    {
        var settings = SignatureImageProcessingSettings.Automatic;
        Assert.Equal(65, settings.BackgroundRemoval);
        Assert.Equal(0, settings.Brightness);
        Assert.Equal(20, settings.Contrast);
        Assert.Equal(SignatureInkStyle.Original, settings.InkStyle);
        Assert.True(settings.AutoCrop);
        settings.Validate();
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureImageProcessingSettings(-1, 0, 0, SignatureInkStyle.Original, true).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureImageProcessingSettings(0, 101, 0, SignatureInkStyle.Original, true).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureImageProcessingSettings(0, 0, -101, SignatureInkStyle.Original, true).Validate());
    }

    [Theory]
    [InlineData(255, 255, 255)]
    [InlineData(230, 230, 230)]
    [InlineData(245, 235, 220)]
    [InlineData(225, 235, 248)]
    public void EstimatePaper_ToleratesWhiteGrayWarmAndCoolPaperFixtures(byte r, byte g, byte b)
    {
        var source = CreatePaperWithInk(12, 12, b, g, r, 255, 4, 4, 4, 4, 0, 0, 0, 255);
        var paper = SignatureImageProcessor.EstimatePaper(source);
        Assert.Equal(r, paper.R);
        Assert.Equal(g, paper.G);
        Assert.Equal(b, paper.B);
    }

    [Fact]
    public void EstimatePaper_RejectsNoPlausibleLightBackground()
    {
        var source = SolidSource(12, 12, 20, 20, 20, 255);
        Assert.Throws<InvalidDataException>(() => SignatureImageProcessor.EstimatePaper(source));
    }

    [Fact]
    public void PurePaper_BecomesTransparent()
    {
        var source = CreatePaperWithInk(16, 16, 255, 255, 255, 255, 5, 5, 6, 6, 0, 0, 0, 255);
        var result = SignatureImageProcessor.Process(source, AutomaticNoCrop(), WhitePaper);
        Assert.Equal(0, Pixel(result, 0, 0).A);
        Assert.Equal(255, Pixel(result, 7, 7).A);
    }

    [Fact]
    public void NearPaper_ProducesIntermediateSoftAlpha()
    {
        var source = CreatePaperWithInk(16, 16, 255, 255, 255, 255, 5, 5, 6, 6, 0, 0, 0, 255,
            (pixels, width) => SetPixel(pixels, width, 1, 1, 230, 230, 230, 255));
        var result = SignatureImageProcessor.Process(source, AutomaticNoCrop(), WhitePaper);
        Assert.InRange(Pixel(result, 1, 1).A, (byte)1, (byte)254);
    }

    [Fact]
    public void BlackAndBlueInk_RemainVisibleInOriginalMode()
    {
        var source = CreatePaperWithInk(18, 18, 255, 255, 255, 255, 4, 4, 5, 5, 0, 0, 0, 255,
            (pixels, width) => FillRect(pixels, width, 10, 10, 5, 5, 180, 70, 30, 255));
        var result = SignatureImageProcessor.Process(source, AutomaticNoCrop(), WhitePaper);
        Assert.True(Pixel(result, 6, 6).A >= 250);
        Assert.True(Pixel(result, 12, 12).A >= 250);
    }

    [Fact]
    public void BlackRecolor_PreservesComputedAlpha()
    {
        var source = CreatePaperWithInk(16, 16, 255, 255, 255, 255, 5, 5, 6, 6, 40, 50, 60, 180);
        var result = SignatureImageProcessor.Process(source, new(65, 0, 0, SignatureInkStyle.Black, false), WhitePaper);
        var pixel = Pixel(result, 7, 7);
        Assert.Equal((byte)0, pixel.R); Assert.Equal((byte)0, pixel.G); Assert.Equal((byte)0, pixel.B);
        Assert.InRange(pixel.A, (byte)1, (byte)180);
    }

    [Fact]
    public void BlueRecolor_Uses194196AndPreservesAlpha()
    {
        var source = CreatePaperWithInk(16, 16, 255, 255, 255, 255, 5, 5, 6, 6, 10, 20, 30, 200);
        var result = SignatureImageProcessor.Process(source, new(65, 0, 0, SignatureInkStyle.Blue, false), WhitePaper);
        var pixel = Pixel(result, 7, 7);
        Assert.Equal((byte)25, pixel.R); Assert.Equal((byte)65, pixel.G); Assert.Equal((byte)150, pixel.B);
        Assert.InRange(pixel.A, (byte)1, (byte)200);
    }

    [Fact]
    public void OriginalAlpha_MultipliesCleanupAlphaAndNeverBecomesMoreOpaque()
    {
        var source = CreatePaperWithInk(16, 16, 255, 255, 255, 255, 5, 5, 6, 6, 0, 0, 0, 128);
        var result = SignatureImageProcessor.Process(source, AutomaticNoCrop(), WhitePaper);
        Assert.InRange(Pixel(result, 7, 7).A, (byte)1, (byte)128);
        Assert.Equal((byte)0, Pixel(result, 0, 0).A);
    }

    [Fact]
    public void StrongerBackgroundRemoval_NeverRestoresPaperOpacity()
    {
        var source = CreatePaperWithInk(16, 16, 255, 255, 255, 255, 5, 5, 6, 6, 0, 0, 0, 255,
            (pixels, width) => SetPixel(pixels, width, 1, 1, 230, 230, 230, 255));
        var weak = SignatureImageProcessor.Process(source, new(20, 0, 0, SignatureInkStyle.Original, false), WhitePaper);
        var strong = SignatureImageProcessor.Process(source, new(80, 0, 0, SignatureInkStyle.Original, false), WhitePaper);
        Assert.True(Pixel(strong, 1, 1).A <= Pixel(weak, 1, 1).A);
    }

    [Fact]
    public void BrightnessAndContrast_ClampAtSupportedExtremes()
    {
        var source = CreatePaperWithInk(16, 16, 255, 255, 255, 255, 5, 5, 6, 6, 60, 80, 100, 255);
        var bright = SignatureImageProcessor.Process(source, new(65, 100, 100, SignatureInkStyle.Original, false), WhitePaper);
        var dark = SignatureImageProcessor.Process(source, new(65, -100, -100, SignatureInkStyle.Original, false), WhitePaper);
        var bp = Pixel(bright, 7, 7); var dp = Pixel(dark, 7, 7);
        Assert.InRange(bp.R, (byte)0, (byte)255); Assert.InRange(bp.G, (byte)0, (byte)255); Assert.InRange(bp.B, (byte)0, (byte)255);
        Assert.InRange(dp.R, (byte)0, (byte)255); Assert.InRange(dp.G, (byte)0, (byte)255); Assert.InRange(dp.B, (byte)0, (byte)255);
    }

    [Fact]
    public void SameInputAndSettings_AreByteDeterministic()
    {
        var source = CreatePaperWithInk(16, 16, 250, 248, 245, 255, 5, 5, 6, 6, 20, 40, 80, 255);
        var paper = SignatureImageProcessor.EstimatePaper(source);
        var first = SignatureImageProcessor.Process(source, SignatureImageProcessingSettings.Automatic, paper);
        var second = SignatureImageProcessor.Process(source, SignatureImageProcessingSettings.Automatic, paper);
        Assert.Equal(first.PixelWidth, second.PixelWidth);
        Assert.Equal(first.PixelHeight, second.PixelHeight);
        Assert.Equal(first.BgraPixels.ToArray(), second.BgraPixels.ToArray());
    }

    [Fact]
    public void BlankWhiteImage_IsRejectedAsNoUsableSignature()
    {
        var error = Assert.Throws<InvalidDataException>(() => SignatureImageProcessor.Process(SolidSource(16, 16, 255, 255, 255, 255), AutomaticNoCrop(), WhitePaper));
        Assert.Contains("firma", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TinyIsolatedNoise_IsRejected()
    {
        var source = SolidSource(16, 16, 255, 255, 255, 255,
            (pixels, width) => SetPixel(pixels, width, 8, 8, 0, 0, 0, 255));
        Assert.Throws<InvalidDataException>(() => SignatureImageProcessor.Process(source, AutomaticNoCrop(), WhitePaper));
    }

    [Fact]
    public void AutoCrop_FindsContentAndAddsBoundedPadding()
    {
        var result = SignatureImageProcessor.Process(
            CreatePaperWithInk(20, 20, 255, 255, 255, 255, 7, 7, 6, 6, 0, 0, 0, 255),
            SignatureImageProcessingSettings.Automatic,
            WhitePaper);
        Assert.Equal(14, result.PixelWidth);
        Assert.Equal(14, result.PixelHeight);
    }

    [Fact]
    public void CropDisabled_PreservesSourceDimensions()
    {
        var result = SignatureImageProcessor.Process(
            CreatePaperWithInk(20, 20, 255, 255, 255, 255, 7, 7, 6, 6, 0, 0, 0, 255),
            AutomaticNoCrop(), WhitePaper);
        Assert.Equal(20, result.PixelWidth); Assert.Equal(20, result.PixelHeight);
    }

    [Fact]
    public void OnePixelEdgeGeometry_DoesNotCrashAndRejectsBlankContentCleanly()
    {
        Assert.Throws<InvalidDataException>(() => SignatureImageProcessor.Process(SolidSource(1, 1, 255, 255, 255, 255), AutomaticNoCrop(), WhitePaper));
    }

    private static SignatureImageProcessingSettings AutomaticNoCrop() => SignatureImageProcessingSettings.Automatic with { AutoCrop = false };

    private static SignaturePhotoSource SolidSource(int width, int height, byte b, byte g, byte r, byte a, Action<byte[], int>? customize = null)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            SetPixel(pixels, width, x, y, b, g, r, a);
        customize?.Invoke(pixels, width);
        return new SignaturePhotoSource(width, height, width * 4, pixels, "synthetic.png");
    }

    private static SignaturePhotoSource CreatePaperWithInk(
        int width, int height, byte paperB, byte paperG, byte paperR, byte paperA,
        int inkX, int inkY, int inkWidth, int inkHeight, byte inkB, byte inkG, byte inkR, byte inkA,
        Action<byte[], int>? customize = null)
    {
        return SolidSource(width, height, paperB, paperG, paperR, paperA, (pixels, strideWidth) =>
        {
            FillRect(pixels, strideWidth, inkX, inkY, inkWidth, inkHeight, inkB, inkG, inkR, inkA);
            customize?.Invoke(pixels, strideWidth);
        });
    }

    private static void FillRect(byte[] pixels, int width, int x, int y, int rectWidth, int rectHeight, byte b, byte g, byte r, byte a)
    {
        for (var yy = y; yy < y + rectHeight; yy++)
        for (var xx = x; xx < x + rectWidth; xx++)
            SetPixel(pixels, width, xx, yy, b, g, r, a);
    }

    private static void SetPixel(byte[] pixels, int width, int x, int y, byte b, byte g, byte r, byte a)
    {
        var offset = (y * width + x) * 4;
        pixels[offset] = b; pixels[offset + 1] = g; pixels[offset + 2] = r; pixels[offset + 3] = a;
    }

    private static (byte B, byte G, byte R, byte A) Pixel(SignatureAsset asset, int x, int y)
    {
        var span = asset.BgraPixels.Span;
        var offset = y * asset.Stride + x * 4;
        return (span[offset], span[offset + 1], span[offset + 2], span[offset + 3]);
    }
}
