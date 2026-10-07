using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

public sealed class SignatureCoordinateMapperTests
{
    [Fact]
    public void DeviceRectToPdfRect_RoundTripsAgainstArbitraryAffineTransform()
    {
        var transform = new PdfPageDeviceTransform(
            OriginX: 10d,
            OriginY: 500d,
            XAxisX: 0.5d,
            XAxisY: 0.1d,
            YAxisX: -0.05d,
            YAxisY: -0.75d,
            DeviceWidth: 1200,
            DeviceHeight: 1800);

        var device = new SignatureDeviceRect(120d, 240d, 360d, 180d);

        var pdf = SignatureCoordinateMapper.DeviceRectToPdfRect(device, transform);
        var roundTrip = SignatureCoordinateMapper.PdfRectToDeviceRect(pdf, transform);

        Assert.InRange(Math.Abs(roundTrip.Left - device.Left), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Top - device.Top), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Width - device.Width), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Height - device.Height), 0d, 0.01d);
    }

    [Fact]
    public void PdfRectToDeviceRect_ScalesAcrossDifferentRenderSizesWithoutMovingPdfPlacement()
    {
        var low = new PdfPageDeviceTransform(0d, 792d, 0.612d, 0d, 0d, -0.612d, 1000, 1294);
        var high = new PdfPageDeviceTransform(0d, 792d, 0.306d, 0d, 0d, -0.306d, 2000, 2588);
        var pdf = new SignaturePdfRect(72d, 72d, 144d, 72d);

        var lowDevice = SignatureCoordinateMapper.PdfRectToDeviceRect(pdf, low);
        var highDevice = SignatureCoordinateMapper.PdfRectToDeviceRect(pdf, high);

        Assert.InRange(Math.Abs(highDevice.Left - lowDevice.Left * 2d), 0d, 0.02d);
        Assert.InRange(Math.Abs(highDevice.Top - lowDevice.Top * 2d), 0d, 0.02d);
        Assert.InRange(Math.Abs(highDevice.Width - lowDevice.Width * 2d), 0d, 0.02d);
        Assert.InRange(Math.Abs(highDevice.Height - lowDevice.Height * 2d), 0d, 0.02d);
    }

    [Fact]
    public void DeviceRectToPdfRect_HandlesNonZeroPdfOrigin()
    {
        var transform = new PdfPageDeviceTransform(36d, 756d, 0.72d, 0d, 0d, -0.72d, 900, 1000);
        var device = new SignatureDeviceRect(100d, 120d, 240d, 80d);

        var pdf = SignatureCoordinateMapper.DeviceRectToPdfRect(device, transform);
        var roundTrip = SignatureCoordinateMapper.PdfRectToDeviceRect(pdf, transform);

        Assert.InRange(Math.Abs(roundTrip.Left - device.Left), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Top - device.Top), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Width - device.Width), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Height - device.Height), 0d, 0.01d);
    }
}
