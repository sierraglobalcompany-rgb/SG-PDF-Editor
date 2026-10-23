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
    private bool _homeActionStarting;
    private Func<MainWindow, string?> _selectHomePdfPath = static owner =>
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Seleccionar PDF para la herramienta",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    };

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
            Margin = new Thickness(0, 8, 0, 8)
        };
        RegisterName(items.Name, items);

        var railHeading = new TextBlock
        {
            Text = "DOCUMENTO",
            FontSize = 9d,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(2, 0, 2, 4)
        };
        railHeading.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        items.Children.Add(railHeading);

        foreach (var descriptor in ToolCatalog.Document)
        {
            FrameworkElement icon;
            if (descriptor.DocumentIconPath is string documentIconPath)
            {
                icon = new System.Windows.Controls.Image
                {
                    Width = 24d,
                    Height = 24d,
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
                    Width = 24d,
                    Height = 24d,
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
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.None,
                Width = 62d,
                Margin = new Thickness(0, 3, 0, 0),
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
            // Selection is applied only by UpdateDocumentToolRailSelection() from actual mode state.
            button.SetResourceReference(FrameworkElement.StyleProperty, "SgRailItemStyle");
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
            Width = 78d,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = scroller,
            Visibility = Visibility.Collapsed
        };
        rail.SetResourceReference(BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
        rail.SetResourceReference(BorderBrushProperty, "CardStrokeColorDefaultBrush");
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
        ApplyReaderNavigationState(ActiveDocument);
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
            _documentToolRailColumn.Width = new GridLength(documentVisible ? 78d : 0d);

        if (documentVisible)
            UpdateDocumentToolRailSelection();
    }

    private async void HandleToolAction(string actionId)
    {
        var isCoreMode = actionId is "read" or "edit" or "comment" or "sign" or "organize";
        if (!isCoreMode && actionId is not ("forms" or "ocr" or "redact" or "compare" or "convert" or "protect"))
            return;

        if (_session is null || _zplDocument is not null || _isBusy || _utilityExecuting)
        {
            UpdateDocumentToolRailSelection();
            return;
        }

        if (isCoreMode && !TryPrepareCoreToolTransition(actionId))
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
            case "forms":
                EditAcroForm_Click(this, new RoutedEventArgs(Button.ClickEvent));
                break;
            case "ocr":
                Ocr_Click(this, new RoutedEventArgs(Button.ClickEvent));
                break;
            case "redact":
                if (TryPrepareCoreToolTransition("edit") && await TryEnterEditModeAsync())
                    StatusText.Text = "Censurar: selecciona texto compatible en Editar, pulsa Redactar y guarda una copia.";
                break;
            case "compare":
                ComparePdf_Click(this, new RoutedEventArgs(Button.ClickEvent));
                break;
            case "convert":
                ExportPdfToPng_Click(this, new RoutedEventArgs(Button.ClickEvent));
                break;
            case "protect":
                Protection_Click(this, new RoutedEventArgs(Button.ClickEvent));
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
                        : _acroFormPropertiesScrollViewer?.Visibility == Visibility.Visible
                            ? "forms"
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

    private async void HomeToolRequested(string actionId)
    {
        try
        {
            await TryStartHomeActionAsync(actionId);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo iniciar la herramienta: {ex.Message}";
        }
    }

    // true means the intention was sent to its owner; it does not mean the operation completed.
    private async Task<bool> TryStartHomeActionAsync(string actionId)
    {
        // Keep the historical ZPL behavior outside the new PDF flow.
        if (actionId == "open-zpl")
        {
            HomeOpenZplRequested();
            return true;
        }

        if (actionId is not ("qr" or "image-to-pdf" or "organize" or "edit" or "sign" or "merge" or "split" or
                "ocr" or "redact" or "protect" or "compare" or "convert" or
                "watermark" or "page-numbering") ||
            _homeActionStarting || _isBusy || _utilityExecuting || _zplDocument is not null)
            return false;

        _homeActionStarting = true;
        try
        {
            if (actionId == "image-to-pdf")
            {
                await StartImageToPdfFromHomeAsync();
                return true;
            }

            if (actionId == "qr")
            {
                if (!CanStartBatch())
                    return false;
                QrCode_Click(this, new RoutedEventArgs(Button.ClickEvent));
                return true;
            }

            if (_session is null)
            {
                var path = _selectHomePdfPath(this);
                if (string.IsNullOrWhiteSpace(path) || !await TryOpenPdfPathAsync(path))
                    return false;
            }

            if (_session is null || _zplDocument is not null || _isBusy || _utilityExecuting)
                return false;

            // Merge and Split are concrete Organize operations, not mode navigation.
            // Keep the existing document/mode guards and the single Organize plan owner.
            if (actionId is "merge" or "split")
            {
                if (!TryPrepareCoreToolTransition("organize") || !TryEnterOrganizeMode())
                    return false;
                return actionId == "merge" ? TryMergeOrganizePdf() : TrySplitOrganizePdf();
            }

            if (actionId == "watermark")
                WatermarkText_Click(this, new RoutedEventArgs(Button.ClickEvent));
            else if (actionId == "page-numbering")
                PageNumbering_Click(this, new RoutedEventArgs(Button.ClickEvent));
            else
                HandleToolAction(actionId);
            return true; // Intención delegada; NO asegura operación/salida completada.
        }
        finally
        {
            _homeActionStarting = false;
        }
    }
}
