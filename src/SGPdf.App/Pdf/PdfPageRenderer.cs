using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

public sealed record PdfRenderBitmap(int Width, int Height, int Stride, byte[] Pixels);

public static class PdfPageRenderer
{
    public static PdfRenderBitmap Render(
        PdfDocumentSession session,
        int pageIndex,
        int pixelWidth,
        int pixelHeight)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (pixelWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        if (pixelHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));

        return session.WithDocument(document =>
        {
            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageIndex < 0 || pageIndex >= pageCount)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));

            var page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1}.");

            IntPtr bitmap = IntPtr.Zero;
            try
            {
                bitmap = PdfiumNative.FPDFBitmap_Create(pixelWidth, pixelHeight, alpha: 1);
                if (bitmap == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium no pudo crear el bitmap de renderizado.");

                _ = PdfiumNative.FPDFBitmap_FillRect(
                    bitmap,
                    0,
                    0,
                    pixelWidth,
                    pixelHeight,
                    0xFFFFFFFF);

                PdfiumNative.FPDF_RenderPageBitmap(
                    bitmap,
                    page,
                    0,
                    0,
                    pixelWidth,
                    pixelHeight,
                    rotate: 0,
                    flags: 0);

                var stride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
                var buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
                if (stride <= 0 || buffer == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium devolvió un bitmap inválido.");

                var pixels = new byte[checked(stride * pixelHeight)];
                Marshal.Copy(buffer, pixels, 0, pixels.Length);

                return new PdfRenderBitmap(pixelWidth, pixelHeight, stride, pixels);
            }
            finally
            {
                if (bitmap != IntPtr.Zero)
                    PdfiumNative.FPDFBitmap_Destroy(bitmap);

                PdfiumNative.FPDF_ClosePage(page);
            }
        });
    }
}
