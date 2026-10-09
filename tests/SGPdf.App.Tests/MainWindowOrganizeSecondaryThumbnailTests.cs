using System.Threading;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOrganizeSecondaryThumbnailTests
{
    [Fact]
    public void InsertedSecondaryPage_RendersThumbnailFromItsOwnSourceSession()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var primary = OrganizeWindowPdfFixture.Create(1);
            using var secondary = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            var renderedPaths = new List<string>();
            try
            {
                OrganizeWindowTestHost.SetField(
                    window,
                    "_getOrganizeRealizedRange",
                    (Func<(int First, int Last)?>)(() => (1, 1)));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_renderOrganizeThumbnail",
                    (Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>)((session, index, dpi, _) =>
                    {
                        renderedPaths.Add(session.FilePath);
                        return OrganizeWindowTestHost.SyntheticRendered(index, dpi, 77);
                    }));

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, primary.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) =>
                        new OrganizePreflightResult(Array.Empty<OrganizeFinding>())));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(
                    window,
                    "TryInsertOrganizePdfCandidate",
                    secondary.Path,
                    null,
                    1)!);

                foreach (var item in OrganizeWindowTestHost.GetOrganizeItems(window))
                    OrganizeWindowTestHost.InvokeOn(item, "ReleaseBitmap");

                await OrganizeWindowTestHost.InvokeTask(window, "RefreshOrganizeThumbnailRenderWindowAsync");

                var items = OrganizeWindowTestHost.GetOrganizeItems(window);
                Assert.Equal(2, items.Count);
                Assert.False(OrganizeWindowTestHost.GetProperty<bool>(items[1], "HasError"));
                Assert.NotNull(OrganizeWindowTestHost.GetProperty<BitmapSource?>(items[1], "Bitmap"));
                Assert.Contains(renderedPaths, path =>
                    string.Equals(path, secondary.Path, StringComparison.OrdinalIgnoreCase));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }
}
