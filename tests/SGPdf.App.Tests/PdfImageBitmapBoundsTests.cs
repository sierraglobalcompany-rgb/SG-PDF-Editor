using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfImageBitmapBoundsTests
{
    [Fact]
    public void BitmapCopyBounds_HugeManagedAllocation_IsRejectedBeforeCopy()
    {
        var method = GetBoundsMethod();

        var exception = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, new object?[]
            {
                100_000,
                100_000,
                400_000,
                PdfiumNative.FPDFBitmap_BGRA
            }));

        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    [Fact]
    public void BitmapCopyBounds_UndersizedStride_IsRejectedBeforeRowAccess()
    {
        var method = GetBoundsMethod();

        var exception = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, new object?[]
            {
                2,
                2,
                3,
                PdfiumNative.FPDFBitmap_BGRA
            }));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void BitmapCopyBounds_RepresentativeBgra_IsAccepted()
    {
        var method = GetBoundsMethod();
        var result = method.Invoke(null, new object?[]
        {
            2,
            2,
            8,
            PdfiumNative.FPDFBitmap_BGRA
        });

        Assert.NotNull(result);
    }

    private static MethodInfo GetBoundsMethod()
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "ValidateImageBitmapCopyBounds",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method;
    }
}
