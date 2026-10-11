using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfiumNativeGateCollection.Name)]
public sealed class CommentNativeCharacterizationTests
{
    private static readonly int[] RequiredSubtypes =
    {
        PdfiumNative.FPDF_ANNOT_HIGHLIGHT,
        PdfiumNative.FPDF_ANNOT_UNDERLINE,
        PdfiumNative.FPDF_ANNOT_STRIKEOUT,
        PdfiumNative.FPDF_ANNOT_TEXT,
        PdfiumNative.FPDF_ANNOT_INK,
        PdfiumNative.FPDF_ANNOT_SQUARE,
        PdfiumNative.FPDF_ANNOT_CIRCLE
    };

    [Fact]
    public void PinnedPdfium_ReportsAllSevenF8SubtypesCreatable()
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        try
        {
            foreach (var subtype in RequiredSubtypes)
                Assert.NotEqual(0, PdfiumNative.FPDFAnnot_IsSupportedSubtype(subtype));
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    [Fact]
    public void PinnedPdfium_SevenSubtypes_CreateSaveReopenReadAndRender()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(
            fixture.SourcePath,
            fixture.OutputPath);

        var snapshots = CommentNativeCharacterizationHarness.Inspect(fixture.OutputPath);
        Assert.Equal(7, snapshots.Count);
        var bySubtype = snapshots.ToDictionary(snapshot => snapshot.Subtype);
        Assert.Equal(RequiredSubtypes.OrderBy(value => value), bySubtype.Keys.OrderBy(value => value));

        AssertMarkup(
            bySubtype[PdfiumNative.FPDF_ANNOT_HIGHLIGHT],
            new NativeColor(255, 220, 0, 160));
        AssertMarkup(
            bySubtype[PdfiumNative.FPDF_ANNOT_UNDERLINE],
            new NativeColor(0, 95, 200, 210));
        AssertMarkup(
            bySubtype[PdfiumNative.FPDF_ANNOT_STRIKEOUT],
            new NativeColor(210, 30, 30, 230));

        var note = bySubtype[PdfiumNative.FPDF_ANNOT_TEXT];
        Assert.Equal(CommentNativeCharacterizationHarness.NoteContents, note.Contents);
        Assert.Equal(new NativeColor(255, 210, 0, 255), note.Color);
        AssertRectNear(note.Rect, 350f, 340f, 375f, 315f);

        var ink = bySubtype[PdfiumNative.FPDF_ANNOT_INK];
        Assert.Equal(new NativeColor(190, 20, 160, 220), ink.Color);
        Assert.Single(ink.InkPaths);
        var inkPath = ink.InkPaths[0];
        Assert.Equal(CommentNativeCharacterizationHarness.InkStroke.Length, inkPath.Length);
        for (var i = 0; i < inkPath.Length; i++)
            AssertPointNear(inkPath[i], CommentNativeCharacterizationHarness.InkStroke[i]);

        var square = bySubtype[PdfiumNative.FPDF_ANNOT_SQUARE];
        Assert.Equal(new NativeColor(20, 80, 210, 255), square.Color);
        AssertRectNear(square.Rect, 220f, 340f, 320f, 260f);
        AssertBorder(square.Border);

        var circle = bySubtype[PdfiumNative.FPDF_ANNOT_CIRCLE];
        Assert.Equal(new NativeColor(20, 150, 70, 255), circle.Color);
        AssertRectNear(circle.Rect, 220f, 230f, 320f, 150f);
        AssertBorder(circle.Border);

        using var session = PdfDocumentSession.Open(fixture.OutputPath);
        var rendered = session.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
        Assert.NotEmpty(rendered.Pixels);
    }

    private static void AssertMarkup(NativeCommentSnapshot snapshot, NativeColor expectedColor)
    {
        Assert.Equal(expectedColor, snapshot.Color);
        var quad = Assert.Single(snapshot.Quads);
        AssertQuadNear(quad, CommentNativeCharacterizationHarness.MarkupQuad);
        Assert.True(snapshot.Rect.Right > snapshot.Rect.Left);
        Assert.True(snapshot.Rect.Top > snapshot.Rect.Bottom);
    }

    private static void AssertBorder(NativeBorder? border)
    {
        var value = Assert.IsType<NativeBorder>(border);
        Assert.InRange(value.HorizontalRadius, -0.001f, 0.001f);
        Assert.InRange(value.VerticalRadius, -0.001f, 0.001f);
        Assert.InRange(value.Width, 2.499f, 2.501f);
    }

    private static void AssertRectNear(
        PdfiumNative.RectF actual,
        float left,
        float top,
        float right,
        float bottom)
    {
        Assert.InRange(actual.Left, left - 0.01f, left + 0.01f);
        Assert.InRange(actual.Top, top - 0.01f, top + 0.01f);
        Assert.InRange(actual.Right, right - 0.01f, right + 0.01f);
        Assert.InRange(actual.Bottom, bottom - 0.01f, bottom + 0.01f);
    }

    private static void AssertQuadNear(PdfiumNative.QuadPointsF actual, PdfiumNative.QuadPointsF expected)
    {
        Assert.InRange(actual.X1, expected.X1 - 0.01f, expected.X1 + 0.01f);
        Assert.InRange(actual.Y1, expected.Y1 - 0.01f, expected.Y1 + 0.01f);
        Assert.InRange(actual.X2, expected.X2 - 0.01f, expected.X2 + 0.01f);
        Assert.InRange(actual.Y2, expected.Y2 - 0.01f, expected.Y2 + 0.01f);
        Assert.InRange(actual.X3, expected.X3 - 0.01f, expected.X3 + 0.01f);
        Assert.InRange(actual.Y3, expected.Y3 - 0.01f, expected.Y3 + 0.01f);
        Assert.InRange(actual.X4, expected.X4 - 0.01f, expected.X4 + 0.01f);
        Assert.InRange(actual.Y4, expected.Y4 - 0.01f, expected.Y4 + 0.01f);
    }

    private static void AssertPointNear(PdfiumNative.PointF actual, PdfiumNative.PointF expected)
    {
        Assert.InRange(actual.X, expected.X - 0.01f, expected.X + 0.01f);
        Assert.InRange(actual.Y, expected.Y - 0.01f, expected.Y + 0.01f);
    }
}