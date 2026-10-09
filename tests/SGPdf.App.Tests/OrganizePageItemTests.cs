using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizePageItemTests
{
    [Fact]
    public void NewItem_HasNoBitmap_AndPreservesLogicalIdentityAcrossPublishRelease()
    {
        var page = new OrganizePage(Guid.NewGuid(), Guid.NewGuid(), 4, 0);
        var item = new OrganizePageItem(page, 5, new PdfPageSize(200d, 300d));

        Assert.Equal(page.ItemId, item.ItemId);
        Assert.Equal(page.SourceId, item.SourceId);
        Assert.Equal(page.SourcePageIndex, item.SourcePageIndex);
        Assert.Equal(5, item.PageNumber);
        Assert.Null(item.Bitmap);

        var bitmap = Bitmap(11);
        item.Publish(bitmap);
        Assert.Same(bitmap, item.Bitmap);

        item.ReleaseBitmap();
        Assert.Null(item.Bitmap);
        Assert.Equal(page.ItemId, item.ItemId);
        Assert.Equal(page.SourceId, item.SourceId);
        Assert.Equal(page.SourcePageIndex, item.SourcePageIndex);
    }

    [Fact]
    public void UpdatePlanState_RenumbersWithoutDroppingBitmap_WhenSourceAndRotationAreUnchanged()
    {
        var page = new OrganizePage(Guid.NewGuid(), Guid.NewGuid(), 0, 0);
        var item = new OrganizePageItem(page, 1, new PdfPageSize(200d, 300d));
        var bitmap = Bitmap(22);
        item.Publish(bitmap);

        item.UpdatePlanState(page, 7);

        Assert.Equal(7, item.PageNumber);
        Assert.Equal(0, item.RotationDeltaQuarterTurns);
        Assert.Same(bitmap, item.Bitmap);
    }

    [Fact]
    public void UpdatePlanState_PlannedRotationChangesDisplayState_AndInvalidatesBitmap()
    {
        var page = new OrganizePage(Guid.NewGuid(), Guid.NewGuid(), 0, 0);
        var item = new OrganizePageItem(page, 1, new PdfPageSize(200d, 300d));
        item.Publish(Bitmap(33));

        var rotated = new OrganizePage(page.ItemId, page.SourceId, page.SourcePageIndex, 1);
        item.UpdatePlanState(rotated, 1);

        Assert.Equal(1, item.RotationDeltaQuarterTurns);
        Assert.Equal(132d, item.DisplayWidth, 6);
        Assert.Equal(88d, item.DisplayHeight, 6);
        Assert.Null(item.Bitmap);
    }

    private static BitmapSource Bitmap(byte marker)
    {
        var pixels = Enumerable.Repeat(marker, 4 * 4 * 4).ToArray();
        var bitmap = BitmapSource.Create(4, 4, 96d, 96d, PixelFormats.Bgra32, null, pixels, 16);
        bitmap.Freeze();
        return bitmap;
    }
}
