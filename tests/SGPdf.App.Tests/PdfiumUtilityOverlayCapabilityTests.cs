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
        Assert.Equal(
            new[] { SupportedOverlayText, UtilityOverlayNativeHarness.BaseText, SupportedOverlayText },
            result.Objects.Select(item => item.Text));

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
        Assert.Equal(UtilityOverlayNativeHarness.ImageAlpha, result.Objects[0].Alpha);
        Assert.Equal(UtilityOverlayNativeHarness.ImageAlpha, result.Objects[2].Alpha);
        AssertMatrix(result.Objects[0].Matrix, UtilityOverlayNativeHarness.BackImageMatrix, 0.002d);
        AssertMatrix(result.Objects[2].Matrix, UtilityOverlayNativeHarness.FrontImageMatrix, 0.002d);
        Assert.True(result.RenderPixelWidth > 0);
        Assert.True(result.RenderPixelHeight > 0);
    }

    private static void AssertOverlayColor(UtilityOverlayObjectSnapshot item, uint alpha)
    {
        Assert.Equal(UtilityOverlayNativeHarness.OverlayRed, item.Red);
        Assert.Equal(UtilityOverlayNativeHarness.OverlayGreen, item.Green);
        Assert.Equal(UtilityOverlayNativeHarness.OverlayBlue, item.Blue);
        Assert.Equal(alpha, item.Alpha);
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
}
