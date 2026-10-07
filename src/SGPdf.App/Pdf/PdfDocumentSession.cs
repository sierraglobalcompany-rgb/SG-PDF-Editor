using System.IO;
using System.Runtime.InteropServices;

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

    public int PageCount => GetPageCount(CancellationToken.None);

    public static PdfDocumentSession Open(string filePath, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("No se encontró el archivo PDF.", filePath);

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        try
        {
            var document = PdfiumNative.FPDF_LoadDocument(filePath, password);
            if (document == IntPtr.Zero)
            {
                var error = PdfiumNative.FPDF_GetLastError();
                throw new InvalidOperationException($"PDFium no pudo abrir el documento. Error: {error}.");
            }

            return new PdfDocumentSession(Path.GetFullPath(filePath), document);
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    public (double Width, double Height) GetPageSize(int pageIndex)
    {
        return GetPageSize(pageIndex, CancellationToken.None);
    }

    public (double Width, double Height) GetPageSize(
        int pageIndex,
        CancellationToken cancellationToken)
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
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1}.");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return (PdfiumNative.FPDF_GetPageWidthF(page), PdfiumNative.FPDF_GetPageHeightF(page));
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

    public PdfRenderedPage RenderPage(int pageIndex, double dpi = 96d)
    {
        return RenderPage(pageIndex, dpi, CancellationToken.None);
    }

    public PdfRenderedPage RenderPage(
        int pageIndex,
        double dpi,
        CancellationToken cancellationToken)
    {
        if (!double.IsFinite(dpi) || dpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi), "El DPI debe ser un número positivo.");

        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1}.");

            IntPtr bitmap = IntPtr.Zero;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageWidth = PdfiumNative.FPDF_GetPageWidthF(page);
                var pageHeight = PdfiumNative.FPDF_GetPageHeightF(page);
                var pixelWidth = Math.Max(1, checked((int)Math.Ceiling(pageWidth * dpi / 72d)));
                var pixelHeight = Math.Max(1, checked((int)Math.Ceiling(pageHeight * dpi / 72d)));
                var deviceTransform = CreateDeviceTransform(page, pixelWidth, pixelHeight);

                bitmap = PdfiumNative.FPDFBitmap_Create(pixelWidth, pixelHeight, 1);
                if (bitmap == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium no pudo crear el bitmap de renderizado.");

                if (PdfiumNative.FPDFBitmap_FillRect(bitmap, 0, 0, pixelWidth, pixelHeight, 0xFFFFFFFF) == 0)
                    throw new InvalidOperationException("PDFium no pudo preparar el bitmap de renderizado.");

                cancellationToken.ThrowIfCancellationRequested();

                PdfiumNative.FPDF_RenderPageBitmap(
                    bitmap,
                    page,
                    0,
                    0,
                    pixelWidth,
                    pixelHeight,
                    0,
                    0);

                // PDFium's current full-page render call is synchronous. We cannot safely
                // interrupt it mid-call without moving to progressive rendering, but a
                // canceled request must never continue into buffer copy or UI publication.
                cancellationToken.ThrowIfCancellationRequested();

                var stride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
                var buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
                if (stride <= 0 || buffer == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium devolvió un bitmap de renderizado inválido.");

                var pixels = new byte[checked(stride * pixelHeight)];
                Marshal.Copy(buffer, pixels, 0, pixels.Length);
                cancellationToken.ThrowIfCancellationRequested();

                return new PdfRenderedPage(pageIndex, pixelWidth, pixelHeight, stride, dpi, pixels)
                {
                    DeviceTransform = deviceTransform
                };
            }
            finally
            {
                if (bitmap != IntPtr.Zero)
                    PdfiumNative.FPDFBitmap_Destroy(bitmap);

                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    public int GetCryptographicSignatureCount()
    {
        ThrowIfDisposed();
        PdfiumRuntime.NativeGate.Wait();
        try
        {
            ThrowIfDisposed();
            var count = PdfiumNative.FPDF_GetSignatureCount(_document);
            if (count < 0)
                throw new InvalidOperationException("PDFium no pudo consultar las firmas criptográficas del documento.");
            return count;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    public void Dispose()
    {
        if (_document == IntPtr.Zero)
            return;

        PdfiumRuntime.NativeGate.Wait();
        try
        {
            if (_document == IntPtr.Zero)
                return;

            PdfiumNative.FPDF_CloseDocument(_document);
            _document = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static PdfPageDeviceTransform CreateDeviceTransform(
        IntPtr page,
        int pixelWidth,
        int pixelHeight)
    {
        if (PdfiumNative.FPDF_DeviceToPage(
                page, 0, 0, pixelWidth, pixelHeight, 0, 0, 0,
                out var originX, out var originY) == 0 ||
            PdfiumNative.FPDF_DeviceToPage(
                page, 0, 0, pixelWidth, pixelHeight, 0, pixelWidth, 0,
                out var xAnchorX, out var xAnchorY) == 0 ||
            PdfiumNative.FPDF_DeviceToPage(
                page, 0, 0, pixelWidth, pixelHeight, 0, 0, pixelHeight,
                out var yAnchorX, out var yAnchorY) == 0)
        {
            throw new InvalidOperationException("PDFium no pudo convertir las coordenadas de la página.");
        }

        var transform = new PdfPageDeviceTransform(
            originX,
            originY,
            (xAnchorX - originX) / pixelWidth,
            (xAnchorY - originY) / pixelWidth,
            (yAnchorX - originX) / pixelHeight,
            (yAnchorY - originY) / pixelHeight,
            pixelWidth,
            pixelHeight);
        transform.Validate();
        return transform;
    }

    private int GetPageCount(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return PdfiumNative.FPDF_GetPageCount(_document);
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private void ValidatePageIndex(int pageIndex, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        var pageCount = GetPageCount(cancellationToken);
        if (pageIndex < 0 || pageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_document == IntPtr.Zero, this);
    }
}
