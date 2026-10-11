using System.IO;
using System.Security.Cryptography;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Utilities;

namespace SGPdf.App.Pdf;

internal sealed record PdfProtectionWriteResult(bool Succeeded, string? Error)
{
    internal static PdfProtectionWriteResult Success { get; } = new(true, null);

    internal static PdfProtectionWriteResult Failure(string error) => new(false, error);
}

internal sealed class PdfProtectionWriter
{
    private readonly Func<string, string, string, string, string?, CancellationToken, Task> _protectCopy;
    private readonly Func<string, string, int, CancellationToken, PdfProtectionValidationResult> _validator;
    private readonly Func<PdfDocumentSession, CancellationToken, int> _signatureCountReader;

    internal PdfProtectionWriter()
        : this(
            (source, destination, userPassword, ownerPassword, sourcePassword, cancellationToken) =>
                PdfProtectionCapabilityAdapter.CreateProtectedCopyAsync(
                    source,
                    destination,
                    userPassword,
                    ownerPassword,
                    sourcePassword,
                    cancellationToken),
            new PdfProtectionOutputValidator().Validate,
            static (session, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = session.GetCryptographicSignatureCount();
                cancellationToken.ThrowIfCancellationRequested();
                return count;
            })
    {
    }

    internal PdfProtectionWriter(
        Func<string, string, string, string, string?, CancellationToken, Task> protectCopy,
        Func<string, string, int, CancellationToken, PdfProtectionValidationResult> validator,
        Func<PdfDocumentSession, CancellationToken, int> signatureCountReader)
    {
        _protectCopy = protectCopy ?? throw new ArgumentNullException(nameof(protectCopy));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _signatureCountReader = signatureCountReader ?? throw new ArgumentNullException(nameof(signatureCountReader));
    }

    internal async Task<PdfProtectionWriteResult> SaveAsProtectedCopyAsync(
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        PdfProtectionSettings settings,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        cancellationToken.ThrowIfCancellationRequested();

        if (!settings.AuthorizationConfirmed)
            return PdfProtectionWriteResult.Failure("Debe confirmar que está autorizado para proteger este PDF.");
        if (string.IsNullOrEmpty(settings.OpenPassword))
            return PdfProtectionWriteResult.Failure("La nueva contraseña de apertura es obligatoria.");

        var sourceFullPath = Path.GetFullPath(sourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            return PdfProtectionWriteResult.Failure("El PDF protegido debe guardarse en una ruta diferente del original.");
        if (!fingerprint.MatchesCurrentFile(sourceFullPath))
            return PdfProtectionWriteResult.Failure("El PDF fuente cambió desde que comenzó la operación.");

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
        if (string.IsNullOrWhiteSpace(destinationDirectory) || !Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException("La carpeta de destino no existe.");

        string? temporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var authorizedSource = OpenAuthorizedSource(sourceFullPath, settings.SourcePassword);
            int pageCount;
            string? sourcePasswordForQpdf;
            using (authorizedSource.Session)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!fingerprint.MatchesCurrentFile(sourceFullPath))
                    return PdfProtectionWriteResult.Failure("El PDF fuente cambió antes de crear la copia protegida.");

                var signatureCount = _signatureCountReader(authorizedSource.Session, cancellationToken);
                if (signatureCount != 0)
                {
                    return PdfProtectionWriteResult.Failure(
                        signatureCount < 0
                            ? "No se pudo comprobar con seguridad si el PDF contiene firmas criptográficas."
                            : "El PDF contiene una firma criptográfica y no se protegerá una copia que pueda invalidarla.");
                }

                pageCount = authorizedSource.Session.PageCount;
                sourcePasswordForQpdf = authorizedSource.SourcePassword;
            }

            cancellationToken.ThrowIfCancellationRequested();
            temporaryPath = CreateTemporaryPath(destinationFullPath);
            var ownerPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await _protectCopy(
                sourceFullPath,
                temporaryPath,
                settings.OpenPassword,
                ownerPassword,
                sourcePasswordForQpdf,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            if (!fingerprint.MatchesCurrentFile(sourceFullPath))
                return PdfProtectionWriteResult.Failure("El PDF fuente cambió durante la creación de la copia protegida.");

            var validation = _validator(
                temporaryPath,
                settings.OpenPassword,
                pageCount,
                cancellationToken);
            if (!validation.IsValid)
                return PdfProtectionWriteResult.Failure(validation.Error ?? "La copia protegida no superó la validación.");

            cancellationToken.ThrowIfCancellationRequested();
            PublishTemporaryFile(temporaryPath, destinationFullPath);
            temporaryPath = null;
            return PdfProtectionWriteResult.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PdfProtectionWriteResult.Failure(ex.Message);
        }
        finally
        {
            if (temporaryPath is not null)
                TryDelete(temporaryPath);
        }
    }

    private static (PdfDocumentSession Session, string? SourcePassword) OpenAuthorizedSource(
        string sourcePath,
        string? suppliedSourcePassword)
    {
        try
        {
            return (PdfDocumentSession.Open(sourcePath), null);
        }
        catch (PdfDocumentOpenException ex) when (ex.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
        {
            if (string.IsNullOrEmpty(suppliedSourcePassword))
                throw new InvalidOperationException("El PDF fuente está protegido; debe indicar la contraseña actual para autorizar la nueva copia.");

            try
            {
                return (PdfDocumentSession.Open(sourcePath, suppliedSourcePassword), suppliedSourcePassword);
            }
            catch (PdfDocumentOpenException passwordError) when (passwordError.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
            {
                throw new InvalidOperationException("La contraseña actual no autoriza la apertura del PDF fuente.");
            }
        }
    }

    private static string CreateTemporaryPath(string destinationFullPath)
    {
        var directory = Path.GetDirectoryName(destinationFullPath)!;
        var stem = Path.GetFileNameWithoutExtension(destinationFullPath);
        return Path.Combine(directory, $"{stem}.{Guid.NewGuid():N}.sgpdf-tmp.pdf");
    }

    private static void PublishTemporaryFile(string temporaryPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
            File.Replace(temporaryPath, destinationPath, destinationBackupFileName: null);
        else
            File.Move(temporaryPath, destinationPath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
