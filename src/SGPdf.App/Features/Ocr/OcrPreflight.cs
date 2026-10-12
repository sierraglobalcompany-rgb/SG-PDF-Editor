using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

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

    internal PdfEditPreflightResult Inspect(
        PdfDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        return _editPreflight(session, cancellationToken);
    }
}
