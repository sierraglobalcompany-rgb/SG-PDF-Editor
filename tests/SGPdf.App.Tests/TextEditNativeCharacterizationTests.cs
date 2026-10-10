using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextEditNativeCharacterizationTests
{
    [Fact]
    public void PinnedPdfium_CanReadAndSetText_SaveGenerateReopen()
    {
        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var before = TextEditNativeCharacterizationHarness.InspectFirstText(fixture.Path);

        Assert.Equal("CASA 123", before.Text);
        Assert.InRange(before.FontSize, 17.99f, 18.01f);
        Assert.Contains("Helvetica", before.FontName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, before.RenderMode);
        Assert.Equal(255u, before.Alpha);

        var output = fixture.NewOutputPath("in-place.pdf");
        TextEditNativeCharacterizationHarness.SetFirstTextAndSave(
            fixture.Path,
            output,
            "CASA 321");

        var after = TextEditNativeCharacterizationHarness.InspectFirstText(output);
        Assert.Equal("CASA 321", after.Text);
        Assert.Equal(before.PageObjectIndex, after.PageObjectIndex);
        Assert.InRange(after.FontSize, 17.99f, 18.01f);
        Assert.Equal(before.FontName, after.FontName);
        Assert.Equal(before.Red, after.Red);
        Assert.Equal(before.Green, after.Green);
        Assert.Equal(before.Blue, after.Blue);
        Assert.Equal(before.Alpha, after.Alpha);
        AssertMatrixNear(before.Matrix, after.Matrix);

        using var session = PdfDocumentSession.Open(output);
        var rendered = session.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
        Assert.NotEmpty(rendered.BgraPixels.Span);
    }

    [Fact]
    public void PinnedPdfium_CanLoadTtfCreateTextObject_InsertSameIndex_SaveReopen()
    {
        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var before = TextEditNativeCharacterizationHarness.InspectFirstText(fixture.Path);
        var fixtureFont = TextEditNativeCharacterizationHarness.ResolveWindowsFixtureTtf();
        var output = fixture.NewOutputPath("fallback.pdf");

        TextEditNativeCharacterizationHarness.ReplaceFirstTextWithTtfAndSave(
            fixture.Path,
            output,
            fixtureFont,
            "NIÑO áé");

        var after = TextEditNativeCharacterizationHarness.InspectFirstText(output);
        Assert.Equal("NIÑO áé", after.Text);
        Assert.Equal(before.PageObjectIndex, after.PageObjectIndex);
        Assert.InRange(after.FontSize, 17.99f, 18.01f);
        Assert.False(string.IsNullOrWhiteSpace(after.FontName));
        Assert.NotEqual(before.FontName, after.FontName);
        Assert.Equal(before.Red, after.Red);
        Assert.Equal(before.Green, after.Green);
        Assert.Equal(before.Blue, after.Blue);
        Assert.Equal(before.Alpha, after.Alpha);
        AssertMatrixNear(before.Matrix, after.Matrix);

        using var session = PdfDocumentSession.Open(output);
        var rendered = session.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
        Assert.NotEmpty(rendered.BgraPixels.Span);
    }

    [Fact]
    public void PinnedPdfium_RepeatedProbe_ReleasesFontPageDocumentHandles()
    {
        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var fixtureFont = TextEditNativeCharacterizationHarness.ResolveWindowsFixtureTtf();

        for (var iteration = 0; iteration < 5; iteration++)
        {
            var output = fixture.NewOutputPath($"repeat-{iteration}.pdf");
            TextEditNativeCharacterizationHarness.ReplaceFirstTextWithTtfAndSave(
                fixture.Path,
                output,
                fixtureFont,
                $"NIÑO {iteration}");

            var snapshot = TextEditNativeCharacterizationHarness.InspectFirstText(output);
            Assert.Equal($"NIÑO {iteration}", snapshot.Text);

            File.Delete(output);
            Assert.False(File.Exists(output));
        }
    }

    private static void AssertMatrixNear(PdfObjectMatrix expected, PdfObjectMatrix actual)
    {
        const double tolerance = 0.05d;
        Assert.InRange(actual.A, expected.A - tolerance, expected.A + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
        Assert.InRange(actual.C, expected.C - tolerance, expected.C + tolerance);
        Assert.InRange(actual.D, expected.D - tolerance, expected.D + tolerance);
        Assert.InRange(actual.E, expected.E - tolerance, expected.E + tolerance);
        Assert.InRange(actual.F, expected.F - tolerance, expected.F + tolerance);
    }
}
