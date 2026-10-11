using System.Reflection;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class UtilityOverlayColorTests
{
    [Fact]
    public void WatermarkColor_RoundTripsThroughPlannerWriterAndReopen()
    {
        var constructor = typeof(TextOverlaySettings).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[]
            {
                typeof(string),
                typeof(double),
                typeof(double),
                typeof(TextOverlayLayer),
                typeof(PdfTextFillColor)
            },
            modifiers: null);
        Assert.NotNull(constructor);

        var requestedColor = new PdfTextFillColor(12u, 34u, 56u, 200u);
        var settings = Assert.IsType<TextOverlaySettings>(constructor!.Invoke(new object[]
        {
            "COLOR-WM",
            22d,
            0.5d,
            TextOverlayLayer.Foreground,
            requestedColor
        }));

        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        IReadOnlyList<PdfPageSize> pageSizes;
        using (var source = PdfDocumentSession.Open(fixture.SourcePath))
            pageSizes = source.GetPageSizes();

        var placements = new WatermarkPlanner().Plan(
            settings,
            UtilityPageRange.All,
            pageSizes,
            WatermarkAnchor.Center,
            0d,
            0d,
            0d);
        var fillColorProperty = typeof(TextOverlayPlacement).GetProperty(
            "FillColor",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(fillColorProperty);
        Assert.Equal(requestedColor, Assert.IsType<PdfTextFillColor>(fillColorProperty!.GetValue(Assert.Single(placements))));

        var destination = fixture.Output("watermark-color.pdf");
        var result = new PdfTextOverlayWriter().SaveAsCopy(
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            placements,
            destination,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        using var output = PdfDocumentSession.Open(destination);
        var overlay = Assert.Single(output.GetTextObjects(0), item => item.Text == "COLOR-WM");
        Assert.Equal(12u, overlay.FillColor.Red);
        Assert.Equal(34u, overlay.FillColor.Green);
        Assert.Equal(56u, overlay.FillColor.Blue);
        Assert.InRange(overlay.FillColor.Alpha, 99u, 101u);
    }

    [Fact]
    public void PageNumberColor_PropagatesFromSettingsIntoPlacement()
    {
        var constructor = typeof(PageNumberingSettings).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[]
            {
                typeof(long),
                typeof(string),
                typeof(string),
                typeof(int),
                typeof(PdfTextFillColor)
            },
            modifiers: null);
        Assert.NotNull(constructor);

        var requestedColor = new PdfTextFillColor(90u, 80u, 70u, 255u);
        var settings = Assert.IsType<PageNumberingSettings>(constructor!.Invoke(new object[]
        {
            1L,
            "B-",
            string.Empty,
            4,
            requestedColor
        }));

        var placements = new PageNumberingPlanner().Plan(
            settings,
            UtilityPageRange.All,
            new[] { new PdfPageSize(600d, 800d) },
            PageNumberAnchor.FooterLeft,
            11d,
            0.75d,
            TextOverlayLayer.Foreground,
            0d,
            0d);

        var fillColorProperty = typeof(TextOverlayPlacement).GetProperty(
            "FillColor",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(fillColorProperty);
        Assert.Equal(requestedColor, Assert.IsType<PdfTextFillColor>(fillColorProperty!.GetValue(Assert.Single(placements))));
    }
}
