using System.Reflection;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextMarkupQuadBuilderTests
{
    [Fact]
    public void BuildQuads_MapsPdfTextRectsToPdfiumQuadOrder()
    {
        var rects = new[]
        {
            new PdfTextRect(10d, 20d, 40d, 30d),
            new PdfTextRect(12d, 5d, 55d, 15d)
        };

        var quads = TextMarkupQuadBuilderContract.BuildQuads(rects);

        Assert.Equal(2, quads.Count);
        Assert.Equal(new CommentQuad(
            new CommentPoint(10d, 30d),
            new CommentPoint(40d, 30d),
            new CommentPoint(10d, 20d),
            new CommentPoint(40d, 20d)), quads[0]);
        Assert.Equal(new CommentQuad(
            new CommentPoint(12d, 15d),
            new CommentPoint(55d, 15d),
            new CommentPoint(12d, 5d),
            new CommentPoint(55d, 5d)), quads[1]);
    }

    [Fact]
    public void GetBounds_ReturnsUnionForMultilineSelection()
    {
        var rects = new[]
        {
            new PdfTextRect(10d, 40d, 80d, 50d),
            new PdfTextRect(20d, 20d, 70d, 30d)
        };

        var bounds = TextMarkupQuadBuilderContract.GetBounds(rects);

        Assert.Equal(new CommentRect(10d, 20d, 80d, 50d), bounds);
    }

    [Fact]
    public void BuildQuads_RejectsEmptyOrInvalidGeometry()
    {
        Assert.Throws<ArgumentException>(() => TextMarkupQuadBuilderContract.BuildQuads(Array.Empty<PdfTextRect>()));
        Assert.Throws<ArgumentException>(() => TextMarkupQuadBuilderContract.BuildQuads(new[]
        {
            new PdfTextRect(10d, 20d, 10d, 30d)
        }));
        Assert.Throws<ArgumentException>(() => TextMarkupQuadBuilderContract.BuildQuads(new[]
        {
            new PdfTextRect(double.NaN, 20d, 30d, 40d)
        }));
    }
}

internal static class TextMarkupQuadBuilderContract
{
    private const string TypeName = "SGPdf.App.Features.Comments.TextMarkupQuadBuilder";

    internal static IReadOnlyList<CommentQuad> BuildQuads(IReadOnlyList<PdfTextRect> rects)
        => (IReadOnlyList<CommentQuad>)Invoke("BuildQuads", rects)!;

    internal static CommentRect GetBounds(IReadOnlyList<PdfTextRect> rects)
        => (CommentRect)Invoke("GetBounds", rects)!;

    private static object? Invoke(string methodName, IReadOnlyList<PdfTextRect> rects)
    {
        var type = typeof(CommentState).Assembly.GetType(TypeName, throwOnError: true)!;
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(TypeName, methodName);
        try
        {
            return method.Invoke(null, new object?[] { rects });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
