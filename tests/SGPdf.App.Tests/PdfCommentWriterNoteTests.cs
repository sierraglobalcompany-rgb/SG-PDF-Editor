using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentWriterNoteTests
{
    [Fact]
    public void SaveAsCopy_CreatesUnicodeTextNoteWithPositionAndColorWithoutInventingPopup()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        var destination = Path.Combine(fixture.DirectoryPath, "created-note.pdf");
        var workspace = CommentWorkspace.Create(fixture.SourcePath, sourceOpenedWithPassword: false);
        var rect = new CommentRect(320d, 280d, 348d, 308d);
        const string contents = "Nota niño café 東京 😀";
        var color = new CommentColor(255, 210, 0, 255);

        AddNewNote(workspace, rect, color, contents);
        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        var output = CommentNativeCharacterizationHarness.Inspect(destination);
        var note = Assert.Single(output);
        Assert.Equal(PdfiumNative.FPDF_ANNOT_TEXT, note.Subtype);
        Assert.Equal(contents, note.Contents);
        Assert.Equal(new NativeColor(color.Red, color.Green, color.Blue, color.Alpha), note.Color);
        AssertRect(rect, note.Rect);
    }

    [Fact]
    public void SaveAsCopy_EditsExistingTextAndMoveUsingStableIdentity()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "edited-note.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);
        var movedRect = new CommentRect(390d, 260d, 420d, 290d);
        const string updatedContents = "Contenido actualizado: Bogotá, niño, 東京 😀";

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var comments = session.GetCommentsOnPage(0);
            var identities = CommentIdentity.CreateForPage(comments);
            var noteIndex = Array.FindIndex(comments.ToArray(), item => item.Subtype == CommentSubtype.Text);
            Assert.True(noteIndex >= 0);

            var noteState = CreateExistingState(comments[noteIndex]);
            CommentWorkspaceIdentityContract.EnsureBaseline(workspace, noteState, identities[noteIndex]);
            workspace.CommitCandidate(workspace.PrepareNoteText(noteState.Key, updatedContents));
            workspace.CommitCandidate(workspace.PrepareMove(noteState.Key, movedRect));
        }

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        using var reopened = PdfDocumentSession.Open(destination);
        var outputNote = Assert.Single(reopened.GetCommentsOnPage(0), item => item.Subtype == CommentSubtype.Text);
        Assert.Equal(updatedContents, outputNote.Contents);
        Assert.Equal(movedRect, outputNote.Rect);

        var native = CommentNativeCharacterizationHarness.Inspect(destination);
        Assert.Equal(7, native.Count);
    }

    [Fact]
    public void SaveAsCopy_DeletesExistingTextNoteAndPreservesOtherAnnotations()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "deleted-note.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var comments = session.GetCommentsOnPage(0);
            var identities = CommentIdentity.CreateForPage(comments);
            var noteIndex = Array.FindIndex(comments.ToArray(), item => item.Subtype == CommentSubtype.Text);
            Assert.True(noteIndex >= 0);

            var noteState = CreateExistingState(comments[noteIndex]);
            CommentWorkspaceIdentityContract.EnsureBaseline(workspace, noteState, identities[noteIndex]);
            workspace.CommitCandidate(workspace.PrepareDelete(noteState.Key));
        }

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        var output = CommentNativeCharacterizationHarness.Inspect(destination);
        Assert.Equal(6, output.Count);
        Assert.DoesNotContain(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_TEXT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_HIGHLIGHT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_UNDERLINE);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_STRIKEOUT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_INK);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_SQUARE);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_CIRCLE);
    }

    [Fact]
    public void CancelEmptyNewNote_RemovesCandidateAndLeavesWorkspaceClean()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        var destination = Path.Combine(fixture.DirectoryPath, "cancelled-note.pdf");
        var workspace = CommentWorkspace.Create(fixture.SourcePath, sourceOpenedWithPassword: false);
        var key = AddNewNote(
            workspace,
            new CommentRect(100d, 100d, 125d, 125d),
            new CommentColor(255, 210, 0, 255),
            string.Empty);

        Assert.True(workspace.IsDirty);
        workspace.CommitCandidate(workspace.PrepareDelete(key));

        Assert.False(workspace.IsDirty);
        Assert.Empty(workspace.EditedStates);
        Assert.Throws<InvalidOperationException>(() => PdfCommentWriterContract.SaveAsCopy(workspace, destination));
        Assert.False(File.Exists(destination));
    }

    private static CommentKey AddNewNote(
        CommentWorkspace workspace,
        CommentRect rect,
        CommentColor color,
        string contents)
    {
        var key = CommentKey.New();
        var state = new CommentState(
            key,
            pageIndex: 0,
            CommentSubtype.Text,
            rect,
            color,
            quadPoints: null,
            contents,
            inkStrokes: null,
            borderWidth: null,
            isNew: true,
            deleted: false);
        workspace.CommitCandidate(workspace.PrepareCreate(state));
        return key;
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

    private static void AssertRect(CommentRect expected, PdfiumNative.RectF actual)
    {
        Assert.Equal(expected.Left, actual.Left, 3);
        Assert.Equal(expected.Bottom, actual.Bottom, 3);
        Assert.Equal(expected.Right, actual.Right, 3);
        Assert.Equal(expected.Top, actual.Top, 3);
    }
}
