using SGPdf.App.Features.Labels;
using SGPdf.App.Pdf;
using Xunit;
using ZXing;

namespace SGPdf.App.Tests;

public sealed class LabelPdfBarcodeIntegrationTests
{
    [Fact]
    public async Task ExportedThermalPdf_Code128AndQrDecodeAfterPdfiumRender()
    {
        var tempDirectory = CreateTempDirectory();
        var destination = Path.Combine(tempDirectory, "codes.pdf");

        try
        {
            var document = ZplDocumentParser.Parse(
                "codes.zpl",
                "^XA^BY3,2,120^FO80,120^BCN,120,Y,N,N^FDSGPDF-C128-2607^FS^XZ" +
                "^XA^FO120,120^BQN,2,8^FDLA,SGPDF-QR-2607^FS^XZ");
            var renderOptions = new ZplRenderOptions(100, 100, 8);
            var rendered = await new LabelizeProcessRenderer().RenderAsync(document, renderOptions);
            var sequence = new LabelOutputSequence(document, new ZplQuantitySelection(ZplQuantityMode.OneEach));
            var plan = LabelLayoutPlanner.CreatePlan(
                sequence,
                new LabelLayoutSettings(LabelMediaKind.Thermal),
                renderOptions.WidthMm,
                renderOptions.HeightMm);

            new LabelPdfExporter().Export(destination, plan, rendered);

            using var session = PdfDocumentSession.Open(destination);
            Assert.Equal(2, session.PageCount);

            var code128Page = session.RenderPage(0, 300d);
            var code128 = BarcodeDecodeAssert.DecodeBgra32(
                code128Page.Pixels,
                code128Page.PixelWidth,
                code128Page.PixelHeight,
                code128Page.Stride,
                BarcodeFormat.CODE_128);
            Assert.Equal(BarcodeFormat.CODE_128, code128.BarcodeFormat);
            Assert.Equal("SGPDF-C128-2607", code128.Text);

            var qrPage = session.RenderPage(1, 300d);
            var qr = BarcodeDecodeAssert.DecodeBgra32(
                qrPage.Pixels,
                qrPage.PixelWidth,
                qrPage.PixelHeight,
                qrPage.Stride,
                BarcodeFormat.QR_CODE);
            Assert.Equal(BarcodeFormat.QR_CODE, qr.BarcodeFormat);
            Assert.Equal("SGPDF-QR-2607", qr.Text);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportedThermalPdf_RotatedQrDecodesAfterPdfiumRender()
    {
        var tempDirectory = CreateTempDirectory();
        var destination = Path.Combine(tempDirectory, "rotated-qr.pdf");

        try
        {
            var document = ZplDocumentParser.Parse(
                "rotated.zpl",
                "^XA^FO120,120^BQN,2,8^FDLA,SGPDF-QR-ROT90^FS^XZ");
            var renderOptions = new ZplRenderOptions(100, 150, 8);
            var rendered = await new LabelizeProcessRenderer().RenderAsync(document, renderOptions);
            var sequence = new LabelOutputSequence(document, new ZplQuantitySelection(ZplQuantityMode.OneEach));
            var plan = LabelLayoutPlanner.CreatePlan(
                sequence,
                new LabelLayoutSettings(LabelMediaKind.Thermal, rotation: LabelRotation.Degrees90),
                renderOptions.WidthMm,
                renderOptions.HeightMm);

            new LabelPdfExporter().Export(destination, plan, rendered);

            using var session = PdfDocumentSession.Open(destination);
            var page = session.RenderPage(0, 300d);
            var result = BarcodeDecodeAssert.DecodeBgra32(
                page.Pixels,
                page.PixelWidth,
                page.PixelHeight,
                page.Stride,
                BarcodeFormat.QR_CODE);

            Assert.Equal(BarcodeFormat.QR_CODE, result.BarcodeFormat);
            Assert.Equal("SGPDF-QR-ROT90", result.Text);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-label-pdf-barcode-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
