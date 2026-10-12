using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterRedactionTests
{
    [Fact]
    public void SaveAsCopy_Redaction_RemovesUnderlyingTextPreservesNeighborAndSource()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var sourceBytes = File.ReadAllBytes(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "redacted.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
        {
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");
        }

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(
            0,
            new[] { new RedactionTarget(target) }));

        var images = ImageEditWorkspace.Create(
            fixture.Path,
            Array.Empty<PdfImageObjectInfo>());
        var text = TextEditWorkspace.Create(
            fixture.Path,
            Array.Empty<PdfTextObjectInfo>());

        new PdfEditWriter().SaveAsCopy(
            images,
            text,
            redaction,
            destination,
            warningsConfirmed: true);

        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBytes, File.ReadAllBytes(fixture.Path));

        using var saved = PdfDocumentSession.Open(destination);
        var texts = saved.GetTextObjects(0).Select(item => item.Text).ToArray();
        Assert.DoesNotContain("CASA", texts);
        Assert.Contains("UNO", texts);

        var extracted = saved.GetTextRange(0, 0, int.MaxValue);
        Assert.DoesNotContain("CASA", extracted, StringComparison.Ordinal);
        Assert.Contains("UNO", extracted, StringComparison.Ordinal);

        var rendered = saved.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
        Assert.Contains(rendered.BgraPixels, value => value != 0);
    }

    [Fact]
    public void SaveAsCopy_RedactionWithChangedSnapshot_RejectsBeforePublishingDestination()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var destination = Path.Combine(fixture.DirectoryPath, "should-not-exist.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
        {
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");
        }

        var staleTarget = target with { Text = "CAMBIADO" };
        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(
            0,
            new[] { new RedactionTarget(staleTarget) }));

        var images = ImageEditWorkspace.Create(
            fixture.Path,
            Array.Empty<PdfImageObjectInfo>());
        var text = TextEditWorkspace.Create(
            fixture.Path,
            Array.Empty<PdfTextObjectInfo>());

        var error = Assert.Throws<InvalidOperationException>(() =>
            new PdfEditWriter().SaveAsCopy(
                images,
                text,
                redaction,
                destination,
                warningsConfirmed: true));

        Assert.Contains("snapshot", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf.tmp"));
    }

    [Fact]
    public void SaveAsCopy_Redaction_WhenSignatureExists_RemainsBlocked()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var destination = Path.Combine(fixture.DirectoryPath, "signed-blocked.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
        {
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");
        }

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(
            0,
            new[] { new RedactionTarget(target) }));
        var images = ImageEditWorkspace.Create(fixture.Path, Array.Empty<PdfImageObjectInfo>());
        var text = TextEditWorkspace.Create(fixture.Path, Array.Empty<PdfTextObjectInfo>());
        var writer = new PdfEditWriter(signatureCountOverride: _ => 1);

        var error = Assert.Throws<InvalidOperationException>(() =>
            writer.SaveAsCopy(images, text, redaction, destination, warningsConfirmed: true));

        Assert.Contains("firma criptográfica", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
    }
}
