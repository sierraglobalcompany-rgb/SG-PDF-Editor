using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Edit.Text.Layout;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextLayoutAnalyzerTests
{
    [Fact]
    public void Analyze_OneColumnOutOfObjectOrder_ReconstructsGeometricReadingOrder()
    {
        var objects = new[]
        {
            TextInfo(0, 1, "Segunda línea", 50d, 650d, 180d, 666d),
            TextInfo(0, 9, "Primera línea", 50d, 700d, 175d, 716d),
            TextInfo(0, 4, "Tercera línea", 50d, 600d, 170d, 616d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, objects);

        Assert.Equal(TextLayoutClassification.Simple, page.Classification);
        var lines = Assert.Single(Assert.Single(page.Columns).Paragraphs).Lines;
        Assert.Equal(new[] { "Primera línea", "Segunda línea", "Tercera línea" }, lines.Select(line => line.Text));
    }

    [Fact]
    public void Analyze_SameLineOutOfObjectOrder_SortsRunsLeftToRight()
    {
        var objects = new[]
        {
            TextInfo(0, 2, "mundo", 150d, 700d, 205d, 716d),
            TextInfo(0, 8, "Hola", 50d, 700d, 90d, 716d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, objects);

        var line = Assert.Single(Assert.Single(Assert.Single(page.Columns).Paragraphs).Lines);
        Assert.Equal(new[] { "Hola", "mundo" }, line.Runs.Select(run => run.Text));
        Assert.Equal("Hola mundo", line.Text);
    }

    [Fact]
    public void Analyze_DropsEmptyDegenerateAndNonFiniteRuns()
    {
        var objects = new[]
        {
            TextInfo(0, 1, "Válido", 40d, 700d, 100d, 716d),
            TextInfo(0, 2, "   ", 120d, 700d, 150d, 716d),
            TextInfo(0, 3, "Degenerado", 200d, 700d, 200d, 716d),
            TextInfo(0, 4, "No finito", double.NaN, 700d, 260d, 716d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, objects);

        var line = Assert.Single(Assert.Single(Assert.Single(page.Columns).Paragraphs).Lines);
        var run = Assert.Single(line.Runs);
        Assert.Equal("Válido", run.Text);
        Assert.Equal(new TextObjectKey(0, 1), run.SourceKey);
    }

    [Fact]
    public void Analyze_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            TextLayoutAnalyzer.Analyze(
                612d,
                792d,
                new[] { TextInfo(0, 1, "Texto", 40d, 700d, 90d, 716d) },
                cts.Token));
    }

    private static PdfTextObjectInfo TextInfo(
        int pageIndex,
        int objectIndex,
        string text,
        double left,
        double bottom,
        double right,
        double top,
        double fontSize = 12d)
        => new(
            new TextObjectKey(pageIndex, objectIndex),
            text,
            new PdfObjectMatrix(1d, 0d, 0d, 1d, left, bottom),
            new PdfObjectBounds(left, bottom, right, top),
            new PdfTextObjectQuad(left, bottom, right, bottom, right, top, left, top),
            "Helvetica",
            fontSize,
            new PdfTextFillColor(0, 0, 0, 255),
            0);
}
