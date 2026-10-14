using System.Windows;
using Microsoft.Win32;
using SGPdf.App.Features.Conversions;

namespace SGPdf.App;

public partial class MainWindow
{
    private async void ConvertPdfToPng_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede convertir el PDF en el estado actual del editor.";
            return;
        }

        var outputDialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta para las imágenes PNG"
        };
        if (outputDialog.ShowDialog(this) != true || string.IsNullOrWhiteSpace(outputDialog.FolderName))
        {
            StatusText.Text = "Conversión cancelada.";
            return;
        }

        string sourcePath = _session!.FilePath;
        string outputDirectory = outputDialog.FolderName;
        _utilityExecuting = true;
        SetBusy(true);
        StatusText.Text = "Convirtiendo PDF a PNG...";
        try
        {
            IReadOnlyList<string> outputs = await Task.Run(() =>
                new PdfToPngConverter().Convert(sourcePath, outputDirectory, CancellationToken.None));

            StatusText.Text = outputs.Count == 1
                ? "Conversión completada: 1 PNG creado."
                : $"Conversión completada: {outputs.Count} PNG creados.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo convertir el PDF a PNG.";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Convertir PDF a PNG",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            _utilityExecuting = false;
            SetBusy(false);
        }
    }
}
