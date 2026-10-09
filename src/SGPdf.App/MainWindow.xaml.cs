using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;
using SGPdf.App.Printing;

namespace SGPdf.App;

public partial class MainWindow : Window
{
    private const double WpfDisplayDpi = 96d;
    private const double PageMarginPixels = 48d;
    private const int ResizeDebounceMilliseconds = 150;

    private readonly PdfRenderScheduler _resizeRenderScheduler = new();
    private PdfDocumentSession? _session;
    private PageNavigationState? _navigation;
    private PdfZoomState _zoom = new();
    private double _currentDpi = WpfDisplayDpi;
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();
        InitializeReaderUi();
        UpdateViewerControlsUi();
    }

    private async void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir PDF",
            Filter = "Archivos PDF (*.pdf)|*.pdf|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
            await TryOpenPdfPathAsync(dialog.FileName);
    }

    private async void PreviousPage_Click(object sender, RoutedEventArgs e)
        => await NavigateAsync(state => state.Previous());

    private async void NextPage_Click(object sender, RoutedEventArgs e)
        => await NavigateAsync(state => state.Next());

    private async void PageNumberTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RestorePageNumberText();
            Keyboard.ClearFocus();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        if (_navigation is null || _isBusy ||
            !int.TryParse(PageNumberTextBox.Text, out var pageNumber) ||
            pageNumber < 1 || pageNumber > _navigation.PageCount)
        {
            RestorePageNumberText();
            PageNumberTextBox.SelectAll();
            return;
        }

        await NavigateAsync(state => state.GoToPageNumber(pageNumber));
        RestorePageNumberText();
        PageNumberTextBox.SelectAll();
    }

    private void PageNumberTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        => RestorePageNumberText();

    private void RestorePageNumberText()
        => PageNumberTextBox.Text = (_navigation?.CurrentPageNumber ?? 1).ToString();

    private bool IsContinuousReaderActive()
        => _session is not null &&
           _navigation is not null &&
           !_signatureModeActive &&
           _zplDocument is null &&
           _readerGeometry.Count == _navigation.PageCount &&
           _readerContinuousSurface?.Visibility == Visibility.Visible;

    private async Task NavigateAsync(Func<PageNavigationState, PageNavigationState> move)
    {
        if (_session is null || _navigation is null || _isBusy)
            return;

        _resizeRenderScheduler.CancelCurrent();
        var sourceNavigation = _navigation;
        var candidateNavigation = move(sourceNavigation);
        if (candidateNavigation.CurrentPageIndex == sourceNavigation.CurrentPageIndex)
            return;

        if (IsContinuousReaderActive())
        {
            ScrollReaderToPage(candidateNavigation.CurrentPageIndex);
            await RefreshReaderRenderWindowAsync();
            return;
        }

        var sourceSession = _session;
        var sourceZoom = _zoom;
        var viewport = GetViewportSize();
        SetBusy(true);
        StatusText.Text = $"Renderizando página {candidateNavigation.CurrentPageNumber}...";

        try
        {
            var rendered = await Task.Run(() => RenderPageForView(
                sourceSession,
                candidateNavigation.CurrentPageIndex,
                sourceZoom,
                viewport.Width,
                viewport.Height));

            if (!ReferenceEquals(_session, sourceSession) ||
                !ReferenceEquals(_navigation, sourceNavigation) ||
                !ReferenceEquals(_zoom, sourceZoom))
                return;

            var bitmap = CreateBitmapSource(rendered);
            _navigation = candidateNavigation;
            _currentDpi = rendered.Dpi;
            _currentPdfDeviceTransform = rendered.DeviceTransform;
            ShowRenderedPage(bitmap);
            UpdateViewerControlsUi();
            UpdateCurrentPageStatus();
            if (_signatureModeActive)
                RefreshSignatureOverlay();
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_session, sourceSession) &&
                ReferenceEquals(_navigation, sourceNavigation) &&
                ReferenceEquals(_zoom, sourceZoom))
            {
                StatusText.Text = $"No se pudo mostrar la página {candidateNavigation.CurrentPageNumber}.";
                if (IsVisible)
                {
                    MessageBox.Show(this,
                        $"No se pudo renderizar la página {candidateNavigation.CurrentPageNumber}.\n\n{ex.Message}",
                        "SG PDF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ZoomOut_Click(object sender, RoutedEventArgs e)
        => await ApplyZoomAsync((zoom, currentDpi) => zoom.ZoomOut(currentDpi));

    private async void ZoomIn_Click(object sender, RoutedEventArgs e)
        => await ApplyZoomAsync((zoom, currentDpi) => zoom.ZoomIn(currentDpi));

    private async void ActualSize_Click(object sender, RoutedEventArgs e)
        => await ApplyZoomAsync((zoom, _) => zoom.ActualSize());

    private async void FitPage_Click(object sender, RoutedEventArgs e)
        => await ApplyZoomAsync((zoom, _) => zoom.FitPage());

    private async void FitWidth_Click(object sender, RoutedEventArgs e)
        => await ApplyZoomAsync((zoom, _) => zoom.FitWidth());

    private async Task ApplyZoomAsync(Func<PdfZoomState, double, PdfZoomState> changeZoom)
    {
        if (_session is null || _navigation is null || _isBusy)
            return;

        _resizeRenderScheduler.CancelCurrent();
        var sourceSession = _session;
        var sourceNavigation = _navigation;
        var sourceZoom = _zoom;
        var candidateZoom = changeZoom(sourceZoom, _currentDpi);

        if (ReferenceEquals(candidateZoom, sourceZoom) ||
            (sourceZoom.Mode == PdfZoomMode.Manual && candidateZoom.Mode == PdfZoomMode.Manual &&
             sourceZoom.ManualPercent == candidateZoom.ManualPercent))
            return;

        if (IsContinuousReaderActive())
        {
            _zoom = candidateZoom;
            RebuildReaderGeometry(preserveCurrentPageAnchor: true);
            await RefreshReaderRenderWindowAsync();
            return;
        }

        var viewport = GetViewportSize();
        SetBusy(true);
        StatusText.Text = "Aplicando zoom...";
        try
        {
            var rendered = await Task.Run(() => RenderPageForView(
                sourceSession,
                sourceNavigation.CurrentPageIndex,
                candidateZoom,
                viewport.Width,
                viewport.Height));

            if (!ReferenceEquals(_session, sourceSession) ||
                !ReferenceEquals(_navigation, sourceNavigation) ||
                !ReferenceEquals(_zoom, sourceZoom))
                return;

            var bitmap = CreateBitmapSource(rendered);
            _zoom = candidateZoom;
            _currentDpi = rendered.Dpi;
            _currentPdfDeviceTransform = rendered.DeviceTransform;
            ShowRenderedPage(bitmap);
            UpdateViewerControlsUi();
            UpdateCurrentPageStatus();
            if (_signatureModeActive)
                RefreshSignatureOverlay();
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_session, sourceSession) &&
                ReferenceEquals(_navigation, sourceNavigation) &&
                ReferenceEquals(_zoom, sourceZoom))
            {
                StatusText.Text = "No se pudo aplicar el zoom.";
                if (IsVisible)
                {
                    MessageBox.Show(this,
                        $"No se pudo renderizar el PDF con el zoom solicitado.\n\n{ex.Message}",
                        "SG PDF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void PdfScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_session is null || _navigation is null || _isBusy ||
            _zoom.Mode == PdfZoomMode.Manual ||
            IsContinuousReaderActive())
            return;

        var request = _resizeRenderScheduler.Begin();
        var sourceSession = _session;
        var sourceNavigation = _navigation;
        var sourceZoom = _zoom;
        try
        {
            await Task.Delay(ResizeDebounceMilliseconds, request.CancellationToken);
            if (!_resizeRenderScheduler.IsCurrent(request) || _isBusy)
                return;

            var viewport = GetViewportSize();
            var rendered = await Task.Run(() => RenderPageForView(
                sourceSession,
                sourceNavigation.CurrentPageIndex,
                sourceZoom,
                viewport.Width,
                viewport.Height,
                request.CancellationToken), request.CancellationToken);

            if (!_resizeRenderScheduler.IsCurrent(request) || _isBusy ||
                !ReferenceEquals(_session, sourceSession) ||
                !ReferenceEquals(_navigation, sourceNavigation) ||
                !ReferenceEquals(_zoom, sourceZoom))
                return;

            var bitmap = CreateBitmapSource(rendered);
            _currentDpi = rendered.Dpi;
            _currentPdfDeviceTransform = rendered.DeviceTransform;
            ShowRenderedPage(bitmap);
            UpdateViewerControlsUi();
            UpdateCurrentPageStatus();
            if (_signatureModeActive)
                RefreshSignatureOverlay();
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (_resizeRenderScheduler.IsCurrent(request) && !_isBusy &&
                ReferenceEquals(_session, sourceSession) &&
                ReferenceEquals(_navigation, sourceNavigation) &&
                ReferenceEquals(_zoom, sourceZoom))
                StatusText.Text = "No se pudo reajustar la vista al nuevo tamaño.";
        }
    }

    private (double Width, double Height) GetViewportSize()
    {
        var width = PdfScrollViewer.ViewportWidth;
        if (!double.IsFinite(width) || width <= 0d)
            width = PdfScrollViewer.ActualWidth;
        var height = PdfScrollViewer.ViewportHeight;
        if (!double.IsFinite(height) || height <= 0d)
            height = PdfScrollViewer.ActualHeight;
        return (Math.Max(1d, width), Math.Max(1d, height));
    }

    private static PdfRenderedPage RenderPageForView(
        PdfDocumentSession session,
        int pageIndex,
        PdfZoomState zoom,
        double viewportWidth,
        double viewportHeight,
        CancellationToken cancellationToken = default)
    {
        var pageSize = session.GetPageSize(pageIndex, cancellationToken);
        var dpi = zoom.ResolveDpi(pageSize.Width, pageSize.Height, viewportWidth, viewportHeight, PageMarginPixels);
        cancellationToken.ThrowIfCancellationRequested();
        return session.RenderPage(pageIndex, dpi, cancellationToken);
    }

    private static BitmapSource CreateBitmapSource(PdfRenderedPage rendered)
    {
        var bitmap = BitmapSource.Create(
            rendered.PixelWidth,
            rendered.PixelHeight,
            WpfDisplayDpi,
            WpfDisplayDpi,
            PixelFormats.Bgra32,
            null,
            rendered.Pixels,
            rendered.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private void ShowRenderedPage(BitmapSource bitmap)
    {
        PdfImage.Source = bitmap;
        PdfImage.Visibility = Visibility.Visible;
        PdfScrollViewer.Visibility = Visibility.Visible;
        EmptyStateText.Visibility = Visibility.Collapsed;
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        OpenPdfMenuItem.IsEnabled = !busy;
        UpdateViewerControlsUi();
    }

    private void UpdateViewerControlsUi()
    {
        if (_session is null || _navigation is null)
        {
            NavigationBar.Visibility = Visibility.Collapsed;
            PrintPdfMenuItem.IsEnabled = false;
            PreviousPageButton.IsEnabled = false;
            NextPageButton.IsEnabled = false;
            PageNumberTextBox.Text = "1";
            PageNumberTextBox.IsEnabled = false;
            PageCountText.Text = "de 1";
            ZoomOutButton.IsEnabled = false;
            ZoomPercentButton.IsEnabled = false;
            ZoomInButton.IsEnabled = false;
            FitPageButton.IsEnabled = false;
            FitWidthButton.IsEnabled = false;
            ZoomPercentButton.Content = "100%";
            if (_zplDocument is not null)
                ShowLegacySurfaceForZpl();
            else if (_readerContinuousSurface is not null)
                _readerContinuousSurface.Visibility = Visibility.Collapsed;
            return;
        }

        NavigationBar.Visibility = Visibility.Visible;
        LabelNavigationBar.Visibility = Visibility.Collapsed;
        PrintPdfMenuItem.IsEnabled = !_isBusy;
        PageNumberTextBox.Text = _navigation.CurrentPageNumber.ToString();
        PageNumberTextBox.IsEnabled = !_isBusy;
        PageCountText.Text = $"de {_navigation.PageCount}";
        ZoomPercentButton.Content = $"{PdfZoomState.PercentFromDpi(_currentDpi)}%";
        PreviousPageButton.IsEnabled = !_isBusy && _navigation.CanMovePrevious;
        NextPageButton.IsEnabled = !_isBusy && _navigation.CanMoveNext;
        ZoomOutButton.IsEnabled = !_isBusy && _currentDpi > WpfDisplayDpi * 0.25d + 0.01d;
        ZoomPercentButton.IsEnabled = !_isBusy;
        ZoomInButton.IsEnabled = !_isBusy && _currentDpi < WpfDisplayDpi * 4d - 0.01d;
        FitPageButton.IsEnabled = !_isBusy;
        FitWidthButton.IsEnabled = !_isBusy;
    }

    private void UpdateCurrentPageStatus()
    {
        if (_session is not null && _navigation is not null)
            StatusText.Text = $"{Path.GetFileName(_session.FilePath)} — página {_navigation.CurrentPageNumber} de {_navigation.PageCount}";
    }

    private void PrintPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || _navigation is null || _isBusy)
            return;

        _resizeRenderScheduler.CancelCurrent();
        var printDialog = new PrintDialog
        {
            CurrentPageEnabled = true,
            SelectedPagesEnabled = false,
            UserPageRangeEnabled = _navigation.PageCount > 1,
            MinPage = 1,
            MaxPage = checked((uint)_navigation.PageCount),
            PageRangeSelection = PageRangeSelection.AllPages
        };
        var printingStarted = false;
        try
        {
            if (printDialog.ShowDialog() != true)
                return;
            var range = ResolvePrintRange(printDialog, _navigation);
            SetBusy(true);
            printingStarted = true;
            StatusText.Text = "Enviando a impresora...";
            var paginator = new PdfDocumentPaginator(_session, range, printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
            printDialog.PrintDocument(paginator, $"SG PDF Editor — {Path.GetFileName(_session.FilePath)}");
            StatusText.Text = "Trabajo de impresión enviado.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo imprimir el PDF.";
            MessageBox.Show(this, $"No se pudo enviar el PDF a la impresora.\n\n{ex.Message}",
                "SG PDF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (printingStarted)
                SetBusy(false);
        }
    }

    private static PdfPrintRange ResolvePrintRange(PrintDialog dialog, PageNavigationState navigation)
        => dialog.PageRangeSelection switch
        {
            PageRangeSelection.CurrentPage => PdfPrintRange.Current(navigation.CurrentPageIndex, navigation.PageCount),
            PageRangeSelection.UserPages => PdfPrintRange.UserPages(dialog.PageRange.PageFrom, dialog.PageRange.PageTo, navigation.PageCount),
            _ => PdfPrintRange.All(navigation.PageCount)
        };

    private void Exit_Click(object sender, RoutedEventArgs e)
        => Close();

    protected override void OnClosed(EventArgs e)
    {
        _resizeRenderScheduler.Dispose();
        _readerRenderScheduler.Dispose();
        _readerPages.Clear();
        _readerPageSizes = Array.Empty<PdfPageSize>();
        _readerGeometry = Array.Empty<Features.Reader.ReaderPageGeometry>();
        _navigation = null;
        _session?.Dispose();
        _session = null;
        base.OnClosed(e);
    }
}
