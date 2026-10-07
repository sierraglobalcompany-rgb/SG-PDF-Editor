using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow : Window
{
    private const double WpfDisplayDpi = 96d;
    private const double PageMarginPixels = 48d;

    private PdfDocumentSession? _session;
    private PageNavigationState? _navigation;
    private PdfZoomState _zoom = new();
    private double _currentDpi = WpfDisplayDpi;
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();
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

        if (dialog.ShowDialog(this) != true)
            return;

        SetBusy(true);
        StatusText.Text = "Abriendo PDF...";
        PdfDocumentSession? candidateSession = null;

        try
        {
            var loaded = await Task.Run(() =>
            {
                var session = PdfDocumentSession.Open(dialog.FileName);
                try
                {
                    var pageCount = session.PageCount;
                    var rendered = session.RenderPage(0, WpfDisplayDpi);
                    return (Session: session, PageCount: pageCount, Rendered: rendered);
                }
                catch
                {
                    session.Dispose();
                    throw;
                }
            });

            candidateSession = loaded.Session;
            var bitmap = CreateBitmapSource(loaded.Rendered);
            var candidateNavigation = new PageNavigationState(loaded.PageCount);
            var candidateZoom = new PdfZoomState();

            var previousSession = _session;
            _session = candidateSession;
            _navigation = candidateNavigation;
            _zoom = candidateZoom;
            _currentDpi = loaded.Rendered.Dpi;
            candidateSession = null;
            previousSession?.Dispose();

            ShowRenderedPage(bitmap);
            Title = $"SG PDF Editor — {Path.GetFileName(_session.FilePath)}";
            UpdateCurrentPageStatus();
        }
        catch (Exception ex)
        {
            candidateSession?.Dispose();
            StatusText.Text = "No se pudo abrir el PDF.";
            MessageBox.Show(
                this,
                $"No se pudo abrir o renderizar el PDF.\n\n{ex.Message}",
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        await NavigateAsync(state => state.Previous());
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        await NavigateAsync(state => state.Next());
    }

    private async Task NavigateAsync(Func<PageNavigationState, PageNavigationState> move)
    {
        if (_session is null || _navigation is null || _isBusy)
            return;

        var sourceSession = _session;
        var sourceNavigation = _navigation;
        var sourceZoom = _zoom;
        var candidateNavigation = move(sourceNavigation);

        if (candidateNavigation.CurrentPageIndex == sourceNavigation.CurrentPageIndex)
            return;

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
            {
                return;
            }

            var bitmap = CreateBitmapSource(rendered);
            _navigation = candidateNavigation;
            _currentDpi = rendered.Dpi;
            ShowRenderedPage(bitmap);
            UpdateCurrentPageStatus();
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_session, sourceSession) &&
                ReferenceEquals(_navigation, sourceNavigation) &&
                ReferenceEquals(_zoom, sourceZoom))
            {
                StatusText.Text = $"No se pudo mostrar la página {candidateNavigation.CurrentPageNumber}.";
                MessageBox.Show(
                    this,
                    $"No se pudo renderizar la página {candidateNavigation.CurrentPageNumber}.\n\n{ex.Message}",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        await ApplyZoomAsync((zoom, currentDpi) => zoom.ZoomOut(currentDpi));
    }

    private async void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        await ApplyZoomAsync((zoom, currentDpi) => zoom.ZoomIn(currentDpi));
    }

    private async void ActualSize_Click(object sender, RoutedEventArgs e)
    {
        await ApplyZoomAsync((zoom, _) => zoom.ActualSize());
    }

    private async void FitPage_Click(object sender, RoutedEventArgs e)
    {
        await ApplyZoomAsync((zoom, _) => zoom.FitPage());
    }

    private async void FitWidth_Click(object sender, RoutedEventArgs e)
    {
        await ApplyZoomAsync((zoom, _) => zoom.FitWidth());
    }

    private async Task ApplyZoomAsync(Func<PdfZoomState, double, PdfZoomState> changeZoom)
    {
        if (_session is null || _navigation is null || _isBusy)
            return;

        var sourceSession = _session;
        var sourceNavigation = _navigation;
        var sourceZoom = _zoom;
        var candidateZoom = changeZoom(sourceZoom, _currentDpi);

        if (ReferenceEquals(candidateZoom, sourceZoom) ||
            (sourceZoom.Mode == PdfZoomMode.Manual &&
             candidateZoom.Mode == PdfZoomMode.Manual &&
             sourceZoom.ManualPercent == candidateZoom.ManualPercent))
        {
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
            {
                return;
            }

            var bitmap = CreateBitmapSource(rendered);
            _zoom = candidateZoom;
            _currentDpi = rendered.Dpi;
            ShowRenderedPage(bitmap);
            UpdateCurrentPageStatus();
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_session, sourceSession) &&
                ReferenceEquals(_navigation, sourceNavigation) &&
                ReferenceEquals(_zoom, sourceZoom))
            {
                StatusText.Text = "No se pudo aplicar el zoom.";
                MessageBox.Show(
                    this,
                    $"No se pudo renderizar el PDF con el zoom solicitado.\n\n{ex.Message}",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            SetBusy(false);
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
        double viewportHeight)
    {
        var pageSize = session.GetPageSize(pageIndex);
        var dpi = zoom.ResolveDpi(
            pageSize.Width,
            pageSize.Height,
            viewportWidth,
            viewportHeight,
            PageMarginPixels);

        return session.RenderPage(pageIndex, dpi);
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
            PreviousPageButton.IsEnabled = false;
            NextPageButton.IsEnabled = false;
            ZoomOutButton.IsEnabled = false;
            ZoomPercentButton.IsEnabled = false;
            ZoomInButton.IsEnabled = false;
            FitPageButton.IsEnabled = false;
            FitWidthButton.IsEnabled = false;
            PageIndicatorText.Text = "Página 1 de 1";
            ZoomPercentButton.Content = "100%";
            return;
        }

        NavigationBar.Visibility = Visibility.Visible;
        PageIndicatorText.Text = $"Página {_navigation.CurrentPageNumber} de {_navigation.PageCount}";
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
        if (_session is null || _navigation is null)
            return;

        StatusText.Text = $"{Path.GetFileName(_session.FilePath)} — página {_navigation.CurrentPageNumber} de {_navigation.PageCount}";
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _navigation = null;
        _session?.Dispose();
        _session = null;
        base.OnClosed(e);
    }
}
