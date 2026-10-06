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
        return Render(session, pageIndex, pixelWidth, pixelHeight, CancellationToken.None);
    }

    public static PdfRenderBitmap Render(
        PdfDocumentSession session,
        int pageIndex,
        int pixelWidth,
        int pixelHeight,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        if (pixelWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        if (pixelHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));

        return session.WithDocument(document =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageIndex < 0 || pageIndex >= pageCount)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));

            var page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1}.");

            IntPtr bitmap = IntPtr.Zero;
            var progressiveRenderStarted = false;
            PdfiumNative.NeedToPauseNowCallback pauseCallback = _ => cancellationToken.IsCancellationRequested ? 1 : 0;
            var pause = new PdfiumNative.IfSdkPause
            {
                Version = 1,
                NeedToPauseNow = pauseCallback,
                User = IntPtr.Zero
            };

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

                var status = PdfiumNative.FPDF_RenderPageBitmap_Start(
                    bitmap,
                    page,
                    0,
                    0,
                    pixelWidth,
                    pixelHeight,
                    rotate: 0,
                    flags: 0,
                    ref pause);
                progressiveRenderStarted = true;

                while (status is PdfiumNative.FPDF_RENDER_READY or PdfiumNative.FPDF_RENDER_TOBECONTINUED)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    status = PdfiumNative.FPDF_RenderPage_Continue(page, ref pause);
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (status != PdfiumNative.FPDF_RENDER_DONE)
                    throw new InvalidOperationException($"PDFium no pudo completar el render progresivo. Estado: {status}.");

                var stride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
                var buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
                if (stride <= 0 || buffer == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium devolvió un bitmap inválido.");

                var pixels = new byte[checked(stride * pixelHeight)];
                Marshal.Copy(buffer, pixels, 0, pixels.Length);

                GC.KeepAlive(pauseCallback);
                return new PdfRenderBitmap(pixelWidth, pixelHeight, stride, pixels);
            }
            finally
            {
                if (progressiveRenderStarted)
                    PdfiumNative.FPDF_RenderPage_Close(page);

                if (bitmap != IntPtr.Zero)
                    PdfiumNative.FPDFBitmap_Destroy(bitmap);

                PdfiumNative.FPDF_ClosePage(page);
            }
        });
    }
}
