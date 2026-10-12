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

    [Fact]
    public void Analyze_TitleAndBodyWithStrongSizeChange_SplitsParagraphs()
    {
        var objects = new[]
        {
            TextInfo(0, 0, "Título", 50d, 720d, 180d, 748d, 24d),
            TextInfo(0, 1, "Primera línea del cuerpo", 50d, 675d, 240d, 691d, 12d),
            TextInfo(0, 2, "Segunda línea del cuerpo", 50d, 650d, 245d, 666d, 12d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, objects);

        var column = Assert.Single(page.Columns);
        Assert.Equal(2, column.Paragraphs.Count);
        Assert.Equal("Título", Assert.Single(column.Paragraphs[0].Lines).Text);
        Assert.Equal(2, column.Paragraphs[1].Lines.Count);
    }

    [Fact]
    public void Analyze_ModestIndentation_KeepsSingleParagraph()
    {
        var objects = new[]
        {
            TextInfo(0, 0, "Primera", 50d, 700d, 180d, 716d),
            TextInfo(0, 1, "Segunda con sangría", 70d, 675d, 220d, 691d),
            TextInfo(0, 2, "Tercera", 50d, 650d, 180d, 666d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, objects);

        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);
        Assert.Equal(3, paragraph.Lines.Count);
    }

    [Fact]
    public void Analyze_TwoClearColumnsWithInterleavedObjectOrder_ReadsLeftThenRight()
    {
        var objects = new[]
        {
            TextInfo(0, 0, "Derecha 1", 340d, 700d, 470d, 716d),
            TextInfo(0, 1, "Izquierda 2", 50d, 650d, 180d, 666d),
            TextInfo(0, 2, "Derecha 2", 340d, 650d, 470d, 666d),
            TextInfo(0, 9, "Izquierda 1", 50d, 700d, 180d, 716d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, objects);

        Assert.Equal(TextLayoutClassification.Simple, page.Classification);
        Assert.Equal(2, page.Columns.Count);
        Assert.Equal(
            new[] { "Izquierda 1", "Izquierda 2" },
            page.Columns[0].Paragraphs.SelectMany(paragraph => paragraph.Lines).Select(line => line.Text));
        Assert.Equal(
            new[] { "Derecha 1", "Derecha 2" },
            page.Columns[1].Paragraphs.SelectMany(paragraph => paragraph.Lines).Select(line => line.Text));
    }

    [Fact]
    public void Analyze_ApparentColumnsWithoutStableGutter_IsAmbiguous()
    {
        var objects = new[]
        {
            TextInfo(0, 0, "Bloque A1", 50d, 700d, 360d, 716d),
            TextInfo(0, 1, "Bloque B1", 320d, 675d, 560d, 691d),
            TextInfo(0, 2, "Bloque A2", 50d, 650d, 360d, 666d),
            TextInfo(0, 3, "Bloque B2", 320d, 625d, 560d, 641d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, objects);

        Assert.Equal(TextLayoutClassification.Ambiguous, page.Classification);
    }

    [Fact]
    public void Analyze_RotatedText_IsUnsupported()
    {
        var item = TextInfo(0, 0, "Rotado", 100d, 500d, 180d, 516d) with
        {
            Matrix = new PdfObjectMatrix(0d, 1d, -1d, 0d, 100d, 500d)
        };

        var page = TextLayoutAnalyzer.Analyze(612d, 792d, new[] { item });

        Assert.Equal(TextLayoutClassification.Unsupported, page.Classification);
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
