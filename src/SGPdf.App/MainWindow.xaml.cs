using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow : Window
{
    private PdfDocumentSession? _session;

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
            var bitmap = BitmapSource.Create(
                loaded.Rendered.PixelWidth,
                loaded.Rendered.PixelHeight,
                loaded.Rendered.Dpi,
                loaded.Rendered.Dpi,
                PixelFormats.Bgra32,
                null,
                loaded.Rendered.Pixels,
                loaded.Rendered.Stride);
            bitmap.Freeze();

            var previousSession = _session;
            _session = candidateSession;
            candidateSession = null;
            previousSession?.Dispose();

            PdfImage.Source = bitmap;
            PdfImage.Visibility = Visibility.Visible;
            EmptyStateText.Visibility = Visibility.Collapsed;
            Title = $"SG PDF Editor — {Path.GetFileName(dialog.FileName)}";
            StatusText.Text = $"{Path.GetFileName(dialog.FileName)} — página 1 de {loaded.PageCount}";
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
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _session?.Dispose();
        _session = null;
        base.OnClosed(e);
    }
}
