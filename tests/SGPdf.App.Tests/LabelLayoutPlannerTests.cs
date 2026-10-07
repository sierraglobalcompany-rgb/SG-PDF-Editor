using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class LabelLayoutPlannerTests
{
    private static readonly ZplDocument Document = ZplDocumentParser.Parse(
        @"C:\labels\orders.zpl",
        "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ");

    [Fact]
    public void Thermal_UsesExactLabelSizeAndOneLabelPerPage()
    {
        var plan = CreatePlan(
            new ZplQuantitySelection(ZplQuantityMode.FromFile),
            new LabelLayoutSettings(LabelMediaKind.Thermal),
            labelWidthMm: 102,
            labelHeightMm: 152);

        Assert.Equal(102, plan.PageWidthMm, 3);
        Assert.Equal(152, plan.PageHeightMm, 3);
        Assert.Equal(1, plan.LabelsPerPage);
        Assert.Equal(5, plan.PageCount);

        var page = plan.GetPage(0);
        var placement = Assert.Single(page.Placements);
        Assert.Equal(0, placement.DesignIndex);
        Assert.Equal(0, placement.XMm, 3);
        Assert.Equal(0, placement.YMm, 3);
        Assert.Equal(102, placement.WidthMm, 3);
        Assert.Equal(152, placement.HeightMm, 3);
    }

    [Fact]
    public void A4PresetFour_UsesTwoByTwoGridWithMarginsAndGaps()
    {
        var settings = new LabelLayoutSettings(
            LabelMediaKind.A4,
            labelsPerPage: 4,
            horizontalMarginMm: 5,
            verticalMarginMm: 5,
            horizontalGapMm: 2,
            verticalGapMm: 3);

        var plan = CreatePlan(
            new ZplQuantitySelection(ZplQuantityMode.Custom, 3),
            settings,
            labelWidthMm: 50,
            labelHeightMm: 30);

        Assert.Equal(210, plan.PageWidthMm, 3);
        Assert.Equal(297, plan.PageHeightMm, 3);
        Assert.Equal(2, plan.Rows);
        Assert.Equal(2, plan.Columns);
        Assert.Equal(2, plan.PageCount);

        var firstPage = plan.GetPage(0);
        Assert.Equal(4, firstPage.Placements.Count);
        Assert.Equal(5, firstPage.Placements[0].XMm, 3);
        Assert.Equal(5, firstPage.Placements[0].YMm, 3);
        Assert.Equal(57, firstPage.Placements[1].XMm, 3);
        Assert.Equal(5, firstPage.Placements[1].YMm, 3);
        Assert.Equal(5, firstPage.Placements[2].XMm, 3);
        Assert.Equal(38, firstPage.Placements[2].YMm, 3);
    }

    [Fact]
    public void LetterLandscape_SwapsPhysicalPageDimensions()
    {
        var settings = new LabelLayoutSettings(
            LabelMediaKind.Letter,
            labelsPerPage: 2,
            orientation: LabelPageOrientation.Landscape);

        var plan = CreatePlan(
            new ZplQuantitySelection(ZplQuantityMode.OneEach),
            settings,
            labelWidthMm: 80,
            labelHeightMm: 40);

        Assert.Equal(279.4, plan.PageWidthMm, 3);
        Assert.Equal(215.9, plan.PageHeightMm, 3);
        Assert.Equal(1, plan.PageCount);
    }

    [Fact]
    public void CustomGrid_ExactFitSucceedsButFractionalOverflowFails()
    {
        var exact = new LabelLayoutSettings(
            LabelMediaKind.Custom,
            labelsPerPage: 4,
            rows: 2,
            columns: 2,
            horizontalMarginMm: 5,
            verticalMarginMm: 5,
            horizontalGapMm: 10,
            verticalGapMm: 10,
            customPageWidthMm: 100,
            customPageHeightMm: 100);

        var plan = CreatePlan(
            new ZplQuantitySelection(ZplQuantityMode.OneEach),
            exact,
            labelWidthMm: 40,
            labelHeightMm: 40);

        Assert.Equal(2, plan.Rows);
        Assert.Equal(2, plan.Columns);
        Assert.Equal(100, plan.PageWidthMm, 3);
        Assert.Equal(100, plan.PageHeightMm, 3);

        var overflow = new LabelLayoutSettings(
            LabelMediaKind.Custom,
            labelsPerPage: 4,
            rows: 2,
            columns: 2,
            horizontalMarginMm: 5,
            verticalMarginMm: 5,
            horizontalGapMm: 10.001,
            verticalGapMm: 10,
            customPageWidthMm: 100,
            customPageHeightMm: 100);

        var ex = Assert.Throws<InvalidOperationException>(() => CreatePlan(
            new ZplQuantitySelection(ZplQuantityMode.OneEach),
            overflow,
            labelWidthMm: 40,
            labelHeightMm: 40));
        Assert.Contains("caben", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rotation90_SwapsOccupiedLabelDimensionsWithoutScaling()
    {
        var settings = new LabelLayoutSettings(
            LabelMediaKind.Custom,
            labelsPerPage: 1,
            rows: 1,
            columns: 1,
            rotation: LabelRotation.Degrees90,
            customPageWidthMm: 100,
            customPageHeightMm: 100);

        var plan = CreatePlan(
            new ZplQuantitySelection(ZplQuantityMode.OneEach),
            settings,
            labelWidthMm: 40,
            labelHeightMm: 60);

        var placement = Assert.Single(plan.GetPage(0).Placements);
        Assert.Equal(60, placement.WidthMm, 3);
        Assert.Equal(40, placement.HeightMm, 3);
        Assert.Equal(LabelRotation.Degrees90, placement.Rotation);
    }

    [Fact]
    public void PresetSix_SelectsDeterministicGridClosestToUsablePageAspectRatio()
    {
        var plan = CreatePlan(
            new ZplQuantitySelection(ZplQuantityMode.Custom, 6),
            new LabelLayoutSettings(LabelMediaKind.A4, labelsPerPage: 6),
            labelWidthMm: 50,
            labelHeightMm: 30);

        Assert.Equal(3, plan.Rows);
        Assert.Equal(2, plan.Columns);
        Assert.Equal(6, plan.LabelsPerPage);
    }

    [Fact]
    public void HugeQuantity_ComputesLongPageCountWithoutExpandingPages()
    {
        var plan = CreatePlan(
            new ZplQuantitySelection(
                ZplQuantityMode.Custom,
                ZplQuantitySelection.MaxCustomQuantity),
            new LabelLayoutSettings(
                LabelMediaKind.Custom,
                labelsPerPage: 12,
                rows: 3,
                columns: 4,
                customPageWidthMm: 400,
                customPageHeightMm: 400),
            labelWidthMm: 20,
            labelHeightMm: 20);

        Assert.Equal(199_999_998L, plan.TotalLabels);
        Assert.Equal(16_666_667L, plan.PageCount);
        Assert.Equal(12, plan.GetPage(0).Placements.Count);
        Assert.Equal(6, plan.GetPage(plan.PageCount - 1).Placements.Count);
    }

    [Theory]
    [InlineData(double.NaN, 100)]
    [InlineData(double.PositiveInfinity, 100)]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    public void CustomMedia_RejectsInvalidPhysicalDimensions(double widthMm, double heightMm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LabelLayoutSettings(
            LabelMediaKind.Custom,
            customPageWidthMm: widthMm,
            customPageHeightMm: heightMm));
    }

    private static LabelLayoutPlan CreatePlan(
        ZplQuantitySelection quantity,
        LabelLayoutSettings settings,
        double labelWidthMm,
        double labelHeightMm)
    {
        var sequence = new LabelOutputSequence(Document, quantity);
        return LabelLayoutPlanner.CreatePlan(sequence, settings, labelWidthMm, labelHeightMm);
    }
}
