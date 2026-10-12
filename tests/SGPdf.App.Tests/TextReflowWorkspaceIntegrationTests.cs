using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextReflowWorkspaceIntegrationTests
{
    [Fact]
    public void ApplyAllowedPlan_ThenSaveAs_ReopensExactAndLeavesSourceUnchanged()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var sourceBytes = File.ReadAllBytes(fixture.Path);
        PdfTextObjectInfo[] originals;
        using (var source = PdfDocumentSession.Open(fixture.Path))
        {
            originals = source.GetTextObjects(0).ToArray();
        }
        Assert.Equal(2, originals.Length);

        var imageWorkspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var textWorkspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        foreach (var original in originals)
            textWorkspace.EnsureObject(original);

        var plan = TextReflowPlan.Allow(new[]
        {
            new TextReflowAssignment(originals[0].Key, "ASAC"),
            new TextReflowAssignment(originals[1].Key, "NOU")
        });

        var apply = textWorkspace.ApplyReflowPlan(plan);

        Assert.True(apply.Applied, apply.Reason);
        Assert.Null(apply.Reason);
        Assert.True(textWorkspace.IsDirty);

        var destination = Path.Combine(fixture.DirectoryPath, "reflow.pdf");
        var writer = new PdfEditWriter();
        writer.SaveAsCopy(imageWorkspace, textWorkspace, destination, warningsConfirmed: false);

        using var saved = PdfDocumentSession.Open(destination);
        var after = saved.GetTextObjects(0);
        Assert.Equal(2, after.Count);
        Assert.Equal("ASAC", after[0].Text);
        Assert.Equal("NOU", after[1].Text);

        var rendered = saved.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
        Assert.NotEmpty(rendered.Pixels);

        Assert.Equal(sourceBytes, File.ReadAllBytes(fixture.Path));
    }

    [Fact]
    public void ApplyRejectedPlan_DoesNotMutateWorkspace()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        using var source = PdfDocumentSession.Open(fixture.Path);
        var originals = source.GetTextObjects(0);
        Assert.Equal(2, originals.Count);

        var workspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        foreach (var original in originals)
            workspace.EnsureObject(original);

        var apply = workspace.ApplyReflowPlan(TextReflowPlan.Reject("Layout no seguro."));

        Assert.False(apply.Applied);
        Assert.Contains("seguro", apply.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.False(workspace.IsDirty);
        Assert.Equal("CASA", workspace.GetState(originals[0].Key).Text);
        Assert.Equal("UNO", workspace.GetState(originals[1].Key).Text);
    }

    [Fact]
    public void ApplyPlan_WithUnknownLaterObject_RejectsBeforeAnyMutation()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        using var source = PdfDocumentSession.Open(fixture.Path);
        var originals = source.GetTextObjects(0);
        Assert.Equal(2, originals.Count);

        var workspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        foreach (var original in originals)
            workspace.EnsureObject(original);

        var plan = TextReflowPlan.Allow(new[]
        {
            new TextReflowAssignment(originals[0].Key, "ASAC"),
            new TextReflowAssignment(new TextObjectKey(0, 999), "NOU")
        });

        var apply = workspace.ApplyReflowPlan(plan);

        Assert.False(apply.Applied);
        Assert.False(workspace.IsDirty);
        Assert.Equal("CASA", workspace.GetState(originals[0].Key).Text);
        Assert.Equal("UNO", workspace.GetState(originals[1].Key).Text);
    }
}
