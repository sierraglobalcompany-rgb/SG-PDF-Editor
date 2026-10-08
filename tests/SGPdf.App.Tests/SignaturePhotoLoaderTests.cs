using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignaturePhotoLoaderTests
{
    [Fact]
    public void Load_Jpeg_NormalizesToBgraAndPreservesDimensions()
    {
        var path = CreateJpeg(2, 2, new byte[]
        {
            10, 20, 30, 255,  40, 50, 60, 255,
            70, 80, 90, 255,  100, 110, 120, 255
        });

        try
        {
            var source = SignaturePhotoLoader.Load(path);
            Assert.Equal(2, source.PixelWidth);
            Assert.Equal(2, source.PixelHeight);
            Assert.Equal(8, source.Stride);
            Assert.Equal(16, source.BgraPixels.Length);
            Assert.All(Enumerable.Range(0, 4), i => Assert.Equal(255, source.BgraPixels.Span[i * 4 + 3]));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_OpaquePng_IsAcceptedForPhotoPreparation()
    {
        var path = CreatePng(1, 1, new byte[] { 255, 255, 255, 255 });
        try
        {
            var source = SignaturePhotoLoader.Load(path);
            Assert.Equal(255, source.BgraPixels.Span[3]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_TransparentPng_PreservesSourceAlpha()
    {
        var path = CreatePng(2, 1, new byte[]
        {
            1, 2, 3, 0,
            4, 5, 6, 128
        });

        try
        {
            var source = SignaturePhotoLoader.Load(path);
            Assert.Equal(0, source.BgraPixels.Span[3]);
            Assert.Equal(128, source.BgraPixels.Span[7]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UnsupportedExtension_IsRejectedBeforeDecode()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-photo-{Guid.NewGuid():N}.bmp");
        File.WriteAllBytes(path, "not an image"u8.ToArray());
        try
        {
            var error = Assert.Throws<InvalidDataException>(() => SignaturePhotoLoader.Load(path));
            Assert.Contains("PNG", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("JPG", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_CorruptAcceptedExtension_IsRejectedWithoutPartialAsset()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-photo-{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(path, "not a jpeg"u8.ToArray());
        try
        {
            Assert.ThrowsAny<Exception>(() => SignaturePhotoLoader.Load(path));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.CanWrite);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidatePixelCount_AllowsExactlyTwentyMillion_AndRejectsTwentyMillionAndOne()
    {
        SignatureImageLimits.ValidatePixelCount(5000, 4000);
        var error = Assert.Throws<InvalidDataException>(() => SignatureImageLimits.ValidatePixelCount(5000, 4001));
        Assert.Contains("20", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PhotoSource_CopiesInputBufferAndValidatesGeometry()
    {
        var pixels = new byte[] { 1, 2, 3, 4, 99, 99, 99, 99 };
        var source = new SignaturePhotoSource(1, 1, 4, pixels, "photo.jpg");
        pixels[0] = 200;

        Assert.Equal(1, source.BgraPixels.Span[0]);
        Assert.Equal(4, source.BgraPixels.Length);
        Assert.Equal("photo.jpg", source.SourceName);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignaturePhotoSource(0, 1, 4, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignaturePhotoSource(1, 1, 3, new byte[4]));
        Assert.Throws<ArgumentException>(() => new SignaturePhotoSource(2, 1, 8, new byte[7]));
    }

    [Fact]
    public void ExistingDirectPngLoader_StillRejectsOpaquePngAndUsesSharedPixelLimit()
    {
        Assert.Equal(20_000_000, SignatureImageLimits.MaxDecodedPixels);
        var path = CreatePng(1, 1, new byte[] { 255, 255, 255, 255 });
        try
        {
            Assert.Throws<InvalidDataException>(() => SignaturePngLoader.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreatePng(int width, int height, byte[] bgra)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-photo-{Guid.NewGuid():N}.png");
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgra, width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
        return path;
    }

    private static string CreateJpeg(int width, int height, byte[] bgra)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-photo-{Guid.NewGuid():N}.jpg");
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgra, width * 4);
        var encoder = new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
        return path;
    }
}
