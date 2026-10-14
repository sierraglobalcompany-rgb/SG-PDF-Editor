using Microsoft.Win32;
using SGPdf.App.Features.Conversions;

namespace SGPdf.App;

public partial class MainWindow
{
    private async void ExportPdfToPng_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede convertir el PDF en el estado actual del editor.";
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta para exportar PNG",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true || string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            StatusText.Text = "Exportación PDF a PNG cancelada.";
            return;
        }

        await TryExportPdfToPngAsync(dialog.FolderName);
    }

    private async Task<bool> TryExportPdfToPngAsync(string? outputDirectory)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede convertir el PDF en el estado actual del editor.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            StatusText.Text = "Exportación PDF a PNG cancelada: no se seleccionó una carpeta.";
            return false;
        }

        _utilityExecuting = true;
        SetBusy(true);
        StatusText.Text = "Exportando PDF a PNG...";

        try
        {
            string sourcePath = _session!.FilePath;
            IReadOnlyList<string> outputs = await Task.Run(() =>
                new PdfToPngExporter().ExportAllPages(
                    sourcePath,
                    outputDirectory,
                    CancellationToken.None));

            StatusText.Text = outputs.Count == 1
                ? $"1 página exportada a PNG en: {outputDirectory}"
                : $"{outputs.Count} páginas exportadas a PNG en: {outputDirectory}";
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Exportación PDF a PNG cancelada.";
            return false;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo exportar el PDF a PNG: {ex.Message}";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    StatusText.Text,
                    "Exportar PDF a PNG",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return false;
        }
        finally
        {
            _utilityExecuting = false;
            SetBusy(false);
        }
    }
}
