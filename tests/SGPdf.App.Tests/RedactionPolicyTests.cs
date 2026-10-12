using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class RedactionPolicyTests
{
    [Fact]
    public void Evaluate_SimpleVisibleWholeTextObject_AllowsAndBuildsPlan()
    {
        var selected = TextInfo(0, "Secreto", 50d, 700d, 130d, 716d);

        var decision = RedactionPolicy.Evaluate(
            new[] { selected },
            new[] { selected.Key },
            ClearPreflight());

        Assert.True(decision.Allowed, decision.Reason);
        Assert.Null(decision.Reason);
        var plan = Assert.IsType<RedactionPlan>(decision.Plan);
        Assert.Equal(0, plan.PageIndex);
        var target = Assert.Single(plan.Targets);
        Assert.Equal(selected, target.Original);
    }

    [Fact]
    public void Evaluate_MultipleCompatibleWholeObjects_AllowsInObjectOrder()
    {
        var second = TextInfo(7, "dos", 140d, 700d, 180d, 716d);
        var first = TextInfo(3, "uno", 50d, 700d, 100d, 716d);

        var decision = RedactionPolicy.Evaluate(
            new[] { second, first },
            new[] { second.Key, first.Key },
            ClearPreflight());

        Assert.True(decision.Allowed, decision.Reason);
        var plan = Assert.IsType<RedactionPlan>(decision.Plan);
        Assert.Equal(new[] { first.Key, second.Key }, plan.Targets.Select(target => target.Original.Key));
    }

    [Fact]
    public void Evaluate_InvisibleOcrText_Rejects()
    {
        var selected = TextInfo(0, "OCR", 50d, 700d, 100d, 716d, renderMode: 3);

        var decision = RedactionPolicy.Evaluate(
            new[] { selected },
            new[] { selected.Key },
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Null(decision.Plan);
        Assert.Contains("visible", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_RotatedOrSkewedText_Rejects()
    {
        var selected = TextInfo(0, "Rotado", 50d, 700d, 110d, 716d) with
        {
            Matrix = new PdfObjectMatrix(0d, 1d, -1d, 0d, 50d, 700d)
        };

        var decision = RedactionPolicy.Evaluate(
            new[] { selected },
            new[] { selected.Key },
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("horizontal", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_DegenerateOrNonFiniteBounds_Rejects()
    {
        var degenerate = TextInfo(0, "Degenerado", 50d, 700d, 110d, 716d) with
        {
            Bounds = new PdfObjectBounds(50d, 700d, 50d, 716d)
        };

        var decision = RedactionPolicy.Evaluate(
            new[] { degenerate },
            new[] { degenerate.Key },
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("geometr", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_SelectedObjectOverlapsUnselectedText_Rejects()
    {
        var selected = TextInfo(0, "Secreto", 50d, 700d, 120d, 716d);
        var neighbor = TextInfo(1, "Conservar", 110d, 700d, 190d, 716d);

        var decision = RedactionPolicy.Evaluate(
            new[] { selected, neighbor },
            new[] { selected.Key },
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("solap", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_BlockedPreflight_Rejects()
    {
        var selected = TextInfo(0, "Secreto", 50d, 700d, 120d, 716d);
        var preflight = new PdfEditPreflightResult(
            new[]
            {
                new PdfEditFinding(
                    PdfEditFindingKind.CryptographicSignature,
                    PdfEditFindingSeverity.Block,
                    "Bloqueado",
                    PdfEditPreservationStatus.Unknown)
            });

        var decision = RedactionPolicy.Evaluate(
            new[] { selected },
            new[] { selected.Key },
            preflight);

        Assert.False(decision.Allowed);
        Assert.Contains("preflight", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_EmptySelection_Rejects()
    {
        var decision = RedactionPolicy.Evaluate(
            new[] { TextInfo(0, "Visible", 50d, 700d, 120d, 716d) },
            Array.Empty<TextObjectKey>(),
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("selección", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_DuplicateSelectionKeys_Rejects()
    {
        var selected = TextInfo(0, "Secreto", 50d, 700d, 120d, 716d);

        var decision = RedactionPolicy.Evaluate(
            new[] { selected },
            new[] { selected.Key, selected.Key },
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("duplic", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_SelectedKeyMissingFromPageSnapshot_Rejects()
    {
        var pageObject = TextInfo(0, "Visible", 50d, 700d, 120d, 716d);

        var decision = RedactionPolicy.Evaluate(
            new[] { pageObject },
            new[] { new TextObjectKey(0, 99) },
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("snapshot", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_SelectionAcrossPages_Rejects()
    {
        var first = TextInfo(0, "Uno", 50d, 700d, 100d, 716d);
        var second = TextInfo(1, "Dos", 50d, 650d, 100d, 666d) with
        {
            Key = new TextObjectKey(1, 1)
        };

        var decision = RedactionPolicy.Evaluate(
            new[] { first, second },
            new[] { first.Key, second.Key },
            ClearPreflight());

        Assert.False(decision.Allowed);
        Assert.Contains("pagina", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private static PdfEditPreflightResult ClearPreflight() => new(Array.Empty<PdfEditFinding>());

    private static PdfTextObjectInfo TextInfo(
        int objectIndex,
        string text,
        double left,
        double bottom,
        double right,
        double top,
        int renderMode = 0)
        => new(
            new TextObjectKey(0, objectIndex),
            text,
            new PdfObjectMatrix(1d, 0d, 0d, 1d, left, bottom),
            new PdfObjectBounds(left, bottom, right, top),
            new PdfTextObjectQuad(left, bottom, right, bottom, right, top, left, top),
            "Helvetica",
            12d,
            new PdfTextFillColor(0, 0, 0, 255),
            renderMode);
}
