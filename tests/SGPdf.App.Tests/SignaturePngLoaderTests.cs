using System.Buffers.Binary;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignaturePngLoaderTests
{
    [Fact]
    public void Load_TransparentPngNormalizesToBgraAndPreservesAlpha()
    {
        var path = CreatePng(
            2,
            1,
            new byte[]
            {
                10, 20, 30, 0,
                40, 50, 60, 128
            });

        try
        {
            var asset = SignaturePngLoader.Load(path);

            Assert.Equal(2, asset.PixelWidth);
            Assert.Equal(1, asset.PixelHeight);
            Assert.Equal(8, asset.Stride);
            Assert.Equal(0, asset.BgraPixels.Span[3]);
            Assert.Equal(128, asset.BgraPixels.Span[7]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MixedOpaqueAndTransparentPixels_IsAcceptedAndDoesNotLockSource()
    {
        var path = CreatePng(
            2,
            1,
            new byte[]
            {
                0, 0, 0, 255,
                0, 0, 0, 64
            });

        try
        {
            _ = SignaturePngLoader.Load(path);
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.CanWrite);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_OpaquePng_IsRejectedInsteadOfSilentlyRemovingBackground()
    {
        var path = CreatePng(1, 1, new byte[] { 255, 255, 255, 255 });

        try
        {
            var error = Assert.Throws<InvalidDataException>(() => SignaturePngLoader.Load(path));
            Assert.Contains("transpar", error.Message, StringComparison.OrdinalIgnoreCase);

            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.CanWrite);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_CorruptOrNonPngData_IsRejectedAndFileIsReleased()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-corrupt-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, "not a png"u8.ToArray());

        try
        {
            Assert.ThrowsAny<Exception>(() => SignaturePngLoader.Load(path));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.CanWrite);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_ZeroDimensionHeader_IsRejectedBeforeDecode()
    {
        var path = CreatePngHeaderOnly(0, 10);
        try
        {
            Assert.Throws<InvalidDataException>(() => SignaturePngLoader.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_AboveTwentyMillionPixels_IsRejectedFromHeaderBeforeBgraAllocation()
    {
        var path = CreatePngHeaderOnly(5000, 4001);
        try
        {
            var error = Assert.Throws<InvalidDataException>(() => SignaturePngLoader.Load(path));
            Assert.Contains("20", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreatePng(int width, int height, byte[] bgra)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-{Guid.NewGuid():N}.png");
        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            bgra,
            width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
        return path;
    }

    private static string CreatePngHeaderOnly(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-header-{Guid.NewGuid():N}.png");
        var bytes = new byte[24];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 13u);
        "IHDR"u8.CopyTo(bytes.AsSpan(12, 4));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), unchecked((uint)width));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), unchecked((uint)height));
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
