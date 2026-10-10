using System.IO;
using System.Windows;
using Microsoft.Win32;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private Func<PdfDocumentSession, int, int, CancellationToken, ReadOnlyMemory<byte>> _extractSelectedImagePng =
        static (session, pageIndex, objectIndex, token) => session.GetImagePng(pageIndex, objectIndex, token);

    private Action<string, ReadOnlyMemory<byte>> _writeExtractedImage =
        static (path, bytes) => File.WriteAllBytes(path, bytes.ToArray());

    private Func<string?> _selectImageReplacementPath = static () =>
    {
        var dialog = new OpenFileDialog
        {
            Title = "Reemplazar imagen",
            Filter = "Imágenes compatibles (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private Func<string?> _selectImageExtractionPath = static () =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Extraer imagen como PNG",
            Filter = "Imagen PNG (*.png)|*.png",
            AddExtension = true,
            DefaultExt = ".png",
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private bool TryReplaceSelectedImageFromDialog()
        => TryReplaceSelectedImageFromPath(_selectImageReplacementPath());

    private bool TryReplaceSelectedImageFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            _imageEditWorkspace is null ||
            !TryGetSelectedImageState(out var state))
        {
            return false;
        }

        ImageReplacementAsset candidate;
        try
        {
            candidate = ImageReplacementAssetLoader.Load(path);
        }
        catch (Exception ex) when (
            ex is InvalidDataException ||
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is ArgumentException ||
            ex is NotSupportedException)
        {
            StatusText.Text = "No se pudo cargar la imagen de reemplazo.";
            ShowImageEditTaskMessage(
                $"La imagen seleccionada no se puede usar como reemplazo.\n\n{ex.Message}",
                MessageBoxImage.Warning);
            return false;
        }

        if (state.ReplacementAsset == candidate)
            return false;

        _imageEditWorkspace.Commit(
            ImageEditOperationKind.Replace,
            state with { ReplacementAsset = candidate });
        RefreshImageEditOverlay();
        StatusText.Text = candidate.HasAlpha
            ? "Imagen de reemplazo preparada con transparencia. El guardado transparente se validará antes de publicarse."
            : "Imagen de reemplazo preparada. Los cambios siguen pendientes de Guardar como...";
        return true;
    }

    private bool TryExtractSelectedImageFromDialog()
        => TryExtractSelectedImageToPath(_selectImageExtractionPath());

    private bool TryExtractSelectedImageToPath(string? destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath) ||
            _session is null ||
            !TryGetSelectedImageState(out var state))
        {
            return false;
        }

        try
        {
            var png = _extractSelectedImagePng(
                _session,
                state.ObjectRef.Key.PageIndex,
                state.ObjectRef.Key.PageObjectIndex,
                CancellationToken.None);
            if (png.IsEmpty)
                throw new InvalidDataException("PDFium no devolvió contenido de imagen extraíble.");

            _writeExtractedImage(destinationPath, png);
            StatusText.Text = $"Imagen extraída: {Path.GetFileName(destinationPath)}";
            return true;
        }
        catch (Exception ex) when (
            ex is InvalidDataException ||
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is ArgumentException ||
            ex is InvalidOperationException ||
            ex is NotSupportedException)
        {
            StatusText.Text = "No se pudo extraer la imagen seleccionada.";
            ShowImageEditTaskMessage(
                $"Esta imagen no se pudo extraer de forma visualmente fiel como PNG.\n\n{ex.Message}",
                MessageBoxImage.Warning);
            return false;
        }
    }

    private void ShowImageEditTaskMessage(string message, MessageBoxImage image)
    {
        if (IsVisible)
            MessageBox.Show(this, message, "SG PDF Editor", MessageBoxButton.OK, image);
    }
}
