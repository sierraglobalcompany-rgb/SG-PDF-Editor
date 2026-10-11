using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentWriterShapeTests
{
    [Fact]
    public void SaveAsCopy_CreatesMultipleSquareAndCircleShapesWithGeometryColorAndBorder()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        var destination = Path.Combine(fixture.DirectoryPath, "created-shapes.pdf");
        var workspace = CommentWorkspace.Create(fixture.SourcePath, sourceOpenedWithPassword: false);

        var squareA = AddNewShape(
            workspace,
            CommentSubtype.Square,
            new CommentRect(60d, 260d, 170d, 340d),
            new CommentColor(25, 85, 210, 255),
            2.25d);
        var circle = AddNewShape(
            workspace,
            CommentSubtype.Circle,
            new CommentRect(210d, 155d, 325d, 245d),
            new CommentColor(20, 155, 75, 230),
            3.5d);
        var squareB = AddNewShape(
            workspace,
            CommentSubtype.Square,
            new CommentRect(350d, 120d, 455d, 205d),
            new CommentColor(215, 55, 45, 200),
            1.5d);

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        using var reopened = PdfDocumentSession.Open(destination);
        var shapes = reopened.GetCommentsOnPage(0)
            .Where(item => item.Subtype is CommentSubtype.Square or CommentSubtype.Circle)
            .ToArray();
        Assert.Equal(3, shapes.Length);
        AssertShape(shapes, squareA, CommentSubtype.Square, new CommentRect(60d, 260d, 170d, 340d), new CommentColor(25, 85, 210, 255), 2.25d);
        AssertShape(shapes, circle, CommentSubtype.Circle, new CommentRect(210d, 155d, 325d, 245d), new CommentColor(20, 155, 75, 230), 3.5d);
        AssertShape(shapes, squareB, CommentSubtype.Square, new CommentRect(350d, 120d, 455d, 205d), new CommentColor(215, 55, 45, 200), 1.5d);

        var native = CommentNativeCharacterizationHarness.Inspect(destination);
        Assert.Equal(2, native.Count(item => item.Subtype == PdfiumNative.FPDF_ANNOT_SQUARE));
        Assert.Single(native, item => item.Subtype == PdfiumNative.FPDF_ANNOT_CIRCLE);
    }

    [Fact]
    public void SaveAsCopy_MovesResizesAndRestylesExistingSquareAndCircleUsingStableIdentity()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "edited-shapes.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var comments = session.GetCommentsOnPage(0);
            var identities = CommentIdentity.CreateForPage(comments);

            RegisterExistingShape(workspace, comments, identities, CommentSubtype.Square, out var squareKey);
            RegisterExistingShape(workspace, comments, identities, CommentSubtype.Circle, out var circleKey);

            workspace.CommitCandidate(workspace.PrepareMove(squareKey, new CommentRect(250d, 280d, 350d, 360d)));
            workspace.CommitCandidate(workspace.PrepareResize(squareKey, new CommentRect(250d, 250d, 390d, 365d)));
            var squareUpdated = workspace.GetState(squareKey) with
            {
                Color = new CommentColor(185, 35, 45, 210),
                BorderWidth = 4.25d
            };
            workspace.CommitCandidate(workspace.PrepareUpdate(CommentOperationKind.Update, squareUpdated));

            workspace.CommitCandidate(workspace.PrepareMove(circleKey, new CommentRect(95d, 95d, 205d, 180d)));
            workspace.CommitCandidate(workspace.PrepareResize(circleKey, new CommentRect(90d, 80d, 235d, 205d)));
            var circleUpdated = workspace.GetState(circleKey) with
            {
                Color = new CommentColor(35, 110, 215, 190),
                BorderWidth = 1.75d
            };
            workspace.CommitCandidate(workspace.PrepareUpdate(CommentOperationKind.Update, circleUpdated));
        }

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        using var reopened = PdfDocumentSession.Open(destination);
        var commentsAfter = reopened.GetCommentsOnPage(0);
        var square = Assert.Single(commentsAfter, item => item.Subtype == CommentSubtype.Square);
        Assert.Equal(new CommentRect(250d, 250d, 390d, 365d), square.Rect!.Value);
        Assert.Equal(new CommentColor(185, 35, 45, 210), square.Color!.Value);
        Assert.Equal(4.25d, square.BorderWidth!.Value, 3);

        var circle = Assert.Single(commentsAfter, item => item.Subtype == CommentSubtype.Circle);
        Assert.Equal(new CommentRect(90d, 80d, 235d, 205d), circle.Rect!.Value);
        Assert.Equal(new CommentColor(35, 110, 215, 190), circle.Color!.Value);
        Assert.Equal(1.75d, circle.BorderWidth!.Value, 3);
        Assert.Equal(7, commentsAfter.Count);
    }

    [Fact]
    public void SaveAsCopy_DeletesExistingSquareAndCircleAndPreservesOtherAnnotations()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "deleted-shapes.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var comments = session.GetCommentsOnPage(0);
            var identities = CommentIdentity.CreateForPage(comments);
            RegisterExistingShape(workspace, comments, identities, CommentSubtype.Square, out var squareKey);
            RegisterExistingShape(workspace, comments, identities, CommentSubtype.Circle, out var circleKey);
            workspace.CommitCandidate(workspace.PrepareDelete(squareKey));
            workspace.CommitCandidate(workspace.PrepareDelete(circleKey));
        }

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        var output = CommentNativeCharacterizationHarness.Inspect(destination);
        Assert.Equal(5, output.Count);
        Assert.DoesNotContain(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_SQUARE);
        Assert.DoesNotContain(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_CIRCLE);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_TEXT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_HIGHLIGHT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_UNDERLINE);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_STRIKEOUT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_INK);
    }

    [Fact]
    public void SaveAsCopy_OnRotatedPage_PreservesShapePdfCoordinatesAndPageRotation()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f8-shape-rotation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var source = Path.Combine(directory, "rotated-source.pdf");
            var destination = Path.Combine(directory, "rotated-shape.pdf");
            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                page.Rotate = 90;
                document.Save(source);
            }

            var workspace = CommentWorkspace.Create(source, sourceOpenedWithPassword: false);
            var expectedRect = new CommentRect(55d, 110d, 185d, 225d);
            AddNewShape(
                workspace,
                CommentSubtype.Circle,
                expectedRect,
                new CommentColor(120, 45, 205, 255),
                2.5d);

            PdfCommentWriterContract.SaveAsCopy(workspace, destination);

            using (var reopened = PdfDocumentSession.Open(destination))
            {
                var circle = Assert.Single(reopened.GetCommentsOnPage(0), item => item.Subtype == CommentSubtype.Circle);
                Assert.Equal(expectedRect, circle.Rect!.Value);
                Assert.Equal(2.5d, circle.BorderWidth!.Value, 3);
            }

            using var pdf = PdfReader.Open(destination, PdfDocumentOpenMode.Import);
            Assert.Equal(90, pdf.Pages[0].Rotate);
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static CommentKey AddNewShape(
        CommentWorkspace workspace,
        CommentSubtype subtype,
        CommentRect rect,
        CommentColor color,
        double borderWidth)
    {
        var key = CommentKey.New();
        var state = new CommentState(
            key,
            pageIndex: 0,
            subtype,
            rect,
            color,
            quadPoints: null,
            contents: null,
            inkStrokes: null,
            borderWidth,
            isNew: true,
            deleted: false);
        workspace.CommitCandidate(workspace.PrepareCreate(state));
        return key;
    }

    private static void RegisterExistingShape(
        CommentWorkspace workspace,
        IReadOnlyList<PdfCommentInfo> comments,
        IReadOnlyList<CommentIdentity> identities,
        CommentSubtype subtype,
        out CommentKey key)
    {
        var index = Array.FindIndex(comments.ToArray(), item => item.Subtype == subtype);
        Assert.True(index >= 0);
        var state = CreateExistingState(comments[index]);
        key = state.Key;
        CommentWorkspaceIdentityContract.EnsureBaseline(workspace, state, identities[index]);
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

    private static void AssertShape(
        IReadOnlyList<PdfCommentInfo> shapes,
        CommentKey key,
        CommentSubtype subtype,
        CommentRect rect,
        CommentColor color,
        double borderWidth)
    {
        _ = key;
        var shape = Assert.Single(shapes, item => item.Subtype == subtype && item.Rect == rect);
        Assert.Equal(color, shape.Color!.Value);
        Assert.Equal(borderWidth, shape.BorderWidth!.Value, 3);
    }
}
