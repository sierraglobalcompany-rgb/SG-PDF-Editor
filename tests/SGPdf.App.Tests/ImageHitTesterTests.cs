using System.Reflection;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageHitTesterTests
{
    [Fact]
    public void HitTest_PlainNonSquareAndNearEdge_UsesPdfSpaceGeometry()
    {
        var images = new[]
        {
            Info(0, 2, new PdfObjectMatrix(120d, 0d, 0d, 40d, 25d, 35d)),
            Info(0, 7, new PdfObjectMatrix(30d, 0d, 0d, 55d, 265d, 340d))
        };

        Assert.Equal(new ImageObjectKey(0, 2), Hit(images, 80d, 55d));
        Assert.Equal(new ImageObjectKey(0, 7), Hit(images, 294.5d, 394d));
        Assert.Null(Hit(images, 24.9d, 55d));
        Assert.Null(Hit(images, 299.5d, 399.5d));
    }

    [Fact]
    public void HitTest_RotatedImage_RejectsAabbOnlyFalsePositive()
    {
        var rotated = Info(
            0,
            4,
            new PdfObjectMatrix(100d, 100d, -50d, 50d, 100d, 100d),
            new PdfObjectBounds(50d, 100d, 200d, 250d));

        Assert.Equal(new ImageObjectKey(0, 4), Hit(new[] { rotated }, 125d, 175d));
        Assert.Null(Hit(new[] { rotated }, 190d, 110d));
    }

    [Fact]
    public void HitTest_OverlappingImages_SelectsLastPaintedCandidateFromProvenEnumerationOrder()
    {
        var matrix = new PdfObjectMatrix(100d, 0d, 0d, 100d, 50d, 60d);
        var images = new[] { Info(0, 1, matrix), Info(0, 5, matrix) };

        Assert.Equal(new ImageObjectKey(0, 5), Hit(images, 90d, 100d));
    }

    [Fact]
    public void HitTest_RotatedOverlappingImages_IsDeterministicAtMultipleZooms()
    {
        var matrix = new PdfObjectMatrix(80d, 80d, -40d, 40d, 100d, 100d);
        var images = new[] { Info(0, 3, matrix), Info(0, 9, matrix) };
        var normal = new PdfPageDeviceTransform(0d, 400d, 1d, 0d, 0d, -1d, 300, 400);
        var zoomed = new PdfPageDeviceTransform(0d, 400d, .5d, 0d, 0d, -.5d, 600, 800);

        var pdfAt100 = DeviceToPdf(120d, 260d, normal);
        var pdfAt200 = DeviceToPdf(240d, 520d, zoomed);

        Assert.Equal(pdfAt100, pdfAt200);
        Assert.Equal(new ImageObjectKey(0, 9), Hit(images, pdfAt100.X, pdfAt100.Y));
        Assert.Equal(new ImageObjectKey(0, 9), Hit(images, pdfAt200.X, pdfAt200.Y));
    }

    private static PdfImageObjectInfo Info(int pageIndex, int objectIndex, PdfObjectMatrix matrix, PdfObjectBounds? bounds = null)
    {
        var quad = new[]
        {
            (X: matrix.E, Y: matrix.F),
            (X: matrix.A + matrix.E, Y: matrix.B + matrix.F),
            (X: matrix.A + matrix.C + matrix.E, Y: matrix.B + matrix.D + matrix.F),
            (X: matrix.C + matrix.E, Y: matrix.D + matrix.F)
        };
        var resolved = bounds ?? new PdfObjectBounds(
            quad.Min(p => p.X), quad.Min(p => p.Y), quad.Max(p => p.X), quad.Max(p => p.Y));
        return new PdfImageObjectInfo(pageIndex, objectIndex, matrix, resolved, new PdfImageObjectMetadata(20, 10, 32, 0));
    }

    private static ImageObjectKey? Hit(IReadOnlyList<PdfImageObjectInfo> images, double x, double y)
    {
        var type = RequireType("SGPdf.App.Features.Edit.Images.ImageHitTester");
        var pointType = RequireType("SGPdf.App.Pdf.PdfPoint");
        var point = Activator.CreateInstance(pointType, x, y)!;
        var method = type.GetMethod("HitTest", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (ImageObjectKey?)method.Invoke(null, new object[] { images, point });
    }

    private static (double X, double Y) DeviceToPdf(double x, double y, PdfPageDeviceTransform transform)
    {
        var type = RequireType("SGPdf.App.Features.Edit.Images.ImageHitTester");
        var method = type.GetMethod("DeviceToPdf", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var point = method.Invoke(null, new object[] { x, y, transform })!;
        return (
            (double)point.GetType().GetProperty("X")!.GetValue(point)!,
            (double)point.GetType().GetProperty("Y")!.GetValue(point)!);
    }

    private static Type RequireType(string fullName)
        => typeof(MainWindow).Assembly.GetType(fullName, throwOnError: true)!;
}
