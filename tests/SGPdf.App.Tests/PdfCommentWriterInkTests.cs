using System.Reflection;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentWriterInkTests
{
    [Fact]
    public void SaveAsCopy_CreatesInkWithMultipleStrokesAndPreservesPointOrderAndColor()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        var destination = Path.Combine(fixture.DirectoryPath, "created-ink.pdf");
        var workspace = CommentWorkspace.Create(fixture.SourcePath, sourceOpenedWithPassword: false);
        var rect = new CommentRect(60d, 140d, 220d, 240d);
        var color = new CommentColor(25, 70, 210, 220);
        var strokes = new[]
        {
            new CommentStroke(new[]
            {
                new CommentPoint(70d, 160d),
                new CommentPoint(95d, 205d),
                new CommentPoint(135d, 180d)
            }),
            new CommentStroke(new[]
            {
                new CommentPoint(145d, 170d),
                new CommentPoint(175d, 215d),
                new CommentPoint(205d, 185d),
                new CommentPoint(215d, 155d)
            })
        };

        AddNewInk(workspace, rect, color, strokes);
        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        using var reopened = PdfDocumentSession.Open(destination);
        var ink = Assert.Single(reopened.GetCommentsOnPage(0), item => item.Subtype == CommentSubtype.Ink);
        Assert.Equal(rect, ink.Rect!.Value);
        Assert.Equal(color, ink.Color!.Value);
        AssertStrokes(strokes, ink.Strokes);

        var native = Assert.Single(CommentNativeCharacterizationHarness.Inspect(destination));
        Assert.Equal(PdfiumNative.FPDF_ANNOT_INK, native.Subtype);
        Assert.Equal(2, native.InkPaths.Length);
        Assert.Equal(3, native.InkPaths[0].Length);
        Assert.Equal(4, native.InkPaths[1].Length);
        Assert.Equal(70f, native.InkPaths[0][0].X, 3);
        Assert.Equal(160f, native.InkPaths[0][0].Y, 3);
        Assert.Equal(215f, native.InkPaths[1][3].X, 3);
        Assert.Equal(155f, native.InkPaths[1][3].Y, 3);
    }

    [Fact]
    public void SaveAsCopy_ReplacesExistingInkListUsingStableIdentity()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "edited-ink.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);
        var updatedRect = new CommentRect(80d, 145d, 250d, 245d);
        var updatedColor = new CommentColor(210, 40, 40, 230);
        var updatedStrokes = new[]
        {
            new CommentStroke(new[]
            {
                new CommentPoint(90d, 155d),
                new CommentPoint(130d, 225d),
                new CommentPoint(170d, 175d)
            }),
            new CommentStroke(new[]
            {
                new CommentPoint(180d, 165d),
                new CommentPoint(220d, 230d),
                new CommentPoint(240d, 190d)
            })
        };

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var comments = session.GetCommentsOnPage(0);
            var identities = CommentIdentity.CreateForPage(comments);
            var inkIndex = Array.FindIndex(comments.ToArray(), item => item.Subtype == CommentSubtype.Ink);
            Assert.True(inkIndex >= 0);

            var inkState = CreateExistingState(comments[inkIndex]);
            CommentWorkspaceIdentityContract.EnsureBaseline(workspace, inkState, identities[inkIndex]);
            var updated = inkState with
            {
                Rect = updatedRect,
                Color = updatedColor,
                InkStrokes = updatedStrokes
            };
            workspace.CommitCandidate(workspace.PrepareUpdate(CommentOperationKind.Update, updated));
        }

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        using var reopened = PdfDocumentSession.Open(destination);
        var outputInk = Assert.Single(reopened.GetCommentsOnPage(0), item => item.Subtype == CommentSubtype.Ink);
        Assert.Equal(updatedRect, outputInk.Rect!.Value);
        Assert.Equal(updatedColor, outputInk.Color!.Value);
        AssertStrokes(updatedStrokes, outputInk.Strokes);
        Assert.Equal(7, CommentNativeCharacterizationHarness.Inspect(destination).Count);
    }

    [Fact]
    public void SaveAsCopy_DeletesExistingInkAndPreservesOtherAnnotations()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        var destination = Path.Combine(fixture.DirectoryPath, "deleted-ink.pdf");
        var workspace = CommentWorkspace.Create(fixture.OutputPath, sourceOpenedWithPassword: false);

        using (var session = PdfDocumentSession.Open(fixture.OutputPath))
        {
            var comments = session.GetCommentsOnPage(0);
            var identities = CommentIdentity.CreateForPage(comments);
            var inkIndex = Array.FindIndex(comments.ToArray(), item => item.Subtype == CommentSubtype.Ink);
            Assert.True(inkIndex >= 0);

            var inkState = CreateExistingState(comments[inkIndex]);
            CommentWorkspaceIdentityContract.EnsureBaseline(workspace, inkState, identities[inkIndex]);
            workspace.CommitCandidate(workspace.PrepareDelete(inkState.Key));
        }

        PdfCommentWriterContract.SaveAsCopy(workspace, destination);

        var output = CommentNativeCharacterizationHarness.Inspect(destination);
        Assert.Equal(6, output.Count);
        Assert.DoesNotContain(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_INK);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_TEXT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_HIGHLIGHT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_UNDERLINE);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_STRIKEOUT);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_SQUARE);
        Assert.Single(output, item => item.Subtype == PdfiumNative.FPDF_ANNOT_CIRCLE);
    }

    [Fact]
    public void SaveAsCopy_CancelledInkPlanDoesNotPublishDestination()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        var destination = Path.Combine(fixture.DirectoryPath, "cancelled-ink.pdf");
        var workspace = CommentWorkspace.Create(fixture.SourcePath, sourceOpenedWithPassword: false);
        AddNewInk(
            workspace,
            new CommentRect(60d, 140d, 220d, 240d),
            new CommentColor(20, 20, 20, 255),
            new[]
            {
                new CommentStroke(new[]
                {
                    new CommentPoint(70d, 160d),
                    new CommentPoint(120d, 220d),
                    new CommentPoint(190d, 170d)
                })
            });

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => SaveAsCopyWithCancellation(workspace, destination, cancellation.Token));
        Assert.False(File.Exists(destination));
        Assert.True(workspace.IsDirty);
    }

    private static CommentKey AddNewInk(
        CommentWorkspace workspace,
        CommentRect rect,
        CommentColor color,
        IReadOnlyList<CommentStroke> strokes)
    {
        var key = CommentKey.New();
        var state = new CommentState(
            key,
            pageIndex: 0,
            CommentSubtype.Ink,
            rect,
            color,
            quadPoints: null,
            contents: null,
            inkStrokes: strokes,
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

    private static void AssertStrokes(
        IReadOnlyList<CommentStroke> expected,
        IReadOnlyList<CommentStroke> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var strokeIndex = 0; strokeIndex < expected.Count; strokeIndex++)
        {
            Assert.Equal(expected[strokeIndex].Points.Count, actual[strokeIndex].Points.Count);
            for (var pointIndex = 0; pointIndex < expected[strokeIndex].Points.Count; pointIndex++)
            {
                Assert.Equal(expected[strokeIndex].Points[pointIndex].X, actual[strokeIndex].Points[pointIndex].X, 3);
                Assert.Equal(expected[strokeIndex].Points[pointIndex].Y, actual[strokeIndex].Points[pointIndex].Y, 3);
            }
        }
    }

    private static void SaveAsCopyWithCancellation(
        CommentWorkspace workspace,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        const string typeName = "SGPdf.App.Pdf.PdfCommentWriter";
        var type = typeof(PdfDocumentSession).Assembly.GetType(typeName, throwOnError: true)!;
        var instance = Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException("No se pudo crear PdfCommentWriter.");
        var method = type.GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(CommentWorkspace), typeof(string), typeof(CancellationToken) },
            modifiers: null)
            ?? throw new MissingMethodException(typeName, "SaveAsCopy");

        try
        {
            method.Invoke(instance, new object?[] { workspace, destinationPath, cancellationToken });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
