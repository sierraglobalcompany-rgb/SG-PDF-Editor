namespace SGPdf.App.Pdf;

public enum PdfDocumentOpenError
{
    PasswordRequiredOrIncorrect,
    OtherPdfiumError
}

public sealed class PdfDocumentOpenException : InvalidOperationException
{
    public PdfDocumentOpenException(PdfDocumentOpenError error, uint pdfiumErrorCode)
        : base(CreateMessage(error, pdfiumErrorCode))
    {
        Error = error;
        PdfiumErrorCode = pdfiumErrorCode;
    }

    public PdfDocumentOpenError Error { get; }
    public uint PdfiumErrorCode { get; }

    private static string CreateMessage(PdfDocumentOpenError error, uint pdfiumErrorCode)
        => error == PdfDocumentOpenError.PasswordRequiredOrIncorrect
            ? "El PDF requiere una contraseña válida."
            : $"PDFium no pudo abrir el documento. Error: {pdfiumErrorCode}.";
}
