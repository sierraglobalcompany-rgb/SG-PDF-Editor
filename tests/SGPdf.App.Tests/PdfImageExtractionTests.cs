using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Pdf;
using SkiaSharp;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfImageExtractionTests
{
    [Fact]
    public void Extract_OpaqueImage_ReturnsValidVisuallyFaithfulPng()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        using var session = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(session.GetImageObjects(0));

        var png = Extract(session, image.PageIndex, image.PageObjectIndex);

        AssertPngSignature(png);
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);
        Assert.True(bitmap.Width > 0);
        Assert.True(bitmap.Height > 0);
        var center = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        Assert.True(center.Green > center.Red);
        Assert.True(center.Green > center.Blue);
        Assert.Equal((byte)255, center.Alpha);
    }

    [Fact]
    public void Extract_RotatedScaledImage_ReturnsValidImageContentPng()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateRotatedImage();
        using var session = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(session.GetImageObjects(0));

        var png = Extract(session, image.PageIndex, image.PageObjectIndex);

        AssertPngSignature(png);
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);
        Assert.True(bitmap.Width > 0);
        Assert.True(bitmap.Height > 0);
        var center = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        Assert.True(center.Red > center.Green);
        Assert.True(center.Blue > center.Green);
    }

    [Fact]
    public void Extract_TransparentImage_PreservesRepresentativeAlpha()
    {
        using var fixture = Task5ImageTestFixture.CreateTransparentPdf();
        using var session = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(session.GetImageObjects(0));

        var png = Extract(session, image.PageIndex, image.PageObjectIndex);

        AssertPngSignature(png);
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);
        var alphas = Enumerable.Range(0, bitmap.Height)
            .SelectMany(y => Enumerable.Range(0, bitmap.Width).Select(x => bitmap.GetPixel(x, y).Alpha))
            .ToArray();
        Assert.Contains(alphas, alpha => alpha < 255);
        Assert.Contains(alphas, alpha => alpha == 255);
    }

    [Fact]
    public void Extract_InvalidObjectIndex_IsControlledAndWritesNothing()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        using var session = PdfDocumentSession.Open(fixture.Path);

        Assert.Throws<ArgumentOutOfRangeException>(() => Extract(session, 0, 999));
    }

    private static byte[] Extract(PdfDocumentSession session, int pageIndex, int objectIndex)
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetImagePng",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int), typeof(int), typeof(CancellationToken) },
            modifiers: null);
        if (method is null)
            throw new MissingMethodException(typeof(PdfDocumentSession).FullName, "GetImagePng");

        try
        {
            return Assert.IsType<byte[]>(method.Invoke(session, new object?[] { pageIndex, objectIndex, CancellationToken.None }));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static void AssertPngSignature(byte[] bytes)
    {
        Assert.True(bytes.Length > 8);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes.Take(8).ToArray());
    }
}
