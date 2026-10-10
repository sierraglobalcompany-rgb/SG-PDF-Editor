using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditSharedInfrastructureNamingTests
{
    [Fact]
    public void SharedEditInfrastructure_UsesPdfEditNames_AndRemovesImageSpecificProductionNames()
    {
        var assembly = typeof(SGPdf.App.Pdf.PdfDocumentSession).Assembly;

        Assert.NotNull(assembly.GetType("SGPdf.App.Features.Edit.PdfEditSourceFingerprint"));
        Assert.NotNull(assembly.GetType("SGPdf.App.Features.Edit.PdfEditPreflightInspector"));
        Assert.NotNull(assembly.GetType("SGPdf.App.Features.Edit.PdfEditPreflightResult"));
        Assert.NotNull(assembly.GetType("SGPdf.App.Pdf.PdfEditWriter"));
        Assert.NotNull(assembly.GetType("SGPdf.App.Pdf.PdfEditOutputValidator"));

        Assert.Null(assembly.GetType("SGPdf.App.Features.Edit.Images.ImageEditSourceFingerprint"));
        Assert.Null(assembly.GetType("SGPdf.App.Features.Edit.Images.ImageEditPreflightInspector"));
        Assert.Null(assembly.GetType("SGPdf.App.Features.Edit.Images.ImageEditPreflightResult"));
        Assert.Null(assembly.GetType("SGPdf.App.Pdf.PdfImageEditWriter"));
        Assert.Null(assembly.GetType("SGPdf.App.Pdf.ImageEditOutputValidator"));

        Assert.Null(assembly.GetType("SGPdf.App.Pdf.IPdfWriter"));
    }
}
