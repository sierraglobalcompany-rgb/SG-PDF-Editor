using SGPdf.App.Navigation;
using SGPdf.App.Shell;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowDocumentStatusTests
{
    [Fact]
    public void V4ShellSelectionFill_UsesSystemAccentTint()
    {
        var styles = XDocument.Load(FindRepoFile("src", "SGPdf.App", "Themes", "Modern.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var selectionFill = styles.Descendants(presentation + "SolidColorBrush")
            .Single(brush => (string?)brush.Attribute(x + "Key") == "SgSelectionFillBrush");

        Assert.Equal("{StaticResource SystemAccentColor}", (string?)selectionFill.Attribute("Color"));
        Assert.Equal("0.14", (string?)selectionFill.Attribute("Opacity"));
    }

    [Fact]
    public void DocumentBrandIcon_DecodesOfficialAppIconIntoVisibleBitmapSource()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                OrganizeWindowTestHost.EnsureLoaded(window);
                var icon = OrganizeWindowTestHost.Element<Image>(window, "DocumentBrandIcon");
                var bitmap = Assert.IsAssignableFrom<BitmapSource>(icon.Source);

                Assert.True(bitmap.PixelWidth >= 24);
                Assert.True(bitmap.PixelHeight >= 24);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void DocumentTab_UiaNameMatchesVisiblePdfFilename()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));

                var document = Assert.Single(Assert.IsAssignableFrom<IEnumerable<PdfDocumentWorkspace>>(
                    OrganizeWindowTestHost.GetField(window, "_openDocuments")));
                var tabs = OrganizeWindowTestHost.Element<TabControl>(window, "DocumentTabs");
                var theme = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/SGPdf.App;component/Themes/Modern.xaml", UriKind.Absolute)
                };
                tabs.ItemContainerStyle = Assert.IsType<Style>(theme["SgDocumentTabItemStyle"]);
                window.WindowState = WindowState.Maximized;
                window.Show();
                OrganizeWindowTestHost.PumpUntil(() => window.IsVisible && tabs.IsLoaded, 5000);
                tabs.ApplyTemplate();
                window.UpdateLayout();
                tabs.UpdateLayout();

                var tab = tabs.ItemContainerGenerator.ContainerFromItem(document) as TabItem;
                if (tab is null)
                {
                    OrganizeWindowTestHost.PumpUntil(
                        () => tabs.ItemContainerGenerator.ContainerFromItem(document) is TabItem);
                    tab = (TabItem)tabs.ItemContainerGenerator.ContainerFromItem(document)!;
                }

                Assert.Same(document, tab.DataContext);
                Assert.Equal("source.pdf", document.DisplayName);

                var automationPeer = UIElementAutomationPeer.CreatePeerForElement(tab);
                Assert.NotNull(automationPeer);
                Assert.Equal(document.DisplayName, AutomationProperties.GetName(tab));
                Assert.Equal(document.DisplayName, automationPeer.GetName());
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void ClosingLastPdf_ClearsStaleCurrentPageStatus()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                await OrganizeWindowTestHost.InvokeTask(
                    window,
                    "NavigateAsync",
                    (Func<PageNavigationState, PageNavigationState>)(state => state.Next()));

                var status = OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText");
                Assert.Equal("source.pdf — página 2 de 2", status.Text);

                var documents = Assert.IsAssignableFrom<IEnumerable<PdfDocumentWorkspace>>(
                    OrganizeWindowTestHost.GetField(window, "_openDocuments"));
                var lastDocument = Assert.Single(documents);

                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(
                    window,
                    "TryCloseDocumentWithGuard",
                    lastDocument));

                Assert.Equal("Listo", status.Text);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void ClosingLastPdf_PreservesVisibleErrorStatus()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            const string visibleError = "No se pudo mostrar la página 2.";
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                await OrganizeWindowTestHost.InvokeTask(
                    window,
                    "NavigateAsync",
                    (Func<PageNavigationState, PageNavigationState>)(state => state.Next()));

                var status = OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText");
                status.Text = visibleError;
                var documents = Assert.IsAssignableFrom<IEnumerable<PdfDocumentWorkspace>>(
                    OrganizeWindowTestHost.GetField(window, "_openDocuments"));

                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(
                    window,
                    "TryCloseDocumentWithGuard",
                    Assert.Single(documents)));

                Assert.Equal(visibleError, status.Text);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    private static string FindRepoFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");
    }
}
