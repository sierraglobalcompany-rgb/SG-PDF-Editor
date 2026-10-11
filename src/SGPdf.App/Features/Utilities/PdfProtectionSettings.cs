namespace SGPdf.App.Features.Utilities;

internal sealed record PdfProtectionSettings(
    string OpenPassword,
    string? SourcePassword,
    bool AuthorizationConfirmed);
