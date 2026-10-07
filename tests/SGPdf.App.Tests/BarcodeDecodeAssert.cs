using SkiaSharp;
using Xunit;
using ZXing;
using ZXing.Common;

namespace SGPdf.App.Tests;

internal static class BarcodeDecodeAssert
{
    public static Result DecodePng(byte[] pngBytes, BarcodeFormat expectedFormat)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);

        using var bitmap = SKBitmap.Decode(pngBytes);
        Assert.NotNull(bitmap);

        var rgb = new byte[checked(bitmap.Width * bitmap.Height * 3)];
        var offset = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                rgb[offset++] = color.Red;
                rgb[offset++] = color.Green;
                rgb[offset++] = color.Blue;
            }
        }

        return DecodeRgb24(rgb, bitmap.Width, bitmap.Height, expectedFormat);
    }

    public static Result DecodeBgra32(
        byte[] pixels,
        int width,
        int height,
        int stride,
        BarcodeFormat expectedFormat)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        Assert.True(width > 0, "Expected a positive bitmap width.");
        Assert.True(height > 0, "Expected a positive bitmap height.");
        Assert.True(stride >= checked(width * 4), "Expected BGRA stride to include every pixel in the row.");
        Assert.True(pixels.Length >= checked(stride * height), "Expected the BGRA buffer to include every row.");

        var rgb = new byte[checked(width * height * 3)];
        var output = 0;
        for (var y = 0; y < height; y++)
        {
            var row = checked(y * stride);
            for (var x = 0; x < width; x++)
            {
                var pixel = checked(row + x * 4);
                rgb[output++] = pixels[pixel + 2];
                rgb[output++] = pixels[pixel + 1];
                rgb[output++] = pixels[pixel];
            }
        }

        return DecodeRgb24(rgb, width, height, expectedFormat);
    }

    private static Result DecodeRgb24(
        byte[] rgb,
        int width,
        int height,
        BarcodeFormat expectedFormat)
    {
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = false,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = [expectedFormat]
            }
        };

        var result = reader.Decode(
            rgb,
            width,
            height,
            RGBLuminanceSource.BitmapFormat.RGB24);

        return Assert.IsType<Result>(result);
    }
}
