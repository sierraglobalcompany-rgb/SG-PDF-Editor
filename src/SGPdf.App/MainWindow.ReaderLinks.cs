using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private const double ReaderLinkClickTolerance = 5d;
    private static readonly bool ReaderLinksLoadedHandlerRegistered = RegisterReaderLinksLoadedHandler();

    private readonly Dictionary<int, IReadOnlyList<PdfPageLink>> _readerPageLinks = new();
    private readonly HashSet<Features.Reader.ReaderPageItem> _readerLinkObservedPages = new();
    private bool _readerLinksUiInitialized;
    private bool _readerNavigationRefreshQueued;
    private Canvas? _readerLinkOverlayCanvas;
    private TreeView? _readerBookmarksTree;
    private PdfDocumentSession? _readerNavigationMetadataSession;
    private PdfDocumentSession? _readerBookmarksLoadedSession;
    private PdfPageLink? _readerPressedLink;
    private Point _readerPressedLinkPoint;

    private Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfBookmarkNode>> _getReaderBookmarks =
        static (session, token) => session.GetBookmarks(token);
    private Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfPageLink>> _getReaderPageLinks =
        static (session, pageIndex, token) => session.GetPageLinks(pageIndex, token);
    private Func<Window, Uri, bool> _confirmExternalUri = static (owner, uri) =>
        MessageBox.Show(
            owner,
            $"Este PDF quiere abrir el siguiente enlace en tu navegador predeterminado:\n\n{uri.AbsoluteUri}\n\n¿Deseas continuar?",
            "Abrir enlace externo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;
    private Action<Uri> _openExternalUri = static uri =>
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });

    private static bool RegisterReaderLinksLoadedHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ReaderLinksHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ReaderLinksHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeReaderLinksUi();
    }

    private void InitializeReaderLinksUi()
    {
        _ = ReaderLinksLoadedHandlerRegistered;
        if (_readerLinksUiInitialized)
            return;

        InitializeReaderUi();
        InitializeReaderThumbnailsUi();
        if (_readerContinuousSurface is null || _readerPageList is null)
            return;

        _readerBookmarksTree = FindName("ReaderBookmarksTree") as TreeView;
        if (_readerBookmarksTree is not null)
        {
            _readerBookmarksTree.ItemTemplate = BuildReaderBookmarkTemplate();
            _readerBookmarksTree.SelectedItemChanged += ReaderBookmarksTree_SelectedItemChanged;
        }

        var overlay = new Canvas
        {
            Name = "ReaderLinkOverlayCanvas",
            Background = null,
            IsHitTestVisible = false,
            ClipToBounds = true
        };
        Panel.SetZIndex(overlay, 10);
        _readerContinuousSurface.Children.Add(overlay);
        RegisterName(overlay.Name, overlay);
        _readerLinkOverlayCanvas = overlay;

        _readerPages.CollectionChanged += ReaderLinksPages_CollectionChanged;
        _readerPageList.AddHandler(
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(ReaderLinks_PreviewMouseLeftButtonDown),
            handledEventsToo: true);
        _readerPageList.AddHandler(
            UIElement.PreviewMouseLeftButtonUpEvent,
            new MouseButtonEventHandler(ReaderLinks_PreviewMouseLeftButtonUp),
            handledEventsToo: true);
        _readerPageList.MouseMove += ReaderLinks_MouseMove;
        _readerPageList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(ReaderLinks_ScrollChanged));
        _readerPageList.SizeChanged += ReaderLinks_SizeChanged;

        SyncReaderLinkObservedPages();
        _readerLinksUiInitialized = true;
        QueueReaderNavigationMetadataRefresh();
    }

    private static HierarchicalDataTemplate BuildReaderBookmarkTemplate()
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(nameof(PdfBookmarkNode.Title)));
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        text.SetValue(FrameworkElement.MarginProperty, new Thickness(2));

        return new HierarchicalDataTemplate(typeof(PdfBookmarkNode))
        {
            ItemsSource = new Binding(nameof(PdfBookmarkNode.Children)),
            VisualTree = text
        };
    }

    private void ReaderLinksPages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncReaderLinkObservedPages();
        QueueReaderNavigationMetadataRefresh();
    }

    private void SyncReaderLinkObservedPages()
    {
        foreach (var item in _readerLinkObservedPages.ToArray())
        {
            if (_readerPages.Contains(item))
                continue;
            item.PropertyChanged -= ReaderLinkPage_PropertyChanged;
            _readerLinkObservedPages.Remove(item);
        }

        foreach (var item in _readerPages)
        {
            if (_readerLinkObservedPages.Add(item))
                item.PropertyChanged += ReaderLinkPage_PropertyChanged;
        }
    }

    private void ReaderLinkPage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Features.Reader.ReaderPageItem.Geometry))
            UpdateReaderLinkOverlay();
    }

    private void QueueReaderNavigationMetadataRefresh()
    {
        if (_readerNavigationRefreshQueued || !_readerLinksUiInitialized)
            return;

        _readerNavigationRefreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
        {
            _readerNavigationRefreshQueued = false;
            await RefreshReaderNavigationMetadataAsync();
        }));
    }

    private async Task RefreshReaderNavigationMetadataAsync()
    {
        var sourceSession = _session;
        if (sourceSession is null || _zplDocument is not null)
        {
            ResetReaderNavigationMetadata(null);
            return;
        }

        if (!ReferenceEquals(_readerNavigationMetadataSession, sourceSession))
            ResetReaderNavigationMetadata(sourceSession);

        if (!ReferenceEquals(_readerBookmarksLoadedSession, sourceSession))
        {
            try
            {
                var bookmarks = await Task.Run(() => _getReaderBookmarks(sourceSession, CancellationToken.None));
                if (!ReferenceEquals(_session, sourceSession))
                    return;
                if (_readerBookmarksTree is not null)
                    _readerBookmarksTree.ItemsSource = bookmarks;
                _readerBookmarksLoadedSession = sourceSession;
            }
            catch (Exception ex)
            {
                if (ReferenceEquals(_session, sourceSession))
                {
                    if (_readerBookmarksTree is not null)
                        _readerBookmarksTree.ItemsSource = Array.Empty<PdfBookmarkNode>();
                    _readerBookmarksLoadedSession = sourceSession;
                    StatusText.Text = $"No se pudieron leer los marcadores: {ex.Message}";
                }
            }
        }

        await RefreshReaderVisibleLinksAsync(sourceSession);
    }

    private async Task RefreshReaderVisibleLinksAsync(PdfDocumentSession sourceSession)
    {
        var visiblePages = GetReaderVisiblePageIndices();
        var missingPages = visiblePages.Where(index => !_readerPageLinks.ContainsKey(index)).ToArray();
        if (missingPages.Length > 0)
        {
            try
            {
                var loaded = await Task.Run(() =>
                {
                    var result = new List<(int PageIndex, IReadOnlyList<PdfPageLink> Links)>();
                    foreach (var pageIndex in missingPages)
                        result.Add((pageIndex, _getReaderPageLinks(sourceSession, pageIndex, CancellationToken.None)));
                    return result;
                });

                if (!ReferenceEquals(_session, sourceSession))
                    return;
                foreach (var item in loaded)
                    _readerPageLinks[item.PageIndex] = item.Links;
            }
            catch (Exception ex)
            {
                if (ReferenceEquals(_session, sourceSession))
                    StatusText.Text = $"No se pudieron leer algunos enlaces del PDF: {ex.Message}";
            }
        }

        if (ReferenceEquals(_session, sourceSession))
            UpdateReaderLinkOverlay();
    }

    private IReadOnlyList<int> GetReaderVisiblePageIndices()
    {
        if (_readerPages.Count == 0)
            return Array.Empty<int>();

        var viewportHeight = GetReaderViewportSize().Height;
        var viewportBottom = _readerVerticalOffset + viewportHeight;
        var visible = new List<int>();
        for (var index = 0; index < _readerPages.Count; index++)
        {
            var geometry = _readerPages[index].Geometry;
            if (geometry.Top + geometry.DisplayHeight < _readerVerticalOffset || geometry.Top > viewportBottom)
                continue;
            visible.Add(index);
        }

        if (visible.Count == 0 && _navigation is not null)
            visible.Add(Math.Clamp(_navigation.CurrentPageIndex, 0, _readerPages.Count - 1));
        return visible;
    }

    private void ResetReaderNavigationMetadata(PdfDocumentSession? session)
    {
        _readerNavigationMetadataSession = session;
        _readerBookmarksLoadedSession = null;
        _readerPageLinks.Clear();
        if (_readerBookmarksTree is not null)
            _readerBookmarksTree.ItemsSource = null;
        if (_readerLinkOverlayCanvas is not null)
            _readerLinkOverlayCanvas.Children.Clear();
    }

    private void ReaderBookmarksTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not PdfBookmarkNode bookmark || !TryActivateReaderBookmark(bookmark))
            return;
        _ = RefreshReaderRenderWindowAsync();
    }

    private bool TryActivateReaderBookmark(PdfBookmarkNode bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        if (bookmark.DestinationPageIndex is not int pageIndex ||
            pageIndex < 0 ||
            pageIndex >= _readerPages.Count ||
            _session is null ||
            _zplDocument is not null)
        {
            return false;
        }

        ClearReaderTextSelection();
        ScrollReaderToPage(pageIndex);
        QueueReaderNavigationMetadataRefresh();
        return true;
    }

    private bool TryActivateReaderLink(PdfPageLink link)
    {
        ArgumentNullException.ThrowIfNull(link);
        if (_session is null || _zplDocument is not null || _readerSelectionGestureActive)
            return false;

        if (link.ActionKind == PdfLinkActionKind.InternalGoto)
        {
            if (link.DestinationPageIndex is not int pageIndex || pageIndex < 0 || pageIndex >= _readerPages.Count)
                return false;
            ClearReaderTextSelection();
            ScrollReaderToPage(pageIndex);
            QueueReaderNavigationMetadataRefresh();
            return true;
        }

        if (link.ActionKind != PdfLinkActionKind.Uri ||
            string.IsNullOrWhiteSpace(link.Uri) ||
            !Uri.TryCreate(link.Uri, UriKind.Absolute, out var uri) ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        ClearReaderTextSelection();
        if (_confirmExternalUri(this, uri))
            _openExternalUri(uri);
        return true;
    }

    private void ReaderLinks_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_readerPageList is null)
            return;
        _readerPressedLinkPoint = e.GetPosition(_readerPageList);
        _readerPressedLink = TryGetReaderLinkAtPoint(_readerPressedLinkPoint, out var link) ? link : null;
    }

    private void ReaderLinks_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_readerPageList is null || _readerPressedLink is null)
            return;

        var released = e.GetPosition(_readerPageList);
        var dx = released.X - _readerPressedLinkPoint.X;
        var dy = released.Y - _readerPressedLinkPoint.Y;
        var candidate = _readerPressedLink;
        _readerPressedLink = null;

        if ((dx * dx) + (dy * dy) > ReaderLinkClickTolerance * ReaderLinkClickTolerance)
            return;
        if (!TryGetReaderLinkAtPoint(released, out var releasedLink) || !Equals(candidate, releasedLink))
            return;
        if (TryActivateReaderLink(candidate))
        {
            e.Handled = true;
            _ = RefreshReaderRenderWindowAsync();
        }
    }

    private void ReaderLinks_MouseMove(object sender, MouseEventArgs e)
    {
        if (_readerPageList is null || e.LeftButton == MouseButtonState.Pressed)
            return;
        _readerPageList.Cursor = TryGetReaderLinkAtPoint(e.GetPosition(_readerPageList), out _)
            ? Cursors.Hand
            : null;
    }

    private bool TryGetReaderLinkAtPoint(Point viewportPoint, out PdfPageLink link)
    {
        link = null!;
        if (_readerContinuousSurface?.Visibility != Visibility.Visible)
            return false;

        var viewport = GetReaderViewportSize();
        foreach (var pageIndex in GetReaderVisiblePageIndices())
        {
            if (!_readerPageLinks.TryGetValue(pageIndex, out var links))
                continue;
            var geometry = _readerPages[pageIndex].Geometry;
            var pageLeft = Math.Max(0d, (viewport.Width - geometry.DisplayWidth) / 2d);
            var pageTop = geometry.Top - _readerVerticalOffset;
            var scaleX = geometry.DisplayWidth / geometry.PdfWidthPoints;
            var scaleY = geometry.DisplayHeight / geometry.PdfHeightPoints;

            foreach (var candidate in links)
            {
                var left = pageLeft + candidate.Rect.Left * scaleX;
                var top = pageTop + (geometry.PdfHeightPoints - candidate.Rect.Top) * scaleY;
                var right = pageLeft + candidate.Rect.Right * scaleX;
                var bottom = pageTop + (geometry.PdfHeightPoints - candidate.Rect.Bottom) * scaleY;
                if (viewportPoint.X >= left && viewportPoint.X <= right && viewportPoint.Y >= top && viewportPoint.Y <= bottom)
                {
                    link = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateReaderLinkOverlay()
    {
        if (_readerLinkOverlayCanvas is null)
            return;
        _readerLinkOverlayCanvas.Children.Clear();
        if (_readerContinuousSurface?.Visibility != Visibility.Visible)
            return;

        var viewport = GetReaderViewportSize();
        foreach (var pageIndex in GetReaderVisiblePageIndices())
        {
            if (!_readerPageLinks.TryGetValue(pageIndex, out var links))
                continue;
            var geometry = _readerPages[pageIndex].Geometry;
            var pageLeft = Math.Max(0d, (viewport.Width - geometry.DisplayWidth) / 2d);
            var pageTop = geometry.Top - _readerVerticalOffset;
            var scaleX = geometry.DisplayWidth / geometry.PdfWidthPoints;
            var scaleY = geometry.DisplayHeight / geometry.PdfHeightPoints;

            foreach (var link in links.Where(item => item.ActionKind is PdfLinkActionKind.InternalGoto or PdfLinkActionKind.Uri))
            {
                var width = (link.Rect.Right - link.Rect.Left) * scaleX;
                var height = (link.Rect.Top - link.Rect.Bottom) * scaleY;
                if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0d || height <= 0d)
                    continue;

                var marker = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Transparent,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(marker, pageLeft + link.Rect.Left * scaleX);
                Canvas.SetTop(marker, pageTop + (geometry.PdfHeightPoints - link.Rect.Top) * scaleY);
                _readerLinkOverlayCanvas.Children.Add(marker);
            }
        }
    }

    private void ReaderLinks_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateReaderLinkOverlay();
        QueueReaderNavigationMetadataRefresh();
    }

    private void ReaderLinks_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateReaderLinkOverlay();
        QueueReaderNavigationMetadataRefresh();
    }
}
