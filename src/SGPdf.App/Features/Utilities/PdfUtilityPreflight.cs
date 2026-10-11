using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal sealed class PdfUtilityPreflight
{
    private readonly PdfEditPreflightInspector _inspector;

    internal PdfUtilityPreflight()
        : this(new PdfEditPreflightInspector())
    {
    }

    internal PdfUtilityPreflight(PdfEditPreflightInspector inspector)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
    }

    internal PdfEditPreflightResult Inspect(
        PdfDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        return _inspector.Inspect(session, cancellationToken);
    }
}
