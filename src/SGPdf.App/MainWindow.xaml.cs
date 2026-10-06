using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow : Window
{
    private const double PdfPointsPerInch = 72.0;
    private const double ScreenDpi = 96.0;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;

    private PdfDocumentSession? _session;
    private CancellationTokenSource? _renderCancellation;
    private int _currentPageIndex;
    private double _zoom = 1.0;

    public MainWindow()
    {
        InitializeComponent();
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

        _renderCancellation?.Cancel();
        StatusText.Text = "Abriendo PDF...";

        try
        {
            var newSession = await Task.Run(() => PdfDocumentSession.Open(dialog.FileName));
            var previousSession = _session;

            _session = newSession;
            _currentPageIndex = 0;
            _zoom = 1.0;
            FileNameText.Text = Path.GetFileName(newSession.FilePath);
            EmptyStateText.Visibility = Visibility.Collapsed;
            SetDocumentControlsEnabled(true);
            UpdateNavigationState();

            if (previousSession is not null)
                await Task.Run(previousSession.Dispose);

            await RenderCurrentPageAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo abrir el PDF.";
            MessageBox.Show(this, ex.Message, "SG PDF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || _currentPageIndex <= 0)
            return;

        _currentPageIndex--;
        UpdateNavigationState();
        await RenderCurrentPageAsync();
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || _currentPageIndex >= _session.PageCount - 1)
            return;

        _currentPageIndex++;
        UpdateNavigationState();
        await RenderCurrentPageAsync();
    }

    private async void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null)
            return;

        _zoom = Math.Max(MinZoom, _zoom / 1.2);
        UpdateZoomText();
        await RenderCurrentPageAsync();
    }

    private async void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null)
            return;

        _zoom = Math.Min(MaxZoom, _zoom * 1.2);
        UpdateZoomText();
        await RenderCurrentPageAsync();
    }

    private async void FitWidth_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null)
            return;

        var session = _session;
        var pageIndex = _currentPageIndex;
        var pageSize = await Task.Run(() => session.GetPageSize(pageIndex));
        var baseWidth = pageSize.Width * ScreenDpi / PdfPointsPerInch;
        var availableWidth = Math.Max(100, GetViewportWidth() - 48);

        _zoom = Math.Clamp(availableWidth / baseWidth, MinZoom, MaxZoom);
        UpdateZoomText();
        await RenderCurrentPageAsync();
    }

    private async void FitPage_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null)
            return;

        var session = _session;
        var pageIndex = _currentPageIndex;
        var pageSize = await Task.Run(() => session.GetPageSize(pageIndex));
        var baseWidth = pageSize.Width * ScreenDpi / PdfPointsPerInch;
        var baseHeight = pageSize.Height * ScreenDpi / PdfPointsPerInch;
        var availableWidth = Math.Max(100, GetViewportWidth() - 48);
        var availableHeight = Math.Max(100, GetViewportHeight() - 48);

        _zoom = Math.Clamp(
            Math.Min(availableWidth / baseWidth, availableHeight / baseHeight),
            MinZoom,
            MaxZoom);

        UpdateZoomText();
        await RenderCurrentPageAsync();
    }

    private async Task RenderCurrentPageAsync()
    {
        var session = _session;
        if (session is null)
            return;

        _renderCancellation?.Cancel();
        _renderCancellation?.Dispose();
        _renderCancellation = new CancellationTokenSource();
        var token = _renderCancellation.Token;
        var pageIndex = _currentPageIndex;
        var zoom = _zoom;

        StatusText.Text = $"Renderizando página {pageIndex + 1}...";

        try
        {
            var pageSize = await Task.Run(() => session.GetPageSize(pageIndex), token);
            token.ThrowIfCancellationRequested();

            var pixelWidth = Math.Clamp(
                (int)Math.Ceiling(pageSize.Width * ScreenDpi / PdfPointsPerInch * zoom),
                1,
                8192);
            var pixelHeight = Math.Clamp(
                (int)Math.Ceiling(pageSize.Height * ScreenDpi / PdfPointsPerInch * zoom),
                1,
                8192);

            var rendered = await Task.Run(
                () => PdfPageRenderer.Render(session, pageIndex, pixelWidth, pixelHeight),
                token);

            token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(session, _session) || pageIndex != _currentPageIndex)
                return;

            var source = BitmapSource.Create(
                rendered.Width,
                rendered.Height,
                ScreenDpi,
                ScreenDpi,
                PixelFormats.Bgra32,
                palette: null,
                rendered.Pixels,
                rendered.Stride);
            source.Freeze();

            PdfImage.Source = source;
            UpdateNavigationState();
            StatusText.Text = $"Página {pageIndex + 1} de {session.PageCount}.";
        }
        catch (OperationCanceledException)
        {
            // Una solicitud más reciente reemplazó este render.
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested || !ReferenceEquals(session, _session))
        {
            // El documento anterior se cerró después de abrir uno nuevo.
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error al renderizar la página.";
            MessageBox.Show(this, ex.Message, "SG PDF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetDocumentControlsEnabled(bool enabled)
    {
        ZoomOutButton.IsEnabled = enabled;
        ZoomInButton.IsEnabled = enabled;
        FitWidthButton.IsEnabled = enabled;
        FitPageButton.IsEnabled = enabled;
    }

    private void UpdateNavigationState()
    {
        if (_session is null)
        {
            PreviousPageButton.IsEnabled = false;
            NextPageButton.IsEnabled = false;
            PageText.Text = "— / —";
            return;
        }

        var pageCount = _session.PageCount;
        PreviousPageButton.IsEnabled = _currentPageIndex > 0;
        NextPageButton.IsEnabled = _currentPageIndex < pageCount - 1;
        PageText.Text = $"{_currentPageIndex + 1} / {pageCount}";
        UpdateZoomText();
    }

    private void UpdateZoomText()
    {
        ZoomText.Text = $"{_zoom:P0}";
    }

    private double GetViewportWidth()
    {
        return PdfScrollViewer.ViewportWidth > 0 ? PdfScrollViewer.ViewportWidth : PdfScrollViewer.ActualWidth;
    }

    private double GetViewportHeight()
    {
        return PdfScrollViewer.ViewportHeight > 0 ? PdfScrollViewer.ViewportHeight : PdfScrollViewer.ActualHeight;
    }

    protected override void OnClosed(EventArgs e)
    {
        _renderCancellation?.Cancel();
        _renderCancellation?.Dispose();
        _session?.Dispose();
        base.OnClosed(e);
    }
}
