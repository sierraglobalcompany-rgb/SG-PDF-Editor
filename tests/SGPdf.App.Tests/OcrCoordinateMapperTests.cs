using System.Reflection;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrCoordinateMapperTests
{
    public static TheoryData<double, double, double, double, double, double, double, double, double, double> RotationEquivalentTransforms => new()
    {
        // originX, originY, xAxisX, xAxisY, yAxisX, yAxisY, expectedLeft, expectedBottom, expectedRight, expectedTop
        { 0d,   100d,  1d,  0d,  0d, -1d, 10d, 40d, 40d, 80d }, // portrait / 0°
        { 0d,     0d,  0d,  1d,  1d,  0d, 20d, 10d, 60d, 40d }, // 90° equivalent
        { 100d,   0d, -1d,  0d,  0d,  1d, 60d, 20d, 90d, 60d }, // 180° equivalent
        { 100d, 100d,  0d, -1d, -1d,  0d, 40d, 60d, 80d, 90d }  // 270° equivalent
    };

    [Theory]
    [MemberData(nameof(RotationEquivalentTransforms))]
    public void Map_UsesAllFourCornersForRotationEquivalentTransforms(
        double originX,
        double originY,
        double xAxisX,
        double xAxisY,
        double yAxisX,
        double yAxisY,
        double expectedLeft,
        double expectedBottom,
        double expectedRight,
        double expectedTop)
    {
        var rendered = CreateRenderedPage(
            originX, originY, xAxisX, xAxisY, yAxisX, yAxisY,
            deviceWidth: 100, deviceHeight: 100);

        var mapped = InvokeMap(rendered, left: 10, top: 20, width: 30, height: 40);

        AssertClose(expectedLeft, Get<double>(mapped, "Left"));
        AssertClose(expectedBottom, Get<double>(mapped, "Bottom"));
        AssertClose(expectedRight, Get<double>(mapped, "Right"));
        AssertClose(expectedTop, Get<double>(mapped, "Top"));
    }

    [Fact]
    public void Map_HandlesLandscapeScaleWithoutOrientationGuesses()
    {
        var rendered = CreateRenderedPage(
            originX: 5d,
            originY: 205d,
            xAxisX: 0.5d,
            xAxisY: 0d,
            yAxisX: 0d,
            yAxisY: -2d,
            deviceWidth: 200,
            deviceHeight: 100);

        var mapped = InvokeMap(rendered, left: 20, top: 10, width: 60, height: 20);

        AssertClose(15d, Get<double>(mapped, "Left"));
        AssertClose(145d, Get<double>(mapped, "Bottom"));
        AssertClose(45d, Get<double>(mapped, "Right"));
        AssertClose(185d, Get<double>(mapped, "Top"));
    }

    [Theory]
    [InlineData(90, 10, 11, 10)]
    [InlineData(10, 90, 10, 11)]
    [InlineData(101, 0, 1, 1)]
    [InlineData(0, 101, 1, 1)]
    public void Map_RejectsRectanglesOutsideRenderedDevice(int left, int top, int width, int height)
    {
        var rendered = CreateRenderedPage(
            0d, 100d, 1d, 0d, 0d, -1d,
            deviceWidth: 100, deviceHeight: 100);

        var error = Assert.Throws<TargetInvocationException>(() =>
            InvokeMap(rendered, left, top, width, height));

        Assert.IsType<ArgumentOutOfRangeException>(error.InnerException);
    }

    [Fact]
    public void Map_RejectsRenderedPageWithoutDeviceTransform()
    {
        var rendered = new PdfRenderedPage(0, 100, 100, 400, 300d, new byte[400 * 100]);

        var error = Assert.Throws<TargetInvocationException>(() =>
            InvokeMap(rendered, left: 10, top: 20, width: 30, height: 40));

        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    private static object InvokeMap(PdfRenderedPage rendered, int left, int top, int width, int height)
    {
        var assembly = typeof(OcrRequest).Assembly;
        var mapperType = assembly.GetType("SGPdf.App.Features.Ocr.OcrCoordinateMapper");
        Assert.NotNull(mapperType);

        var pixelRectType = assembly.GetType("SGPdf.App.Features.Ocr.OcrPixelRect");
        Assert.NotNull(pixelRectType);
        var bounds = Activator.CreateInstance(pixelRectType!, left, top, width, height);
        Assert.NotNull(bounds);

        var method = mapperType!.GetMethod(
            "Map",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(PdfRenderedPage), pixelRectType! },
            modifiers: null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new[] { rendered, bounds });
        Assert.NotNull(result);
        return result!;
    }

    private static PdfRenderedPage CreateRenderedPage(
        double originX,
        double originY,
        double xAxisX,
        double xAxisY,
        double yAxisX,
        double yAxisY,
        int deviceWidth,
        int deviceHeight)
    {
        var rendered = new PdfRenderedPage(
            0,
            deviceWidth,
            deviceHeight,
            checked(deviceWidth * 4),
            300d,
            new byte[checked(deviceWidth * deviceHeight * 4)]);

        var transformProperty = typeof(PdfRenderedPage).GetProperty(
            "DeviceTransform",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(transformProperty);

        var transformType = transformProperty!.PropertyType.GetGenericArguments().Single();
        var transform = Activator.CreateInstance(
            transformType,
            originX,
            originY,
            xAxisX,
            xAxisY,
            yAxisX,
            yAxisY,
            deviceWidth,
            deviceHeight);
        Assert.NotNull(transform);

        transformProperty.SetValue(rendered, transform);
        return rendered;
    }

    private static T Get<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        var value = property!.GetValue(target);
        Assert.NotNull(value);
        return Assert.IsType<T>(value);
    }

    private static void AssertClose(double expected, double actual)
    {
        Assert.InRange(Math.Abs(expected - actual), 0d, 1e-6d);
    }
}
