using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterRedactionCombinedTests
{
    [Fact]
    public void SaveAsCopy_TextEditAndRedaction_CommitTogetherWithoutMutatingSource()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var sourceBytes = File.ReadAllBytes(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "edited-and-redacted.pdf");

        PdfTextObjectInfo redacted;
        PdfTextObjectInfo edited;
        using (var source = PdfDocumentSession.Open(fixture.Path))
        {
            var objects = source.GetTextObjects(0);
            redacted = objects.Single(item => item.Text == "CASA");
            edited = objects.Single(item => item.Text == "UNO");
        }

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(0, new[] { new RedactionTarget(redacted) }));

        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        text.EnsureObject(edited);
        var edit = text.PrepareCandidate(edited.Key, "NUN", edited.FontSize, edited.FillColor);
        Assert.NotNull(edit.Candidate);
        text.CommitCandidate(edit.Candidate!);

        new PdfEditWriter().SaveAsCopy(
            images,
            text,
            redaction,
            destination,
            warningsConfirmed: true);

        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBytes, File.ReadAllBytes(fixture.Path));

        using var saved = PdfDocumentSession.Open(destination);
        var savedTexts = saved.GetTextObjects(0).Select(item => item.Text).ToArray();
        Assert.DoesNotContain("CASA", savedTexts);
        Assert.DoesNotContain("UNO", savedTexts);
        Assert.Contains("NUN", savedTexts);

        var extracted = saved.GetTextRange(0, 0, 100);
        Assert.DoesNotContain("CASA", extracted, StringComparison.Ordinal);
        Assert.DoesNotContain("UNO", extracted, StringComparison.Ordinal);
        Assert.Contains("NUN", extracted, StringComparison.Ordinal);
    }
}
