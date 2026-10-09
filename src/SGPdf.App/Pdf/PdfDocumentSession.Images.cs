using System.Runtime.InteropServices;

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

internal sealed record PdfImageBitmap(
    int PixelWidth,
    int PixelHeight,
    int Stride,
    ReadOnlyMemory<byte> BgraPixels);

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

    internal PdfImageBitmap GetImageBitmap(
        int pageIndex,
        int pageObjectIndex,
        bool preferRendered,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);
        if (pageObjectIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageObjectIndex));

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para extraer la imagen.");

            IntPtr bitmap = IntPtr.Zero;
            try
            {
                var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
                if (objectCount < 0 || pageObjectIndex >= objectCount)
                    throw new ArgumentOutOfRangeException(nameof(pageObjectIndex));

                var pageObject = PdfiumNative.FPDFPage_GetObject(page, pageObjectIndex);
                if (pageObject == IntPtr.Zero || PdfiumNative.FPDFPageObj_GetType(pageObject) != PdfiumNative.FPDF_PAGEOBJ_IMAGE)
                    throw new InvalidOperationException("El objeto solicitado no es una imagen PDF editable.");

                _ = preferRendered; // Task 1 characterizes the optional rendered-object route separately.
                bitmap = PdfiumNative.FPDFImageObj_GetBitmap(pageObject);
                if (bitmap == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium no pudo obtener el bitmap de la imagen.");

                return CopyBitmapAsBgra(bitmap, cancellationToken);
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

    private static PdfImageBitmap CopyBitmapAsBgra(IntPtr bitmap, CancellationToken cancellationToken)
    {
        var width = PdfiumNative.FPDFBitmap_GetWidth(bitmap);
        var height = PdfiumNative.FPDFBitmap_GetHeight(bitmap);
        var sourceStride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
        var format = PdfiumNative.FPDFBitmap_GetFormat(bitmap);
        var buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);

        if (width <= 0 || height <= 0 || sourceStride <= 0 || buffer == IntPtr.Zero)
            throw new InvalidOperationException("PDFium devolvió un bitmap de imagen inválido.");

        var sourceBytes = new byte[checked(sourceStride * height)];
        Marshal.Copy(buffer, sourceBytes, 0, sourceBytes.Length);
        cancellationToken.ThrowIfCancellationRequested();

        var outputStride = checked(width * 4);
        var output = new byte[checked(outputStride * height)];
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceRow = y * sourceStride;
            var outputRow = y * outputStride;

            for (var x = 0; x < width; x++)
            {
                var destination = outputRow + (x * 4);
                switch (format)
                {
                    case PdfiumNative.FPDFBitmap_Gray:
                    {
                        var gray = sourceBytes[sourceRow + x];
                        output[destination] = gray;
                        output[destination + 1] = gray;
                        output[destination + 2] = gray;
                        output[destination + 3] = 255;
                        break;
                    }
                    case PdfiumNative.FPDFBitmap_BGR:
                    {
                        var source = sourceRow + (x * 3);
                        output[destination] = sourceBytes[source];
                        output[destination + 1] = sourceBytes[source + 1];
                        output[destination + 2] = sourceBytes[source + 2];
                        output[destination + 3] = 255;
                        break;
                    }
                    case PdfiumNative.FPDFBitmap_BGRx:
                    case PdfiumNative.FPDFBitmap_BGRA:
                    {
                        var source = sourceRow + (x * 4);
                        output[destination] = sourceBytes[source];
                        output[destination + 1] = sourceBytes[source + 1];
                        output[destination + 2] = sourceBytes[source + 2];
                        output[destination + 3] = format == PdfiumNative.FPDFBitmap_BGRA
                            ? sourceBytes[source + 3]
                            : (byte)255;
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Formato de bitmap PDFium no soportado: {format}.");
                }
            }
        }

        return new PdfImageBitmap(width, height, outputStride, output);
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
