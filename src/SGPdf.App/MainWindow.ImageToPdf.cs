using System.IO;
using System.Windows;
using Microsoft.Win32;
using SGPdf.App.Features.Conversions;

namespace SGPdf.App;

public partial class MainWindow
{
    private Func<MainWindow, IReadOnlyList<string>?> _selectImageToPdfSources = static owner =>
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar imágenes para crear un PDF",
            Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = true
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileNames : null;
    };

    private Func<MainWindow, IReadOnlyList<string>, IReadOnlyList<string>?> _selectImageToPdfOrder = static (owner, paths) =>
    {
        var dialog = new ImageToPdfOrderDialog(paths) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.OrderedPaths : null;
    };

    private Func<MainWindow, string?> _selectImageToPdfDestination = static owner =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar como PDF",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = false,
            FileName = "imagenes.pdf"
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    };

    private async Task StartImageToPdfFromHomeAsync()
    {
        if (!CanStartBatch())
        {
            StatusText.Text = "No se pueden convertir imágenes en el estado actual del editor.";
            return;
        }

        IReadOnlyList<string>? selected = _selectImageToPdfSources(this);
        if (selected is null || selected.Count == 0)
        {
            StatusText.Text = "Conversión de imágenes a PDF cancelada.";
            return;
        }

        IReadOnlyList<string>? ordered = _selectImageToPdfOrder(this, selected);
        if (ordered is null || ordered.Count == 0)
        {
            StatusText.Text = "Conversión de imágenes a PDF cancelada.";
            return;
        }

        string? destination = _selectImageToPdfDestination(this);
        if (string.IsNullOrWhiteSpace(destination))
        {
            StatusText.Text = "Conversión de imágenes a PDF cancelada: no se eligió dónde guardar.";
            return;
        }

        await TryCreatePdfFromImagesAsync(ordered, destination);
    }

    private async Task<bool> TryCreatePdfFromImagesAsync(
        IReadOnlyList<string>? orderedPaths,
        string? destinationPdfPath)
    {
        if (!CanStartBatch())
        {
            StatusText.Text = "No se pueden convertir imágenes en el estado actual del editor.";
            return false;
        }

        if (orderedPaths is null || orderedPaths.Count == 0 || string.IsNullOrWhiteSpace(destinationPdfPath))
        {
            StatusText.Text = "Conversión de imágenes a PDF cancelada.";
            return false;
        }

        string[] paths = orderedPaths.ToArray();
        _utilityExecuting = true;
        SetBusy(true);
        StatusText.Text = "Creando PDF desde las imágenes...";

        try
        {
            await Task.Run(() => new ImageToPdfExporter().Export(paths, destinationPdfPath, CancellationToken.None));
            StatusText.Text = paths.Length == 1
                ? $"PDF creado con 1 imagen: {Path.GetFileName(destinationPdfPath)}"
                : $"PDF creado con {paths.Length} imágenes: {Path.GetFileName(destinationPdfPath)}";
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Conversión de imágenes a PDF cancelada.";
            return false;
        }
        catch (Exception exception)
        {
            StatusText.Text = $"No se pudo crear el PDF desde las imágenes: {exception.Message}";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    StatusText.Text,
                    "Imagen a PDF",
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
