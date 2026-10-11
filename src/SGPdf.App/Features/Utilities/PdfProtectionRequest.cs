namespace SGPdf.App.Features.Utilities;

internal sealed record PdfProtectionRequest(
    string OpenPassword,
    string ConfirmOpenPassword,
    string? SourcePassword,
    bool AuthorizationConfirmed)
{
    internal bool TryBuild(
        bool sourceOpenedWithPassword,
        out PdfProtectionSettings? settings,
        out string? error)
    {
        settings = null;
        error = null;

        if (!AuthorizationConfirmed)
        {
            error = "Debes confirmar que estás autorizado para proteger este PDF.";
            return false;
        }

        if (string.IsNullOrEmpty(OpenPassword))
        {
            error = "La nueva contraseña de apertura es obligatoria.";
            return false;
        }

        if (!string.Equals(OpenPassword, ConfirmOpenPassword, StringComparison.Ordinal))
        {
            error = "Las contraseñas nuevas no coinciden.";
            return false;
        }

        if (sourceOpenedWithPassword && string.IsNullOrEmpty(SourcePassword))
        {
            error = "Debes escribir la contraseña actual del PDF para autorizar una nueva copia protegida.";
            return false;
        }

        settings = new PdfProtectionSettings(
            OpenPassword,
            sourceOpenedWithPassword ? SourcePassword : null,
            AuthorizationConfirmed);
        return true;
    }
}
