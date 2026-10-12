using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterRedactionMultiTargetTests
{
    [Fact]
    public void SaveAsCopy_TwoTargets_PublishTwoValidatedRedactionMarks()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var destination = Path.Combine(fixture.DirectoryPath, "two-redactions.pdf");

        PdfTextObjectInfo[] targets;
        using (var source = PdfDocumentSession.Open(fixture.Path))
            targets = source.GetTextObjects(0).OrderBy(item => item.Key.PageObjectIndex).ToArray();
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

        using var saved = PdfDocumentSession.Open(destination);
        Assert.Empty(saved.GetTextObjects(0));
        var extracted = saved.GetTextRange(0, 0, 100);
        Assert.DoesNotContain("CASA", extracted, StringComparison.Ordinal);
        Assert.DoesNotContain("UNO", extracted, StringComparison.Ordinal);
    }
}
