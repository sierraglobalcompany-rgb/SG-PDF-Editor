using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using SGPdf.App.Shell;

namespace SGPdf.App;

public partial class MainWindow
{
    private const double ReaderNavigationDrawerWidth = 240d;

    private readonly PdfRenderScheduler _thumbnailRenderScheduler = new();
    private readonly ObservableCollection<ReaderThumbnailItem> _readerThumbnails = new();
    private bool _thumbnailUiInitialized;
    private bool _updatingThumbnailSelection;
    private bool _thumbnailRefreshQueued;
    private bool _syncingReaderNavigationState;
    private ColumnDefinition? _readerNavigationColumn;
    private Border? _readerNavigationDrawer;
    private Button? _readerNavigationToggleButton;
    private TabControl? _readerNavigationTabs;
    private ListBox? _readerThumbnailList;
    private TextBlock? _readerBookmarksFeedback;

    private Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage> _renderReaderThumbnail =
        static (session, pageIndex, dpi, token) => session.RenderPage(pageIndex, dpi, token);
    private Func<(int First, int Last)?> _getThumbnailRealizedRange = static () => null;

    static MainWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ReaderThumbnailHost_Loaded),
            handledEventsToo: true);
    }

    private static void ReaderThumbnailHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeReaderThumbnailsUi();
    }

    private void InitializeReaderThumbnailsUi()
    {
        if (_thumbnailUiInitialized)
            return;

        var host = FindReaderNavigationHost();
        InitializeReaderNavigationDrawer(host);

        var tabs = new TabControl
        {
            Name = "ReaderNavigationTabs",
            Visibility = Visibility.Collapsed
        };
        var pagesTab = new TabItem
        {
            Name = "ReaderPagesTab",
            Header = "Páginas"
        };
        var bookmarksTab = new TabItem
        {
            Name = "ReaderBookmarksTab",
            Header = "Marcadores"
        };
        var thumbnailList = new ListBox
        {
            Name = "ReaderThumbnailList",
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            SelectionMode = SelectionMode.Single,
            ItemsSource = _readerThumbnails,
            ItemTemplate = BuildReaderThumbnailTemplate()
        };
        var bookmarksTree = new TreeView
        {
            Name = "ReaderBookmarksTree",
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent
        };

        VirtualizingPanel.SetIsVirtualizing(thumbnailList, true);
        VirtualizingPanel.SetVirtualizationMode(thumbnailList, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(thumbnailList, ScrollUnit.Pixel);
        ScrollViewer.SetCanContentScroll(thumbnailList, true);
        ScrollViewer.SetVerticalScrollBarVisibility(thumbnailList, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(thumbnailList, ScrollBarVisibility.Disabled);

        pagesTab.Content = thumbnailList;
        var bookmarksContent = new Grid();
        var feedback = new TextBlock
        {
            Name = "ReaderBookmarksFeedback",
            Margin = new Thickness(12),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed
        };
        feedback.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        AutomationProperties.SetLiveSetting(feedback, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(bookmarksTree, "Marcadores del PDF");
        ScrollViewer.SetHorizontalScrollBarVisibility(bookmarksTree, ScrollBarVisibility.Disabled);
        bookmarksContent.Children.Add(bookmarksTree);
        bookmarksContent.Children.Add(feedback);
        bookmarksTab.Content = bookmarksContent;
        _readerBookmarksFeedback = feedback;
        RegisterName(feedback.Name, feedback);
        tabs.Items.Add(pagesTab);
        tabs.Items.Add(bookmarksTab);
        host.Child = tabs;

        RegisterName(tabs.Name, tabs);
        RegisterName(pagesTab.Name, pagesTab);
        RegisterName(bookmarksTab.Name, bookmarksTab);
        RegisterName(thumbnailList.Name, thumbnailList);
        RegisterName(bookmarksTree.Name, bookmarksTree);

        _readerNavigationTabs = tabs;
        _readerThumbnailList = thumbnailList;
        _thumbnailUiInitialized = true;

        _readerPages.CollectionChanged += ReaderPages_CollectionChanged;
        PageNumberTextBox.TextChanged += ReaderPageNumberTextBox_TextChanged;
        NavigationBar.IsVisibleChanged += ReaderNavigationVisibilityChanged;
        LabelNavigationBar.IsVisibleChanged += ReaderNavigationVisibilityChanged;
        tabs.SelectionChanged += ReaderNavigationTabs_SelectionChanged;
        thumbnailList.SelectionChanged += ReaderThumbnailList_SelectionChanged;
        thumbnailList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(ReaderThumbnailList_ScrollChanged));
        if (_readerPageList is not null)
        {
            _readerPageList.AddHandler(
                ScrollViewer.ScrollChangedEvent,
                new ScrollChangedEventHandler(ReaderPageList_ThumbnailSyncScrollChanged));
        }
        Closing += ReaderThumbnailWindow_Closing;

        RebuildReaderThumbnailsFromPages();
        SyncReaderThumbnailSelection(_navigation?.CurrentPageIndex ?? 0);
        UpdateReaderNavigationVisibility();
    }

    private void InitializeReaderNavigationDrawer(Border host)
    {
        var workspace = host.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el workspace del panel de navegación.");
        var columnIndex = Grid.GetColumn(host);
        if (columnIndex < 0 || columnIndex >= workspace.ColumnDefinitions.Count)
            throw new InvalidOperationException("No se encontró la columna del panel de navegación.");

        var column = workspace.ColumnDefinitions[columnIndex];
        column.Name = "ReaderNavigationColumn";
        column.Width = new GridLength(0d);
        host.Name = "ReaderNavigationDrawer";
        host.Visibility = Visibility.Collapsed;

        RegisterName(column.Name, column);
        RegisterName(host.Name, host);

        var center = PdfScrollViewer.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el canvas central del lector.");
        var toggle = new Button
        {
            Name = "ReaderNavigationToggleButton",
            Content = "›",
            Width = 30d,
            Height = 36d,
            MinWidth = 30d,
            Padding = new Thickness(0),
            Margin = new Thickness(8, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed,
            ToolTip = "Mostrar páginas y marcadores"
        };
        toggle.SetResourceReference(FrameworkElement.StyleProperty, "SgIconButtonStyle");
        toggle.Click += ToggleReaderNavigation;
        AutomationProperties.SetName(toggle, "Mostrar páginas y marcadores");
        Panel.SetZIndex(toggle, 80);
        center.Children.Add(toggle);
        RegisterName(toggle.Name, toggle);

        _readerNavigationColumn = column;
        _readerNavigationDrawer = host;
        _readerNavigationToggleButton = toggle;
    }

    private Border FindReaderNavigationHost()
    {
        var center = PdfScrollViewer.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el canvas central del lector.");
        var workspace = center.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el contenedor principal de navegación.");
        var navigationColumn = Grid.GetColumn(center) - 1;
        if (navigationColumn < 0)
            throw new InvalidOperationException("No se encontró la columna izquierda del lector.");

        var host = workspace.Children.OfType<Border>()
            .FirstOrDefault(border => Grid.GetColumn(border) == navigationColumn);
        return host ?? throw new InvalidOperationException("No se encontró el panel izquierdo del lector.");
    }

    private void ToggleReaderNavigation(object sender, RoutedEventArgs e)
    {
        if (ActiveDocument is null || _zplDocument is not null)
            return;

        ActiveDocument.IsReaderNavigationOpen = !ActiveDocument.IsReaderNavigationOpen;
        ApplyReaderNavigationState(ActiveDocument);
        if (ActiveDocument.IsReaderNavigationOpen)
            QueueThumbnailRefresh();
    }

    private void ReaderNavigationTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingReaderNavigationState || ActiveDocument is null || sender is not TabControl tabs || tabs.SelectedIndex < 0)
            return;

        ActiveDocument.ReaderNavigationSelectedIndex = tabs.SelectedIndex;
        UpdateReaderBookmarkContext();
    }

    private void ApplyReaderNavigationState(PdfDocumentWorkspace? document)
    {
        if (_readerNavigationColumn is null || _readerNavigationDrawer is null || _readerNavigationToggleButton is null)
            return;

        var canToggle = document is not null &&
                        _zplDocument is null &&
                        NavigationBar.Visibility == Visibility.Visible;
        var isOpen = canToggle && document!.IsReaderNavigationOpen;

        _readerNavigationColumn.Width = new GridLength(isOpen ? ReaderNavigationDrawerWidth : 0d);
        _readerNavigationDrawer.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
        _readerNavigationToggleButton.Visibility = canToggle ? Visibility.Visible : Visibility.Collapsed;
        _readerNavigationToggleButton.Content = isOpen ? "‹" : "›";

        var accessibilityName = isOpen
            ? "Ocultar páginas y marcadores"
            : "Mostrar páginas y marcadores";
        _readerNavigationToggleButton.ToolTip = accessibilityName;
        AutomationProperties.SetName(_readerNavigationToggleButton, accessibilityName);

        if (_readerNavigationTabs is null || document is null)
        {
            ClearReaderBookmarks();
            return;
        }

        var selectedIndex = Math.Clamp(
            document.ReaderNavigationSelectedIndex,
            0,
            Math.Max(0, _readerNavigationTabs.Items.Count - 1));
        _syncingReaderNavigationState = true;
        try
        {
            _readerNavigationTabs.SelectedIndex = selectedIndex;
        }
        finally
        {
            _syncingReaderNavigationState = false;
        }
        UpdateReaderBookmarkContext();
    }

    private static DataTemplate BuildReaderThumbnailTemplate()
    {
        var outer = new FrameworkElementFactory(typeof(Border));
        outer.SetValue(Border.MarginProperty, new Thickness(8));
        outer.SetValue(Border.PaddingProperty, new Thickness(8));
        outer.SetValue(Border.BackgroundProperty, Brushes.White);
        outer.SetValue(Border.BorderBrushProperty, Brushes.LightGray);
        outer.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        var stack = new FrameworkElementFactory(typeof(StackPanel));
        stack.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        var preview = new FrameworkElementFactory(typeof(Grid));
        preview.SetValue(FrameworkElement.WidthProperty, 132d);
        preview.SetBinding(FrameworkElement.HeightProperty, new Binding(nameof(ReaderThumbnailItem.DisplayHeight)));
        preview.SetValue(Panel.BackgroundProperty, Brushes.WhiteSmoke);

        var image = new FrameworkElementFactory(typeof(Image));
        image.SetValue(Image.StretchProperty, Stretch.Fill);
        image.SetBinding(Image.SourceProperty, new Binding(nameof(ReaderThumbnailItem.Bitmap)));
        preview.AppendChild(image);

        var error = new FrameworkElementFactory(typeof(TextBlock));
        error.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        error.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        error.SetValue(TextBlock.ForegroundProperty, Brushes.DimGray);
        error.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        error.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        error.SetValue(TextBlock.MarginProperty, new Thickness(8));
        error.SetBinding(TextBlock.TextProperty, new Binding(nameof(ReaderThumbnailItem.ErrorMessage)));
        preview.AppendChild(error);
        stack.AppendChild(preview);

        var pageNumber = new FrameworkElementFactory(typeof(TextBlock));
        pageNumber.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pageNumber.SetValue(TextBlock.MarginProperty, new Thickness(0, 6, 0, 0));
        pageNumber.SetBinding(TextBlock.TextProperty, new Binding(nameof(ReaderThumbnailItem.PageNumber))
        {
            StringFormat = "Página {0}"
        });
        stack.AppendChild(pageNumber);
        outer.AppendChild(stack);

        return new DataTemplate(typeof(ReaderThumbnailItem)) { VisualTree = outer };
    }

    private void ReaderPages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_thumbnailUiInitialized)
            return;

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _readerThumbnails.Clear();
        }
        else if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (ReaderPageItem page in e.NewItems)
            {
                var size = new PdfPageSize(page.Geometry.PdfWidthPoints, page.Geometry.PdfHeightPoints);
                var item = new ReaderThumbnailItem(page.PageIndex, size);
                if (page.PageIndex >= _readerThumbnails.Count)
                    _readerThumbnails.Add(item);
                else
                    _readerThumbnails.Insert(page.PageIndex, item);
            }
        }
        else
        {
            RebuildReaderThumbnailsFromPages();
        }

        SyncReaderThumbnailSelection(_navigation?.CurrentPageIndex ?? 0);
        QueueThumbnailRefresh();
    }

    private void RebuildReaderThumbnailsFromPages()
    {
        _readerThumbnails.Clear();
        foreach (var page in _readerPages)
        {
            _readerThumbnails.Add(new ReaderThumbnailItem(
                page.PageIndex,
                new PdfPageSize(page.Geometry.PdfWidthPoints, page.Geometry.PdfHeightPoints)));
        }
    }

    private void ReaderPageNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_navigation is null)
            return;

        SyncReaderThumbnailSelection(_navigation.CurrentPageIndex);
        QueueThumbnailRefresh();
    }

    private void SyncReaderThumbnailSelection(int pageIndex)
    {
        if (_readerThumbnailList is null || pageIndex < 0 || pageIndex >= _readerThumbnails.Count)
            return;

        _updatingThumbnailSelection = true;
        try
        {
            _readerThumbnailList.SelectedIndex = pageIndex;
            _readerThumbnailList.ScrollIntoView(_readerThumbnails[pageIndex]);
        }
        finally
        {
            _updatingThumbnailSelection = false;
        }
    }

    private async void ReaderThumbnailList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingThumbnailSelection ||
            _session is null ||
            _navigation is null ||
            _signatureModeActive ||
            _zplDocument is not null ||
            _readerThumbnailList?.SelectedItem is not ReaderThumbnailItem item ||
            item.PageIndex == _navigation.CurrentPageIndex)
        {
            return;
        }

        ScrollReaderToPage(item.PageIndex);
        await RefreshReaderRenderWindowAsync();
        await RefreshThumbnailRenderWindowAsync();
    }

    private void ReaderThumbnailList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => QueueThumbnailRefresh();

    private void ReaderPageList_ThumbnailSyncScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_navigation is null)
            return;

        SyncReaderThumbnailSelection(_navigation.CurrentPageIndex);
        QueueThumbnailRefresh();
    }

    private void ReaderNavigationVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        => UpdateReaderNavigationVisibility();

    private void UpdateReaderNavigationVisibility()
    {
        if (_readerNavigationTabs is null)
            return;

        var readerVisible =
            ActiveDocument is not null &&
            _session is not null &&
            _zplDocument is null &&
            NavigationBar.Visibility == Visibility.Visible;
        _readerNavigationTabs.Visibility = readerVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        ApplyReaderNavigationState(readerVisible ? ActiveDocument : null);
    }

    private void QueueThumbnailRefresh()
    {
        if (!_thumbnailUiInitialized || _thumbnailRefreshQueued)
            return;

        _thumbnailRefreshQueued = true;
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            new Action(async () =>
            {
                _thumbnailRefreshQueued = false;
                if (_session is null || _zplDocument is not null)
                    return;

                try
                {
                    await RefreshThumbnailRenderWindowAsync();
                }
                catch (ObjectDisposedException)
                {
                    // Window shutdown raced a queued low-priority thumbnail refresh.
                }
            }));
    }

    private async Task RefreshThumbnailRenderWindowAsync()
    {
        if (_session is null || _readerThumbnails.Count == 0)
            return;

        var sourceSession = _session;
        var request = _thumbnailRenderScheduler.Begin();
        var requestedRange = _getThumbnailRealizedRange() ?? GetRealizedThumbnailRange();
        if (requestedRange is null)
            return;

        var first = Math.Clamp(requestedRange.Value.First, 0, _readerThumbnails.Count - 1);
        var last = Math.Clamp(requestedRange.Value.Last, 0, _readerThumbnails.Count - 1);
        if (first > last)
            (first, last) = (last, first);

        var retainedFirst = Math.Max(0, first - 1);
        var retainedLast = Math.Min(_readerThumbnails.Count - 1, last + 1);
        for (var index = 0; index < _readerThumbnails.Count; index++)
        {
            if (index < retainedFirst || index > retainedLast)
                _readerThumbnails[index].ReleaseBitmap();
        }

        var priority = new List<int>(last - first + 3);
        for (var index = first; index <= last; index++)
            priority.Add(index);
        if (first > 0)
            priority.Add(first - 1);
        if (last + 1 < _readerThumbnails.Count)
            priority.Add(last + 1);

        foreach (var pageIndex in priority)
        {
            if (!_thumbnailRenderScheduler.IsCurrent(request) || !ReferenceEquals(_session, sourceSession))
                return;

            var item = _readerThumbnails[pageIndex];
            if (item.Bitmap is not null || item.HasError)
                continue;

            item.MarkLoading();
            try
            {
                var dpi = ReaderThumbnailItem.ResolveThumbnailDpi(item.PageSize);
                var rendered = await Task.Run(() =>
                    _renderReaderThumbnail(sourceSession, pageIndex, dpi, request.CancellationToken));

                if (!_thumbnailRenderScheduler.IsCurrent(request) || !ReferenceEquals(_session, sourceSession))
                    return;

                var bitmap = CreateBitmapSource(rendered);
                if (!_thumbnailRenderScheduler.IsCurrent(request) || !ReferenceEquals(_session, sourceSession))
                    return;

                item.Publish(bitmap);
            }
            catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (_thumbnailRenderScheduler.IsCurrent(request) && ReferenceEquals(_session, sourceSession))
                    item.MarkError(ex.Message);
            }
        }
    }

    private (int First, int Last)? GetRealizedThumbnailRange()
    {
        if (_readerThumbnailList is null || _readerThumbnails.Count == 0)
            return null;

        _readerThumbnailList.ApplyTemplate();
        var panel = FindVisualDescendant<VirtualizingStackPanel>(_readerThumbnailList);
        if (panel is not null)
        {
            var first = int.MaxValue;
            var last = -1;
            foreach (UIElement child in panel.Children)
            {
                var index = _readerThumbnailList.ItemContainerGenerator.IndexFromContainer(child);
                if (index < 0)
                    continue;
                first = Math.Min(first, index);
                last = Math.Max(last, index);
            }

            if (last >= 0)
                return (first, last);
        }

        var fallback = Math.Clamp(_navigation?.CurrentPageIndex ?? 0, 0, _readerThumbnails.Count - 1);
        return (fallback, fallback);
    }

    private static T? FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                return match;
            var nested = FindVisualDescendant<T>(child);
            if (nested is not null)
                return nested;
        }
        return null;
    }

    private void ReaderThumbnailWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (e.Cancel)
            return;

        _thumbnailRenderScheduler.Dispose();
        _readerNavigationClosed = true;
        _readerBookmarkScheduler.Dispose();
        ClearReaderBookmarks();
        _readerThumbnails.Clear();
    }
}
