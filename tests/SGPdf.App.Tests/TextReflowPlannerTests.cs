using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Edit.Text.Layout;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextReflowPlannerTests
{
    [Fact]
    public void Plan_SimpleParagraph_WrapsWordsAcrossExistingRunsInReadingOrder()
    {
        var page = Analyze(
            TextInfo(0, "abcdefghij", 50d, 700d, 150d, 716d),
            TextInfo(1, "klmnopqrst", 50d, 675d, 150d, 691d));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);

        var plan = TextReflowPlanner.Plan(
            page,
            paragraph,
            ClearPreflight(),
            "uno dos tres seis");

        Assert.True(plan.Allowed, plan.Reason);
        Assert.Null(plan.Reason);
        Assert.Collection(
            plan.Assignments,
            first =>
            {
                Assert.Equal(new TextObjectKey(0, 0), first.Key);
                Assert.Equal("uno dos", first.Text);
            },
            second =>
            {
                Assert.Equal(new TextObjectKey(0, 1), second.Key);
                Assert.Equal("tres seis", second.Text);
            });
    }

    [Fact]
    public void Plan_TextExceedsExistingCapacity_RejectsWithoutAssignments()
    {
        var page = Analyze(TextInfo(0, "corto", 50d, 700d, 100d, 716d));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);

        var plan = TextReflowPlanner.Plan(
            page,
            paragraph,
            ClearPreflight(),
            "este texto ya no cabe");

        Assert.False(plan.Allowed);
        Assert.Empty(plan.Assignments);
        Assert.Contains("cabe", plan.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plan_TooFewWordsToReplaceEveryExistingRun_RejectsWithoutAssignments()
    {
        var page = Analyze(
            TextInfo(0, "primero", 50d, 700d, 120d, 716d),
            TextInfo(1, "segundo", 50d, 675d, 120d, 691d));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);

        var plan = TextReflowPlanner.Plan(
            page,
            paragraph,
            ClearPreflight(),
            "único");

        Assert.False(plan.Allowed);
        Assert.Empty(plan.Assignments);
        Assert.Contains("objetos", plan.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plan_UnsafeLayout_RejectsBeforeCreatingAssignments()
    {
        var page = Analyze(
            TextInfo(0, "Izquierda 1", 50d, 700d, 180d, 716d),
            TextInfo(1, "Derecha 1", 340d, 700d, 470d, 716d),
            TextInfo(2, "Izquierda 2", 50d, 650d, 180d, 666d),
            TextInfo(3, "Derecha 2", 340d, 650d, 470d, 666d));
        var paragraph = page.Columns[0].Paragraphs[0];

        var plan = TextReflowPlanner.Plan(
            page,
            paragraph,
            ClearPreflight(),
            "texto seguro de prueba");

        Assert.False(plan.Allowed);
        Assert.Empty(plan.Assignments);
        Assert.NotNull(plan.Reason);
    }

    private static TextLayoutPage Analyze(params PdfTextObjectInfo[] objects) =>
        TextLayoutAnalyzer.Analyze(612d, 792d, objects);

    private static PdfEditPreflightResult ClearPreflight() => new(Array.Empty<PdfEditFinding>());

    private static PdfTextObjectInfo TextInfo(
        int objectIndex,
        string text,
        double left,
        double bottom,
        double right,
        double top)
        => new(
            new TextObjectKey(0, objectIndex),
            text,
            new PdfObjectMatrix(1d, 0d, 0d, 1d, left, bottom),
            new PdfObjectBounds(left, bottom, right, top),
            new PdfTextObjectQuad(left, bottom, right, bottom, right, top, left, top),
            "Helvetica",
            12d,
            new PdfTextFillColor(0, 0, 0, 255),
            0);
}
