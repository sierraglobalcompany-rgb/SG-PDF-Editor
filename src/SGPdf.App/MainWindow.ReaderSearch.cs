using System.Collections.Specialized;
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
    private static readonly bool ReaderSearchLoadedHandlerRegistered = RegisterReaderSearchLoadedHandler();

    private bool _readerSearchUiInitialized;
    private Border? _readerFindBar;
    private TextBox? _readerFindTextBox;
    private TextBlock? _readerFindStatusText;
    private Canvas? _readerSearchHighlightCanvas;
    private ReaderSearchCursor? _readerSearchCursor;
    private PdfTextMatch? _activeReaderSearchMatch;
    private CancellationTokenSource? _readerSearchCancellation;
    private long _readerSearchGeneration;

    private Func<PdfDocumentSession, int, string, CancellationToken, IReadOnlyList<PdfTextMatch>> _findReaderTextOnPage =
        static (session, pageIndex, query, token) => session.FindTextOnPage(pageIndex, query, token);

    private static bool RegisterReaderSearchLoadedHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ReaderSearchHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ReaderSearchHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeReaderSearchUi();
    }

    private void InitializeReaderSearchUi()
    {
        _ = ReaderSearchLoadedHandlerRegistered;
        if (_readerSearchUiInitialized)
            return;
        if (PdfScrollViewer.Parent is not Grid host || _readerContinuousSurface is null || _readerPageList is null)
            return;

        var findBar = new Border
        {
            Name = "ReaderFindBar",
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Margin = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var textBox = new TextBox
        {
            Name = "ReaderFindTextBox",
            Width = 220d,
            MinHeight = 28d,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 2, 6, 2),
            ToolTip = "Buscar texto en el PDF"
        };
        var previous = new Button
        {
            Name = "ReaderFindPreviousButton",
            Content = "↑",
            Width = 32d,
            Margin = new Thickness(6, 0, 0, 0),
            ToolTip = "Coincidencia anterior"
        };
        var next = new Button
        {
            Name = "ReaderFindNextButton",
            Content = "↓",
            Width = 32d,
            Margin = new Thickness(4, 0, 0, 0),
            ToolTip = "Coincidencia siguiente"
        };
        var close = new Button
        {
            Name = "ReaderFindCloseButton",
            Content = "×",
            Width = 32d,
            Margin = new Thickness(4, 0, 0, 0),
            ToolTip = "Cerrar búsqueda"
        };
        var status = new TextBlock
        {
            Name = "ReaderFindStatusText",
            Margin = new Thickness(8, 0, 2, 0),
            MinWidth = 82d,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.DimGray
        };

        row.Children.Add(textBox);
        row.Children.Add(previous);
        row.Children.Add(next);
        row.Children.Add(close);
        row.Children.Add(status);
        findBar.Child = row;

        var highlightCanvas = new Canvas
        {
            Name = "ReaderSearchHighlightCanvas",
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            ClipToBounds = true
        };
        Panel.SetZIndex(highlightCanvas, 20);
        _readerContinuousSurface.Children.Add(highlightCanvas);
        Panel.SetZIndex(findBar, 100);
        host.Children.Add(findBar);

        RegisterName(findBar.Name, findBar);
        RegisterName(textBox.Name, textBox);
        RegisterName(previous.Name, previous);
        RegisterName(next.Name, next);
        RegisterName(close.Name, close);
        RegisterName(status.Name, status);
        RegisterName(highlightCanvas.Name, highlightCanvas);

        _readerFindBar = findBar;
        _readerFindTextBox = textBox;
        _readerFindStatusText = status;
        _readerSearchHighlightCanvas = highlightCanvas;
        _readerSearchUiInitialized = true;

        textBox.TextChanged += ReaderFindTextBox_TextChanged;
        previous.Click += async (_, _) => await RunReaderSearchAsync(ReaderSearchDirection.Backward);
        next.Click += async (_, _) => await RunReaderSearchAsync(ReaderSearchDirection.Forward);
        close.Click += (_, _) => CloseReaderFindBar();
        PreviewKeyDown += ReaderSearchWindow_PreviewKeyDown;
        _readerPageList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(ReaderSearchReaderScrollChanged));
        _readerPageList.SizeChanged += ReaderSearchReaderSizeChanged;
        _readerPages.CollectionChanged += ReaderSearchPagesCollectionChanged;
        if (_signModeButton is not null)
            _signModeButton.Click += ReaderSearchSignMode_Click;
        Closing += ReaderSearchWindow_Closing;
    }

    private async void ReaderSearchWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (await HandleReaderSearchKeyAsync(e.Key, Keyboard.Modifiers, e.OriginalSource as DependencyObject))
            e.Handled = true;
    }

    private async Task<bool> HandleReaderSearchKeyAsync(Key key, ModifierKeys modifiers)
    {
        if (key == Key.F && (modifiers & ModifierKeys.Control) != 0)
        {
            OpenReaderFindBar();
            return true;
        }

        if (_readerFindBar?.Visibility != Visibility.Visible)
            return false;

        if (key == Key.Escape)
        {
            CloseReaderFindBar();
            return true;
        }

        if (key == Key.Enter &&
            (_readerFindTextBox?.IsKeyboardFocusWithin == true ||
             ReferenceEquals(FocusManager.GetFocusedElement(this), _readerFindTextBox)))
        {
            var direction = (modifiers & ModifierKeys.Shift) != 0
                ? ReaderSearchDirection.Backward
                : ReaderSearchDirection.Forward;
            await RunReaderSearchAsync(direction);
            return true;
        }

        return false;
    }

    private void OpenReaderFindBar()
    {
        if (_session is null || _zplDocument is not null || _signatureModeActive || _readerFindBar is null || _readerFindTextBox is null)
            return;

        _readerFindBar.Visibility = Visibility.Visible;
        FocusManager.SetFocusedElement(this, _readerFindTextBox);
        _readerFindTextBox.Focus();
        _readerFindTextBox.SelectAll();
    }

    private void CloseReaderFindBar()
    {
        CancelReaderSearch(clearQueryState: true);
        if (_readerFindBar is not null)
            _readerFindBar.Visibility = Visibility.Collapsed;
        Keyboard.ClearFocus();
    }

    private void ReaderFindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        => CancelReaderSearch(clearQueryState: true);

    private async Task RunReaderSearchAsync(ReaderSearchDirection direction)
    {
        if (_session is null || _navigation is null || _readerFindTextBox is null || _readerFindStatusText is null)
            return;

        var query = _readerFindTextBox.Text;
        if (query.Length == 0)
        {
            CancelReaderSearch(clearQueryState: true);
            return;
        }

        _readerSearchCancellation?.Cancel();
        _readerSearchCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _readerSearchCancellation = cancellation;
        var generation = Interlocked.Increment(ref _readerSearchGeneration);
        var sourceSession = _session;
        var startPageIndex = _navigation.CurrentPageIndex;
        var sourceCursor = _readerSearchCursor;
        _readerFindStatusText.Text = "Buscando...";

        ReaderSearchResult? result;
        try
        {
            result = await Task.Run(() => ReaderSearchNavigator.Find(
                query,
                startPageIndex,
                sourceCursor,
                direction,
                _navigation.PageCount,
                (pageIndex, text, token) => _findReaderTextOnPage(sourceSession, pageIndex, text, token),
                cancellation.Token));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            if (generation == _readerSearchGeneration && ReferenceEquals(_session, sourceSession))
            {
                _readerFindStatusText.Text = "Error de búsqueda";
                StatusText.Text = $"No se pudo buscar en el PDF: {ex.Message}";
            }
            return;
        }

        if (generation != _readerSearchGeneration ||
            cancellation.IsCancellationRequested ||
            !ReferenceEquals(_session, sourceSession) ||
            !string.Equals(_readerFindTextBox.Text, query, StringComparison.Ordinal))
        {
            return;
        }

        if (result is null)
        {
            _readerFindStatusText.Text = "Sin resultados";
            _readerSearchCursor = null;
            _activeReaderSearchMatch = null;
            UpdateReaderSearchHighlightOverlay();
            return;
        }

        _readerSearchCursor = result.Cursor;
        _activeReaderSearchMatch = result.Match;
        ScrollReaderToPage(result.Match.PageIndex);
        await RefreshReaderRenderWindowAsync();

        if (generation != _readerSearchGeneration ||
            cancellation.IsCancellationRequested ||
            !ReferenceEquals(_session, sourceSession))
        {
            return;
        }

        _readerFindStatusText.Text = $"Página {result.Match.PageIndex + 1}";
        UpdateReaderSearchHighlightOverlay();
    }

    private void CancelReaderSearch(bool clearQueryState)
    {
        Interlocked.Increment(ref _readerSearchGeneration);
        _readerSearchCancellation?.Cancel();
        _readerSearchCancellation?.Dispose();
        _readerSearchCancellation = null;
        if (clearQueryState)
            _readerSearchCursor = null;
        _activeReaderSearchMatch = null;
        if (_readerFindStatusText is not null)
            _readerFindStatusText.Text = string.Empty;
        UpdateReaderSearchHighlightOverlay();
    }

    private void UpdateReaderSearchHighlightOverlay()
    {
        if (_readerSearchHighlightCanvas is null)
            return;

        _readerSearchHighlightCanvas.Children.Clear();
        var match = _activeReaderSearchMatch;
        if (match is null ||
            _readerFindBar?.Visibility != Visibility.Visible ||
            _readerContinuousSurface?.Visibility != Visibility.Visible ||
            match.PageIndex < 0 ||
            match.PageIndex >= _readerGeometry.Count)
        {
            return;
        }

        var geometry = _readerGeometry[match.PageIndex];
        if (geometry.PdfWidthPoints <= 0d || geometry.PdfHeightPoints <= 0d)
            return;

        var viewport = GetReaderViewportSize();
        var pageLeft = Math.Max(0d, (viewport.Width - geometry.DisplayWidth) / 2d);
        var pageTop = geometry.Top - _readerVerticalOffset;
        var scaleX = geometry.DisplayWidth / geometry.PdfWidthPoints;
        var scaleY = geometry.DisplayHeight / geometry.PdfHeightPoints;

        foreach (var rect in match.Rects)
        {
            var width = (rect.Right - rect.Left) * scaleX;
            var height = (rect.Top - rect.Bottom) * scaleY;
            if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0d || height <= 0d)
                continue;

            var highlight = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.Gold,
                Opacity = 0.38,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(highlight, pageLeft + (rect.Left * scaleX));
            Canvas.SetTop(highlight, pageTop + ((geometry.PdfHeightPoints - rect.Top) * scaleY));
            _readerSearchHighlightCanvas.Children.Add(highlight);
        }
    }

    private void ReaderSearchReaderScrollChanged(object sender, ScrollChangedEventArgs e)
        => UpdateReaderSearchHighlightOverlay();

    private void ReaderSearchReaderSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateReaderSearchHighlightOverlay();

    private void ReaderSearchPagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
            CloseReaderFindBar();
    }

    private void ReaderSearchSignMode_Click(object sender, RoutedEventArgs e)
        => CloseReaderFindBar();

    private void ReaderSearchWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (e.Cancel)
            return;
        _readerSearchCancellation?.Cancel();
        _readerSearchCancellation?.Dispose();
        _readerSearchCancellation = null;
    }
}
