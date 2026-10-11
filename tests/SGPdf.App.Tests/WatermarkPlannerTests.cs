using System.Reflection;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class WatermarkPlannerTests
{
    private const string PlannerTypeName = "SGPdf.App.Features.Utilities.WatermarkPlanner";
    private const string AnchorTypeName = "SGPdf.App.Features.Utilities.WatermarkAnchor";

    [Fact]
    public void Plan_SelectedRange_ProducesPlacementsInResolvedOrder()
    {
        var placements = Plan(
            new TextOverlaySettings("CONFIDENTIAL", 24d, 0.35d, TextOverlayLayer.Background),
            UtilityPageRange.Explicit(1, 2),
            Pages(600d, 800d, 3),
            "Center",
            0d,
            0d,
            0d);

        Assert.Equal(new[] { 1, 2 }, placements.Select(item => item.PageIndex).ToArray());
        Assert.All(placements, item =>
        {
            Assert.Equal("CONFIDENTIAL", item.Text);
            Assert.Equal(24d, item.FontSize);
            Assert.Equal(0.35d, item.Opacity);
            Assert.Equal(TextOverlayLayer.Background, item.Layer);
        });
    }

    [Theory]
    [InlineData("BottomLeft", 36d, 36d)]
    [InlineData("BottomCenter", 300d, 36d)]
    [InlineData("BottomRight", 564d, 36d)]
    [InlineData("MiddleLeft", 36d, 400d)]
    [InlineData("Center", 300d, 400d)]
    [InlineData("MiddleRight", 564d, 400d)]
    [InlineData("TopLeft", 36d, 764d)]
    [InlineData("TopCenter", 300d, 764d)]
    [InlineData("TopRight", 564d, 764d)]
    public void Plan_UsesNineStandardAnchorOrigins(string anchorName, double expectedX, double expectedY)
    {
        var placement = Assert.Single(Plan(
            new TextOverlaySettings("WM", 18d, 0.5d, TextOverlayLayer.Foreground),
            UtilityPageRange.All,
            Pages(600d, 800d, 1),
            anchorName,
            0d,
            0d,
            0d));

        AssertMatrix(
            new PdfObjectMatrix(1d, 0d, 0d, 1d, expectedX, expectedY),
            placement.Matrix,
            0.000001d);
    }

    [Fact]
    public void Plan_AppliesOffsetsAndRotationToTheAnchorOrigin()
    {
        const double degrees = 30d;
        var radians = degrees * Math.PI / 180d;

        var placement = Assert.Single(Plan(
            new TextOverlaySettings("ROTATED", 20d, 0.6d, TextOverlayLayer.Foreground),
            UtilityPageRange.Current(0),
            Pages(600d, 800d, 1),
            "Center",
            12d,
            -8d,
            degrees));

        AssertMatrix(
            new PdfObjectMatrix(
                Math.Cos(radians),
                Math.Sin(radians),
                -Math.Sin(radians),
                Math.Cos(radians),
                312d,
                392d),
            placement.Matrix,
            0.000001d);
    }

    [Fact]
    public void Plan_RejectsNonFiniteGeometryInputs()
    {
        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Plan(
            new TextOverlaySettings("WM", 18d, 0.5d, TextOverlayLayer.Foreground),
            UtilityPageRange.All,
            new[] { new PdfPageSize(double.NaN, 800d) },
            "Center",
            0d,
            0d,
            0d));

        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Plan(
            new TextOverlaySettings("WM", 18d, 0.5d, TextOverlayLayer.Foreground),
            UtilityPageRange.All,
            Pages(600d, 800d, 1),
            "Center",
            0d,
            0d,
            double.PositiveInfinity));
    }

    private static IReadOnlyList<TextOverlayPlacement> Plan(
        TextOverlaySettings settings,
        UtilityPageRange range,
        IReadOnlyList<PdfPageSize> pageSizes,
        string anchorName,
        double offsetXPoints,
        double offsetYPoints,
        double rotationDegrees)
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
            offsetXPoints,
            offsetYPoints,
            rotationDegrees
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
