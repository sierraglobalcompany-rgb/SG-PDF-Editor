using System.IO;

namespace SGPdf.App.Pdf;

internal sealed record PdfProtectionValidationResult(bool IsValid, string? Error)
{
    internal static PdfProtectionValidationResult Success { get; } = new(true, null);

    internal static PdfProtectionValidationResult Failure(string error) => new(false, error);
}

internal sealed class PdfProtectionOutputValidator
{
    internal PdfProtectionValidationResult Validate(
        string path,
        string openPassword,
        int expectedPageCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (string.IsNullOrEmpty(openPassword))
            throw new ArgumentException("La contraseña de apertura es obligatoria.", nameof(openPassword));
        if (expectedPageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedPageCount));

        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
            return PdfProtectionValidationResult.Failure("La copia protegida temporal no existe.");

        try
        {
            using var unprotectedOpen = PdfDocumentSession.Open(path);
            return PdfProtectionValidationResult.Failure("La copia protegida se puede abrir sin contraseña.");
        }
        catch (PdfDocumentOpenException ex) when (ex.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
        {
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var session = PdfDocumentSession.Open(path, openPassword);
            if (!session.OpenedWithPassword)
                return PdfProtectionValidationResult.Failure("La copia protegida no quedó marcada como apertura autorizada por contraseña.");
            if (session.PageCount != expectedPageCount)
                return PdfProtectionValidationResult.Failure("La copia protegida cambió el número de páginas.");

            var pagesToRender = expectedPageCount == 1
                ? new[] { 0 }
                : new[] { 0, expectedPageCount - 1 };
            foreach (var pageIndex in pagesToRender)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rendered = session.RenderPage(pageIndex, 36d, cancellationToken);
                if (rendered.PixelWidth <= 0 || rendered.PixelHeight <= 0 || rendered.Pixels.Length == 0)
                    return PdfProtectionValidationResult.Failure("La copia protegida no superó la validación de renderizado.");
            }

            return PdfProtectionValidationResult.Success;
        }
        catch (PdfDocumentOpenException)
        {
            return PdfProtectionValidationResult.Failure("La copia protegida no se puede reabrir con la nueva contraseña.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return PdfProtectionValidationResult.Failure($"La copia protegida no superó la validación: {ex.Message}");
        }
    }
}
