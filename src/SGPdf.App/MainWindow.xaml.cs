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
    private PdfDocumentSession? _session;
    private PageNavigationState? _navigation;
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();
        UpdateNavigationUi();
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
                    var rendered = session.RenderPage(0, 96d);
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

            var previousSession = _session;
            _session = candidateSession;
            _navigation = candidateNavigation;
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
        var candidateNavigation = move(sourceNavigation);

        if (candidateNavigation.CurrentPageIndex == sourceNavigation.CurrentPageIndex)
            return;

        SetBusy(true);
        StatusText.Text = $"Renderizando página {candidateNavigation.CurrentPageNumber}...";

        try
        {
            var rendered = await Task.Run(() =>
                sourceSession.RenderPage(candidateNavigation.CurrentPageIndex, 96d));

            if (!ReferenceEquals(_session, sourceSession) ||
                !ReferenceEquals(_navigation, sourceNavigation))
            {
                return;
            }

            var bitmap = CreateBitmapSource(rendered);
            _navigation = candidateNavigation;
            ShowRenderedPage(bitmap);
            UpdateCurrentPageStatus();
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_session, sourceSession) &&
                ReferenceEquals(_navigation, sourceNavigation))
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

    private static BitmapSource CreateBitmapSource(PdfRenderedPage rendered)
    {
        var bitmap = BitmapSource.Create(
            rendered.PixelWidth,
            rendered.PixelHeight,
            rendered.Dpi,
            rendered.Dpi,
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
        UpdateNavigationUi();
    }

    private void UpdateNavigationUi()
    {
        if (_session is null || _navigation is null)
        {
            NavigationBar.Visibility = Visibility.Collapsed;
            PreviousPageButton.IsEnabled = false;
            NextPageButton.IsEnabled = false;
            PageIndicatorText.Text = "Página 1 de 1";
            return;
        }

        NavigationBar.Visibility = Visibility.Visible;
        PageIndicatorText.Text = $"Página {_navigation.CurrentPageNumber} de {_navigation.PageCount}";
        PreviousPageButton.IsEnabled = !_isBusy && _navigation.CanMovePrevious;
        NextPageButton.IsEnabled = !_isBusy && _navigation.CanMoveNext;
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
