using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;
using SGPdf.App.Features.Reader;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private const double ReaderFallbackViewportWidth = 800d;
    private const double ReaderFallbackViewportHeight = 600d;

    private readonly PdfRenderScheduler _readerRenderScheduler = new();
    private readonly ObservableCollection<ReaderPageItem> _readerPages = new();
    private IReadOnlyList<PdfPageSize> _readerPageSizes = Array.Empty<PdfPageSize>();
    private IReadOnlyList<ReaderPageGeometry> _readerGeometry = Array.Empty<ReaderPageGeometry>();
    private double _readerVerticalOffset;
    private bool _readerUiInitialized;
    private bool _updatingReaderScroll;
    private Grid? _readerContinuousSurface;
    private ListBox? _readerPageList;

    private Func<PdfDocumentSession, CancellationToken, IReadOnlyList<PdfPageSize>> _getReaderPageSizes =
        static (session, token) => session.GetPageSizes(token);
    private Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage> _renderReaderPage =
        static (session, pageIndex, dpi, token) => session.RenderPage(pageIndex, dpi, token);
    private Func<Window, string, string?> _requestPdfPassword =
        static (owner, fileName) => PdfPasswordDialog.Request(owner, fileName);

    private void InitializeReaderUi()
    {
        if (_readerUiInitialized)
            return;

        _readerUiInitialized = true;
        var host = PdfScrollViewer.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el contenedor central del visor PDF.");

        var surface = new Grid
        {
            Name = "ReaderContinuousSurface",
            Visibility = Visibility.Collapsed,
            Background = Brushes.Transparent
        };
        var list = new ListBox
        {
            Name = "ReaderPageList",
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            SelectionMode = SelectionMode.Single,
            ItemsSource = _readerPages,
            ItemTemplate = BuildReaderPageTemplate()
        };

        VirtualizingPanel.SetIsVirtualizing(list, true);
        VirtualizingPanel.SetVirtualizationMode(list, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(list, ScrollUnit.Pixel);
        ScrollViewer.SetCanContentScroll(list, true);
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        list.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ReaderPageList_ScrollChanged));
        list.SizeChanged += ReaderPageList_SizeChanged;

        surface.Children.Add(list);
        Panel.SetZIndex(surface, 0);
        host.Children.Insert(0, surface);
        RegisterName(surface.Name, surface);
        RegisterName(list.Name, list);
        _readerContinuousSurface = surface;
        _readerPageList = list;

        if (_signModeButton is not null)
            _signModeButton.Click += ReaderSignModeAfterClick;
        if (_readModeButton is not null)
            _readModeButton.Click += ReaderReadModeAfterClick;
    }

    private static DataTemplate BuildReaderPageTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.White);
        border.SetValue(Border.BorderBrushProperty, Brushes.LightGray);
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetValue(FrameworkElement.MarginProperty, new Thickness(24, 0, 24, 16));
        border.SetBinding(FrameworkElement.WidthProperty, new Binding("Geometry.DisplayWidth"));
        border.SetBinding(FrameworkElement.HeightProperty, new Binding("Geometry.DisplayHeight"));

        var grid = new FrameworkElementFactory(typeof(Grid));
        var image = new FrameworkElementFactory(typeof(Image));
        image.SetValue(Image.StretchProperty, Stretch.Fill);
        image.SetBinding(Image.SourceProperty, new Binding(nameof(ReaderPageItem.Bitmap)));
        grid.AppendChild(image);

        var error = new FrameworkElementFactory(typeof(TextBlock));
        error.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        error.SetValue(TextBlock.ForegroundProperty, Brushes.DimGray);
        error.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        error.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        error.SetValue(TextBlock.MarginProperty, new Thickness(24));
        error.SetBinding(TextBlock.TextProperty, new Binding(nameof(ReaderPageItem.ErrorMessage)));
        grid.AppendChild(error);
        border.AppendChild(grid);

        return new DataTemplate(typeof(ReaderPageItem)) { VisualTree = border };
    }

    private async Task<bool> TryOpenPdfPathAsync(string path, string? password = null)
    {
        if (_organizeModeActive && !TryLeaveOrganizeModeWithGuard())
            return false;

        InitializeReaderUi();
        _resizeRenderScheduler.CancelCurrent();
        _readerRenderScheduler.CancelCurrent();
        SetBusy(true);
        var previousStatus = StatusText.Text;
        StatusText.Text = "Abriendo PDF...";
        PdfDocumentSession? candidateSession = null;
        var attemptPassword = password;
        var hasAttemptedPassword = password is not null;

        try
        {
            while (true)
            {
                (PdfDocumentSession Session, IReadOnlyList<PdfPageSize> Sizes) loaded;
                try
                {
                    var passwordForAttempt = attemptPassword;
                    loaded = await Task.Run(() =>
                    {
                        var session = PdfDocumentSession.Open(path, passwordForAttempt);
                        try
                        {
                            var sizes = _getReaderPageSizes(session, CancellationToken.None);
                            if (sizes.Count == 0)
                                throw new InvalidOperationException("El PDF no contiene páginas.");
                            return (Session: session, Sizes: sizes);
                        }
                        catch
                        {
                            session.Dispose();
                            throw;
                        }
                    });
                }
                catch (PdfDocumentOpenException ex) when (ex.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
                {
                    candidateSession?.Dispose();
                    candidateSession = null;

                    if (hasAttemptedPassword && IsVisible)
                    {
                        MessageBox.Show(
                            this,
                            "La contraseña no es correcta. Inténtalo de nuevo.",
                            "PDF protegido",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }

                    var requested = _requestPdfPassword(this, Path.GetFileName(path));
                    if (requested is null)
                    {
                        StatusText.Text = previousStatus;
                        return false;
                    }

                    attemptPassword = requested;
                    hasAttemptedPassword = true;
                    StatusText.Text = "Abriendo PDF...";
                    continue;
                }

                attemptPassword = null;
                candidateSession = loaded.Session;
                var previousSession = _session;
                _session = candidateSession;
                _navigation = new PageNavigationState(loaded.Sizes.Count);
                _zoom = new PdfZoomState();
                _zplDocument = null;
                _renderedZplLabels = Array.Empty<ZplRenderedLabel>();
                _signatureModeActive = false;
                _signatureEditState = null;
                candidateSession = null;

                _updatingReaderScroll = true;
                InitializeContinuousReader(loaded.Sizes);
                previousSession?.Dispose();

                Title = $"SG PDF Editor — {Path.GetFileName(_session.FilePath)}";
                UpdateViewerControlsUi();
                UpdateCurrentPageStatus();
                await RefreshReaderRenderWindowAsync();
                TryRecordRecentPdf(_session.FilePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            candidateSession?.Dispose();
            StatusText.Text = "No se pudo abrir el PDF.";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    $"No se pudo abrir el PDF.\n\n{ex.Message}",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return false;
        }
        finally
        {
            attemptPassword = null;
            _updatingReaderScroll = false;
            SetBusy(false);
        }
    }

    private void InitializeContinuousReader(IReadOnlyList<PdfPageSize> sizes)
    {
        ArgumentNullException.ThrowIfNull(sizes);
        _readerPageSizes = sizes.ToArray();
        var viewport = GetReaderViewportSize();
        _readerGeometry = ReaderLayoutPlanner.Build(_readerPageSizes, _zoom, viewport.Width, viewport.Height);
        _readerPages.Clear();
        foreach (var geometry in _readerGeometry)
            _readerPages.Add(new ReaderPageItem(geometry));

        _readerVerticalOffset = 0d;
        if (_readerPageList is not null)
            _readerPageList.ItemsSource = _readerPages;
        _currentDpi = _readerGeometry[0].ResolvedDpi;
        SeedEditTransformForPage(0);
        ShowContinuousReaderSurface();
    }

    private void RebuildReaderGeometry(bool preserveCurrentPageAnchor)
    {
        if (_readerPageSizes.Count == 0 || _navigation is null)
            return;

        var currentIndex = Math.Clamp(_navigation.CurrentPageIndex, 0, _readerPageSizes.Count - 1);
        var viewport = GetReaderViewportSize();
        var rebuilt = ReaderLayoutPlanner.Build(_readerPageSizes, _zoom, viewport.Width, viewport.Height);
        _readerGeometry = rebuilt;

        while (_readerPages.Count < rebuilt.Count)
            _readerPages.Add(new ReaderPageItem(rebuilt[_readerPages.Count]));
        while (_readerPages.Count > rebuilt.Count)
            _readerPages.RemoveAt(_readerPages.Count - 1);

        for (var index = 0; index < rebuilt.Count; index++)
        {
            _readerPages[index].ApplyGeometry(rebuilt[index]);
            _readerPages[index].ReleaseBitmap();
        }

        _readerVerticalOffset = preserveCurrentPageAnchor ? rebuilt[currentIndex].Top : 0d;
        _currentDpi = rebuilt[currentIndex].ResolvedDpi;
        SeedEditTransformForPage(currentIndex);
        ScrollInternalReader(_readerVerticalOffset);
        UpdateViewerControlsUi();
        UpdateCurrentPageStatus();
    }

    private async Task RefreshReaderRenderWindowAsync()
    {
        if (_session is null || _navigation is null || _readerGeometry.Count == 0)
            return;

        var sourceSession = _session;
        var sourceGeometry = _readerGeometry;
        var request = _readerRenderScheduler.Begin();
        var viewportHeight = GetReaderViewportSize().Height;
        var window = ReaderLayoutPlanner.GetRenderWindow(sourceGeometry, _readerVerticalOffset, viewportHeight);

        for (var index = 0; index < _readerPages.Count; index++)
        {
            if (index < window.FirstRetainedPageIndex || index > window.LastRetainedPageIndex)
                _readerPages[index].ReleaseBitmap();
        }

        foreach (var pageIndex in window.RenderPriority)
        {
            if (!_readerRenderScheduler.IsCurrent(request) || !ReferenceEquals(_session, sourceSession))
                return;

            var item = _readerPages[pageIndex];
            var expectedGeometry = sourceGeometry[pageIndex];
            if (item.RenderState == ReaderPageRenderState.Ready && item.Bitmap is not null)
                continue;

            item.MarkLoading();
            try
            {
                var rendered = await Task.Run(() =>
                    _renderReaderPage(sourceSession, pageIndex, expectedGeometry.ResolvedDpi, request.CancellationToken));

                if (!_readerRenderScheduler.IsCurrent(request) ||
                    !ReferenceEquals(_session, sourceSession) ||
                    !item.Geometry.Equals(expectedGeometry))
                {
                    return;
                }

                var bitmap = CreateBitmapSource(rendered);
                if (!_readerRenderScheduler.IsCurrent(request) ||
                    !ReferenceEquals(_session, sourceSession) ||
                    !item.Geometry.Equals(expectedGeometry))
                {
                    return;
                }

                item.Publish(rendered, bitmap);
                if (_navigation?.CurrentPageIndex == pageIndex)
                {
                    _currentPdfDeviceTransform = rendered.DeviceTransform;
                    PdfImage.Source = bitmap;
                }
            }
            catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (_readerRenderScheduler.IsCurrent(request) &&
                    ReferenceEquals(_session, sourceSession) &&
                    item.Geometry.Equals(expectedGeometry))
                {
                    item.MarkError(ex.Message);
                }
            }
        }
    }

    private void UpdateReaderCurrentPageFromViewport()
    {
        if (_navigation is null || _readerGeometry.Count == 0)
            return;

        var index = ReaderLayoutPlanner.FindCurrentPageIndex(
            _readerGeometry,
            _readerVerticalOffset,
            GetReaderViewportSize().Height);
        if (index != _navigation.CurrentPageIndex)
            _navigation = _navigation.GoToPageNumber(index + 1);

        _currentDpi = _readerGeometry[index].ResolvedDpi;
        SeedEditTransformForPage(index);
        UpdateViewerControlsUi();
        UpdateCurrentPageStatus();
    }

    private void ScrollReaderToPage(int pageIndex)
    {
        if (_navigation is null || _readerGeometry.Count == 0 || pageIndex < 0 || pageIndex >= _readerGeometry.Count)
            return;

        _navigation = _navigation.GoToPageNumber(pageIndex + 1);
        _readerVerticalOffset = _readerGeometry[pageIndex].Top;
        _currentDpi = _readerGeometry[pageIndex].ResolvedDpi;
        SeedEditTransformForPage(pageIndex);
        ScrollInternalReader(_readerVerticalOffset);
        UpdateViewerControlsUi();
        UpdateCurrentPageStatus();
    }

    private async Task EnsureEditSurfaceForCurrentPageAsync()
    {
        if (_session is null || _navigation is null || _readerGeometry.Count == 0)
            return;

        var pageIndex = _navigation.CurrentPageIndex;
        var item = _readerPages[pageIndex];
        BitmapSource bitmap;
        PdfPageDeviceTransform? transform;

        if (item.RenderState == ReaderPageRenderState.Ready && item.Bitmap is not null && item.DeviceTransform is not null)
        {
            bitmap = item.Bitmap;
            transform = item.DeviceTransform;
        }
        else
        {
            var sourceSession = _session;
            var geometry = _readerGeometry[pageIndex];
            var rendered = await Task.Run(() =>
                _renderReaderPage(sourceSession, pageIndex, geometry.ResolvedDpi, CancellationToken.None));
            if (!ReferenceEquals(_session, sourceSession) || _navigation?.CurrentPageIndex != pageIndex)
                return;
            bitmap = CreateBitmapSource(rendered);
            transform = rendered.DeviceTransform;
            item.Publish(rendered, bitmap);
        }

        PdfImage.Source = bitmap;
        PdfImage.Visibility = Visibility.Visible;
        _currentPdfDeviceTransform = transform;
        _readerContinuousSurface!.Visibility = Visibility.Collapsed;
        PdfScrollViewer.Visibility = Visibility.Visible;
        EmptyStateText.Visibility = Visibility.Collapsed;
    }

    private void SeedEditTransformForPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= _readerGeometry.Count)
            return;

        var item = pageIndex < _readerPages.Count ? _readerPages[pageIndex] : null;
        if (item?.DeviceTransform is PdfPageDeviceTransform readyTransform)
        {
            _currentPdfDeviceTransform = readyTransform;
            if (item.Bitmap is not null)
                PdfImage.Source = item.Bitmap;
            return;
        }

        var geometry = _readerGeometry[pageIndex];
        var width = Math.Max(1, checked((int)Math.Ceiling(geometry.DisplayWidth)));
        var height = Math.Max(1, checked((int)Math.Ceiling(geometry.DisplayHeight)));
        _currentPdfDeviceTransform = new PdfPageDeviceTransform(
            0d,
            geometry.PdfHeightPoints,
            geometry.PdfWidthPoints / width,
            0d,
            0d,
            -geometry.PdfHeightPoints / height,
            width,
            height);
    }

    private void ShowContinuousReaderSurface()
    {
        InitializeReaderUi();
        _readerContinuousSurface!.Visibility = Visibility.Visible;
        PdfScrollViewer.Visibility = Visibility.Collapsed;
        PdfImage.Visibility = Visibility.Collapsed;
        LabelSheetCanvas.Visibility = Visibility.Collapsed;
        LabelNavigationBar.Visibility = Visibility.Collapsed;
        EmptyStateText.Visibility = Visibility.Collapsed;
        if (_signatureOverlayCanvas is not null)
            _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
    }

    private void ShowLegacySurfaceForZpl()
    {
        if (_readerContinuousSurface is not null)
            _readerContinuousSurface.Visibility = Visibility.Collapsed;
        PdfScrollViewer.Visibility = Visibility.Visible;
        if (_signatureOverlayCanvas is not null)
            _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
        if (_signaturePropertiesPanel is not null)
            _signaturePropertiesPanel.Visibility = Visibility.Collapsed;
        _signatureModeActive = false;
    }

    private async void ReaderSignModeAfterClick(object sender, RoutedEventArgs e)
    {
        if (!_signatureModeActive || _session is null || _navigation is null || _readerGeometry.Count == 0)
            return;

        _signatureModeActive = false;
        if (_signatureOverlayCanvas is not null)
            _signatureOverlayCanvas.Visibility = Visibility.Collapsed;

        try
        {
            await EnsureEditSurfaceForCurrentPageAsync();
            if (_session is null || _navigation is null)
                return;
            _signatureModeActive = true;
            _signatureOverlayCanvas!.Visibility = Visibility.Visible;
            _signaturePropertiesPanel!.Visibility = Visibility.Visible;
            PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
            RefreshSignatureOverlay();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo preparar la página para firmar: {ex.Message}";
            ShowContinuousReaderSurface();
        }
    }

    private async void ReaderReadModeAfterClick(object sender, RoutedEventArgs e)
    {
        if (_signatureModeActive || _session is null || _zplDocument is not null || _readerGeometry.Count == 0)
            return;

        ShowContinuousReaderSurface();
        UpdateReaderCurrentPageFromViewport();
        await RefreshReaderRenderWindowAsync();
    }

    private async void ReaderPageList_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_updatingReaderScroll || _readerContinuousSurface?.Visibility != Visibility.Visible || _session is null)
            return;

        _readerVerticalOffset = Math.Max(0d, e.VerticalOffset);
        UpdateReaderCurrentPageFromViewport();
        await RefreshReaderRenderWindowAsync();
    }

    private async void ReaderPageList_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_readerContinuousSurface?.Visibility != Visibility.Visible ||
            _session is null ||
            _navigation is null ||
            _zoom.Mode == PdfZoomMode.Manual)
        {
            return;
        }

        RebuildReaderGeometry(preserveCurrentPageAnchor: true);
        await RefreshReaderRenderWindowAsync();
    }

    private (double Width, double Height) GetReaderViewportSize()
    {
        var width = _readerPageList?.ActualWidth ?? 0d;
        var height = _readerPageList?.ActualHeight ?? 0d;
        if (!double.IsFinite(width) || width <= 0d)
            width = _readerPageList?.Width > 0d && double.IsFinite(_readerPageList.Width)
                ? _readerPageList.Width
                : ReaderFallbackViewportWidth;
        if (!double.IsFinite(height) || height <= 0d)
            height = _readerPageList?.Height > 0d && double.IsFinite(_readerPageList.Height)
                ? _readerPageList.Height
                : ReaderFallbackViewportHeight;
        return (Math.Max(1d, width), Math.Max(1d, height));
    }

    private void ScrollInternalReader(double offset)
    {
        if (_readerPageList is null)
            return;

        _readerPageList.ApplyTemplate();
        var scrollViewer = FindDescendantScrollViewer(_readerPageList);
        if (scrollViewer is null)
            return;

        _updatingReaderScroll = true;
        try
        {
            scrollViewer.ScrollToVerticalOffset(Math.Max(0d, offset));
        }
        finally
        {
            _updatingReaderScroll = false;
        }
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is ScrollViewer scrollViewer)
                return scrollViewer;
            var nested = FindDescendantScrollViewer(child);
            if (nested is not null)
                return nested;
        }
        return null;
    }
}
