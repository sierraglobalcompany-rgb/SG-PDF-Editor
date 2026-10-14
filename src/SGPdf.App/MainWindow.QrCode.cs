using System.IO;
using System.Windows;
using Microsoft.Win32;
using SGPdf.App.Features.Labels;

namespace SGPdf.App;

public partial class MainWindow
{
    private async void QrCode_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStartBatch())
        {
            StatusText.Text = "No se puede generar un QR en el estado actual del editor.";
            return;
        }

        var dialog = new QrCodeDialog
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true)
        {
            StatusText.Text = "Generación QR cancelada.";
            return;
        }

        var destinationDialog = new SaveFileDialog
        {
            Title = "Guardar código QR como",
            Filter = "Imagen PNG (*.png)|*.png",
            AddExtension = true,
            DefaultExt = ".png",
            OverwritePrompt = true,
            FileName = "codigo-qr.png"
        };
        if (destinationDialog.ShowDialog(this) != true || string.IsNullOrWhiteSpace(destinationDialog.FileName))
        {
            StatusText.Text = "Generación QR cancelada: no se seleccionó un destino.";
            return;
        }

        await TryRunQrCodeGeneratorAsync(dialog.Value, destinationDialog.FileName);
    }

    private async Task<bool> TryRunQrCodeGeneratorAsync(string value, string destinationPath)
    {
        if (!CanStartBatch())
        {
            StatusText.Text = "No se puede generar un QR en el estado actual del editor.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            StatusText.Text = "Generación QR cancelada: no se seleccionó un destino.";
            return false;
        }

        _utilityExecuting = true;
        SetBusy(true);
        StatusText.Text = "Generando código QR...";
        try
        {
            byte[] png = await new QrCodeGenerator().GeneratePngAsync(value, CancellationToken.None);
            await File.WriteAllBytesAsync(destinationPath, png);
            StatusText.Text = $"Código QR guardado: {destinationPath}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo generar el código QR: {ex.Message}";
            return false;
        }
        finally
        {
            _utilityExecuting = false;
            SetBusy(false);
        }
    }
}
