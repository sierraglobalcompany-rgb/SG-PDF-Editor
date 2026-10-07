using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;
using Xunit;

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

        Assert.InRange(Math.Abs(roundTrip.X - device.X), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Y - device.Y), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Width - device.Width), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Height - device.Height), 0d, 0.01d);
    }

    [Fact]
    public void PdfRectToDeviceRect_ScalesAcrossEquivalentRenderSizesWithoutMovingPdfPlacement()
    {
        var low = new PdfPageDeviceTransform(0d, 792d, 0.612d, 0d, 0d, -0.612d, 1000, 1294);
        var high = new PdfPageDeviceTransform(0d, 792d, 0.306d, 0d, 0d, -0.306d, 2000, 2588);
        var pdf = new PdfRect(72d, 72d, 144d, 72d);

        var lowDevice = SignatureCoordinateMapper.PdfRectToDeviceRect(pdf, low);
        var highDevice = SignatureCoordinateMapper.PdfRectToDeviceRect(pdf, high);

        Assert.InRange(Math.Abs(highDevice.X - lowDevice.X * 2d), 0d, 0.02d);
        Assert.InRange(Math.Abs(highDevice.Y - lowDevice.Y * 2d), 0d, 0.02d);
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

        Assert.InRange(Math.Abs(roundTrip.X - device.X), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Y - device.Y), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Width - device.Width), 0d, 0.01d);
        Assert.InRange(Math.Abs(roundTrip.Height - device.Height), 0d, 0.01d);
    }

    [Fact]
    public void GetVisiblePdfBounds_EnclosesAllMappedDeviceCorners()
    {
        var transform = new PdfPageDeviceTransform(20d, 700d, 0.5d, 0.15d, -0.1d, -0.65d, 800, 1000);

        var bounds = SignatureCoordinateMapper.GetVisiblePdfBounds(transform);

        var fullDevice = new SignatureDeviceRect(0d, 0d, 800d, 1000d);
        var mapped = SignatureCoordinateMapper.DeviceRectToPdfRect(fullDevice, transform);
        Assert.InRange(Math.Abs(bounds.Left - mapped.Left), 0d, 0.01d);
        Assert.InRange(Math.Abs(bounds.Bottom - mapped.Bottom), 0d, 0.01d);
        Assert.InRange(Math.Abs(bounds.Width - mapped.Width), 0d, 0.01d);
        Assert.InRange(Math.Abs(bounds.Height - mapped.Height), 0d, 0.01d);
    }
}
