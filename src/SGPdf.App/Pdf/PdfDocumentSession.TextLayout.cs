using SGPdf.App.Features.Edit.Text.Layout;

namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    internal TextLayoutPage AnalyzeTextLayout(
        int pageIndex,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (width, height) = GetPageSize(pageIndex, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var objects = GetTextObjects(pageIndex, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return TextLayoutAnalyzer.Analyze(
            width,
            height,
            objects,
            cancellationToken);
    }
}
