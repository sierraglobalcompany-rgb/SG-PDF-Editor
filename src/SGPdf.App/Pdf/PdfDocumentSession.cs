namespace SGPdf.App.Pdf;

public sealed class PdfDocumentSession : IDisposable
{
    private IntPtr _document;

    private PdfDocumentSession(string filePath, IntPtr document)
    {
        FilePath = filePath;
        _document = document;
    }

    public string FilePath { get; }

    public int PageCount
    {
        get
        {
            ThrowIfDisposed();
            return PdfiumNative.FPDF_GetPageCount(_document);
        }
    }

    public static PdfDocumentSession Open(string filePath, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("No se encontró el archivo PDF.", filePath);

        PdfiumRuntime.EnsureInitialized();
        var document = PdfiumNative.FPDF_LoadDocument(filePath, password);

        if (document == IntPtr.Zero)
            throw new InvalidOperationException($"PDFium no pudo abrir el documento. Error: {PdfiumNative.FPDF_GetLastError()}.");

        return new PdfDocumentSession(Path.GetFullPath(filePath), document);
    }

    public (double Width, double Height) GetPageSize(int pageIndex)
    {
        ThrowIfDisposed();

        if (pageIndex < 0 || pageIndex >= PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));

        var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
        if (page == IntPtr.Zero)
            throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1}.");

        try
        {
            return (PdfiumNative.FPDF_GetPageWidthF(page), PdfiumNative.FPDF_GetPageHeightF(page));
        }
        finally
        {
            PdfiumNative.FPDF_ClosePage(page);
        }
    }

    public void Dispose()
    {
        if (_document == IntPtr.Zero)
            return;

        PdfiumNative.FPDF_CloseDocument(_document);
        _document = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_document == IntPtr.Zero, this);
    }
}
