using System.IO;
using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Signing;

internal sealed class PdfCryptographicSigningService
{
    private readonly Action<string, string>? _beforeValidation;

    internal PdfCryptographicSigningService(Action<string, string>? beforeValidation)
    {
        _beforeValidation = beforeValidation;
    }

    internal void Sign(
        string sourcePath,
        string destinationPath,
        string pfxPath,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta del PDF fuente es obligatoria.", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("La ruta del PDF de destino es obligatoria.", nameof(destinationPath));
        if (string.IsNullOrWhiteSpace(pfxPath))
            throw new ArgumentException("La ruta del certificado PFX/P12 es obligatoria.", nameof(pfxPath));
        ArgumentNullException.ThrowIfNull(password);

        cancellationToken.ThrowIfCancellationRequested();

        string sourceFullPath = Path.GetFullPath(sourcePath);
        string destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El PDF firmado debe guardarse en una ruta distinta del archivo fuente.",
                nameof(destinationPath));
        }

        using (var session = PdfDocumentSession.Open(sourceFullPath))
        {
            var preflight = new PdfEditPreflightInspector().Inspect(session, cancellationToken);
            if (!preflight.CanProceed)
            {
                string reason = preflight.Findings
                    .First(finding => finding.Severity == PdfEditFindingSeverity.Block)
                    .Message;
                throw new InvalidOperationException($"El PDF fuente está bloqueado para firma: {reason}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        PdfEditSourceFingerprint sourceFingerprint = PdfEditSourceFingerprint.Capture(sourceFullPath);

        using var certificate = PfxCertificateLoader.Load(
            pfxPath,
            password,
            DateTimeOffset.UtcNow);

        cancellationToken.ThrowIfCancellationRequested();

        string destinationDirectory = Path.GetDirectoryName(destinationFullPath)
            ?? throw new InvalidOperationException("No se pudo resolver el directorio de destino.");
        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException($"No existe el directorio de destino: {destinationDirectory}");

        string tempPath = Path.Combine(
            destinationDirectory,
            $".sgpdf-sign-{Guid.NewGuid():N}.tmp.pdf");

        try
        {
            PdfCryptographicSignatureWriter.Write(
                sourceFullPath,
                tempPath,
                certificate,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _beforeValidation?.Invoke(sourceFullPath, tempPath);
            cancellationToken.ThrowIfCancellationRequested();

            new PdfCryptographicSignatureValidator().Validate(tempPath, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (!sourceFingerprint.MatchesCurrentFile(sourceFullPath))
                throw new IOException("El PDF fuente cambió durante la firma; no se publicará el resultado.");

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(destinationFullPath))
                File.Replace(tempPath, destinationFullPath, destinationBackupFileName: null);
            else
                File.Move(tempPath, destinationFullPath);
        }
        finally
        {
            TryDeleteTemp(tempPath);
        }
    }

    private static void TryDeleteTemp(string tempPath)
    {
        if (!File.Exists(tempPath))
            return;

        try
        {
            File.Delete(tempPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
