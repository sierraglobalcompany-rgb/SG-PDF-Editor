using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;
using SGPdf.App.Shell;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool DocumentLifecycleHookRegistered = RegisterDocumentLifecycleHook();
    private readonly ObservableCollection<PdfDocumentWorkspace> _openDocuments = new();
    private bool _documentLifecycleInitialized;
    private bool _documentTabsUiInitialized;
    private bool _syncingDocumentTabs;
    private Border? _documentTopShell;
    private Grid? _documentTabRow;
    private TabControl? _documentTabs;
    private Button? _addDocumentTabButton;

    private Func<PdfDocumentWorkspace, bool> _confirmCloseDirtyDocument = static document =>
        MessageBox.Show(
            $"{document.DisplayName} tiene cambios sin guardar.\n\n¿Deseas cerrar este documento y descartar esos cambios?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    internal IReadOnlyList<PdfDocumentWorkspace> OpenDocuments => _openDocuments;

    internal PdfDocumentWorkspace? ActiveDocument { get; private set; }

    private TabControl DocumentTabs => _documentTabs
        ?? throw new InvalidOperationException("Las pestañas de documentos no están inicializadas.");

    private Button AddDocumentTabButton => _addDocumentTabButton
        ?? throw new InvalidOperationException("El botón para abrir otro PDF no está inicializado.");

    private static bool RegisterDocumentLifecycleHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(DocumentLifecycle_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void DocumentLifecycle_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || window._documentLifecycleInitialized)
            return;

        window._documentLifecycleInitialized = true;
        _ = DocumentLifecycleHookRegistered;
        window.InitializeDocumentTabsUi();
        window.Closed += DocumentsWindow_Closed;
    }

    private static void DocumentsWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is MainWindow window)
            window.DisposeTrackedDocuments();
    }

    private void InitializeDocumentTabsUi()
    {
        if (_documentTabsUiInitialized)
            return;

        if (Content is not DockPanel root)
            throw new InvalidOperationException("No se encontró el shell principal de la ventana.");

        var existingMenu = root.Children.OfType<Menu>().FirstOrDefault()
            ?? throw new InvalidOperationException("No se encontró el menú principal.");

        root.Children.Remove(existingMenu);
        root.Children.Remove(NavigationBar);

        var shellGrid = new Grid();
        shellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var tabRow = new Grid
        {
            Name = "DocumentTabRow",
            MinHeight = 38d,
            Margin = new Thickness(8, 6, 8, 0),
            Visibility = Visibility.Collapsed
        };
        tabRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
        tabRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var brand = new TextBlock
        {
            Text = "SG PDF",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 12, 0)
        };
        brand.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        Grid.SetColumn(brand, 0);
        tabRow.Children.Add(brand);

        _documentTabs = new TabControl
        {
            Name = "DocumentTabs",
            MinHeight = 34d,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = BuildDocumentTabHeaderTemplate()
        };
        DocumentTabs.SetResourceReference(FrameworkElement.StyleProperty, "SgDocumentTabsStyle");
        DocumentTabs.ItemsSource = _openDocuments;
        DocumentTabs.SelectionChanged += DocumentTabs_SelectionChanged;
        AutomationProperties.SetName(DocumentTabs, "Documentos PDF abiertos");
        RegisterName(DocumentTabs.Name, DocumentTabs);
        Grid.SetColumn(DocumentTabs, 1);
        tabRow.Children.Add(DocumentTabs);

        _addDocumentTabButton = new Button
        {
            Name = "AddDocumentTabButton",
            Content = "+",
            Width = 32d,
            Height = 32d,
            MinWidth = 32d,
            Padding = new Thickness(0),
            Margin = new Thickness(6, 0, 2, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Abrir otro PDF"
        };
        AddDocumentTabButton.SetResourceReference(FrameworkElement.StyleProperty, "SgIconButtonStyle");
        AddDocumentTabButton.Click += AddDocumentTabButton_Click;
        AutomationProperties.SetName(AddDocumentTabButton, "Abrir otro PDF");
        RegisterName(AddDocumentTabButton.Name, AddDocumentTabButton);
        Grid.SetColumn(AddDocumentTabButton, 2);
        tabRow.Children.Add(AddDocumentTabButton);

        Grid.SetRow(tabRow, 0);
        shellGrid.Children.Add(tabRow);

        var commandRow = new Grid
        {
            Margin = new Thickness(4, 2, 4, 4)
        };
        commandRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
        commandRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        existingMenu.HorizontalAlignment = HorizontalAlignment.Left;
        existingMenu.VerticalAlignment = VerticalAlignment.Center;
        existingMenu.Background = Brushes.Transparent;
        Grid.SetColumn(existingMenu, 0);
        commandRow.Children.Add(existingMenu);

        NavigationBar.HorizontalAlignment = HorizontalAlignment.Right;
        NavigationBar.BorderThickness = new Thickness(0);
        NavigationBar.Background = Brushes.Transparent;
        if (NavigationBar.Child is StackPanel navigationItems)
        {
            navigationItems.HorizontalAlignment = HorizontalAlignment.Right;
            navigationItems.Margin = new Thickness(6, 4, 6, 4);
        }

        PreviousPageButton.Content = "‹";
        PreviousPageButton.MinWidth = 32d;
        PreviousPageButton.ToolTip = "Página anterior";
        PreviousPageButton.SetResourceReference(FrameworkElement.StyleProperty, "SgIconButtonStyle");
        NextPageButton.Content = "›";
        NextPageButton.MinWidth = 32d;
        NextPageButton.ToolTip = "Página siguiente";
        NextPageButton.SetResourceReference(FrameworkElement.StyleProperty, "SgIconButtonStyle");
        FitPageButton.Content = "Página";
        FitWidthButton.Content = "Ancho";

        Grid.SetColumn(NavigationBar, 1);
        commandRow.Children.Add(NavigationBar);
        Grid.SetRow(commandRow, 1);
        shellGrid.Children.Add(commandRow);

        var topShell = new Border
        {
            Name = "DocumentTopShell",
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = shellGrid,
            Visibility = Visibility.Collapsed
        };
        topShell.SetResourceReference(Border.BackgroundProperty, "ApplicationBackgroundBrush");
        topShell.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
        RegisterName(topShell.Name, topShell);
        DockPanel.SetDock(topShell, Dock.Top);
        root.Children.Insert(0, topShell);

        NavigationBar.IsVisibleChanged += DocumentTopShellChildVisibilityChanged;
        LabelNavigationBar.IsVisibleChanged += DocumentTopShellChildVisibilityChanged;

        _documentTopShell = topShell;
        _documentTabRow = tabRow;
        _documentTabsUiInitialized = true;
        SyncDocumentTabsSelection();
        UpdateDocumentTopShellVisibility();
    }

    private DataTemplate BuildDocumentTabHeaderTemplate()
    {
        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var documentIcon = new FrameworkElementFactory(typeof(Image));
        documentIcon.SetValue(FrameworkElement.WidthProperty, 18d);
        documentIcon.SetValue(FrameworkElement.HeightProperty, 18d);
        documentIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        documentIcon.SetValue(Image.StretchProperty, Stretch.Uniform);
        documentIcon.SetValue(
            Image.SourceProperty,
            new BitmapImage(new Uri("pack://application:,,,/SGPdf.App;component/Assets/SGPdf.Document.png", UriKind.Absolute)));
        panel.AppendChild(documentIcon);

        var title = new FrameworkElementFactory(typeof(TextBlock));
        title.SetBinding(TextBlock.TextProperty, new Binding(nameof(PdfDocumentWorkspace.DisplayName)));
        title.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        title.SetValue(FrameworkElement.MaxWidthProperty, 210d);
        title.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        title.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        panel.AppendChild(title);

        var close = new FrameworkElementFactory(typeof(Button));
        close.SetValue(ContentControl.ContentProperty, "×");
        close.SetValue(FrameworkElement.WidthProperty, 24d);
        close.SetValue(FrameworkElement.HeightProperty, 24d);
        close.SetValue(Control.PaddingProperty, new Thickness(0));
        close.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        close.SetValue(ToolTipService.ToolTipProperty, "Cerrar documento");
        close.SetValue(AutomationProperties.NameProperty, "Cerrar documento");
        close.SetResourceReference(FrameworkElement.StyleProperty, "SgIconButtonStyle");
        close.AddHandler(Button.ClickEvent, new RoutedEventHandler(DocumentTabClose_Click));
        panel.AppendChild(close);

        return new DataTemplate(typeof(PdfDocumentWorkspace)) { VisualTree = panel };
    }

    private void DocumentTopShellChildVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        => UpdateDocumentTopShellVisibility();

    private void UpdateDocumentTopShellVisibility()
    {
        if (!_documentTabsUiInitialized || _documentTopShell is null || _documentTabRow is null)
            return;

        var pdfActive = ActiveDocument is not null && _zplDocument is null;
        var zplActive = _zplDocument is not null || LabelNavigationBar.Visibility == Visibility.Visible;
        _documentTopShell.Visibility = pdfActive || zplActive
            ? Visibility.Visible
            : Visibility.Collapsed;
        _documentTabRow.Visibility = pdfActive
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SyncDocumentTabsSelection()
    {
        if (_documentTabs is null)
            return;

        _syncingDocumentTabs = true;
        try
        {
            DocumentTabs.SelectedItem = ActiveDocument;
        }
        finally
        {
            _syncingDocumentTabs = false;
        }
    }

    private async void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingDocumentTabs || DocumentTabs.SelectedItem is not PdfDocumentWorkspace document ||
            ReferenceEquals(document, ActiveDocument))
        {
            return;
        }

        try
        {
            await ActivateDocumentAsync(document);
        }
        finally
        {
            SyncDocumentTabsSelection();
        }
    }

    private async void DocumentTabClose_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not Button { DataContext: PdfDocumentWorkspace document })
            return;

        await TryCloseDocumentWithGuard(document);
        SyncDocumentTabsSelection();
        UpdateDocumentTopShellVisibility();
    }

    private void AddDocumentTabButton_Click(object sender, RoutedEventArgs e)
        => OpenPdf_Click(OpenPdfMenuItem, new RoutedEventArgs(MenuItem.ClickEvent));

    private void TrackOpenedDocument(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_openDocuments.Contains(document))
            return;

        _syncingDocumentTabs = true;
        try
        {
            _openDocuments.Add(document);
        }
        finally
        {
            _syncingDocumentTabs = false;
        }
    }

    private void UntrackOpenedDocument(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _syncingDocumentTabs = true;
        try
        {
            _openDocuments.Remove(document);
        }
        finally
        {
            _syncingDocumentTabs = false;
        }

        if (ReferenceEquals(ActiveDocument, document))
        {
            ActiveDocument = null;
            _session = null;
            _navigation = null;
            _zoom = new Pdf.PdfZoomState();
        }

        document.Dispose();
        SyncDocumentTabsSelection();
        UpdateDocumentTopShellVisibility();
    }

    private void SaveActiveDocumentReaderState()
    {
        if (ActiveDocument is null)
            return;

        if (_navigation is not null)
            ActiveDocument.SetNavigation(_navigation);
        ActiveDocument.SetZoom(_zoom);
        ActiveDocument.VerticalOffset = _readerVerticalOffset;
    }

    private void BindLegacyReaderStateToDocument(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ActiveDocument = document;
        _session = ActiveDocument.Session;
        _navigation = ActiveDocument.Navigation;
        _zoom = ActiveDocument.Zoom;
        SyncDocumentTabsSelection();
        UpdateDocumentTopShellVisibility();
    }

    private async Task ActivateDocumentAsync(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (ReferenceEquals(ActiveDocument, document))
        {
            SyncDocumentTabsSelection();
            return;
        }

        CaptureActiveDocumentPendingState();
        SaveActiveDocumentReaderState();
        DetachActiveDocumentPendingUi();
        _resizeRenderScheduler.CancelCurrent();
        _readerRenderScheduler.CancelCurrent();

        BindLegacyReaderStateToDocument(document);
        _zplDocument = null;
        _renderedZplLabels = Array.Empty<ZplRenderedLabel>();
        // Document identity drives the shell; two PDFs can share a window title.
        SyncShellWorkspaceFromActiveDocument();
        UpdateDocumentTopShellVisibility();

        _updatingReaderScroll = true;
        try
        {
            InitializeContinuousReader(document.PageSizes);
            var pageIndex = Math.Clamp(document.Navigation.CurrentPageIndex, 0, document.PageSizes.Count - 1);
            if (pageIndex > 0)
                ScrollReaderToPage(pageIndex);

            if (document.VerticalOffset > 0d)
            {
                _readerVerticalOffset = document.VerticalOffset;
                ScrollInternalReader(_readerVerticalOffset);
                UpdateReaderCurrentPageFromViewport();
            }

            Title = $"SG PDF Editor — {Path.GetFileName(document.FilePath)}";
            UpdateViewerControlsUi();
            UpdateCurrentPageStatus();
            await RefreshReaderRenderWindowAsync();
            RestoreDocumentPendingState(document);
        }
        finally
        {
            _updatingReaderScroll = false;
            SyncDocumentTabsSelection();
            UpdateDocumentTopShellVisibility();
        }
    }

    private async Task<bool> TryCloseDocumentWithGuard(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!_openDocuments.Contains(document))
            return true;

        var wasActive = ReferenceEquals(ActiveDocument, document);
        if (wasActive)
        {
            CaptureActiveDocumentPendingState();
            SaveActiveDocumentReaderState();
        }

        if (document.PendingState.IsDirty && !_confirmCloseDirtyDocument(document))
            return false;

        var fallback = wasActive
            ? _openDocuments.FirstOrDefault(candidate => !ReferenceEquals(candidate, document))
            : null;

        if (fallback is not null)
        {
            await ActivateDocumentAsync(fallback);
        }
        else if (wasActive)
        {
            DetachActiveDocumentPendingUi();
            ActiveDocument = null;
            _session = null;
            _navigation = null;
            _zoom = new Pdf.PdfZoomState();
            _readerPages.Clear();
            _readerPageSizes = Array.Empty<Pdf.PdfPageSize>();
            _readerGeometry = Array.Empty<Features.Reader.ReaderPageGeometry>();
            if (_readerContinuousSurface is not null)
                _readerContinuousSurface.Visibility = Visibility.Collapsed;
            PdfScrollViewer.Visibility = Visibility.Visible;
            PdfImage.Source = null;
            PdfImage.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Visible;
            Title = "SG PDF Editor";
            UpdateViewerControlsUi();
            SyncDocumentTabsSelection();
            UpdateDocumentTopShellVisibility();
        }

        UntrackOpenedDocument(document);
        return true;
    }

    private void DisposeTrackedDocuments()
    {
        foreach (var document in _openDocuments.ToArray())
            document.Dispose();

        _openDocuments.Clear();
        ActiveDocument = null;
        _session = null;
        _navigation = null;
        _zoom = new Pdf.PdfZoomState();
        SyncDocumentTabsSelection();
    }
}
