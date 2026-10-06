using System.IO;

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

    public int PageCount => WithDocument(PdfiumNative.FPDF_GetPageCount);

    public static PdfDocumentSession Open(string filePath, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("No se encontró el archivo PDF.", filePath);

        var document = PdfiumRuntime.RunExclusive(() =>
        {
            var handle = PdfiumNative.FPDF_LoadDocument(filePath, password);
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException($"PDFium no pudo abrir el documento. Error: {PdfiumNative.FPDF_GetLastError()}.");

            return handle;
        });

        return new PdfDocumentSession(Path.GetFullPath(filePath), document);
    }

    public (double Width, double Height) GetPageSize(int pageIndex)
    {
        return WithDocument(document =>
        {
            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageIndex < 0 || pageIndex >= pageCount)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));

            var page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1}.");

            try
            {
                return ((double)PdfiumNative.FPDF_GetPageWidthF(page), (double)PdfiumNative.FPDF_GetPageHeightF(page));
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        });
    }

    internal T WithDocument<T>(Func<IntPtr, T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return PdfiumRuntime.RunExclusive(() =>
        {
            ThrowIfDisposed();
            return action(_document);
        });
    }

    public void Dispose()
    {
        PdfiumRuntime.RunExclusive(() =>
        {
            if (_document == IntPtr.Zero)
                return;

            PdfiumNative.FPDF_CloseDocument(_document);
            _document = IntPtr.Zero;
        });

        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_document == IntPtr.Zero, this);
    }
}
