using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterRedactionMultipleTargetsTests
{
    [Fact]
    public void SaveAsCopy_TwoSafeTargetsOnSamePage_RemovesBothAndKeepsSourceUntouched()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var sourceBytes = File.ReadAllBytes(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "two-redactions.pdf");

        PdfTextObjectInfo[] targets;
        using (var session = PdfDocumentSession.Open(fixture.Path))
        {
            targets = session.GetTextObjects(0)
                .OrderBy(item => item.Key.PageObjectIndex)
                .ToArray();
        }

        Assert.Equal(2, targets.Length);

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(
            0,
            targets.Select(target => new RedactionTarget(target)).ToArray()));
        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);

        new PdfEditWriter().SaveAsCopy(
            images,
            text,
            redaction,
            destination,
            warningsConfirmed: true);

        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBytes, File.ReadAllBytes(fixture.Path));

        using var saved = PdfDocumentSession.Open(destination);
        Assert.Empty(saved.GetTextObjects(0));

        var rendered = saved.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
        Assert.NotEmpty(rendered.Pixels);
    }
}
