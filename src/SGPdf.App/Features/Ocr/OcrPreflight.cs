using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal sealed record OcrPreflightResult(IReadOnlyList<PdfEditFinding> Findings)
{
    internal bool CanProceed =>
        Findings.All(finding => finding.Severity != PdfEditFindingSeverity.Block);

    internal bool IsPasswordBlocked =>
        Findings.Any(finding =>
            finding.Kind == PdfEditFindingKind.PasswordProtectedSource &&
            finding.Severity == PdfEditFindingSeverity.Block);

    internal bool IsSignedBlocked =>
        Findings.Any(finding =>
            finding.Kind == PdfEditFindingKind.CryptographicSignature &&
            finding.Severity == PdfEditFindingSeverity.Block);

    internal string? Reason =>
        Findings.FirstOrDefault(finding => finding.Severity == PdfEditFindingSeverity.Block)?.Message;
}

internal sealed class OcrPreflight
{
    private readonly Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> _editPreflight;

    internal OcrPreflight()
        : this(new PdfEditPreflightInspector().Inspect)
    {
    }

    internal OcrPreflight(
        Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> editPreflight)
    {
        _editPreflight = editPreflight ?? throw new ArgumentNullException(nameof(editPreflight));
    }

    internal OcrPreflightResult Inspect(
        PdfDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var editResult = _editPreflight(session, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return new OcrPreflightResult(editResult.Findings);
    }
}
