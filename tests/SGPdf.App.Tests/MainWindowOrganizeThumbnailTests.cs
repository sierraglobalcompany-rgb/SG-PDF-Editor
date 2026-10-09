using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Media.Imaging;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOrganizeThumbnailTests
{
    [Fact]
    public void LargeDocument_OnlyRealizedPlusNeighborThumbnailsHoldBitmaps()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(100);
            var window = new MainWindow();
            var calls = new ConcurrentQueue<int>();
            try
            {
                OrganizeWindowTestHost.SetField(window, "_getOrganizeRealizedRange",
                    (Func<(int First, int Last)?>)(() => (50, 52)));
                OrganizeWindowTestHost.SetField(window, "_renderOrganizeThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) =>
                    {
                        calls.Enqueue(index);
                        return OrganizeWindowTestHost.SyntheticRendered(index, dpi, (byte)(index + 1));
                    }));

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                await OrganizeWindowTestHost.InvokeTask(window, "RefreshOrganizeThumbnailRenderWindowAsync");

                var items = OrganizeWindowTestHost.GetOrganizeItems(window);
                Assert.Equal(100, items.Count);
                var holding = items
                    .Select((item, index) => (item, index))
                    .Where(entry => OrganizeWindowTestHost.GetProperty<BitmapSource?>(entry.item, "Bitmap") is not null)
                    .Select(entry => entry.index)
                    .ToArray();

                Assert.Equal(new[] { 49, 50, 51, 52, 53 }, holding);
                Assert.All(calls, index => Assert.InRange(index, 49, 53));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void RapidScroll_RejectsStaleThumbnailPublication()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(8);
            var window = new MainWindow();
            var firstStarted = new ManualResetEventSlim(false);
            var releaseFirst = new ManualResetEventSlim(false);
            var range = (First: 0, Last: 0);
            var call = 0;
            try
            {
                OrganizeWindowTestHost.SetField(window, "_getOrganizeRealizedRange",
                    (Func<(int First, int Last)?>)(() => range));
                OrganizeWindowTestHost.SetField(window, "_renderOrganizeThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) =>
                        OrganizeWindowTestHost.SyntheticRendered(index, dpi, 9)));

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                await OrganizeWindowTestHost.InvokeTask(window, "RefreshOrganizeThumbnailRenderWindowAsync");
                foreach (var item in OrganizeWindowTestHost.GetOrganizeItems(window))
                    OrganizeWindowTestHost.InvokeOn(item, "ReleaseBitmap");

                OrganizeWindowTestHost.SetField(window, "_renderOrganizeThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) =>
                    {
                        var current = Interlocked.Increment(ref call);
                        if (current == 1)
                        {
                            firstStarted.Set();
                            releaseFirst.Wait(TimeSpan.FromSeconds(5));
                            return OrganizeWindowTestHost.SyntheticRendered(index, dpi, 1);
                        }
                        return OrganizeWindowTestHost.SyntheticRendered(index, dpi, 2);
                    }));

                var first = OrganizeWindowTestHost.InvokeTask(window, "RefreshOrganizeThumbnailRenderWindowAsync");
                Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(3)));
                range = (4, 4);
                var second = OrganizeWindowTestHost.InvokeTask(window, "RefreshOrganizeThumbnailRenderWindowAsync");
                await second;
                releaseFirst.Set();
                await first;

                var items = OrganizeWindowTestHost.GetOrganizeItems(window);
                Assert.Null(OrganizeWindowTestHost.GetProperty<BitmapSource?>(items[0], "Bitmap"));
                var currentBitmap = OrganizeWindowTestHost.GetProperty<BitmapSource?>(items[4], "Bitmap");
                Assert.NotNull(currentBitmap);
                Assert.Equal((byte)2, OrganizeWindowTestHost.ReadFirstPixelByte(currentBitmap!));
            }
            finally
            {
                releaseFirst.Set();
                firstStarted.Dispose();
                releaseFirst.Dispose();
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void OneTileRenderFailure_IsIsolatedFromOtherTilesAndReaderSession()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                OrganizeWindowTestHost.SetField(window, "_getOrganizeRealizedRange",
                    (Func<(int First, int Last)?>)(() => (0, 2)));
                OrganizeWindowTestHost.SetField(window, "_renderOrganizeThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) =>
                    {
                        if (index == 1)
                            throw new InvalidOperationException("forced tile failure");
                        return OrganizeWindowTestHost.SyntheticRendered(index, dpi, 7);
                    }));

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                await OrganizeWindowTestHost.InvokeTask(window, "RefreshOrganizeThumbnailRenderWindowAsync");

                var items = OrganizeWindowTestHost.GetOrganizeItems(window);
                Assert.NotNull(OrganizeWindowTestHost.GetProperty<BitmapSource?>(items[0], "Bitmap"));
                Assert.True(OrganizeWindowTestHost.GetProperty<bool>(items[1], "HasError"));
                Assert.NotNull(OrganizeWindowTestHost.GetProperty<BitmapSource?>(items[2], "Bitmap"));
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_session"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void RebuildAfterReorder_ReusesBitmapWhenSourceAndRotationAreUnchanged()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(5);
            var window = new MainWindow();
            try
            {
                OrganizeWindowTestHost.SetField(window, "_getOrganizeRealizedRange",
                    (Func<(int First, int Last)?>)(() => (0, 4)));
                OrganizeWindowTestHost.SetField(window, "_renderOrganizeThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((_, index, dpi, _) =>
                        OrganizeWindowTestHost.SyntheticRendered(index, dpi, (byte)(20 + index))));

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                await OrganizeWindowTestHost.InvokeTask(window, "RefreshOrganizeThumbnailRenderWindowAsync");

                var before = OrganizeWindowTestHost.GetOrganizeItems(window);
                var retainedId = OrganizeWindowTestHost.GetProperty<Guid>(before[1], "ItemId");
                var retainedBitmap = OrganizeWindowTestHost.GetProperty<BitmapSource?>(before[1], "Bitmap");
                Assert.NotNull(retainedBitmap);

                var plan = Assert.IsType<SGPdf.App.Features.Organize.OrganizePlan>(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                var moved = SGPdf.App.Features.Organize.OrganizePlanOperations.MoveSelection(plan, new[] { retainedId }, 5);
                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizePlanSnapshot", moved);

                var after = OrganizeWindowTestHost.GetOrganizeItems(window);
                var movedItem = after.Single(item => OrganizeWindowTestHost.GetProperty<Guid>(item, "ItemId") == retainedId);
                Assert.Same(retainedBitmap, OrganizeWindowTestHost.GetProperty<BitmapSource?>(movedItem, "Bitmap"));
                Assert.Equal(5, OrganizeWindowTestHost.GetProperty<int>(movedItem, "PageNumber"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }
}
