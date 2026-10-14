using SGPdf.App.Features.Labels;
using Xunit;
using ZXing;

namespace SGPdf.App.Tests;

public sealed class QrCodeGeneratorTests
{
    [Fact]
    public async Task GeneratePngAsync_RendersDecodableQrWithExactValue()
    {
        const string value = "https://example.com/sg-pdf?source=qr";
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
