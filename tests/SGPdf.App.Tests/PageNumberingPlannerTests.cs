using System.Reflection;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PageNumberingPlannerTests
{
    private const string PlannerTypeName = "SGPdf.App.Features.Utilities.PageNumberingPlanner";
    private const string AnchorTypeName = "SGPdf.App.Features.Utilities.PageNumberAnchor";

    [Fact]
    public void Plan_SelectedRange_FormatsNumbersInResolvedOrder()
    {
        var placements = Plan(
            new PageNumberingSettings(startNumber: 100, prefix: "B-", suffix: "-X", paddingWidth: 4),
            UtilityPageRange.Explicit(1, 3),
            Pages(600d, 800d, 4),
            "FooterCenter",
            12d,
            0.85d,
            TextOverlayLayer.Foreground,
            0d,
            0d);

        Assert.Equal(new[] { 1, 2, 3 }, placements.Select(item => item.PageIndex).ToArray());
        Assert.Equal(new[] { "B-0100-X", "B-0101-X", "B-0102-X" }, placements.Select(item => item.Text).ToArray());
        Assert.All(placements, item =>
        {
            Assert.Equal(12d, item.FontSize);
            Assert.Equal(0.85d, item.Opacity);
            Assert.Equal(TextOverlayLayer.Foreground, item.Layer);
        });
    }

    [Theory]
    [InlineData("HeaderLeft", 36d, 764d)]
    [InlineData("HeaderCenter", 300d, 764d)]
    [InlineData("HeaderRight", 564d, 764d)]
    [InlineData("FooterLeft", 36d, 36d)]
    [InlineData("FooterCenter", 300d, 36d)]
    [InlineData("FooterRight", 564d, 36d)]
    public void Plan_UsesSixHeaderFooterAnchorOrigins(string anchorName, double expectedX, double expectedY)
    {
        var placement = Assert.Single(Plan(
            new PageNumberingSettings(startNumber: 7),
            UtilityPageRange.All,
            Pages(600d, 800d, 1),
            anchorName,
            11d,
            1d,
            TextOverlayLayer.Foreground,
            0d,
            0d));

        Assert.Equal("7", placement.Text);
        AssertMatrix(new PdfObjectMatrix(1d, 0d, 0d, 1d, expectedX, expectedY), placement.Matrix, 0.000001d);
    }

    [Fact]
    public void Plan_AppliesOffsetsAndPreservesOverlayStyle()
    {
        var placement = Assert.Single(Plan(
            new PageNumberingSettings(startNumber: 9, prefix: "P"),
            UtilityPageRange.Current(0),
            Pages(600d, 800d, 1),
            "HeaderRight",
            13d,
            0.4d,
            TextOverlayLayer.Background,
            -10d,
            -5d));

        Assert.Equal("P9", placement.Text);
        Assert.Equal(13d, placement.FontSize);
        Assert.Equal(0.4d, placement.Opacity);
        Assert.Equal(TextOverlayLayer.Background, placement.Layer);
        AssertMatrix(new PdfObjectMatrix(1d, 0d, 0d, 1d, 554d, 759d), placement.Matrix, 0.000001d);
    }

    [Fact]
    public void Plan_UnsupportedFormattedGlyphFailsClosed()
    {
        AssertInvocationThrows<NotSupportedException>(() => Plan(
            new PageNumberingSettings(prefix: "東"),
            UtilityPageRange.All,
            Pages(600d, 800d, 1),
            "FooterLeft",
            11d,
            1d,
            TextOverlayLayer.Foreground,
            0d,
            0d));
    }

    [Fact]
    public void Plan_NumberOverflowFailsClosed()
    {
        AssertInvocationThrows<OverflowException>(() => Plan(
            new PageNumberingSettings(startNumber: long.MaxValue),
            UtilityPageRange.All,
            Pages(600d, 800d, 2),
            "FooterLeft",
            11d,
            1d,
            TextOverlayLayer.Foreground,
            0d,
            0d));
    }

    [Fact]
    public void Plan_RejectsInvalidGeometryAndStyle()
    {
        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Plan(
            new PageNumberingSettings(),
            UtilityPageRange.All,
            Pages(600d, 800d, 1),
            "FooterLeft",
            0d,
            1d,
            TextOverlayLayer.Foreground,
            0d,
            0d));

        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Plan(
            new PageNumberingSettings(),
            UtilityPageRange.All,
            Pages(600d, 800d, 1),
            "FooterLeft",
            11d,
            1.1d,
            TextOverlayLayer.Foreground,
            0d,
            0d));

        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Plan(
            new PageNumberingSettings(),
            UtilityPageRange.All,
            new[] { new PdfPageSize(600d, double.NaN) },
            "FooterLeft",
            11d,
            1d,
            TextOverlayLayer.Foreground,
            0d,
            0d));
    }

    private static IReadOnlyList<TextOverlayPlacement> Plan(
        PageNumberingSettings settings,
        UtilityPageRange range,
        IReadOnlyList<PdfPageSize> pageSizes,
        string anchorName,
        double fontSize,
        double opacity,
        TextOverlayLayer layer,
        double offsetXPoints,
        double offsetYPoints)
    {
        var plannerType = RequireType(PlannerTypeName);
        var anchorType = RequireType(AnchorTypeName);
        var planner = Activator.CreateInstance(plannerType, nonPublic: true);
        Assert.NotNull(planner);
        var anchor = Enum.Parse(anchorType, anchorName, ignoreCase: false);
        var method = plannerType.GetMethod("Plan", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(planner, new object[]
        {
            settings,
            range,
            pageSizes,
            anchor,
            fontSize,
            opacity,
            layer,
            offsetXPoints,
            offsetYPoints
        });

        return Assert.IsAssignableFrom<IReadOnlyList<TextOverlayPlacement>>(result);
    }

    private static PdfPageSize[] Pages(double width, double height, int count) =>
        Enumerable.Range(0, count).Select(_ => new PdfPageSize(width, height)).ToArray();

    private static Type RequireType(string typeName)
    {
        var type = typeof(TextOverlaySettings).Assembly.GetType(typeName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static TException AssertInvocationThrows<TException>(Action action)
        where TException : Exception
    {
        var wrapper = Assert.Throws<TargetInvocationException>(action);
        return Assert.IsType<TException>(wrapper.InnerException);
    }

    private static void AssertMatrix(PdfObjectMatrix expected, PdfObjectMatrix actual, double tolerance)
    {
        Assert.InRange(actual.A, expected.A - tolerance, expected.A + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
        Assert.InRange(actual.C, expected.C - tolerance, expected.C + tolerance);
        Assert.InRange(actual.D, expected.D - tolerance, expected.D + tolerance);
        Assert.InRange(actual.E, expected.E - tolerance, expected.E + tolerance);
        Assert.InRange(actual.F, expected.F - tolerance, expected.F + tolerance);
    }
}
