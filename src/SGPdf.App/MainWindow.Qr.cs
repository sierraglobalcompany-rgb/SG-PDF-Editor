using System.IO;
using System.Windows;
using Microsoft.Win32;
using SGPdf.App.Features.Labels;

namespace SGPdf.App;

public partial class MainWindow
{
    private async void QrCode_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || _utilityExecuting)
        {
            StatusText.Text = "No se puede generar el código QR mientras hay otra operación en curso.";
            return;
        }

        var inputDialog = new QrCodeDialog
        {
            Owner = this
        };
        if (inputDialog.ShowDialog() != true || inputDialog.Value is null)
        {
            StatusText.Text = "Generación QR cancelada.";
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Guardar código QR como PNG",
            Filter = "Archivo PNG (*.png)|*.png",
            AddExtension = true,
            DefaultExt = ".png",
            OverwritePrompt = true,
            FileName = "codigo-qr.png"
        };
        if (saveDialog.ShowDialog(this) != true || string.IsNullOrWhiteSpace(saveDialog.FileName))
        {
            StatusText.Text = "Generación QR cancelada: no se seleccionó un destino.";
            return;
        }

        _utilityExecuting = true;
        SetBusy(true);
        StatusText.Text = "Generando código QR...";
        try
        {
            byte[] pngBytes = await new QrCodeGenerator().GeneratePngAsync(
                inputDialog.Value,
                CancellationToken.None);
            await File.WriteAllBytesAsync(saveDialog.FileName, pngBytes);
            StatusText.Text = $"Código QR guardado: {saveDialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo generar el código QR: {ex.Message}";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    StatusText.Text,
                    "Generar código QR",
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
