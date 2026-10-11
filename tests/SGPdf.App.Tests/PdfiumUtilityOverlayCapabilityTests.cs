using System.Text;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfiumUtilityOverlayCapabilityTests
{
    private const string SupportedOverlayText = "NIÑO Bogotá — € Ω";

    [Fact]
    public void PinnedFallbackFont_CoversRepresentativeOverlayUnicode_AndRejectsKnownUnsupportedCjk()
    {
        foreach (var rune in SupportedOverlayText.EnumerateRunes().Where(rune => !Rune.IsWhiteSpace(rune)))
            Assert.True(FallbackFontAsset.SupportsRune(rune), $"Expected pinned fallback support for U+{rune.Value:X4}.");

        Assert.False(FallbackFontAsset.SupportsRune(new Rune('東')));
    }

    [Fact]
    public void TextOverlay_SaveReopen_RoundTripsUnicodeColorAlphaMatrixZOrderAndRender()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var output = fixture.Output("text-overlay.pdf");

        var result = UtilityOverlayNativeHarness.CreateTextOverlayCopy(
            fixture.SourcePath,
            output,
            SupportedOverlayText);

        Assert.True(File.Exists(output));
        Assert.Equal(3, result.Objects.Count);
        Assert.Equal(new[] { 1, 1, 1 }, result.Objects.Select(item => item.Type));
        Assert.Equal(SupportedOverlayText, result.Objects[0].Text);
        Assert.Equal(UtilityOverlayNativeHarness.BaseText, result.Objects[1].Text?.TrimEnd());
        Assert.Equal(SupportedOverlayText, result.Objects[2].Text);

        AssertOverlayColor(result.Objects[0], UtilityOverlayNativeHarness.OverlayAlpha);
        AssertOverlayColor(result.Objects[2], UtilityOverlayNativeHarness.OverlayAlpha);
        AssertMatrix(result.Objects[0].Matrix, UtilityOverlayNativeHarness.BackTextMatrix, 0.002d);
        AssertMatrix(result.Objects[2].Matrix, UtilityOverlayNativeHarness.FrontTextMatrix, 0.002d);
        Assert.True(result.RenderPixelWidth > 0);
        Assert.True(result.RenderPixelHeight > 0);
    }

    [Fact]
    public void ImageOverlay_SaveReopen_RoundTripsBitmapAlphaMatrixZOrderAndRender()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var output = fixture.Output("image-overlay.pdf");

        var result = UtilityOverlayNativeHarness.CreateImageOverlayCopy(fixture.SourcePath, output);

        Assert.True(File.Exists(output));
        Assert.Equal(3, result.Objects.Count);
        Assert.Equal(new[] { 3, 1, 3 }, result.Objects.Select(item => item.Type));
        AssertMatrix(result.Objects[0].Matrix, UtilityOverlayNativeHarness.BackImageMatrix, 0.002d);
        AssertMatrix(result.Objects[2].Matrix, UtilityOverlayNativeHarness.FrontImageMatrix, 0.002d);

        var backSample = PixelAt(result.RenderedPage, 73, 189);
        Assert.InRange(backSample.R, (byte)165, (byte)190);
        Assert.InRange(backSample.G, (byte)195, (byte)220);
        Assert.InRange(backSample.B, (byte)230, (byte)252);
        Assert.Equal((byte)255, backSample.A);

        var frontSample = PixelAt(result.RenderedPage, 143, 282);
        Assert.InRange(frontSample.R, (byte)235, (byte)252);
        Assert.InRange(frontSample.G, (byte)180, (byte)215);
        Assert.InRange(frontSample.B, (byte)130, (byte)180);
        Assert.Equal((byte)255, frontSample.A);

        Assert.True(result.RenderPixelWidth > 0);
        Assert.True(result.RenderPixelHeight > 0);
    }

    private static void AssertOverlayColor(UtilityOverlayObjectSnapshot item, uint alpha)
    {
        Assert.True(item.FillColorReadable);
        Assert.Equal(UtilityOverlayNativeHarness.OverlayRed, item.Red);
        Assert.Equal(UtilityOverlayNativeHarness.OverlayGreen, item.Green);
        Assert.Equal(UtilityOverlayNativeHarness.OverlayBlue, item.Blue);
        Assert.Equal(alpha, item.Alpha);
    }

    private static Rgba PixelAt(PdfRenderedPage page, int x, int y)
    {
        Assert.InRange(x, 0, page.PixelWidth - 1);
        Assert.InRange(y, 0, page.PixelHeight - 1);
        var offset = checked((y * page.Stride) + (x * 4));
        return new Rgba(
            page.Pixels[offset + 2],
            page.Pixels[offset + 1],
            page.Pixels[offset],
            page.Pixels[offset + 3]);
    }

    private static void AssertMatrix(PdfObjectMatrix actual, PdfObjectMatrix expected, double tolerance)
    {
        Assert.InRange(actual.A, expected.A - tolerance, expected.A + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
        Assert.InRange(actual.C, expected.C - tolerance, expected.C + tolerance);
        Assert.InRange(actual.D, expected.D - tolerance, expected.D + tolerance);
        Assert.InRange(actual.E, expected.E - tolerance, expected.E + tolerance);
        Assert.InRange(actual.F, expected.F - tolerance, expected.F + tolerance);
    }

    private readonly record struct Rgba(byte R, byte G, byte B, byte A);
}
