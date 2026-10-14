using System.IO;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Batch;

internal sealed class PdfBatchTextWatermarkProcessor
{
    internal IReadOnlyList<PdfBatchItemResult> Process(
        PdfBatchRequest request,
        WatermarkUtilityRequest watermarkRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.InputPaths);
        ArgumentNullException.ThrowIfNull(watermarkRequest);

        if (request.Operation != PdfBatchOperation.TextWatermark)
            throw new ArgumentOutOfRangeException(nameof(request), "La operación por lotes no es compatible.");

        var results = new PdfBatchItemResult[request.InputPaths.Count];
        for (int index = 0; index < request.InputPaths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results[index] = ProcessOne(
                request.InputPaths[index],
                request.OutputDirectory,
                watermarkRequest,
                cancellationToken);
        }

        return Array.AsReadOnly(results);
    }

    internal PdfBatchItemResult ProcessOne(
        string inputPath,
        string outputDirectory,
        WatermarkUtilityRequest watermarkRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(watermarkRequest);
        cancellationToken.ThrowIfCancellationRequested();

        string outputPath = Path.Combine(
            Path.GetFullPath(outputDirectory),
            $"{Path.GetFileNameWithoutExtension(inputPath)}-marca-agua.pdf");

        try
        {
            if (watermarkRequest.UseCurrentPage)
            {
                return Failure(
                    inputPath,
                    outputPath,
                    "La opción de página actual no aplica al procesamiento por lotes.");
            }

            Directory.CreateDirectory(outputDirectory);
            if (File.Exists(outputPath))
            {
                return Failure(
                    inputPath,
                    outputPath,
                    "El archivo de salida ya existe.");
            }

            using var session = PdfDocumentSession.Open(inputPath);
            if (!watermarkRequest.TryBuild(
                    session.PageCount,
                    currentPageIndex: 0,
                    out var settings,
                    out var range,
                    out var anchor,
                    out var validationError))
            {
                return Failure(
                    inputPath,
                    outputPath,
                    validationError ?? "Los valores de la marca de agua no son válidos.");
            }

            var placements = new WatermarkPlanner().Plan(
                settings!,
                range!,
                session.GetPageSizes(cancellationToken),
                anchor,
                watermarkRequest.OffsetXPoints,
                watermarkRequest.OffsetYPoints,
                watermarkRequest.RotationDegrees);
            var fingerprint = PdfEditSourceFingerprint.Capture(inputPath);
            var writeResult = new PdfTextOverlayWriter().SaveAsCopy(
                inputPath,
                fingerprint,
                placements,
                outputPath,
                cancellationToken);

            return writeResult.Succeeded
                ? new PdfBatchItemResult(inputPath, outputPath, true, null)
                : Failure(inputPath, outputPath, writeResult.Error ?? "No se pudo guardar la copia con marca de agua.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failure(inputPath, outputPath, ex.Message);
        }
    }

    private static PdfBatchItemResult Failure(string inputPath, string outputPath, string error)
        => new(inputPath, outputPath, false, error);
}
