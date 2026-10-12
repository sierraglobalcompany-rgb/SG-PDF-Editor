using System.Windows;
using SGPdf.App.Features.Ocr;

namespace SGPdf.App;

public partial class MainWindow
{
    private CancellationTokenSource? _ocrCancellation;
    private OcrDialog? _activeOcrDialog;
    private Func<string, string, OcrRequest, IProgress<OcrProgress>?, CancellationToken, Task<OcrResult>>? _ocrWorkflowRunnerOverride = null;

    private void Ocr_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar OCR en el estado actual del editor.";
            return;
        }

        var dialog = new OcrDialog(_session!.PageCount, _navigation!.CurrentPageIndex)
        {
            Owner = this
        };
        _activeOcrDialog = dialog;

        dialog.RunRequested += async (_, _) =>
        {
            var request = dialog.Request;
            if (request is null)
            {
                dialog.ResetAfterRun();
                return;
            }

            var destination = SelectUtilityDestination("Guardar PDF buscable como", "ocr-buscable.pdf");
            var succeeded = await TryRunOcrRequestAsync(request, destination);
            if (succeeded)
                dialog.CompleteAndClose();
            else if (dialog.IsVisible)
                dialog.ResetAfterRun();
        };
        dialog.CancelRequested += (_, _) => CancelOcr();
        dialog.Closed += (_, _) =>
        {
            if (ReferenceEquals(_activeOcrDialog, dialog))
                _activeOcrDialog = null;
        };

        dialog.ShowDialog();
    }

    private async Task<bool> TryRunOcrRequestAsync(OcrRequest request, string? destinationPath)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar OCR en el estado actual del editor.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            StatusText.Text = "OCR cancelado: no se seleccionó un destino.";
            return false;
        }

        var sourcePath = _session!.FilePath;
        var cancellation = new CancellationTokenSource();
        _ocrCancellation = cancellation;
        _utilityExecuting = true;
        SetBusy(true);

        try
        {
            var progress = new OcrUiProgress(UpdateOcrProgress);
            var result = await RunOcrWorkflowAsync(
                sourcePath,
                destinationPath,
                request,
                progress,
                cancellation.Token);

            if (result.Kind == OcrResultKind.NoOp)
            {
                StatusText.Text = "OCR no necesario: el PDF ya es buscable en las páginas seleccionadas.";
                return false;
            }

            var processed = result.ProcessedPageIndices.Count;
            var skipped = result.SkippedSearchablePageIndices.Count;

            if (!PathsEqual(sourcePath, destinationPath))
            {
                var reopened = await TryOpenPdfPathAsync(destinationPath);
                if (!reopened)
                {
                    StatusText.Text = "OCR terminó, pero no se pudo abrir el PDF buscable generado.";
                    return false;
                }
            }

            StatusText.Text = $"OCR completado: {processed} página(s) procesada(s), {skipped} omitida(s) por ya ser buscables.";
            return true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            StatusText.Text = "OCR cancelado.";
            return false;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo completar el OCR: {ex.Message}";
            return false;
        }
        finally
        {
            if (ReferenceEquals(_ocrCancellation, cancellation))
                _ocrCancellation = null;
            cancellation.Dispose();
            _utilityExecuting = false;
            SetBusy(false);
        }
    }

    private Task<OcrResult> RunOcrWorkflowAsync(
        string sourcePath,
        string destinationPath,
        OcrRequest request,
        IProgress<OcrProgress>? progress,
        CancellationToken cancellationToken)
    {
        var runner = _ocrWorkflowRunnerOverride;
        if (runner is not null)
            return runner(sourcePath, destinationPath, request, progress, cancellationToken);

        return new OcrWorkflow(sourcePath, destinationPath)
            .RunAsync(request, progress, cancellationToken);
    }

    private void UpdateOcrProgress(OcrProgress progress)
    {
        _activeOcrDialog?.UpdateProgress(progress);
        StatusText.Text = $"OCR: página {progress.CurrentOrdinal} de {progress.TotalEligiblePages} — {OcrStageText(progress.Stage)}";
    }

    private void CancelOcr()
    {
        var cancellation = _ocrCancellation;
        if (cancellation is null || cancellation.IsCancellationRequested)
            return;

        StatusText.Text = "Cancelando OCR…";
        cancellation.Cancel();
    }

    private static bool PathsEqual(string left, string right)
        => string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);

    private static string OcrStageText(OcrProgressStage stage) => stage switch
    {
        OcrProgressStage.Rendering => "preparando imagen",
        OcrProgressStage.Recognizing => "reconociendo texto",
        OcrProgressStage.Writing => "guardando",
        OcrProgressStage.Validating => "validando",
        _ => "procesando"
    };

    private sealed class OcrUiProgress(Action<OcrProgress> report) : IProgress<OcrProgress>
    {
        private readonly Action<OcrProgress> _report = report ?? throw new ArgumentNullException(nameof(report));

        public void Report(OcrProgress value)
            => _report(value);
    }
}
