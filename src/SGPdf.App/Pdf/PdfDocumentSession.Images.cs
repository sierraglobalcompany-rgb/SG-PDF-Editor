namespace SGPdf.App.Pdf;

internal readonly record struct PdfObjectMatrix(
    double A,
    double B,
    double C,
    double D,
    double E,
    double F);

internal readonly record struct PdfObjectBounds(
    double Left,
    double Bottom,
    double Right,
    double Top);

internal readonly record struct PdfImageObjectMetadata(
    uint PixelWidth,
    uint PixelHeight,
    uint BitsPerPixel,
    int ColorSpace);

internal sealed record PdfImageObjectInfo(
    int PageIndex,
    int PageObjectIndex,
    PdfObjectMatrix Matrix,
    PdfObjectBounds Bounds,
    PdfImageObjectMetadata Metadata);

public sealed partial class PdfDocumentSession
{
    internal IReadOnlyList<PdfImageObjectInfo> GetImageObjects(
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
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para inspeccionar imágenes.");

            try
            {
                var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
                if (objectCount < 0)
                    throw new InvalidOperationException("PDFium devolvió una cantidad inválida de objetos de página.");

                var images = new List<PdfImageObjectInfo>();
                for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var pageObject = PdfiumNative.FPDFPage_GetObject(page, objectIndex);
                    if (pageObject == IntPtr.Zero)
                        throw new InvalidOperationException($"PDFium no pudo resolver el objeto {objectIndex} de la página {pageIndex + 1}.");

                    if (PdfiumNative.FPDFPageObj_GetType(pageObject) != PdfiumNative.FPDF_PAGEOBJ_IMAGE)
                        continue;

                    if (PdfiumNative.FPDFPageObj_GetMatrix(pageObject, out var matrix) == 0)
                        throw new InvalidOperationException("PDFium no pudo consultar la matriz de una imagen.");

                    if (PdfiumNative.FPDFPageObj_GetBounds(
                            pageObject,
                            out var left,
                            out var bottom,
                            out var right,
                            out var top) == 0)
                    {
                        throw new InvalidOperationException("PDFium no pudo consultar los límites de una imagen.");
                    }

                    if (PdfiumNative.FPDFImageObj_GetImageMetadata(pageObject, page, out var metadata) == 0)
                        throw new InvalidOperationException("PDFium no pudo consultar los metadatos de una imagen.");

                    var managedMatrix = new PdfObjectMatrix(
                        matrix.A,
                        matrix.B,
                        matrix.C,
                        matrix.D,
                        matrix.E,
                        matrix.F);
                    var managedBounds = new PdfObjectBounds(left, bottom, right, top);
                    var managedMetadata = new PdfImageObjectMetadata(
                        metadata.Width,
                        metadata.Height,
                        metadata.BitsPerPixel,
                        metadata.ColorSpace);

                    ValidateImageSnapshot(managedMatrix, managedBounds, managedMetadata);
                    images.Add(new PdfImageObjectInfo(
                        pageIndex,
                        objectIndex,
                        managedMatrix,
                        managedBounds,
                        managedMetadata));
                }

                return images.AsReadOnly();
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

    private static void ValidateImageSnapshot(
        PdfObjectMatrix matrix,
        PdfObjectBounds bounds,
        PdfImageObjectMetadata metadata)
    {
        if (!double.IsFinite(matrix.A) || !double.IsFinite(matrix.B) ||
            !double.IsFinite(matrix.C) || !double.IsFinite(matrix.D) ||
            !double.IsFinite(matrix.E) || !double.IsFinite(matrix.F))
        {
            throw new InvalidOperationException("PDFium devolvió una matriz de imagen inválida.");
        }

        if (!double.IsFinite(bounds.Left) || !double.IsFinite(bounds.Bottom) ||
            !double.IsFinite(bounds.Right) || !double.IsFinite(bounds.Top) ||
            bounds.Right <= bounds.Left || bounds.Top <= bounds.Bottom)
        {
            throw new InvalidOperationException("PDFium devolvió límites de imagen inválidos.");
        }

        if (metadata.PixelWidth == 0 || metadata.PixelHeight == 0)
            throw new InvalidOperationException("PDFium devolvió dimensiones de imagen inválidas.");
    }
}
