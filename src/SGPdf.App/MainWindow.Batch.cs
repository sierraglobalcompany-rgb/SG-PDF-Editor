using System.IO;
using System.Windows;
using Microsoft.Win32;
using SGPdf.App.Features.Batch;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private async void BatchWatermark_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStartBatch())
        {
            StatusText.Text = "No se puede ejecutar el lote en el estado actual del editor.";
            return;
        }

        var inputDialog = new OpenFileDialog
        {
            Title = "Seleccionar PDFs para marca de agua por lotes",
            Filter = "Archivos PDF (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = true
        };
        if (inputDialog.ShowDialog(this) != true || inputDialog.FileNames.Length == 0)
        {
            StatusText.Text = "Lote cancelado.";
            return;
        }

        string[] inputPaths = inputDialog.FileNames
            .Select(Path.GetFullPath)
            .ToArray();
        int maxPageCount = GetMaxReadableBatchPageCount(inputPaths);
        if (maxPageCount == 0)
        {
            ShowBatchError("No se encontró ningún PDF legible para configurar el lote.");
            return;
        }

        var watermarkDialog = new WatermarkDialog(maxPageCount, currentPageNumber: 1, allowCurrentPage: false)
        {
            Owner = this
        };
        if (watermarkDialog.ShowDialog() != true || watermarkDialog.Request is null)
        {
            StatusText.Text = "Lote cancelado.";
            return;
        }

        var outputDialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta de salida del lote"
        };
        if (outputDialog.ShowDialog(this) != true || string.IsNullOrWhiteSpace(outputDialog.FolderName))
        {
            StatusText.Text = "Lote cancelado: no se seleccionó una carpeta de salida.";
            return;
        }

        var request = new PdfBatchRequest(
            inputPaths,
            Path.GetFullPath(outputDialog.FolderName),
            PdfBatchOperation.TextWatermark);

        _utilityExecuting = true;
        SetBusy(true);
        StatusText.Text = $"Procesando lote ({inputPaths.Length} PDF)...";
        try
        {
            IReadOnlyList<PdfBatchItemResult> results = await Task.Run(() =>
                new PdfBatchTextWatermarkProcessor().Process(
                    request,
                    watermarkDialog.Request,
                    CancellationToken.None));

            string summary = BuildBatchSummary(results);
            StatusText.Text = summary.Split('\n')[0];
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    summary,
                    "Procesamiento por lotes",
                    MessageBoxButton.OK,
                    results.Any(result => !result.Success)
                        ? MessageBoxImage.Warning
                        : MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            ShowBatchError($"No se pudo completar el lote: {ex.Message}");
        }
        finally
        {
            _utilityExecuting = false;
            SetBusy(false);
        }
    }

    private bool CanStartBatch()
        => !_isBusy &&
           !_utilityExecuting &&
           !_signatureModeActive &&
           !_editModeActive &&
           !_organizeModeActive &&
           _zplDocument is null;

    private static int GetMaxReadableBatchPageCount(IReadOnlyList<string> inputPaths)
    {
        int maxPageCount = 0;
        foreach (string inputPath in inputPaths)
        {
            try
            {
                using var session = PdfDocumentSession.Open(inputPath);
                maxPageCount = Math.Max(maxPageCount, session.PageCount);
            }
            catch
            {
                // El processor reportará el error concreto de este archivo sin detener los demás.
            }
        }

        return maxPageCount;
    }

    private void ShowBatchError(string message)
    {
        StatusText.Text = message;
        if (IsVisible)
        {
            MessageBox.Show(
                this,
                message,
                "Procesamiento por lotes",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string BuildBatchSummary(IReadOnlyList<PdfBatchItemResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        int successes = results.Count(result => result.Success);
        int failures = results.Count - successes;
        string successLabel = successes == 1 ? "1 correcto" : $"{successes} correctos";
        string failureLabel = failures == 1 ? "1 con error" : $"{failures} con error";
        string summary = $"Lote completado: {successLabel}, {failureLabel}.";

        var failedItems = results.Where(result => !result.Success).ToArray();
        if (failedItems.Length == 0)
            return summary;

        return summary + "\n\nErrores:\n" + string.Join(
            "\n",
            failedItems.Select(result =>
                $"- {Path.GetFileName(result.InputPath)}: {result.ErrorMessage ?? "Error no especificado."}"));
    }
}
