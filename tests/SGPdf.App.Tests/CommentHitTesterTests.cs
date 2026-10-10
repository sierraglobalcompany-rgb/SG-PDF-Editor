using System.Reflection;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class CommentHitTesterTests
{
    [Fact]
    public void HitTest_ReturnsTopmostAnnotationDeterministically()
    {
        var comments = new[]
        {
            CreateShape(1, CommentSubtype.Square, new CommentRect(10, 10, 80, 80)),
            CreateShape(7, CommentSubtype.Circle, new CommentRect(20, 20, 70, 70))
        };

        var hit = CommentHitTesterContractApi.HitTest(comments, new CommentPoint(40, 40), 0d);

        Assert.NotNull(hit);
        Assert.Equal(7, hit!.AnnotationIndex);
    }

    [Fact]
    public void HitTest_TextMarkupUsesQuadGeometryInsteadOfCoarseRect()
    {
        var rect = new CommentRect(10, 10, 200, 100);
        var quad = new CommentQuad(
            new CommentPoint(20, 40),
            new CommentPoint(80, 40),
            new CommentPoint(20, 20),
            new CommentPoint(80, 20));
        var highlight = new PdfCommentInfo(
            0, 3, PdfiumNative.FPDF_ANNOT_HIGHLIGHT, CommentSubtype.Highlight, true,
            rect, new CommentColor(255, 220, 0, 160), new[] { quad }, string.Empty,
            Array.Empty<CommentStroke>(), null);

        Assert.NotNull(CommentHitTesterContractApi.HitTest(new[] { highlight }, new CommentPoint(50, 30), 0d));
        Assert.Null(CommentHitTesterContractApi.HitTest(new[] { highlight }, new CommentPoint(150, 70), 0d));
    }

    [Fact]
    public void HitTest_InkUsesStrokeDistanceWithTolerance()
    {
        var stroke = new CommentStroke(new[]
        {
            new CommentPoint(10, 10),
            new CommentPoint(50, 50),
            new CommentPoint(90, 10)
        });
        var ink = new PdfCommentInfo(
            0, 4, PdfiumNative.FPDF_ANNOT_INK, CommentSubtype.Ink, true,
            new CommentRect(8, 8, 92, 52), new CommentColor(0, 0, 0, 255),
            Array.Empty<CommentQuad>(), string.Empty, new[] { stroke }, 2d);

        Assert.NotNull(CommentHitTesterContractApi.HitTest(new[] { ink }, new CommentPoint(52, 47), 4d));
        Assert.Null(CommentHitTesterContractApi.HitTest(new[] { ink }, new CommentPoint(52, 30), 2d));
    }

    [Fact]
    public void HitTestDevice_UsesPdfDeviceTransformSoRotatedPagesNeedNoSpecialCaseGeometry()
    {
        var comment = CreateShape(5, CommentSubtype.Square, new CommentRect(490, 390, 510, 410));
        var rotatedTransform = new PdfPageDeviceTransform(
            OriginX: 600,
            OriginY: 0,
            XAxisX: 0,
            XAxisY: 1,
            YAxisX: -1,
            YAxisY: 0,
            DeviceWidth: 600,
            DeviceHeight: 800);

        var hit = CommentHitTesterContractApi.HitTestDevice(
            new[] { comment },
            deviceX: 400,
            deviceY: 100,
            rotatedTransform,
            deviceTolerance: 0d);

        Assert.NotNull(hit);
        Assert.Equal(5, hit!.AnnotationIndex);
    }

    [Fact]
    public void HitTest_RejectsInvalidToleranceAndSkipsAnnotationsWithoutUsableGeometry()
    {
        var invalid = new PdfCommentInfo(
            0, 9, 999, null, false, null, null,
            Array.Empty<CommentQuad>(), string.Empty, Array.Empty<CommentStroke>(), null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommentHitTesterContractApi.HitTest(new[] { invalid }, new CommentPoint(1, 1), -0.1d));
        Assert.Null(CommentHitTesterContractApi.HitTest(new[] { invalid }, new CommentPoint(1, 1), 0d));
    }

    private static PdfCommentInfo CreateShape(int annotationIndex, CommentSubtype subtype, CommentRect rect)
    {
        var nativeSubtype = subtype == CommentSubtype.Circle
            ? PdfiumNative.FPDF_ANNOT_CIRCLE
            : PdfiumNative.FPDF_ANNOT_SQUARE;
        return new PdfCommentInfo(
            0, annotationIndex, nativeSubtype, subtype, true,
            rect, new CommentColor(20, 80, 210, 255), Array.Empty<CommentQuad>(), string.Empty,
            Array.Empty<CommentStroke>(), 2d);
    }
}

internal static class CommentHitTesterContractApi
{
    private const string TypeName = "SGPdf.App.Features.Comments.CommentHitTester";

    internal static PdfCommentInfo? HitTest(
        IReadOnlyList<PdfCommentInfo> comments,
        CommentPoint point,
        double tolerance)
    {
        var method = RequiredType().GetMethod(
            "HitTest",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(IReadOnlyList<PdfCommentInfo>), typeof(CommentPoint), typeof(double) },
            modifiers: null) ?? throw new MissingMethodException(TypeName, "HitTest");
        try
        {
            return (PdfCommentInfo?)method.Invoke(null, new object[] { comments, point, tolerance });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    internal static PdfCommentInfo? HitTestDevice(
        IReadOnlyList<PdfCommentInfo> comments,
        double deviceX,
        double deviceY,
        PdfPageDeviceTransform transform,
        double deviceTolerance)
    {
        var method = RequiredType().GetMethod(
            "HitTestDevice",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(IReadOnlyList<PdfCommentInfo>), typeof(double), typeof(double),
                typeof(PdfPageDeviceTransform), typeof(double)
            },
            modifiers: null) ?? throw new MissingMethodException(TypeName, "HitTestDevice");
        try
        {
            return (PdfCommentInfo?)method.Invoke(
                null,
                new object[] { comments, deviceX, deviceY, transform, deviceTolerance });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static Type RequiredType() =>
        typeof(CommentWorkspace).Assembly.GetType(TypeName, throwOnError: true)!;
}
