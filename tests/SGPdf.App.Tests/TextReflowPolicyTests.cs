using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Edit.Text.Layout;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextReflowPolicyTests
{
    [Fact]
    public void Evaluate_SimpleSingleColumnHomogeneousParagraph_AllowsReflow()
    {
        var page = Analyze(
            TextInfo(0, "Primera línea", 50d, 700d, 250d, 716d),
            TextInfo(1, "Segunda línea", 50d, 675d, 245d, 691d));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);

        var decision = TextReflowPolicy.Evaluate(page, paragraph, ClearPreflight());

        Assert.True(decision.Allowed, decision.Reason);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public void Evaluate_TwoColumns_Rejects()
    {
        var page = Analyze(
            TextInfo(0, "Izquierda 1", 50d, 700d, 180d, 716d),
            TextInfo(1, "Derecha 1", 340d, 700d, 470d, 716d),
            TextInfo(2, "Izquierda 2", 50d, 650d, 180d, 666d),
            TextInfo(3, "Derecha 2", 340d, 650d, 470d, 666d));
        var paragraph = page.Columns[0].Paragraphs[0];

        var decision = TextReflowPolicy.Evaluate(page, paragraph, ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("una sola columna", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_AmbiguousOrUnsupportedPage_Rejects()
    {
        var ambiguous = Analyze(
            TextInfo(0, "Bloque A1", 50d, 700d, 360d, 716d),
            TextInfo(1, "Bloque B1", 320d, 675d, 560d, 691d),
            TextInfo(2, "Bloque A2", 50d, 650d, 360d, 666d),
            TextInfo(3, "Bloque B2", 320d, 625d, 560d, 641d));
        var rotatedSource = TextInfo(4, "Rotado", 100d, 500d, 180d, 516d) with
        {
            Matrix = new PdfObjectMatrix(0d, 1d, -1d, 0d, 100d, 500d)
        };
        var unsupported = Analyze(rotatedSource);

        var ambiguousDecision = TextReflowPolicy.Evaluate(
            ambiguous,
            ambiguous.Columns[0].Paragraphs[0],
            ClearPreflight());
        var unsupportedDecision = TextReflowPolicy.Evaluate(
            unsupported,
            paragraph: null,
            ClearPreflight());

        Assert.False(ambiguousDecision.Allowed);
        Assert.False(unsupportedDecision.Allowed);
    }

    [Fact]
    public void Evaluate_OverlappingRuns_Rejects()
    {
        var page = Analyze(
            TextInfo(0, "Uno", 50d, 700d, 110d, 716d),
            TextInfo(1, "Dos", 100d, 700d, 150d, 716d));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);

        var decision = TextReflowPolicy.Evaluate(page, paragraph, ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("solap", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_HeterogeneousStyle_Rejects()
    {
        var page = Analyze(
            TextInfo(0, "Normal", 50d, 700d, 130d, 716d, fontName: "Helvetica", fontSize: 12d),
            TextInfo(1, "Distinto", 140d, 700d, 220d, 716d, fontName: "Times-Roman", fontSize: 12d));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);

        var decision = TextReflowPolicy.Evaluate(page, paragraph, ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("homog", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_InvisibleOcrRun_RejectsAsReadOnly()
    {
        var page = Analyze(TextInfo(0, "OCR", 50d, 700d, 100d, 716d, renderMode: 3));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);

        var decision = TextReflowPolicy.Evaluate(page, paragraph, ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("solo lectura", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_BlockedPreflight_Rejects()
    {
        var page = Analyze(TextInfo(0, "Texto", 50d, 700d, 100d, 716d));
        var paragraph = Assert.Single(Assert.Single(page.Columns).Paragraphs);
        var preflight = new PdfEditPreflightResult(
            new[]
            {
                new PdfEditFinding(
                    PdfEditFindingKind.CryptographicSignature,
                    PdfEditFindingSeverity.Block,
                    "Bloqueado",
                    PdfEditPreservationStatus.Unknown)
            });

        var decision = TextReflowPolicy.Evaluate(page, paragraph, preflight);

        Assert.False(decision.Allowed);
        Assert.Contains("preflight", decision.Reason, StringComparison.OrdinalIgnoreCase);
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
        double top,
        string fontName = "Helvetica",
        double fontSize = 12d,
        int renderMode = 0)
        => new(
            new TextObjectKey(0, objectIndex),
            text,
            new PdfObjectMatrix(1d, 0d, 0d, 1d, left, bottom),
            new PdfObjectBounds(left, bottom, right, top),
            new PdfTextObjectQuad(left, bottom, right, bottom, right, top, left, top),
            fontName,
            fontSize,
            new PdfTextFillColor(0, 0, 0, 255),
            renderMode);
}
