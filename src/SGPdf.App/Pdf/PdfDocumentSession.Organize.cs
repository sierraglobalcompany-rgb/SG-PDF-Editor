namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    private static readonly string[] OrganizeMetadataTags =
    {
        "Title",
        "Author",
        "Subject",
        "Keywords",
        "Creator",
        "Producer",
        "CreationDate",
        "ModDate"
    };

    public int GetPageRotation(int pageIndex, CancellationToken cancellationToken = default)
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
                throw new InvalidOperationException($"PDFium no pudo cargar la página {pageIndex + 1} para consultar su rotación.");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rotation = PdfiumNative.FPDFPage_GetRotation(page);
                if (rotation is < 0 or > 3)
                    throw new InvalidOperationException($"PDFium devolvió una rotación inválida para la página {pageIndex + 1}: {rotation}.");
                return rotation;
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

    internal int GetOrganizeFormType(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return PdfiumNative.FPDF_GetFormType(_document);
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal bool HasOrganizeMetadata(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            foreach (var tag in OrganizeMetadataTags)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (PdfiumNative.FPDF_GetMetaText(_document, tag, IntPtr.Zero, 0) > 2u)
                    return true;
            }

            return false;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }
}
