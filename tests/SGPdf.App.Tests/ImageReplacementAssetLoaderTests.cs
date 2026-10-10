using System.Reflection;
using System.Runtime.ExceptionServices;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkiaSharp;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageReplacementAssetLoaderTests
{
    [Fact]
    public void Load_PngCapturesEncodedBytesBgraDimensionsAndActualAlpha()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, "alpha.png");
        Task5ImageTestFixture.WritePng(path, transparent: true);
        var expectedBytes = File.ReadAllBytes(path);

        var asset = Load(path);

        Assert.Equal("Png", Property(asset, "Format")!.ToString());
        Assert.Equal(expectedBytes, Property<ReadOnlyMemory<byte>>(asset, "EncodedBytes").ToArray());
        Assert.Equal(4, Property<int>(asset, "PixelWidth"));
        Assert.Equal(2, Property<int>(asset, "PixelHeight"));
        Assert.Equal(16, Property<int>(asset, "Stride"));
        Assert.Equal(32, Property<ReadOnlyMemory<byte>>(asset, "BgraPixels").Length);
        Assert.True(Property<bool>(asset, "HasAlpha"));
    }

    [Fact]
    public void Load_OpaquePngWithAlphaCapableContainer_ReportsNoActualTransparency()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, "opaque.png");
        Task5ImageTestFixture.WritePng(path, transparent: false);

        var asset = Load(path);

        Assert.Equal("Png", Property(asset, "Format")!.ToString());
        Assert.False(Property<bool>(asset, "HasAlpha"));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    public void Load_JpgAndJpeg_AreAcceptedAsJpegWithoutAlpha(string fileName)
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, fileName);
        Task5ImageTestFixture.WriteJpeg(path);

        var asset = Load(path);

        Assert.Equal("Jpeg", Property(asset, "Format")!.ToString());
        Assert.False(Property<bool>(asset, "HasAlpha"));
        Assert.Equal(4, Property<int>(asset, "PixelWidth"));
        Assert.Equal(2, Property<int>(asset, "PixelHeight"));
        Assert.NotEmpty(Property<ReadOnlyMemory<byte>>(asset, "EncodedBytes").ToArray());
        Assert.NotEmpty(Property<ReadOnlyMemory<byte>>(asset, "BgraPixels").ToArray());
    }

    [Fact]
    public void Load_InvalidBytesAndUnsupportedExtension_AreRejectedCandidateFirst()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var invalidPng = Path.Combine(directory.Path, "invalid.png");
        var unsupported = Path.Combine(directory.Path, "unsupported.gif");
        File.WriteAllBytes(invalidPng, new byte[] { 1, 2, 3, 4, 5 });
        File.WriteAllBytes(unsupported, new byte[] { 0x47, 0x49, 0x46, 0x38 });

        Assert.Throws<InvalidDataException>(() => Load(invalidPng));
        Assert.Throws<NotSupportedException>(() => Load(unsupported));
    }

    [Fact]
    public void ReplacementAsset_SourceFileDeletedAfterSelection_SaveStillUsesCapturedBytes()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, "captured.png");
        Task5ImageTestFixture.WritePng(path, transparent: true);

        var asset = Load(path);
        var encoded = Property<ReadOnlyMemory<byte>>(asset, "EncodedBytes").ToArray();
        var pixels = Property<ReadOnlyMemory<byte>>(asset, "BgraPixels").ToArray();
        File.Delete(path);

        Assert.False(File.Exists(path));
        Assert.Equal(encoded, Property<ReadOnlyMemory<byte>>(asset, "EncodedBytes").ToArray());
        Assert.Equal(pixels, Property<ReadOnlyMemory<byte>>(asset, "BgraPixels").ToArray());
        Assert.True(Property<bool>(asset, "HasAlpha"));
    }

    private static object Load(string path)
    {
        var type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Edit.Images.ImageReplacementAssetLoader",
            throwOnError: true)!;
        var method = type.GetMethod("Load", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        try
        {
            return method.Invoke(null, new object?[] { path })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static object? Property(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return property.GetValue(instance);
    }

    private static T Property<T>(object instance, string name)
        => Assert.IsType<T>(Property(instance, name));
}

internal static class Task5ImageTestFixture
{
    internal static TempDirectory CreateDirectory() => new();

    internal static void WritePng(string path, bool transparent)
    {
        using var bitmap = new SKBitmap(4, 2, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var alpha = transparent && x < 2 ? (byte)64 : (byte)255;
                bitmap.SetPixel(x, y, new SKColor(20, 120, 220, alpha));
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    internal static void WriteJpeg(string path)
    {
        using var bitmap = new SKBitmap(4, 2, SKColorType.Bgra8888, SKAlphaType.Opaque);
        bitmap.Erase(new SKColor(210, 80, 30, 255));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    internal static ImageEditPdfFixture CreateTransparentPdf()
    {
        var directory = new TempDirectory();
        try
        {
            var imagePath = Path.Combine(directory.Path, "alpha.png");
            WritePng(imagePath, transparent: true);
            var pdfPath = Path.Combine(directory.Path, "alpha.pdf");
            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                page.Width = XUnit.FromPoint(300d);
                page.Height = XUnit.FromPoint(400d);
                using var graphics = XGraphics.FromPdfPage(page);
                using var image = XImage.FromFile(imagePath);
                graphics.DrawImage(image, 60d, 80d, 160d, 80d);
                document.Save(pdfPath);
            }

            var fixture = new ImageEditPdfFixture(directory.Path, pdfPath);
            directory.Detach();
            return fixture;
        }
        finally
        {
            directory.Dispose();
        }
    }

    internal sealed class TempDirectory : IDisposable
    {
        private bool _detached;

        internal TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-f6-task5-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        internal void Detach() => _detached = true;

        public void Dispose()
        {
            if (!_detached && Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
