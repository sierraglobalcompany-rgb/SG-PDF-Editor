namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    internal string GetPageText(int pageIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para evaluar OCR.");

            try
            {
                var textPage = PdfiumNative.FPDFText_LoadPage(page);
                if (textPage == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la capa de texto de la página {pageIndex + 1}.");

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var characterCount = PdfiumNative.FPDFText_CountChars(textPage);
                    if (characterCount <= 0)
                        return string.Empty;

                    var buffer = new ushort[checked(characterCount + 1)];
                    var written = PdfiumNative.FPDFText_GetText(textPage, 0, characterCount, buffer);
                    if (written <= 0)
                        return string.Empty;

                    var length = Math.Min(written, buffer.Length);
                    while (length > 0 && buffer[length - 1] == 0)
                        length--;

                    var chars = new char[length];
                    for (var index = 0; index < length; index++)
                        chars[index] = (char)buffer[index];

                    cancellationToken.ThrowIfCancellationRequested();
                    return new string(chars);
                }
                finally
                {
                    PdfiumNative.FPDFText_ClosePage(textPage);
                }
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }
}
