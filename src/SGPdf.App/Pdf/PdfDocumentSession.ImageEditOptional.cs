namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    internal int GetPageObjectCount(
        int pageIndex,
        CancellationToken cancellationToken = default)
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
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para consultar su orden de objetos.");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = PdfiumNative.FPDFPage_CountObjects(page);
                if (count < 0)
                    throw new InvalidOperationException("PDFium devolvió una cantidad inválida de objetos de página.");
                return count;
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
