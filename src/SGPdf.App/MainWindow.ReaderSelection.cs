using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool ReaderSelectionLoadedHandlerRegistered = RegisterReaderSelectionLoadedHandler();

    private bool _readerSelectionUiInitialized;
    private Canvas? _readerSelectionHighlightCanvas;
    private ReaderTextSelection? _readerTextSelection;
    private int? _readerSelectionAnchorPageIndex;
    private int _readerSelectionAnchorCharIndex = -1;
    private bool _readerSelectionGestureActive;

    private Func<PdfDocumentSession, int, double, double, double, double, CancellationToken, int> _getReaderCharacterIndexAtPoint =
        static (session, pageIndex, pdfX, pdfY, xTolerance, yTolerance, token) =>
            session.GetCharacterIndexAtPoint(pageIndex, pdfX, pdfY, xTolerance, yTolerance, token);
    private Func<PdfDocumentSession, int, int, int, CancellationToken, IReadOnlyList<PdfTextRect>> _getReaderTextRangeRects =
        static (session, pageIndex, startIndex, count, token) =>
            session.GetTextRangeRects(pageIndex, startIndex, count, token);
    private Func<PdfDocumentSession, int, int, int, CancellationToken, string> _getReaderTextRange =
        static (session, pageIndex, startIndex, count, token) =>
            session.GetTextRange(pageIndex, startIndex, count, token);
    private Action<string> _setReaderClipboardText = static text => Clipboard.SetText(text);

    private static bool RegisterReaderSelectionLoadedHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ReaderSelectionHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ReaderSelectionHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeReaderSelectionUi();
    }

    private void InitializeReaderSelectionUi()
    {
        _ = ReaderSelectionLoadedHandlerRegistered;
        if (_readerSelectionUiInitialized || _readerContinuousSurface is null || _readerPageList is null)
            return;

        var highlightCanvas = new Canvas
        {
            Name = "ReaderSelectionHighlightCanvas",
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            ClipToBounds = true
        };
        Panel.SetZIndex(highlightCanvas, 15);
        _readerContinuousSurface.Children.Add(highlightCanvas);
        RegisterName(highlightCanvas.Name, highlightCanvas);

        _readerSelectionHighlightCanvas = highlightCanvas;
        _readerSelectionUiInitialized = true;

        _readerPageList.PreviewMouseLeftButtonDown += ReaderSelectionMouseLeftButtonDown;
        _readerPageList.PreviewMouseMove += ReaderSelectionMouseMove;
        _readerPageList.PreviewMouseLeftButtonUp += ReaderSelectionMouseLeftButtonUp;
        _readerPageList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(ReaderSelectionReaderScrollChanged));
        _readerPageList.SizeChanged += ReaderSelectionReaderSizeChanged;
        _readerPages.CollectionChanged += ReaderSelectionPagesCollectionChanged;
        foreach (var page in _readerPages)
            page.PropertyChanged += ReaderSelectionPage_PropertyChanged;
        PreviewKeyDown += ReaderSelectionWindow_PreviewKeyDown;
        if (_signModeButton is not null)
            _signModeButton.Click += ReaderSelectionSignMode_Click;
    }

    private void ReaderSelectionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_readerPageList is null)
            return;

        var point = e.GetPosition(_readerPageList);
        if (!TryGetReaderTextHit(point, out var pageIndex, out var charIndex))
        {
            ClearReaderTextSelection();
            return;
        }

        if (BeginReaderTextSelection(pageIndex, charIndex))
        {
            _readerPageList.CaptureMouse();
            e.Handled = true;
        }
    }

    private void ReaderSelectionMouseMove(object sender, MouseEventArgs e)
    {
        if (!_readerSelectionGestureActive || e.LeftButton != MouseButtonState.Pressed || _readerPageList is null)
            return;

        var point = e.GetPosition(_readerPageList);
        if (TryGetReaderTextHit(point, out var pageIndex, out var charIndex) &&
            _readerSelectionAnchorPageIndex == pageIndex)
        {
            UpdateReaderTextSelectionFromAnchor(pageIndex, charIndex);
        }
        else
        {
            _readerTextSelection = null;
            UpdateReaderSelectionOverlay();
        }

        e.Handled = true;
    }

    private void ReaderSelectionMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_readerSelectionGestureActive || _readerPageList is null)
            return;

        var point = e.GetPosition(_readerPageList);
        if (TryGetReaderTextHit(point, out var pageIndex, out var charIndex))
            CompleteReaderTextSelection(pageIndex, charIndex);
        else
            CompleteReaderTextSelection(-1, -1);

        _readerPageList.ReleaseMouseCapture();
        e.Handled = true;
    }

    private bool BeginReaderTextSelection(int pageIndex, int charIndex)
    {
        if (_session is null ||
            _signatureModeActive ||
            _zplDocument is not null ||
            pageIndex < 0 ||
            pageIndex >= _readerPages.Count ||
            charIndex < 0)
        {
            ClearReaderTextSelection();
            return false;
        }

        _readerTextSelection = null;
        _readerSelectionAnchorPageIndex = pageIndex;
        _readerSelectionAnchorCharIndex = charIndex;
        _readerSelectionGestureActive = true;
        UpdateReaderSelectionOverlay();
        return true;
    }

    private bool CompleteReaderTextSelection(int pageIndex, int charIndex)
    {
        if (!_readerSelectionGestureActive || _readerSelectionAnchorPageIndex is null)
            return false;

        var anchorPageIndex = _readerSelectionAnchorPageIndex.Value;
        var anchorCharIndex = _readerSelectionAnchorCharIndex;
        _readerSelectionGestureActive = false;
        _readerSelectionAnchorPageIndex = null;
        _readerSelectionAnchorCharIndex = -1;

        if (pageIndex != anchorPageIndex || charIndex < 0)
        {
            _readerTextSelection = null;
            UpdateReaderSelectionOverlay();
            return false;
        }

        return PublishReaderTextSelection(anchorPageIndex, anchorCharIndex, charIndex);
    }

    private bool UpdateReaderTextSelectionFromAnchor(int pageIndex, int charIndex)
    {
        if (!_readerSelectionGestureActive ||
            _readerSelectionAnchorPageIndex != pageIndex ||
            _readerSelectionAnchorCharIndex < 0 ||
            charIndex < 0)
        {
            return false;
        }

        return PublishReaderTextSelection(pageIndex, _readerSelectionAnchorCharIndex, charIndex);
    }

    private bool PublishReaderTextSelection(int pageIndex, int firstCharIndex, int lastCharIndex)
    {
        if (_session is null)
            return false;

        var range = ReaderTextSelectionRange.Normalize(firstCharIndex, lastCharIndex);
        if (range is null)
        {
            _readerTextSelection = null;
            UpdateReaderSelectionOverlay();
            return false;
        }

        try
        {
            var rects = _getReaderTextRangeRects(
                _session,
                pageIndex,
                range.Value.StartIndex,
                range.Value.CharacterCount,
                CancellationToken.None);
            _readerTextSelection = new ReaderTextSelection(
                pageIndex,
                range.Value.StartIndex,
                range.Value.CharacterCount,
                rects.ToArray());
            UpdateReaderSelectionOverlay();
            return true;
        }
        catch (Exception ex)
        {
            _readerTextSelection = null;
            UpdateReaderSelectionOverlay();
            StatusText.Text = $"No se pudo seleccionar texto: {ex.Message}";
            return false;
        }
    }

    private void ClearReaderTextSelection()
    {
        _readerTextSelection = null;
        _readerSelectionAnchorPageIndex = null;
        _readerSelectionAnchorCharIndex = -1;
        _readerSelectionGestureActive = false;
        if (_readerSelectionHighlightCanvas is not null)
            _readerSelectionHighlightCanvas.Children.Clear();
    }

    private bool HandleReaderSelectionKey(Key key, ModifierKeys modifiers)
    {
        if (key != Key.C || (modifiers & ModifierKeys.Control) == 0)
            return false;
        if (_session is null || _readerTextSelection is null || _signatureModeActive || _zplDocument is not null)
            return false;

        var selection = _readerTextSelection;
        try
        {
            var text = _getReaderTextRange(
                _session,
                selection.PageIndex,
                selection.StartIndex,
                selection.CharacterCount,
                CancellationToken.None);
            if (text.Length > 0)
                _setReaderClipboardText(text);
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo copiar el texto: {ex.Message}";
            return true;
        }
    }

    private void ReaderSelectionWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (HandleReaderSelectionKey(e.Key, Keyboard.Modifiers))
            e.Handled = true;
    }

    private bool TryGetReaderTextHit(Point viewportPoint, out int pageIndex, out int charIndex)
    {
        pageIndex = -1;
        charIndex = -1;
        if (_session is null ||
            _readerContinuousSurface?.Visibility != Visibility.Visible ||
            _readerGeometry.Count == 0)
        {
            return false;
        }

        var viewport = GetReaderViewportSize();
        for (var index = 0; index < _readerPages.Count; index++)
        {
            var item = _readerPages[index];
            var geometry = item.Geometry;
            var pageTop = geometry.Top - _readerVerticalOffset;
            var pageLeft = Math.Max(0d, (viewport.Width - geometry.DisplayWidth) / 2d);
            if (viewportPoint.X < pageLeft || viewportPoint.X > pageLeft + geometry.DisplayWidth ||
                viewportPoint.Y < pageTop || viewportPoint.Y > pageTop + geometry.DisplayHeight ||
                item.DeviceTransform is not PdfPageDeviceTransform transform)
            {
                continue;
            }

            var deviceX = ((viewportPoint.X - pageLeft) / geometry.DisplayWidth) * transform.DeviceWidth;
            var deviceY = ((viewportPoint.Y - pageTop) / geometry.DisplayHeight) * transform.DeviceHeight;
            var pdfX = transform.OriginX + (deviceX * transform.XAxisX) + (deviceY * transform.YAxisX);
            var pdfY = transform.OriginY + (deviceX * transform.XAxisY) + (deviceY * transform.YAxisY);
            var xTolerance = Math.Max(1d, Math.Sqrt((transform.XAxisX * transform.XAxisX) + (transform.XAxisY * transform.XAxisY)) * 2d);
            var yTolerance = Math.Max(1d, Math.Sqrt((transform.YAxisX * transform.YAxisX) + (transform.YAxisY * transform.YAxisY)) * 2d);

            var hit = _getReaderCharacterIndexAtPoint(
                _session,
                index,
                pdfX,
                pdfY,
                xTolerance,
                yTolerance,
                CancellationToken.None);
            if (hit < 0)
                return false;

            pageIndex = index;
            charIndex = hit;
            return true;
        }

        return false;
    }

    private void UpdateReaderSelectionOverlay()
    {
        if (_readerSelectionHighlightCanvas is null)
            return;

        _readerSelectionHighlightCanvas.Children.Clear();
        var selection = _readerTextSelection;
        if (selection is null ||
            _readerContinuousSurface?.Visibility != Visibility.Visible ||
            selection.PageIndex < 0 ||
            selection.PageIndex >= _readerPages.Count)
        {
            return;
        }

        var geometry = _readerPages[selection.PageIndex].Geometry;
        if (geometry.PdfWidthPoints <= 0d || geometry.PdfHeightPoints <= 0d)
            return;

        var viewport = GetReaderViewportSize();
        var pageLeft = Math.Max(0d, (viewport.Width - geometry.DisplayWidth) / 2d);
        var pageTop = geometry.Top - _readerVerticalOffset;
        var scaleX = geometry.DisplayWidth / geometry.PdfWidthPoints;
        var scaleY = geometry.DisplayHeight / geometry.PdfHeightPoints;

        foreach (var rect in selection.PdfRects)
        {
            var width = (rect.Right - rect.Left) * scaleX;
            var height = (rect.Top - rect.Bottom) * scaleY;
            if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0d || height <= 0d)
                continue;

            var highlight = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.DodgerBlue,
                Opacity = 0.28,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(highlight, pageLeft + (rect.Left * scaleX));
            Canvas.SetTop(highlight, pageTop + ((geometry.PdfHeightPoints - rect.Top) * scaleY));
            _readerSelectionHighlightCanvas.Children.Add(highlight);
        }
    }

    private void ReaderSelectionReaderScrollChanged(object sender, ScrollChangedEventArgs e)
        => UpdateReaderSelectionOverlay();

    private void ReaderSelectionReaderSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateReaderSelectionOverlay();

    private void ReaderSelectionPagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ReaderPageItem page in e.OldItems)
                page.PropertyChanged -= ReaderSelectionPage_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (ReaderPageItem page in e.NewItems)
                page.PropertyChanged += ReaderSelectionPage_PropertyChanged;
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
            ClearReaderTextSelection();
    }

    private void ReaderSelectionPage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReaderPageItem.Geometry) &&
            sender is ReaderPageItem page &&
            _readerTextSelection?.PageIndex == page.PageIndex)
        {
            UpdateReaderSelectionOverlay();
        }
    }

    private void ReaderSelectionSignMode_Click(object sender, RoutedEventArgs e)
        => ClearReaderTextSelection();
}
