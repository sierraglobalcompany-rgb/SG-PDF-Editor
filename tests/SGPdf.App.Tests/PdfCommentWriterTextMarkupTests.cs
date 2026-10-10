using System.Reflection;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentWriterTextMarkupTests
{
    [Fact]
    public void SaveAsCopy_CreatesThreeMarkupTypesAndKeepsSourceUntouched()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        var destination = Path.Combine(fixture.DirectoryPath, "created-markups.pdf");
        var workspace = CommentWorkspace.Create(fixture.SourcePath, sourceOpenedWithPassword: false);

        AddNewMarkup(workspace, CommentSubtype.Highlight, new CommentRect(50d, 300d, 190d, 325d), new CommentColor(255, 225, 0, 150));
        AddNewMarkup(workspace, CommentSubtype.Underline, new CommentRect(50d, 250d, 190d, 275d), new CommentColor(0, 90, 210, 205));
        AddNewMarkup(workspace, CommentSubtype.Strikeout, new CommentRect(50d, 200d, 190d, 225d), new CommentColor(220, 25, 30, 230));

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        var source = CommentNativeCharacterizationHarness.Inspect(fixture.SourcePath);
        var output = CommentNativeCharacterizationHarness.Inspect(destination);
        Assert.Empty(source);
        Assert.Equal(3, output.Count);
        Assert.Equal(new[]
        {
            PdfiumNative.FPDF_ANNOT_HIGHLIGHT,
            PdfiumNative.FPDF_ANNOT_UNDERLINE,
            PdfiumNative.FPDF_ANNOT_STRIKEOUT
        }, output.Select(item => item.Subtype).OrderBy(value => value).ToArray());
        Assert.All(output, item => Assert.Single(item.Quads));
    }

    [Fact]
    public void SaveAsCopy_UsesStableIdentityToEditAndDeleteExistingMarkupWhilePreservingUnrelatedAnnotations()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "edited-markups.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var comments = session.GetCommentsOnPage(0);
            var identities = CommentIdentity.CreateForPage(comments);

            var highlightIndex = comments.Select((item, index) => (item, index))
                .Single(pair => pair.item.Subtype == CommentSubtype.Highlight).index;
            var underlineIndex = comments.Select((item, index) => (item, index))
                .Single(pair => pair.item.Subtype == CommentSubtype.Underline).index;

            var highlight = CreateExistingState(comments[highlightIndex]);
            CommentWorkspaceIdentityContract.EnsureBaseline(workspace, highlight, identities[highlightIndex]);
            workspace.CommitCandidate(workspace.PrepareUpdate(
                CommentOperationKind.Update,
                highlight with { Color = new CommentColor(12, 34, 56, 178) }));

            var underline = CreateExistingState(comments[underlineIndex]);
            CommentWorkspaceIdentityContract.EnsureBaseline(workspace, underline, identities[underlineIndex]);
            workspace.CommitCandidate(workspace.PrepareDelete(underline.Key));
        }

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        var original = CommentNativeCharacterizationHarness.Inspect(fixture.OutputPath);
        var output = CommentNativeCharacterizationHarness.Inspect(destination);
        Assert.Equal(7, original.Count);
        Assert.Equal(6, output.Count);
        Assert.DoesNotContain(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_UNDERLINE);
        var highlightOutput = Assert.Single(output.Where(item => item.Subtype == PdfiumNative.FPDF_ANNOT_HIGHLIGHT));
        Assert.Equal(new NativeColor(12, 34, 56, 178), highlightOutput.Color);

        foreach (var preservedSubtype in new[]
        {
            PdfiumNative.FPDF_ANNOT_STRIKEOUT,
            PdfiumNative.FPDF_ANNOT_TEXT,
            PdfiumNative.FPDF_ANNOT_INK,
            PdfiumNative.FPDF_ANNOT_SQUARE,
            PdfiumNative.FPDF_ANNOT_CIRCLE
        })
        {
            Assert.Single(output.Where(item => item.Subtype == preservedSubtype));
        }
    }

    [Fact]
    public void SaveAsCopy_ExistingMutationWithoutStableSourceIdentityFailsClosed()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "must-not-exist.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var info = session.GetCommentsOnPage(0).Single(item => item.Subtype == CommentSubtype.Highlight);
            var state = CreateExistingState(info);
            workspace.EnsureBaseline(state);
            workspace.CommitCandidate(workspace.PrepareUpdate(
                CommentOperationKind.Update,
                state with { Color = new CommentColor(1, 2, 3, 120) }));
        }

        var error = Assert.Throws<InvalidOperationException>(() =>
            PdfCommentWriterContract.SaveAsCopy(workspace, destination));

        Assert.Contains("identidad", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void Validator_RejectsOutputThatDoesNotContainExpectedMarkup()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        var workspace = CommentWorkspace.Create(fixture.SourcePath, sourceOpenedWithPassword: false);
        AddNewMarkup(workspace, CommentSubtype.Highlight, new CommentRect(40d, 100d, 140d, 120d), new CommentColor(255, 220, 0, 160));

        Assert.Throws<InvalidDataException>(() =>
            PdfCommentValidatorContract.Validate(fixture.SourcePath, workspace));
    }

    private static void AddNewMarkup(
        CommentWorkspace workspace,
        CommentSubtype subtype,
        CommentRect rect,
        CommentColor color)
    {
        var quads = new[]
        {
            new CommentQuad(
                new CommentPoint(rect.Left, rect.Top),
                new CommentPoint(rect.Right, rect.Top),
                new CommentPoint(rect.Left, rect.Bottom),
                new CommentPoint(rect.Right, rect.Bottom))
        };
        var state = new CommentState(
            CommentKey.New(), 0, subtype, rect, color, quads,
            contents: null, inkStrokes: null, borderWidth: null,
            isNew: true, deleted: false);
        workspace.CommitCandidate(workspace.PrepareCreate(state));
    }

    private static CommentState CreateExistingState(PdfCommentInfo info)
        => new(
            CommentKey.New(),
            info.PageIndex,
            info.Subtype ?? throw new InvalidOperationException("Fixture expected a supported subtype."),
            info.Rect ?? throw new InvalidOperationException("Fixture expected a rect."),
            info.Color ?? new CommentColor(0, 0, 0, 255),
            info.Quads,
            info.Contents,
            info.Strokes,
            info.BorderWidth,
            isNew: false,
            deleted: false);
}

internal static class CommentWorkspaceIdentityContract
{
    internal static void EnsureBaseline(CommentWorkspace workspace, CommentState state, CommentIdentity identity)
    {
        var method = typeof(CommentWorkspace).GetMethod(
            "EnsureBaseline",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(CommentState), typeof(CommentIdentity) },
            modifiers: null)
            ?? throw new MissingMethodException(typeof(CommentWorkspace).FullName, "EnsureBaseline(CommentState, CommentIdentity)");
        try
        {
            method.Invoke(workspace, new object[] { state, identity });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}

internal static class PdfCommentWriterContract
{
    private const string TypeName = "SGPdf.App.Pdf.PdfCommentWriter";

    internal static void SaveAsCopy(CommentWorkspace workspace, string destinationPath)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(TypeName, throwOnError: true)!;
        var instance = Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException("No se pudo crear PdfCommentWriter.");
        var method = type.GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(CommentWorkspace), typeof(string), typeof(CancellationToken) },
            modifiers: null)
            ?? throw new MissingMethodException(TypeName, "SaveAsCopy");
        try
        {
            method.Invoke(instance, new object?[] { workspace, destinationPath, CancellationToken.None });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}

internal static class PdfCommentValidatorContract
{
    private const string TypeName = "SGPdf.App.Pdf.PdfCommentOutputValidator";

    internal static void Validate(string outputPath, CommentWorkspace workspace)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(TypeName, throwOnError: true)!;
        var method = type.GetMethod(
            "Validate",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(CommentWorkspace), typeof(CancellationToken) },
            modifiers: null)
            ?? throw new MissingMethodException(TypeName, "Validate");
        try
        {
            method.Invoke(null, new object?[] { outputPath, workspace, CancellationToken.None });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
