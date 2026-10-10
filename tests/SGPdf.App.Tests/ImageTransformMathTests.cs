using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageTransformMathTests
{
    [Fact]
    public void Translate_PreservesLinearPart_AndMovesOrigin()
    {
        var source = new PdfObjectMatrix(100d, 20d, -10d, 45d, 30d, 40d);

        var moved = InvokeMatrix("Translate", source, 12.5d, -7d);

        Assert.Equal(source.A, moved.A);
        Assert.Equal(source.B, moved.B);
        Assert.Equal(source.C, moved.C);
        Assert.Equal(source.D, moved.D);
        Assert.Equal(42.5d, moved.E, 8);
        Assert.Equal(33d, moved.F, 8);
    }

    [Fact]
    public void RotateAroundCenter_RotatedNonSquare_KeepsCenterAndNonSingularGeometry()
    {
        var source = new PdfObjectMatrix(0d, 120d, -40d, 0d, 200d, 100d);
        var beforeCenter = PointAt(source, .5d, .5d);

        var rotated = InvokeMatrix("RotateAroundCenter", source, 37d);
        var afterCenter = PointAt(rotated, .5d, .5d);

        AssertPoint(beforeCenter, afterCenter);
        Assert.True(double.IsFinite(rotated.A) && double.IsFinite(rotated.B) &&
                    double.IsFinite(rotated.C) && double.IsFinite(rotated.D) &&
                    double.IsFinite(rotated.E) && double.IsFinite(rotated.F));
        Assert.True(Math.Abs(Determinant(rotated)) > 1e-6d);
        Assert.Equal(Math.Abs(Determinant(source)), Math.Abs(Determinant(rotated)), 6);
    }

    [Fact]
    public void ResizeFromCorner_AllFourCorners_KeepOppositeAnchorFixed()
    {
        var source = new PdfObjectMatrix(90d, 30d, -20d, 60d, 140d, 80d);
        var cases = new[]
        {
            (Name: "BottomLeft", U: 0d, V: 0d, AnchorU: 1d, AnchorV: 1d),
            (Name: "BottomRight", U: 1d, V: 0d, AnchorU: 0d, AnchorV: 1d),
            (Name: "TopRight", U: 1d, V: 1d, AnchorU: 0d, AnchorV: 0d),
            (Name: "TopLeft", U: 0d, V: 1d, AnchorU: 1d, AnchorV: 0d)
        };

        foreach (var item in cases)
        {
            var corner = ResizeCorner(item.Name);
            var anchor = PointAt(source, item.AnchorU, item.AnchorV);
            var current = PointAt(source, item.U, item.V);
            var target = new PdfPoint(
                anchor.X + ((current.X - anchor.X) * 1.5d),
                anchor.Y + ((current.Y - anchor.Y) * 1.5d));

            var resized = InvokeMatrix("ResizeFromCorner", source, corner, target, true, 5d);

            AssertPoint(anchor, PointAt(resized, item.AnchorU, item.AnchorV));
            AssertPoint(target, PointAt(resized, item.U, item.V));
            Assert.True(Math.Abs(Determinant(resized)) > 1e-6d);
        }
    }

    [Fact]
    public void ResizeFromCorner_DefaultPreservesAspect_ShiftStyleFreeResizeDoesNot()
    {
        var source = new PdfObjectMatrix(120d, 0d, 0d, 60d, 20d, 30d);
        var target = new PdfPoint(200d, 100d);
        var corner = ResizeCorner("TopRight");

        var proportional = InvokeMatrix("ResizeFromCorner", source, corner, target, true, 5d);
        var free = InvokeMatrix("ResizeFromCorner", source, corner, target, false, 5d);

        Assert.Equal(EdgeRatio(source), EdgeRatio(proportional), 6);
        Assert.NotEqual(Math.Round(EdgeRatio(source), 5), Math.Round(EdgeRatio(free), 5));
        AssertPoint(target, PointAt(free, 1d, 1d));
    }

    [Fact]
    public void ResizeFromCorner_RejectsMinimumDegenerateNonFiniteAndSingularInputs()
    {
        var source = new PdfObjectMatrix(100d, 0d, 0d, 50d, 0d, 0d);
        var corner = ResizeCorner("TopRight");

        AssertArgumentFailure(() => InvokeMatrix("ResizeFromCorner", source, corner, new PdfPoint(2d, 2d), false, 5d));
        AssertArgumentFailure(() => InvokeMatrix("ResizeFromCorner", source, corner, new PdfPoint(double.NaN, 20d), true, 5d));
        AssertArgumentFailure(() => InvokeMatrix(
            "ResizeFromCorner",
            new PdfObjectMatrix(100d, 0d, 50d, 0d, 0d, 0d),
            corner,
            new PdfPoint(120d, 60d),
            true,
            5d));
        AssertArgumentFailure(() => InvokeMatrix("Translate", source, double.PositiveInfinity, 0d));
    }

    private static PdfObjectMatrix InvokeMatrix(string name, params object[] args)
    {
        var type = RequireType("SGPdf.App.Features.Edit.Images.ImageTransformMath");
        var method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == args.Length);
        return (PdfObjectMatrix)method.Invoke(null, args)!;
    }

    private static object ResizeCorner(string name)
        => Enum.Parse(RequireType("SGPdf.App.Features.Edit.Images.ImageResizeCorner"), name);

    private static Type RequireType(string fullName)
        => typeof(MainWindow).Assembly.GetType(fullName, throwOnError: true)!;

    private static PdfPoint PointAt(PdfObjectMatrix matrix, double u, double v)
        => new(
            (matrix.A * u) + (matrix.C * v) + matrix.E,
            (matrix.B * u) + (matrix.D * v) + matrix.F);

    private static double Determinant(PdfObjectMatrix matrix)
        => (matrix.A * matrix.D) - (matrix.B * matrix.C);

    private static double EdgeRatio(PdfObjectMatrix matrix)
        => Math.Sqrt((matrix.A * matrix.A) + (matrix.B * matrix.B)) /
           Math.Sqrt((matrix.C * matrix.C) + (matrix.D * matrix.D));

    private static void AssertPoint(PdfPoint expected, PdfPoint actual)
    {
        Assert.Equal(expected.X, actual.X, 6);
        Assert.Equal(expected.Y, actual.Y, 6);
    }

    private static void AssertArgumentFailure(Action action)
    {
        var error = Assert.Throws<TargetInvocationException>(action);
        Assert.IsAssignableFrom<ArgumentException>(error.InnerException);
    }
}
