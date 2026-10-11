using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SGPdf.App.Features.Comments;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowCommentModeTests
{
    [Fact]
    public void CommentUi_IsPresentDisabledWithoutPdf_AndHiddenOutsideMode()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var mode = OrganizeWindowTestHost.Element<Button>(window, "CommentModeButton");
                var toolbar = OrganizeWindowTestHost.Element<Border>(window, "CommentToolbar");
                var properties = OrganizeWindowTestHost.Element<StackPanel>(window, "CommentPropertiesPanel");

                Assert.False(mode.IsEnabled);
                Assert.Equal(Visibility.Collapsed, toolbar.Visibility);
                Assert.Equal(Visibility.Collapsed, properties.Visibility);

                foreach (var name in new[]
                {
                    "CommentSelectButton", "CommentHighlightButton", "CommentUnderlineButton",
                    "CommentStrikeoutButton", "CommentNoteButton", "CommentInkButton",
                    "CommentRectangleButton", "CommentEllipseButton", "CommentUndoButton",
                    "CommentRedoButton", "CommentSaveAsButton"
                })
                {
                    Assert.NotNull(window.FindName(name));
                }
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void EnterCommentMode_KeepsReaderVisible_AndOwnsNonMarkupGesturesOnly()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);

                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_commentModeActive")!);
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<Grid>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<Border>(window, "CommentToolbar").Visibility);
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<StackPanel>(window, "CommentPropertiesPanel").Visibility);
                Assert.Equal(CommentTool.Select, OrganizeWindowTestHost.GetField(window, "_commentTool"));

                OrganizeWindowTestHost.Invoke(window, "SetCommentTool", CommentTool.Note);
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "ReaderSelectionCanOwnGesture")!);

                OrganizeWindowTestHost.Invoke(window, "SetCommentTool", CommentTool.Highlight);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "ReaderSelectionCanOwnGesture")!);
            }
            finally { CloseCommentWindow(window); }
        });
    }

    [Fact]
    public void TextMarkup_FromReaderSelection_CreatesOneUndoableManagedComment()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);
                OrganizeWindowTestHost.Invoke(window, "SetCommentTool", CommentTool.Highlight);

                var selection = new ReaderTextSelection(
                    0,
                    0,
                    4,
                    new[]
                    {
                        new PdfTextRect(20d, 30d, 80d, 44d),
                        new PdfTextRect(20d, 48d, 60d, 62d)
                    });
                OrganizeWindowTestHost.SetField(window, "_readerTextSelection", selection);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryCreateCommentFromReaderSelection")!);

                var workspace = Workspace(window);
                var state = Assert.Single(workspace.EditedStates);
                Assert.Equal(CommentSubtype.Highlight, state.Subtype);
                Assert.Equal(2, state.QuadPoints.Count);
                Assert.True(workspace.IsDirty);
                Assert.True(workspace.CanUndo);
            }
            finally { CloseCommentWindow(window); }
        });
    }

    [Fact]
    public void NoteGesture_CreatesSelectedNote_AndInlineEditor()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);
                OrganizeWindowTestHost.Invoke(window, "SetCommentTool", CommentTool.Note);

                var created = OrganizeWindowTestHost.Invoke(window, "CreateCommentNoteAtPdfPoint", 0, new CommentPoint(100d, 120d));

                Assert.NotNull(created);
                var state = Assert.Single(Workspace(window).EditedStates);
                Assert.Equal(CommentSubtype.Text, state.Subtype);
                Assert.Equal(created, state.Key);
                Assert.Equal(state.Key, OrganizeWindowTestHost.GetField(window, "_selectedCommentKey"));
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_commentInlineNoteEditor"));
            }
            finally { CloseCommentWindow(window); }
        });
    }

    [Fact]
    public void ShapeAndInkGestures_ExposePreviewThenCommitSingleWorkspaceUnits()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);

                OrganizeWindowTestHost.Invoke(window, "SetCommentTool", CommentTool.Rectangle);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "BeginCommentCreationGesture", 0, new CommentPoint(30d, 40d))!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "UpdateCommentCreationGesture", new CommentPoint(90d, 110d))!);
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_commentShapePreviewRect"));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "CompleteCommentCreationGesture", new CommentPoint(90d, 110d))!);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_commentShapePreviewRect"));

                OrganizeWindowTestHost.Invoke(window, "SetCommentTool", CommentTool.Ink);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "BeginCommentCreationGesture", 0, new CommentPoint(10d, 10d))!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "UpdateCommentCreationGesture", new CommentPoint(20d, 20d))!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "UpdateCommentCreationGesture", new CommentPoint(30d, 18d))!);
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_commentInkPreviewPoints"));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "CompleteCommentCreationGesture", new CommentPoint(35d, 25d))!);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_commentInkPreviewPoints"));

                var edits = Workspace(window).EditedStates;
                Assert.Equal(2, edits.Count);
                Assert.Contains(edits, state => state.Subtype == CommentSubtype.Square);
                Assert.Contains(edits, state => state.Subtype == CommentSubtype.Ink && state.InkStrokes.Single().Points.Count >= 3);
            }
            finally { CloseCommentWindow(window); }
        });
    }

    [Fact]
    public void CommentShortcuts_UseWorkspaceUndoRedo_ButYieldToTextBoxes()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);
                OrganizeWindowTestHost.Invoke(window, "CreateCommentNoteAtPdfPoint", 0, new CommentPoint(100d, 120d));
                var workspace = Workspace(window);
                Assert.True(workspace.IsDirty);

                Assert.False((bool)OrganizeWindowTestHost.Invoke(
                    window,
                    "TryHandleCommentShortcut",
                    Key.Z,
                    ModifierKeys.Control,
                    new TextBox())!);
                Assert.True(workspace.IsDirty);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(
                    window,
                    "TryHandleCommentShortcut",
                    Key.Z,
                    ModifierKeys.Control,
                    null)!);
                Assert.False(workspace.IsDirty);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(
                    window,
                    "TryHandleCommentShortcut",
                    Key.Y,
                    ModifierKeys.Control,
                    null)!);
                Assert.True(workspace.IsDirty);
            }
            finally { CloseCommentWindow(window); }
        });
    }

    [Fact]
    public void DirtyGuard_CancelKeepsCommentModeAndWorkspace()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);
                OrganizeWindowTestHost.Invoke(window, "CreateCommentNoteAtPdfPoint", 0, new CommentPoint(100d, 120d));
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardCommentChanges", (Func<bool>)(() => false));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryLeaveCommentModeWithGuard")!);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_commentModeActive")!);
                Assert.True(Workspace(window).IsDirty);
            }
            finally
            {
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardCommentChanges", (Func<bool>)(() => true));
                CloseCommentWindow(window);
            }
        });
    }

    [Fact]
    public void SaveAs_MarksBaselineOnlyAfterWriterSuccess()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);
                OrganizeWindowTestHost.Invoke(window, "CreateCommentNoteAtPdfPoint", 0, new CommentPoint(100d, 120d));
                var workspace = Workspace(window);
                var destination = Path.Combine(fixture.DirectoryPath, "commented.pdf");
                OrganizeWindowTestHost.SetField(window, "_selectCommentPdfDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCommentPreflight",
                    (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) =>
                        new PdfEditPreflightResult(Array.Empty<PdfEditFinding>())));

                var writerCalls = 0;
                var dirtyDuringWriter = false;
                OrganizeWindowTestHost.SetField(
                    window,
                    "_saveCommentCopy",
                    (Action<CommentWorkspace, string, bool, CancellationToken>)((passed, path, confirmed, _) =>
                    {
                        writerCalls++;
                        dirtyDuringWriter = passed.IsDirty;
                        Assert.Same(workspace, passed);
                        Assert.Equal(destination, path);
                        Assert.False(confirmed);
                    }));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveCommentWorkspace")!);
                Assert.Equal(1, writerCalls);
                Assert.True(dirtyDuringWriter);
                Assert.False(workspace.IsDirty);
            }
            finally { CloseCommentWindow(window); }
        });
    }

    [Fact]
    public void SaveAs_WriterFailure_PreservesDirtyBaseline()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterCommentMode")!);
                OrganizeWindowTestHost.Invoke(window, "CreateCommentNoteAtPdfPoint", 0, new CommentPoint(100d, 120d));
                var workspace = Workspace(window);
                OrganizeWindowTestHost.SetField(window, "_selectCommentPdfDestination", (Func<string?>)(() => Path.Combine(fixture.DirectoryPath, "fail.pdf")));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCommentPreflight",
                    (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) =>
                        new PdfEditPreflightResult(Array.Empty<PdfEditFinding>())));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_saveCommentCopy",
                    (Action<CommentWorkspace, string, bool, CancellationToken>)((_, _, _, _) =>
                        throw new IOException("forced comment save failure")));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveCommentWorkspace")!);
                Assert.True(workspace.IsDirty);
            }
            finally { CloseCommentWindow(window); }
        });
    }

    private static CommentWorkspace Workspace(MainWindow window)
        => Assert.IsType<CommentWorkspace>(OrganizeWindowTestHost.GetField(window, "_commentWorkspace"));

    private static void CloseCommentWindow(MainWindow window)
    {
        try
        {
            if (OrganizeWindowTestHost.GetField(window, "_commentModeActive") is true)
            {
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardCommentChanges", (Func<bool>)(() => true));
                OrganizeWindowTestHost.Invoke(window, "TryLeaveCommentModeWithGuard");
            }
        }
        catch
        {
        }
        OrganizeWindowTestHost.CloseClean(window);
    }
}
