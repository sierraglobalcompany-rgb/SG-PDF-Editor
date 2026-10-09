using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderThumbnailTests
{
    [Fact]
    public void ThumbnailDpi_Targets132PixelDisplayWidth()
    {
        var size = new PdfPageSize(612d, 792d);

        var dpi = ReaderThumbnailItem.ResolveThumbnailDpi(size);

        var renderedWidth = size.WidthPoints * dpi / 72d;
        Assert.Equal(132d, renderedWidth, precision: 6);
    }

    [Fact]
    public void ThumbnailItem_ReleaseDropsBitmap()
    {
        var size = new PdfPageSize(300d, 400d);
        var item = new ReaderThumbnailItem(2, size);
        var bitmap = CreateBitmap(12, 16, 77);

        item.Publish(bitmap);
        Assert.Same(bitmap, item.Bitmap);
        Assert.False(item.IsLoading);
        Assert.False(item.HasError);

        item.ReleaseBitmap();

        Assert.Equal(2, item.PageIndex);
        Assert.Equal(size, item.PageSize);
        Assert.Null(item.Bitmap);
        Assert.False(item.IsLoading);
        Assert.False(item.HasError);
    }

    private static BitmapSource CreateBitmap(int width, int height, byte marker)
    {
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, marker);
        var bitmap = BitmapSource.Create(width, height, 96d, 96d, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }
}
