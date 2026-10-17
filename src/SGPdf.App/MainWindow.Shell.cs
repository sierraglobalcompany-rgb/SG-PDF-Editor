using System.IO;
using System.Windows;
using System.Windows.Automation;
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
    private Border? _documentToolRail;
    private ColumnDefinition? _documentToolRailColumn;
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

        InitializeDocumentToolRail(workspace);

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

    private void InitializeDocumentToolRail(Grid workspace)
    {
        var railColumn = new ColumnDefinition { Width = new GridLength(0d) };
        workspace.ColumnDefinitions.Insert(0, railColumn);
        _documentToolRailColumn = railColumn;

        foreach (UIElement child in workspace.Children.Cast<UIElement>().ToArray())
            Grid.SetColumn(child, Grid.GetColumn(child) + 1);

        var items = new StackPanel
        {
            Name = "DocumentToolRailItems",
            Margin = new Thickness(0, 6, 0, 6)
        };
        RegisterName(items.Name, items);

        foreach (var descriptor in ToolCatalog.Document)
        {
            FrameworkElement icon;
            if (descriptor.DocumentIconPath is string documentIconPath)
            {
                icon = new System.Windows.Controls.Image
                {
                    Width = 22d,
                    Height = 22d,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(documentIconPath, UriKind.Relative))
                };
            }
            else
            {
                var fallbackIcon = new System.Windows.Shapes.Path
                {
                    Width = 22d,
                    Height = 22d,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    StrokeThickness = 1.5d
                };
                fallbackIcon.SetResourceReference(System.Windows.Shapes.Path.DataProperty, descriptor.IconKey);
                fallbackIcon.SetResourceReference(
                    System.Windows.Shapes.Shape.StrokeProperty,
                    "TextFillColorPrimaryBrush");
                icon = fallbackIcon;
            }

            var label = new TextBlock
            {
                Text = descriptor.Title,
                FontSize = 10.5d,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            var content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            content.Children.Add(icon);
            content.Children.Add(label);

            var button = new Button
            {
                Content = content,
                Tag = descriptor.Id,
                Focusable = true,
                IsEnabled = descriptor.ActionId is not null,
                ToolTip = descriptor.DisabledReason ?? descriptor.Description
            };
            button.SetResourceReference(
                FrameworkElement.StyleProperty,
                string.Equals(descriptor.Id, "read", StringComparison.Ordinal)
                    ? "SgRailItemSelectedStyle"
                    : "SgRailItemStyle");
            AutomationProperties.SetName(button, descriptor.Title);
            AutomationProperties.SetHelpText(button, descriptor.DisabledReason ?? descriptor.Description);

            if (descriptor.ActionId is string actionId)
                button.Click += (_, _) => HandleToolAction(actionId);

            items.Children.Add(button);
        }

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = items
        };

        var rail = new Border
        {
            Name = "DocumentToolRail",
            Width = 96d,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = scroller,
            Visibility = Visibility.Collapsed
        };
        rail.SetResourceReference(BackgroundProperty, "SgPanelBrush");
        rail.SetResourceReference(BorderBrushProperty, "SgBorderBrush");
        RegisterName(rail.Name, rail);
        Grid.SetColumn(rail, 0);
        workspace.Children.Add(rail);
        _documentToolRail = rail;
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

        var documentVisible = _shellWorkspaceKind == ShellWorkspaceKind.Pdf;
        _homeDashboard.Visibility = _shellWorkspaceKind == ShellWorkspaceKind.Home
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_documentToolRail is not null)
            _documentToolRail.Visibility = documentVisible ? Visibility.Visible : Visibility.Collapsed;
        if (_documentToolRailColumn is not null)
            _documentToolRailColumn.Width = new GridLength(documentVisible ? 96d : 0d);

        if (documentVisible)
            UpdateDocumentToolRailSelection();
    }

    private async void HandleToolAction(string actionId)
    {
        if (actionId is not ("read" or "edit" or "comment" or "sign" or "organize"))
            return;

        if (!TryPrepareCoreToolTransition(actionId))
        {
            UpdateDocumentToolRailSelection();
            return;
        }

        switch (actionId)
        {
            case "read":
                await TryEnterReadModeAsync();
                break;
            case "edit":
                await TryEnterEditModeAsync();
                break;
            case "comment":
                TryEnterCommentMode();
                break;
            case "sign":
                await TryEnterSignModeAsync();
                break;
            case "organize":
                TryEnterOrganizeMode();
                break;
        }

        UpdateDocumentToolRailSelection();
    }

    private bool TryPrepareCoreToolTransition(string actionId)
    {
        if (!string.Equals(actionId, "comment", StringComparison.Ordinal) &&
            _commentModeActive &&
            !TryLeaveCommentModeWithGuard())
        {
            return false;
        }

        if (!string.Equals(actionId, "edit", StringComparison.Ordinal) &&
            _editModeActive &&
            !TryLeaveEditModeWithGuard())
        {
            return false;
        }

        if (!string.Equals(actionId, "organize", StringComparison.Ordinal) &&
            _organizeModeActive &&
            !TryLeaveOrganizeModeWithGuard())
        {
            return false;
        }

        if (!string.Equals(actionId, "sign", StringComparison.Ordinal) &&
            _signatureEditState?.IsDirty == true &&
            !TryResolvePendingSignatureEdits(SignatureGuardReason.LeaveSignMode))
        {
            return false;
        }

        return true;
    }

    private void UpdateDocumentToolRailSelection()
    {
        if (FindName("DocumentToolRailItems") is not StackPanel items)
            return;

        var selected = _commentModeActive
            ? "comment"
            : _editModeActive
                ? "edit"
                : _organizeModeActive
                    ? "organize"
                    : _signatureModeActive
                        ? "sign"
                        : "read";

        foreach (var button in items.Children.OfType<Button>())
        {
            button.SetResourceReference(
                FrameworkElement.StyleProperty,
                string.Equals(button.Tag as string, selected, StringComparison.Ordinal)
                    ? "SgRailItemSelectedStyle"
                    : "SgRailItemStyle");
        }
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
