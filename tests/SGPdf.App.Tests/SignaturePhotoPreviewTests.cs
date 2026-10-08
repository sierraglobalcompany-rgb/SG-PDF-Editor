using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignaturePhotoPreviewTests
{
    [Fact]
    public void CreateReducedSource_PreservesSmallSourceDimensionsAndPixels()
    {
        var source = CreateSource(20, 10);
        var reduced = SignaturePhotoPreview.CreateReducedSource(source);
        Assert.Equal(20, reduced.PixelWidth);
        Assert.Equal(10, reduced.PixelHeight);
        Assert.Equal(source.BgraPixels.ToArray(), reduced.BgraPixels.ToArray());
    }

    [Fact]
    public void CreateReducedSource_LimitsLongestSideTo1200AndPreservesAspectRatio()
    {
        var source = CreateSource(1600, 800);
        var reduced = SignaturePhotoPreview.CreateReducedSource(source);
        Assert.Equal(1200, reduced.PixelWidth);
        Assert.Equal(600, reduced.PixelHeight);
    }

    [Fact]
    public void PreviewVersion_OlderRequestBecomesStaleAfterNewerRequest()
    {
        var version = new SignaturePhotoPreviewVersion();
        var first = version.BeginRequest();
        Assert.True(version.IsCurrent(first));
        var second = version.BeginRequest();
        Assert.False(version.IsCurrent(first));
        Assert.True(version.IsCurrent(second));
    }

    [Fact]
    public void FinalProcessingContract_UsesFullSourceRatherThanReducedPreview()
    {
        var source = CreatePaperWithInk(1400, 100);
        var reduced = SignaturePhotoPreview.CreateReducedSource(source);
        Assert.Equal(1200, reduced.PixelWidth);

        var settings = SignatureImageProcessingSettings.Automatic with { AutoCrop = false };
        var asset = SignaturePhotoDialog.ProcessFullSource(source, settings, new SignaturePaperColor(255, 255, 255));
        Assert.Equal(1400, asset.PixelWidth);
        Assert.Equal(100, asset.PixelHeight);
    }

    private static SignaturePhotoSource CreateSource(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            pixels[i * 4] = 255;
            pixels[i * 4 + 1] = 255;
            pixels[i * 4 + 2] = 255;
            pixels[i * 4 + 3] = 255;
        }
        return new SignaturePhotoSource(width, height, width * 4, pixels, "preview.png");
    }

    private static SignaturePhotoSource CreatePaperWithInk(int width, int height)
    {
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var y = 35; y < 65; y++)
        for (var x = 650; x < 750; x++)
        {
            var offset = (y * width + x) * 4;
            pixels[offset] = 0;
            pixels[offset + 1] = 0;
            pixels[offset + 2] = 0;
            pixels[offset + 3] = 255;
        }
        return new SignaturePhotoSource(width, height, width * 4, pixels, "full.png");
    }
}
