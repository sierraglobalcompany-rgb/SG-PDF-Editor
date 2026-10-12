using System.Reflection;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Edit.Text.Layout;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextReflowPresentationTests
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void TextEditUi_ExposesMinimalSafeReflowControlsAndCommand()
    {
        var type = typeof(MainWindow);

        Assert.NotNull(type.GetField("_editTextReflowTextBox", InstanceFlags));
        Assert.NotNull(type.GetField("_editTextReflowStateText", InstanceFlags));
        Assert.NotNull(type.GetField("_editTextReflowApplyButton", InstanceFlags));
        Assert.NotNull(type.GetMethod("RefreshTextReflowProperties", InstanceFlags));
        Assert.NotNull(type.GetMethod("TryApplySelectedTextReflow", InstanceFlags));
    }

    [Fact]
    public void FindSelectedTextParagraph_SelectedRun_ReturnsContainingParagraphOnly()
    {
        var page = TextLayoutAnalyzer.Analyze(
            612d,
            792d,
            new[]
            {
                TextInfo(0, "Primer párrafo", 50d, 700d, 180d, 716d),
                TextInfo(1, "Segundo párrafo", 50d, 620d, 190d, 636d)
            });
        var expected = page.Columns
            .SelectMany(column => column.Paragraphs)
            .Single(paragraph => paragraph.Lines.SelectMany(line => line.Runs)
                .Any(run => run.SourceKey == new TextObjectKey(0, 1)));
        var method = typeof(MainWindow).GetMethod("FindSelectedTextParagraph", StaticFlags);

        Assert.NotNull(method);
        var actual = method!.Invoke(null, new object[] { page, new TextObjectKey(0, 1) });

        Assert.Same(expected, actual);
    }

    [Fact]
    public void FormatTextReflowAvailability_Rejected_PreservesSpanishReason()
    {
        var method = typeof(MainWindow).GetMethod("FormatTextReflowAvailability", StaticFlags);
        Assert.NotNull(method);

        var message = Assert.IsType<string>(method!.Invoke(
            null,
            new object?[] { false, "El párrafo contiene texto de solo lectura y no admite reflow." }));

        Assert.Contains("Reflow seguro no disponible", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("solo lectura", message, StringComparison.OrdinalIgnoreCase);
    }

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
