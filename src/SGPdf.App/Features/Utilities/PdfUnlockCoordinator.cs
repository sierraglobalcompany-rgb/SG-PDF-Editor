using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Labels;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal sealed record PdfUnlockResult(bool Succeeded, string? Error)
{
    internal static PdfUnlockResult Success { get; } = new(true, null);
    internal static PdfUnlockResult Failure(string error) => new(false, error);
}

internal sealed class PdfUnlockCoordinator
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);
    private readonly string _qpdfPath;
    private readonly Func<ProcessStartInfo, TimeSpan, CancellationToken, string?, Task<ChildProcessResult>> _runProcessAsync;
    private readonly Func<PdfDocumentSession, int> _signatureCountReader;

    internal PdfUnlockCoordinator()
        : this(
            Path.Combine(AppContext.BaseDirectory, "qpdf", "qpdf.exe"),
            ChildProcessRunner.RunAsync,
            static session => session.GetCryptographicSignatureCount())
    {
    }

    internal PdfUnlockCoordinator(
        string qpdfPath,
        Func<ProcessStartInfo, TimeSpan, CancellationToken, string?, Task<ChildProcessResult>> runProcessAsync,
        Func<PdfDocumentSession, int>? signatureCountReader = null)
    {
        _qpdfPath = qpdfPath ?? throw new ArgumentNullException(nameof(qpdfPath));
        _runProcessAsync = runProcessAsync ?? throw new ArgumentNullException(nameof(runProcessAsync));
        _signatureCountReader = signatureCountReader ?? (static session => session.GetCryptographicSignatureCount());
    }

    internal async Task<PdfUnlockResult> CreateUnlockedCopyAsync(
        string sourcePath,
        string destinationPath,
        string password,
        bool authorizationConfirmed,
        CancellationToken cancellationToken)
    {
        string? temporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!authorizationConfirmed)
                return PdfUnlockResult.Failure("Confirma que tienes autorización para crear una copia sin contraseña.");
            if (string.IsNullOrEmpty(password) || password.Contains('\r') || password.Contains('\n'))
                return PdfUnlockResult.Failure("Escribe la contraseña actual del PDF en una sola línea.");

            var paths = ValidatePaths(sourcePath, destinationPath);
            if (!File.Exists(_qpdfPath))
                return PdfUnlockResult.Failure("No está disponible el qpdf validado para esta operación.");

            var sourceHash = await ComputeSha256Async(paths.Source, cancellationToken).ConfigureAwait(false);
            var encrypted = await RunQpdfAsync(["--is-encrypted", paths.Source], null, null, cancellationToken)
                .ConfigureAwait(false);
            if (encrypted is null)
                return PdfUnlockResult.Failure("No se pudo verificar el cifrado del PDF; no se creó una copia.");
            if (encrypted.ExitCode == 2)
                return PdfUnlockResult.Failure("El PDF no está cifrado con contraseña de apertura.");
            if (encrypted.ExitCode != 0)
                return PdfUnlockResult.Failure("qpdf no pudo verificar el cifrado del PDF.");

            var passwordCheck = await RunQpdfAsync(
                ["--password-file=-", "--requires-password", paths.Source],
                password,
                password,
                cancellationToken).ConfigureAwait(false);
            if (passwordCheck is null)
                return PdfUnlockResult.Failure("La validación segura de la contraseña no terminó correctamente.");
            if (passwordCheck.ExitCode == 0)
                return PdfUnlockResult.Failure("La contraseña no es válida para este PDF.");
            if (passwordCheck.ExitCode != 3 || !string.IsNullOrWhiteSpace(passwordCheck.StandardError))
                return PdfUnlockResult.Failure("qpdf no pudo validar la contraseña sin advertencias.");

            using var sourceSession = PdfDocumentSession.Open(paths.Source, password);
            var sourcePageCount = sourceSession.PageCount;
            if (sourcePageCount <= 0)
                return PdfUnlockResult.Failure("El PDF original no contiene páginas legibles.");
            if (_signatureCountReader(sourceSession) != 0)
                return PdfUnlockResult.Failure("El PDF contiene una firma criptográfica y no se puede desbloquear de forma segura.");

            var sourceSnapshot = PdfOperationPreservationSnapshot.Capture(sourceSession, cancellationToken);
            var sourceGeometry = CapturePageGeometry(sourceSession, cancellationToken);
            var destinationDirectory = Path.GetDirectoryName(paths.Destination)!;
            temporaryPath = Path.Combine(destinationDirectory, $".sgpdf-unlock-{Guid.NewGuid():N}.pdf");

            var decrypted = await RunQpdfAsync(
                ["--password-file=-", "--decrypt", paths.Source, temporaryPath],
                password,
                password,
                cancellationToken).ConfigureAwait(false);
            if (decrypted is null || decrypted.ExitCode != 0 ||
                !string.IsNullOrWhiteSpace(decrypted.StandardError) || !File.Exists(temporaryPath))
            {
                return PdfUnlockResult.Failure("qpdf no pudo crear una copia desbloqueada sin advertencias.");
            }

            var outputEncryption = await RunQpdfAsync(
                ["--is-encrypted", temporaryPath], null, null, cancellationToken).ConfigureAwait(false);
            if (outputEncryption is null || outputEncryption.ExitCode != 2)
                return PdfUnlockResult.Failure("La copia temporal todavía está cifrada o no se pudo verificar.");

            using (var outputSession = PdfDocumentSession.Open(temporaryPath))
            {
                if (outputSession.OpenedWithPassword ||
                    !sourceSnapshot.HasSameContentAndStructuresAs(
                        PdfOperationPreservationSnapshot.Capture(outputSession, cancellationToken)) ||
                    CapturePageGeometry(outputSession, cancellationToken) != sourceGeometry)
                {
                    return PdfUnlockResult.Failure("La copia temporal no conservó la estructura comprobable del original.");
                }

                if (_signatureCountReader(outputSession) != 0)
                    return PdfUnlockResult.Failure("La salida contiene una firma inesperada; no se publicó.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var finalHash = await ComputeSha256Async(paths.Source, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(sourceHash, finalHash))
                return PdfUnlockResult.Failure("El PDF original cambió durante la operación; no se publicó la copia.");

            File.Move(temporaryPath, paths.Destination, overwrite: false);
            temporaryPath = null;
            return PdfUnlockResult.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PdfUnlockValidationException ex)
        {
            return PdfUnlockResult.Failure(ex.Message);
        }
        catch (PdfDocumentOpenException)
        {
            return PdfUnlockResult.Failure("No se pudo abrir el PDF con la contraseña proporcionada.");
        }
        catch (FileNotFoundException)
        {
            return PdfUnlockResult.Failure("No se encontró el PDF original.");
        }
        catch (DirectoryNotFoundException)
        {
            return PdfUnlockResult.Failure("No existe la carpeta de destino.");
        }
        catch (IOException)
        {
            return PdfUnlockResult.Failure("Ese destino ya existe o no está disponible; el original quedó intacto.");
        }
        catch (Exception)
        {
            return PdfUnlockResult.Failure("No se pudo completar el desbloqueo; el original quedó intacto.");
        }
        finally
        {
            if (temporaryPath is not null)
                TryDelete(temporaryPath);
        }
    }

    private async Task<ChildProcessResult?> RunQpdfAsync(
        IReadOnlyList<string> arguments,
        string? standardInput,
        string? secret,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(_qpdfPath);
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        var result = await _runProcessAsync(startInfo, ProcessTimeout, cancellationToken, standardInput)
            .ConfigureAwait(false);
        if (secret is not null &&
            (result.StandardOutput.Contains(secret, StringComparison.Ordinal) ||
             result.StandardError.Contains(secret, StringComparison.Ordinal)))
        {
            return null;
        }

        return result;
    }

    private static (string Source, string Destination) ValidatePaths(string sourcePath, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
            throw new PdfUnlockValidationException("La ruta de origen y el destino son obligatorios.");
        var source = Path.GetFullPath(sourcePath);
        var destination = Path.GetFullPath(destinationPath);
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            throw new PdfUnlockValidationException("El destino debe ser distinto del PDF original.");
        if (!string.Equals(Path.GetExtension(destination), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new PdfUnlockValidationException("El destino debe tener extensión .pdf.");
        if (!File.Exists(source))
            throw new FileNotFoundException("No se encontró el PDF original.");
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new PdfUnlockValidationException("Ese destino ya existe. Elige otro nombre para conservarlo.");
        var directory = Path.GetDirectoryName(destination);
        if (directory is null || !Directory.Exists(directory))
            throw new DirectoryNotFoundException("No existe la carpeta de destino.");
        return (source, destination);
    }

    private static string CapturePageGeometry(PdfDocumentSession session, CancellationToken cancellationToken)
    {
        var geometry = new List<string>(session.PageCount);
        for (var pageIndex = 0; pageIndex < session.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var size = session.GetPageSize(pageIndex, cancellationToken);
            geometry.Add(string.Join(",",
                size.Width.ToString("R", CultureInfo.InvariantCulture),
                size.Height.ToString("R", CultureInfo.InvariantCulture),
                session.GetPageRotation(pageIndex, cancellationToken).ToString(CultureInfo.InvariantCulture)));
        }
        return string.Join("|", geometry);
    }

    private static async Task<byte[]> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class PdfUnlockValidationException(string message) : Exception(message) { }
}
