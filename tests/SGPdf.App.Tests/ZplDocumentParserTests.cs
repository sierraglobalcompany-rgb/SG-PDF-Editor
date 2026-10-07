using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ZplDocumentParserTests
{
    [Fact]
    public void Parse_TwoPrintableBlocks_ReturnsTwoDesignsInSourceOrder()
    {
        var source = "^XA^FO10,10^FDUno^FS^XZ\n^XA^FO10,10^FDDos^FS^XZ";

        var document = ZplDocumentParser.Parse("labels.zpl", source);

        Assert.Equal(2, document.Designs.Count);
        Assert.Equal(0, document.Designs[0].Index);
        Assert.Equal(0, document.Designs[0].SourceBlockIndex);
        Assert.Equal(1, document.Designs[1].Index);
        Assert.Equal(1, document.Designs[1].SourceBlockIndex);
    }

    [Fact]
    public void Parse_DfDefinitionThenXfInvocation_ExposesOnlyPrintableInvocation()
    {
        var source = "^XA^DFR:FORM.ZPL^FO10,10^FN1^FS^XZ\n^XA^XFR:FORM.ZPL^FS^FN1^FDOK^FS^XZ";

        var document = ZplDocumentParser.Parse("template.zpl", source);

        var design = Assert.Single(document.Designs);
        Assert.Equal(1, design.SourceBlockIndex);
        Assert.Contains("^DFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("^XFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_NoXaXzBlocks_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() =>
            ZplDocumentParser.Parse("empty.zpl", "^FO10,10^FDNo block^FS"));
    }

    [Fact]
    public void Parse_UnterminatedXa_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() =>
            ZplDocumentParser.Parse("broken.zpl", "^XA^FO10,10^FDBroken^FS"));
    }

    [Fact]
    public void Parse_StrayXzBeforeXa_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() =>
            ZplDocumentParser.Parse("broken.zpl", "^XZ^XA^FO10,10^FDLabel^FS^XZ"));
    }

    [Fact]
    public void Parse_Pq28_StoresQuantityAndRemovesPrintMultiplicationFromRenderSource()
    {
        var source = "^XA^FO10,10^FDLabel^FS^PQ28^XZ";

        var document = ZplDocumentParser.Parse("qty.zpl", source);

        var design = Assert.Single(document.Designs);
        Assert.Equal(28, design.QuantityFromFile);
        Assert.Equal(28L, document.TotalQuantityFromFile);
        Assert.DoesNotContain("^PQ", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WithoutPq_DefaultsQuantityToOne()
    {
        var document = ZplDocumentParser.Parse("default.zpl", "^XA^FO10,10^FDLabel^FS^XZ");

        Assert.Equal(1, Assert.Single(document.Designs).QuantityFromFile);
        Assert.Equal(1L, document.TotalQuantityFromFile);
    }

    [Fact]
    public void Parse_PqZero_NormalizesQuantityToOne()
    {
        var document = ZplDocumentParser.Parse("zero.zpl", "^XA^FO10,10^FDLabel^FS^PQ0^XZ");

        Assert.Equal(1, Assert.Single(document.Designs).QuantityFromFile);
        Assert.DoesNotContain("^PQ", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_PqWithWhitespaceAndParameters_UsesFirstParameterAsQuantity()
    {
        var document = ZplDocumentParser.Parse("params.zpl", "^XA^FO10,10^FDLabel^FS^PQ 3,0,0,N^XZ");

        Assert.Equal(3, Assert.Single(document.Designs).QuantityFromFile);
        Assert.DoesNotContain("^PQ", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_MultiplePqCommands_LastQuantityWinsAndAllCommandsAreRemoved()
    {
        var document = ZplDocumentParser.Parse("multi-pq.zpl", "^XA^PQ2^FO10,10^FDLabel^FS^PQ7,0,0,N^XZ");

        Assert.Equal(7, Assert.Single(document.Designs).QuantityFromFile);
        Assert.DoesNotContain("^PQ", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_MultiplePrintableBlocks_SumsQuantities()
    {
        var source = "^XA^FO10,10^FDOne^FS^PQ2^XZ\n^XA^FO10,10^FDTwo^FS^PQ3^XZ";

        var document = ZplDocumentParser.Parse("sum.zpl", source);

        Assert.Equal(2, document.Designs.Count);
        Assert.Equal(2, document.Designs[0].QuantityFromFile);
        Assert.Equal(3, document.Designs[1].QuantityFromFile);
        Assert.Equal(5L, document.TotalQuantityFromFile);
    }

    [Fact]
    public void Parse_PqAboveSupportedMaximum_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() =>
            ZplDocumentParser.Parse("too-many.zpl", "^XA^FO10,10^FDLabel^FS^PQ100000000^XZ"));
    }

    [Fact]
    public void Parse_MalformedPqQuantity_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() =>
            ZplDocumentParser.Parse("bad-pq.zpl", "^XA^FO10,10^FDLabel^FS^PQABC,0,0,N^XZ"));
    }

    [Fact]
    public void Parse_PqInsideStoredFormatDefinition_ThrowsInvalidDataException()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            ZplDocumentParser.Parse("stored-pq.zpl", "^XA^DFR:FORM.ZPL^FO10,10^FN1^FS^PQ4^XZ\n^XA^XFR:FORM.ZPL^FS^XZ"));

        Assert.Contains("stored format", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_QuantityNormalization_PreservesOtherCommandsAndSourceOrder()
    {
        var source = "^XA^DFR:FORM.ZPL^FO10,10^FN1^FS^XZ\n^XA^CI28^XFR:FORM.ZPL^FS^FN1^FDMálaga Ñandú^FS^PQ2^XZ";

        var document = ZplDocumentParser.Parse("preserve.zpl", source);

        Assert.Contains("^DFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("^XFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("^CI28", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Málaga Ñandú", document.NormalizedRenderSource, StringComparison.Ordinal);
        Assert.True(
            document.NormalizedRenderSource.IndexOf("^DFR:FORM.ZPL", StringComparison.OrdinalIgnoreCase) <
            document.NormalizedRenderSource.IndexOf("^XFR:FORM.ZPL", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("^PQ", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
    }
}
