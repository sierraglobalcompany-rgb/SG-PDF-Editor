using SGPdf.App.Features.Labels;
using Xunit;
using ZXing;

namespace SGPdf.App.Tests;

public sealed class QrCodeGeneratorTests
{
    [Theory]
    [InlineData("https://example.com/sg-pdf?source=qr")]
    [InlineData("Medellín ^XZ ~JA café")]
    public async Task GeneratePngAsync_RendersDecodableQrWithExactValue(string value)
    {
        var generator = new QrCodeGenerator();

        var pngBytes = await generator.GeneratePngAsync(value);

        var decoded = BarcodeDecodeAssert.DecodePng(pngBytes, BarcodeFormat.QR_CODE);
        Assert.Equal(value, decoded.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GeneratePngAsync_RejectsEmptyValue(string value)
    {
        var generator = new QrCodeGenerator();

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GeneratePngAsync(value));
    }
}
