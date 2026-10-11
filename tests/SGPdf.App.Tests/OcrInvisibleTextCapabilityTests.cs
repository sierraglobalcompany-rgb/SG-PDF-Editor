using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrInvisibleTextCapabilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvisibleText_SaveReopen_IsSearchableSelectableInvisibleAndRotationSafe(bool rotate90)
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90);

        Assert.False(snapshot.SourceHadSearchableText);
        Assert.True(File.Exists(snapshot.OutputPath));
        Assert.Equal(snapshot.SourceBytesBefore, snapshot.SourceBytesAfter);

        Assert.Equal(snapshot.SourceRender.PixelWidth, snapshot.OutputRender.PixelWidth);
        Assert.Equal(snapshot.SourceRender.PixelHeight, snapshot.OutputRender.PixelHeight);
        Assert.Equal(snapshot.SourceRender.Stride, snapshot.OutputRender.Stride);
        Assert.Equal(snapshot.SourceRender.Pixels, snapshot.OutputRender.Pixels);

        Assert.Equal(PdfiumNative.FPDF_TEXTRENDERMODE_INVISIBLE, snapshot.RenderMode);
        Assert.Contains("número", snapshot.ExtractedText, StringComparison.Ordinal);
        Assert.Contains("NIÑO", snapshot.ExtractedText, StringComparison.Ordinal);
        Assert.Contains("Bogotá", snapshot.ExtractedText, StringComparison.Ordinal);
        Assert.Contains("información", snapshot.ExtractedText, StringComparison.Ordinal);
        Assert.Contains("útil", snapshot.ExtractedText, StringComparison.Ordinal);
        Assert.Contains("98.765,43", snapshot.ExtractedText, StringComparison.Ordinal);

        Assert.NotEmpty(snapshot.NinoMatches);
        Assert.NotEmpty(snapshot.BogotaMatches);
        Assert.NotEmpty(snapshot.NumberMatches);
        Assert.NotEmpty(snapshot.TextRects);
        Assert.True(snapshot.TextGeometryIntersectsExpectedVisibleRegion);
    }
}
