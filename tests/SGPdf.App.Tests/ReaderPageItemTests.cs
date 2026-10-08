using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderPageItemTests
{
    [Fact]
    public void PageItem_DefaultsToPlaceholderWithoutBitmap()
    {
        var geometry = Geometry(0);
        var item = CreateItem(geometry);

        Assert.Equal(0, Get<int>(item, "PageIndex"));
        Assert.Equal(geometry, Get<ReaderPageGeometry>(item, "Geometry"));
        Assert.Equal("Placeholder", Get(item, "RenderState")?.ToString());
        Assert.Null(Get(item, "Bitmap"));
        Assert.Null(Get(item, "DeviceTransform"));
        Assert.Null(Get(item, "ErrorMessage"));
    }

    [Fact]
    public void Publish_SetsReadyBitmapAndTransform()
    {
        var geometry = Geometry(0);
        var item = CreateItem(geometry);
        var transform = new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 8, 8);
        var rendered = new PdfRenderedPage(0, 8, 8, 32, 96d, new byte[8 * 8 * 4])
        {
            DeviceTransform = transform
        };
        var bitmap = CreateBitmap(8, 8, 17);

        Invoke(item, "Publish", rendered, bitmap);

        Assert.Equal("Ready", Get(item, "RenderState")?.ToString());
        Assert.Same(bitmap, Get(item, "Bitmap"));
        Assert.Equal(transform, Get<PdfPageDeviceTransform?>(item, "DeviceTransform"));
        Assert.Null(Get(item, "ErrorMessage"));
    }

    [Fact]
    public void ReleaseBitmap_ClearsFullResolutionDataButPreservesGeometry()
    {
        var geometry = Geometry(2);
        var item = CreateItem(geometry);
        var rendered = new PdfRenderedPage(2, 8, 8, 32, 96d, new byte[8 * 8 * 4])
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 8, 8)
        };
        Invoke(item, "Publish", rendered, CreateBitmap(8, 8, 41));

        Invoke(item, "ReleaseBitmap");

        Assert.Equal(geometry, Get<ReaderPageGeometry>(item, "Geometry"));
        Assert.Equal("Placeholder", Get(item, "RenderState")?.ToString());
        Assert.Null(Get(item, "Bitmap"));
        Assert.Null(Get(item, "DeviceTransform"));
        Assert.Null(Get(item, "ErrorMessage"));
    }

    [Fact]
    public void MarkError_ClearsBitmapAndKeepsPageNavigable()
    {
        var geometry = Geometry(1);
        var item = CreateItem(geometry);
        var rendered = new PdfRenderedPage(1, 8, 8, 32, 96d, new byte[8 * 8 * 4])
        {
            DeviceTransform = new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 8, 8)
        };
        Invoke(item, "Publish", rendered, CreateBitmap(8, 8, 77));

        Invoke(item, "MarkError", "forced render failure");

        Assert.Equal(1, Get<int>(item, "PageIndex"));
        Assert.Equal(geometry, Get<ReaderPageGeometry>(item, "Geometry"));
        Assert.Equal("Error", Get(item, "RenderState")?.ToString());
        Assert.Null(Get(item, "Bitmap"));
        Assert.Null(Get(item, "DeviceTransform"));
        Assert.Equal("forced render failure", Get<string>(item, "ErrorMessage"));
    }

    private static ReaderPageGeometry Geometry(int pageIndex)
        => new(pageIndex, 300d, 400d, 400d, 533.333d, pageIndex * 550d, pageIndex * 550d + 533.333d, 96d);

    private static object CreateItem(ReaderPageGeometry geometry)
    {
        var type = typeof(MainWindow).Assembly.GetType("SGPdf.App.Features.Reader.ReaderPageItem");
        Assert.NotNull(type);
        var instance = Activator.CreateInstance(
            type!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [geometry],
            culture: null);
        Assert.NotNull(instance);
        return instance!;
    }

    private static BitmapSource CreateBitmap(int width, int height, byte marker)
    {
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, marker);
        var bitmap = BitmapSource.Create(width, height, 96d, 96d, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static object? Get(object instance, string property)
    {
        var info = instance.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(info);
        return info.GetValue(instance);
    }

    private static T Get<T>(object instance, string property)
        => (T)Get(instance, property)!;

    private static object? Invoke(object instance, string method, params object?[] args)
    {
        var candidate = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(item => item.Name == method && item.GetParameters().Length == args.Length);
        return candidate.Invoke(instance, args);
    }
}
