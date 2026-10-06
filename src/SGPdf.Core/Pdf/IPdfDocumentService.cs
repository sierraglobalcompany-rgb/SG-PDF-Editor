namespace SGPdf.Core.Pdf;

public interface IPdfDocumentService
{
    Task<PdfDocumentInfo> OpenAsync(string path, CancellationToken cancellationToken = default);
}
