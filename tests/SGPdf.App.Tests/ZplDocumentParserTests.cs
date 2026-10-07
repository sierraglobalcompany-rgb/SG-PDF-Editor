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
}
