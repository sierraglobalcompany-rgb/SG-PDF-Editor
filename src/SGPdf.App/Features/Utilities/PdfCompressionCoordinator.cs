using System.IO;
using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal sealed record PdfCompressionResult(
    bool Succeeded,
    bool NoReduction,
    long SourceLength,
    long OutputLength,
    string? Error);

internal sealed class PdfCompressionCoordinator
{
    private readonly Func<string, string, CancellationToken, Task<PdfOptimizeCapabilityResult>> _optimizeCopyAsync;

    internal PdfCompressionCoordinator()
        : this(PdfOptimizeRepairCapabilityAdapter.OptimizeCopyAsync)
    {
    }

    internal PdfCompressionCoordinator(
        Func<string, string, CancellationToken, Task<PdfOptimizeCapabilityResult>> optimizeCopyAsync)
    {
        _optimizeCopyAsync = optimizeCopyAsync ?? throw new ArgumentNullException(nameof(optimizeCopyAsync));
    }

    internal async Task<PdfCompressionResult> CompressCopyAsync(
        PdfDocumentSession sourceSession,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceSession);
        string? temporaryPath = null;
        long sourceLength = 0;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourcePath = Path.GetFullPath(sourceSession.FilePath);
            var destinationFullPath = Path.GetFullPath(destinationPath);
            if (string.Equals(sourcePath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
                return Failure("El destino debe ser distinto del PDF original.");
            if (!string.Equals(Path.GetExtension(destinationFullPath), ".pdf", StringComparison.OrdinalIgnoreCase))
                return Failure("El destino debe tener extensión .pdf.");
            if (!File.Exists(sourcePath))
                return Failure("No se encontró el PDF original.");

            var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
            if (destinationDirectory is null || !Directory.Exists(destinationDirectory))
                return Failure("La carpeta de destino no existe.");
            if (File.Exists(destinationFullPath) || Directory.Exists(destinationFullPath))
                return Failure("Ese destino ya existe. Elige otro nombre para conservarlo.");

            var preflight = new PdfUtilityPreflight().Inspect(sourceSession, cancellationToken);
            if (!preflight.CanProceed)
            {
                var blockedFinding = preflight.Findings.First(finding =>
                    finding.Severity == PdfEditFindingSeverity.Block);
                return Failure(DescribeBlockedFinding(blockedFinding));
            }

            var pageCount = sourceSession.PageCount;
            if (pageCount <= 0)
                return Failure("El PDF original no contiene páginas legibles.");

            var sourceFingerprint = PdfEditSourceFingerprint.Capture(sourcePath);
            sourceLength = new FileInfo(sourcePath).Length;
            if (sourceLength <= 0)
                return Failure("El PDF original está vacío.");

            temporaryPath = Path.Combine(
                destinationDirectory,
                $".sgpdf-compress-{Guid.NewGuid():N}.pdf");
            var capability = await _optimizeCopyAsync(
                sourcePath,
                temporaryPath,
                cancellationToken).ConfigureAwait(false);

            if (!capability.Succeeded || !File.Exists(temporaryPath))
            {
                var error = string.IsNullOrWhiteSpace(capability.Error)
                    ? "qpdf no pudo crear una copia optimizada válida."
                    : capability.Error;
                return Failure(error);
            }

            var outputLength = new FileInfo(temporaryPath).Length;
            if (outputLength >= sourceLength)
            {
                return new PdfCompressionResult(
                    Succeeded: false,
                    NoReduction: true,
                    SourceLength: sourceLength,
                    OutputLength: outputLength,
                    Error: "No se obtuvo reducción; el original no cambió.");
            }

            using (var optimizedSession = PdfDocumentSession.Open(temporaryPath))
            {
                if (optimizedSession.PageCount != pageCount)
                    return Failure("La copia optimizada no conserva la cantidad de páginas del original.");
            }

            if (!sourceFingerprint.MatchesCurrentFile(sourcePath))
                return Failure("El PDF original cambió durante la compresión; no se publicó la copia.");

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, destinationFullPath);
            temporaryPath = null;

            return new PdfCompressionResult(
                Succeeded: true,
                NoReduction: false,
                SourceLength: sourceLength,
                OutputLength: outputLength,
                Error: null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failure(ex.Message, sourceLength);
        }
        finally
        {
            if (temporaryPath is not null && File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static string DescribeBlockedFinding(PdfEditFinding finding)
        => finding.Kind switch
        {
            PdfEditFindingKind.PasswordProtectedSource
                => "El PDF se abrió con contraseña y no se reescribió.",
            PdfEditFindingKind.CryptographicSignature
                => "El PDF tiene una firma criptográfica o no se pudo verificar; no se comprimió.",
            _ => "El PDF no superó la verificación de seguridad; no se creó una copia."
        };

    private static PdfCompressionResult Failure(string error, long sourceLength = 0)
        => new(
            Succeeded: false,
            NoReduction: false,
            SourceLength: sourceLength,
            OutputLength: 0,
            Error: error);
}
