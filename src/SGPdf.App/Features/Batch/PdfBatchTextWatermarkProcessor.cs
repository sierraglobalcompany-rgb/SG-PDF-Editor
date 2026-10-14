using System.IO;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Batch;

internal sealed class PdfBatchTextWatermarkProcessor
{
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
