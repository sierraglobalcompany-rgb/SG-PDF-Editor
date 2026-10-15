using System.IO;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Shell;

namespace SGPdf.App;

public partial class MainWindow
{
    private enum ShellWorkspaceKind
    {
        Home,
        Pdf,
        Zpl
    }

    private static readonly bool ShellLoadedHandlerRegistered = RegisterShellLoadedHandler();

    private ShellWorkspaceKind _shellWorkspaceKind = ShellWorkspaceKind.Home;
    private HomeDashboard? _homeDashboard;
    private bool _shellUiInitialized;

    private static bool RegisterShellLoadedHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ShellHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ShellHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeShellUi();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_shellUiInitialized && e.Property == TitleProperty)
            SyncShellWorkspaceFromActiveDocument();
    }

    private void InitializeShellUi()
    {
        _ = ShellLoadedHandlerRegistered;
        if (_shellUiInitialized)
            return;

        var center = PdfScrollViewer.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el contenedor central del workspace.");
        var workspace = center.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el contenedor principal del workspace.");

        var dashboard = new HomeDashboard
        {
            Name = "HomeDashboard",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumn(dashboard, 0);
        Grid.SetColumnSpan(dashboard, Math.Max(1, workspace.ColumnDefinitions.Count));
        Panel.SetZIndex(dashboard, 100);

        dashboard.OpenPdfRequested += HomeOpenPdfRequested;
        dashboard.OpenZplRequested += HomeOpenZplRequested;
        dashboard.RecentPdfRequested += HomeRecentPdfRequested;
        dashboard.ToolRequested += HomeToolRequested;

        workspace.Children.Add(dashboard);
        _homeDashboard = dashboard;
        _shellUiInitialized = true;

        RefreshHomeRecentFiles();
        SyncShellWorkspaceFromActiveDocument();
    }

    private void SyncShellWorkspaceFromActiveDocument()
    {
        var kind = _zplDocument is not null
            ? ShellWorkspaceKind.Zpl
            : _session is not null
                ? ShellWorkspaceKind.Pdf
                : ShellWorkspaceKind.Home;
        SetShellWorkspace(kind);
    }

    private void SetShellWorkspace(ShellWorkspaceKind kind)
    {
        _shellWorkspaceKind = kind;
        ApplyShellWorkspaceVisibility();
        if (kind == ShellWorkspaceKind.Home)
            RefreshHomeRecentFiles();
    }

    private void ApplyShellWorkspaceVisibility()
    {
        if (_homeDashboard is null)
            return;

        _homeDashboard.Visibility = _shellWorkspaceKind == ShellWorkspaceKind.Home
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RefreshHomeRecentFiles()
    {
        if (_homeDashboard is null)
            return;

        try
        {
            _homeDashboard.RecentFiles = _recentPdfStore.Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _homeDashboard.RecentFiles = Array.Empty<Features.Reader.RecentPdfEntry>();
        }
    }

    private void HomeOpenPdfRequested()
        => OpenPdf_Click(OpenPdfMenuItem, new RoutedEventArgs(MenuItem.ClickEvent));

    private void HomeOpenZplRequested()
        => OpenZpl_Click(OpenZplMenuItem, new RoutedEventArgs(MenuItem.ClickEvent));

    private async void HomeRecentPdfRequested(string path)
    {
        await OpenRecentPdfPathAsync(path);
        RefreshHomeRecentFiles();
    }

    private void HomeToolRequested(string actionId)
    {
        if (string.Equals(actionId, "open-zpl", StringComparison.Ordinal))
            HomeOpenZplRequested();
    }
}
