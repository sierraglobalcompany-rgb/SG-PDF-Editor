using System.Reflection;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class EditObjectHitTesterTests
{
    [Fact]
    public void TextHitTest_NormalQuad_SelectsInsideAndRejectsOutside()
    {
        var text = TextInfo(
            0,
            2,
            new PdfTextObjectQuad(10d, 10d, 110d, 10d, 110d, 60d, 10d, 60d),
            new PdfObjectBounds(10d, 10d, 110d, 60d));

        Assert.Equal(new TextObjectKey(0, 2), TextHit(new[] { text }, 80d, 40d));
        Assert.Null(TextHit(new[] { text }, 5d, 40d));
    }

    [Fact]
    public void TextHitTest_RotatedQuad_RejectsAabbOnlyFalsePositive()
    {
        var text = TextInfo(
            0,
            4,
            new PdfTextObjectQuad(100d, 100d, 170d, 170d, 140d, 200d, 70d, 130d),
            new PdfObjectBounds(70d, 100d, 170d, 200d));

        Assert.Equal(new TextObjectKey(0, 4), TextHit(new[] { text }, 120d, 150d));
        Assert.Null(TextHit(new[] { text }, 165d, 105d));
    }

    [Fact]
    public void TextHitTest_OverlappingTextObjects_SelectsTopmostByPageObjectIndex()
    {
        var quad = new PdfTextObjectQuad(20d, 20d, 120d, 20d, 120d, 70d, 20d, 70d);
        var bounds = new PdfObjectBounds(20d, 20d, 120d, 70d);
        var texts = new[]
        {
            TextInfo(0, 2, quad, bounds),
            TextInfo(0, 9, quad, bounds)
        };

        Assert.Equal(new TextObjectKey(0, 9), TextHit(texts, 60d, 40d));
    }

    [Fact]
    public void TextHitTest_NonFinitePoint_ReturnsNull()
    {
        var text = TextInfo(
            0,
            1,
            new PdfTextObjectQuad(0d, 0d, 100d, 0d, 100d, 50d, 0d, 50d),
            new PdfObjectBounds(0d, 0d, 100d, 50d));

        Assert.Null(TextHit(new[] { text }, double.NaN, 20d));
        Assert.Null(TextHit(new[] { text }, 20d, double.PositiveInfinity));
    }

    [Fact]
    public void MixedHitTest_TextAndImageOverlap_SelectsTopmostByPageObjectIndex()
    {
        var point = new PdfPoint(50d, 50d);
        var quad = new PdfTextObjectQuad(0d, 0d, 100d, 0d, 100d, 100d, 0d, 100d);
        var bounds = new PdfObjectBounds(0d, 0d, 100d, 100d);

        var lowerText = TextInfo(0, 4, quad, bounds);
        var upperImage = ImageInfo(0, 8, new PdfObjectMatrix(100d, 0d, 0d, 100d, 0d, 0d));
        var imageHit = MixedHit(new[] { upperImage }, new[] { lowerText }, point);
        AssertHit(imageHit, "Image", 0, 8);

        var lowerImage = ImageInfo(0, 3, new PdfObjectMatrix(100d, 0d, 0d, 100d, 0d, 0d));
        var upperText = TextInfo(0, 9, quad, bounds);
        var textHit = MixedHit(new[] { lowerImage }, new[] { upperText }, point);
        AssertHit(textHit, "Text", 0, 9);
    }

    [Fact]
    public void MixedHitTest_NoCandidateOrNonFinitePoint_ReturnsNull()
    {
        var text = TextInfo(
            0,
            5,
            new PdfTextObjectQuad(0d, 0d, 40d, 0d, 40d, 20d, 0d, 20d),
            new PdfObjectBounds(0d, 0d, 40d, 20d));
        var image = ImageInfo(0, 6, new PdfObjectMatrix(30d, 0d, 0d, 30d, 60d, 60d));

        Assert.Null(MixedHit(new[] { image }, new[] { text }, new PdfPoint(50d, 50d)));
        Assert.Null(MixedHit(new[] { image }, new[] { text }, new PdfPoint(double.NaN, 10d)));
    }

    private static TextObjectKey? TextHit(IReadOnlyList<PdfTextObjectInfo> texts, double x, double y)
    {
        var type = RequireType("SGPdf.App.Features.Edit.Text.TextHitTester");
        var method = type.GetMethod("HitTest", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (TextObjectKey?)method!.Invoke(null, new object[] { texts, new PdfPoint(x, y) });
    }

    private static object? MixedHit(
        IReadOnlyList<PdfImageObjectInfo> images,
        IReadOnlyList<PdfTextObjectInfo> texts,
        PdfPoint point)
    {
        var type = RequireType("SGPdf.App.Features.Edit.EditObjectHitTester");
        var method = type.GetMethod("HitTest", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(null, new object[] { images, texts, point });
    }

    private static void AssertHit(object? hit, string expectedKind, int pageIndex, int objectIndex)
    {
        Assert.NotNull(hit);
        var type = hit!.GetType();
        Assert.Equal(expectedKind, type.GetProperty("Kind")!.GetValue(hit)!.ToString());
        Assert.Equal(pageIndex, (int)type.GetProperty("PageIndex")!.GetValue(hit)!);
        Assert.Equal(objectIndex, (int)type.GetProperty("PageObjectIndex")!.GetValue(hit)!);
    }

    private static PdfTextObjectInfo TextInfo(
        int pageIndex,
        int objectIndex,
        PdfTextObjectQuad quad,
        PdfObjectBounds bounds)
        => new(
            new TextObjectKey(pageIndex, objectIndex),
            $"TEXT-{objectIndex}",
            new PdfObjectMatrix(1d, 0d, 0d, 1d, 0d, 0d),
            bounds,
            quad,
            "Helvetica",
            12d,
            new PdfTextFillColor(0, 0, 0, 255),
            0);

    private static PdfImageObjectInfo ImageInfo(int pageIndex, int objectIndex, PdfObjectMatrix matrix)
    {
        var quad = new[]
        {
            (X: matrix.E, Y: matrix.F),
            (X: matrix.A + matrix.E, Y: matrix.B + matrix.F),
            (X: matrix.A + matrix.C + matrix.E, Y: matrix.B + matrix.D + matrix.F),
            (X: matrix.C + matrix.E, Y: matrix.D + matrix.F)
        };
        var bounds = new PdfObjectBounds(
            quad.Min(p => p.X), quad.Min(p => p.Y), quad.Max(p => p.X), quad.Max(p => p.Y));
        return new PdfImageObjectInfo(pageIndex, objectIndex, matrix, bounds, new PdfImageObjectMetadata(20, 10, 32, 0));
    }

    private static Type RequireType(string fullName)
        => typeof(MainWindow).Assembly.GetType(fullName, throwOnError: true)!;
}
