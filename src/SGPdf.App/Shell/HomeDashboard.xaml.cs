using System.IO;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Features.Reader;

namespace SGPdf.App.Shell;

public partial class HomeDashboard : UserControl
{
    private IReadOnlyList<ToolDescriptor> _tools = Array.Empty<ToolDescriptor>();
    private IReadOnlyList<RecentPdfEntry> _recentFiles = Array.Empty<RecentPdfEntry>();

    public HomeDashboard()
    {
        InitializeComponent();
        Tools = ToolCatalog.Home;
        RecentFiles = Array.Empty<RecentPdfEntry>();
    }

    internal IReadOnlyList<ToolDescriptor> Tools
    {
        get => _tools;
        set
        {
            _tools = value ?? Array.Empty<ToolDescriptor>();
            HomeToolsItemsControl.ItemsSource = _tools;
        }
    }

    internal IReadOnlyList<RecentPdfEntry> RecentFiles
    {
        get => _recentFiles;
        set
        {
            _recentFiles = value ?? Array.Empty<RecentPdfEntry>();
            var displayItems = _recentFiles.Select(CreateRecentDisplayItem).ToArray();
            HomeRecentItemsControl.ItemsSource = displayItems;
            HomeRecentEmptyText.Visibility = displayItems.Length == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    internal event Action? OpenPdfRequested;
    internal event Action? OpenZplRequested;
    internal event Action<string>? RecentPdfRequested;
    internal event Action<string>? ToolRequested;

    internal void RequestTool(ToolDescriptor descriptor)
    {
        if (descriptor.ActionId is null)
            return;

        ToolRequested?.Invoke(descriptor.ActionId);
    }

    private void OpenPdf_Click(object sender, RoutedEventArgs e)
        => OpenPdfRequested?.Invoke();

    private void OpenZpl_Click(object sender, RoutedEventArgs e)
        => OpenZplRequested?.Invoke();

    private void ToolCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ToolDescriptor descriptor })
            RequestTool(descriptor);
    }

    private void RecentPdf_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RecentDisplayItem item })
            RecentPdfRequested?.Invoke(item.FullPath);
    }

    private void HomeRailInicio_Click(object sender, RoutedEventArgs e)
        => HomeHero.BringIntoView();

    private void HomeRailTools_Click(object sender, RoutedEventArgs e)
        => HomeToolsSection.BringIntoView();

    private void HomeRailRecent_Click(object sender, RoutedEventArgs e)
        => HomeRecentSection.BringIntoView();

    private static RecentDisplayItem CreateRecentDisplayItem(RecentPdfEntry entry)
    {
        var name = Path.GetFileName(entry.FullPath);
        var location = Path.GetDirectoryName(entry.FullPath) ?? entry.FullPath;
        return new RecentDisplayItem(
            entry.FullPath,
            name,
            location,
            entry.LastOpenedUtc.ToLocalTime().ToString("g"),
            TryGetFileSize(entry.FullPath),
            $"Abrir archivo reciente {name}");
    }

    private static string TryGetFileSize(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                return "—";

            var bytes = info.Length;
            if (bytes >= 1024L * 1024L)
                return $"{bytes / (1024d * 1024d):0.0} MB";
            return $"{Math.Max(1d, bytes / 1024d):0} KB";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return "—";
        }
    }

    private sealed record RecentDisplayItem(
        string FullPath,
        string Name,
        string Location,
        string LastOpened,
        string Size,
        string AccessibleName);
}
