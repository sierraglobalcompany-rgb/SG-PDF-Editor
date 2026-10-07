using SkiaSharp;
using SGPdf.App.Features.Labels;
using Xunit;
using ZXing;

namespace SGPdf.App.Tests;

public sealed class LabelizeBarcodeRegressionTests
{
    [Fact]
    public async Task RenderAsync_Code128_RemainsDecodable()
    {
        var png = await RenderSingleAsync(
            "^XA^BY2,2,100^FO60,80^BCN,100,Y,N,N^FD123456789012^FS^XZ");

        var result = BarcodeDecodeAssert.DecodePng(png, BarcodeFormat.CODE_128);

        Assert.Equal(BarcodeFormat.CODE_128, result.BarcodeFormat);
        Assert.Equal("123456789012", result.Text);
    }

    [Fact]
    public async Task RenderAsync_Qr_RemainsDecodable()
    {
        var png = await RenderSingleAsync(
            "^XA^FO80,80^BQN,2,6^FDLA,SG-PDF-QR-12345^FS^XZ");

        var result = BarcodeDecodeAssert.DecodePng(png, BarcodeFormat.QR_CODE);

        Assert.Equal(BarcodeFormat.QR_CODE, result.BarcodeFormat);
        Assert.Equal("SG-PDF-QR-12345", result.Text);
    }

    [Fact]
    public async Task RenderAsync_FieldTypesetQr_RemainsDecodableAndOffsetFromOrigin()
    {
        var png = await RenderSingleAsync(
            "^XA^FT120,360^BQN,2,6^FDLA,SG-PDF-FT-QR^FS^XZ");

        var result = BarcodeDecodeAssert.DecodePng(png, BarcodeFormat.QR_CODE);
        var (minX, minY) = FindInkOrigin(png);

        Assert.Equal("SG-PDF-FT-QR", result.Text);
        Assert.True(minX > 20, $"Expected ^FT QR to be offset horizontally, minX={minX}.");
        Assert.True(minY > 20, $"Expected ^FT QR to be offset vertically, minY={minY}.");
    }

    private static async Task<byte[]> RenderSingleAsync(string source)
    {
        var document = ZplDocumentParser.Parse("barcode.zpl", source);
        var renderer = new LabelizeProcessRenderer();
        var labels = await renderer.RenderAsync(document, ZplRenderOptions.Default);
        return Assert.Single(labels).PngBytes;
    }

    private static (int MinX, int MinY) FindInkOrigin(byte[] pngBytes)
    {
        using var bitmap = SKBitmap.Decode(pngBytes);
        Assert.NotNull(bitmap);

        var minX = bitmap.Width;
        var minY = bitmap.Height;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                var luminance = (color.Red * 299 + color.Green * 587 + color.Blue * 114) / 1000;
                if (luminance >= 128)
                    continue;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
            }
        }

        Assert.True(minX < bitmap.Width && minY < bitmap.Height, "Expected rendered label to contain black ink.");
        return (minX, minY);
    }
}
