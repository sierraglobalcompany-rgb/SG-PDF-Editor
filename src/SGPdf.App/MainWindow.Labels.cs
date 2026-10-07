using System.IO;
using System.Windows;
using Microsoft.Win32;
using SGPdf.App.Features.Labels;

namespace SGPdf.App;

public partial class MainWindow
{
    private ZplDocument? _zplDocument;

    private async void OpenZpl_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir etiquetas ZPL",
            Filter = "ZPL / TXT / PRN (*.zpl;*.txt;*.prn)|*.zpl;*.txt;*.prn|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        SetBusy(true);
        StatusText.Text = "Abriendo archivo ZPL...";

        try
        {
            var candidate = await Task.Run(() => ZplFileLoader.Load(dialog.FileName));
            CommitLoadedZpl(candidate);
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo abrir el archivo ZPL.";
            MessageBox.Show(
                this,
                $"No se pudo abrir o analizar el archivo ZPL.\n\n{ex.Message}",
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CommitLoadedZpl(ZplDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _resizeRenderScheduler.CancelCurrent();

        var previousSession = _session;
        _session = null;
        _navigation = null;
        _zplDocument = document;
        previousSession?.Dispose();

        PdfImage.Source = null;
        PdfImage.Visibility = Visibility.Collapsed;
        EmptyStateText.Text = $"Archivo ZPL cargado\n{document.Designs.Count} diseño(s) detectado(s)";
        EmptyStateText.Visibility = Visibility.Visible;

        UpdateViewerControlsUi();

        var fileName = Path.GetFileName(document.SourcePath);
        Title = $"SG PDF Editor — {fileName}";
        StatusText.Text = $"{fileName} — {document.Designs.Count} diseño(s) — cantidad total {document.TotalQuantityFromFile}";
    }

    private void PdfImage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (PdfImage.Visibility == Visibility.Visible && _session is not null)
            _zplDocument = null;
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _zplDocument = null;
    }
}
