using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterRedactionSourceRaceTests
{
    [Fact]
    public void SaveAsCopy_SourceFingerprintChangesAfterStaging_RejectsBeforePublishingDestination()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var destination = Path.Combine(fixture.DirectoryPath, "stale-source.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(0, new[] { new RedactionTarget(target) }));
        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var originalLastWriteTimeUtc = File.GetLastWriteTimeUtc(fixture.Path);
        var writer = new PdfEditWriter(
            validateOutputOverride: (_, _, _) =>
                File.SetLastWriteTimeUtc(fixture.Path, originalLastWriteTimeUtc.AddMinutes(1)));

        var error = Capture(() =>
            writer.SaveAsCopy(
                images,
                text,
                redaction,
                destination,
                warningsConfirmed: true));

        Assert.IsType<IOException>(error);
        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf.tmp"));
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
