using System.IO;
using System.Runtime.InteropServices;
using SGPdf.App.Features.Utilities;

namespace SGPdf.App.Pdf;

internal static class PdfImageWatermarkWriter
{
    internal static void WriteTemporaryCopy(
        string sourcePath,
        string temporaryPath,
        PdfImageWatermarkAsset asset,
        IReadOnlyList<PdfImageWatermarkPlacement> placements,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(placements);
        if (placements.Count == 0)
            throw new ArgumentException("No hay páginas seleccionadas para la marca de agua.", nameof(placements));
        if (asset.PixelWidth <= 0 || asset.PixelHeight <= 0 || asset.Stride < checked(asset.PixelWidth * 4))
            throw new InvalidDataException("La imagen decodificada tiene dimensiones inválidas.");

        var pixels = asset.BgraPixels.ToArray();
        if (pixels.Length != checked(asset.Stride * asset.PixelHeight))
            throw new InvalidDataException("La imagen decodificada no contiene todos sus píxeles.");

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        IntPtr document = IntPtr.Zero;
        IntPtr bitmap = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            bitmap = CreateBitmap(asset, pixels, cancellationToken);
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo abrir el PDF original para insertar la imagen.");

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageCount <= 0 || placements.Any(item => item.PageIndex < 0 || item.PageIndex >= pageCount))
                throw new InvalidOperationException("Una página seleccionada no existe en el PDF original.");

            foreach (var placement in placements.OrderBy(item => item.PageIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();
                InsertImage(document, placement, bitmap);
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveDocument(document, temporaryPath);
        }
        finally
        {
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            if (bitmap != IntPtr.Zero)
                PdfiumNative.FPDFBitmap_Destroy(bitmap);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static IntPtr CreateBitmap(
        PdfImageWatermarkAsset asset,
        byte[] pixels,
        CancellationToken cancellationToken)
    {
        var bitmap = PdfiumNative.FPDFBitmap_Create(asset.PixelWidth, asset.PixelHeight, 1);
        if (bitmap == IntPtr.Zero)
            throw new InvalidOperationException("PDFium no pudo crear el bitmap de la marca de agua.");

        try
        {
            var width = PdfiumNative.FPDFBitmap_GetWidth(bitmap);
            var height = PdfiumNative.FPDFBitmap_GetHeight(bitmap);
            var stride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
            var buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
            if (width != asset.PixelWidth || height != asset.PixelHeight ||
                stride < checked(asset.PixelWidth * 4) || buffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("PDFium devolvió un bitmap inválido para la marca de agua.");
            }

            var rowBytes = checked(asset.PixelWidth * 4);
            for (var row = 0; row < asset.PixelHeight; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Marshal.Copy(pixels, checked(row * asset.Stride), IntPtr.Add(buffer, checked(row * stride)), rowBytes);
            }

            return bitmap;
        }
        catch
        {
            PdfiumNative.FPDFBitmap_Destroy(bitmap);
            throw;
        }
    }

    private static void InsertImage(
        IntPtr document,
        PdfImageWatermarkPlacement placement,
        IntPtr bitmap)
    {
        var page = PdfiumNative.FPDF_LoadPage(document, placement.PageIndex);
        if (page == IntPtr.Zero)
            throw new InvalidOperationException($"PDFium no pudo cargar la página {placement.PageIndex + 1}.");

        try
        {
            var imageObject = PdfiumNative.FPDFPageObj_NewImageObj(document);
            if (imageObject == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo crear el objeto de imagen.");

            var inserted = false;
            IntPtr pages = IntPtr.Zero;
            try
            {
                pages = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(pages, page);
                if (PdfiumNative.FPDFImageObj_SetBitmap(pages, 1, imageObject, bitmap) == 0)
                    throw new InvalidOperationException("PDFium no pudo asignar los píxeles BGRA a la imagen.");

                var matrix = placement.Matrix;
                if (PdfiumNative.FPDFImageObj_SetMatrix(
                        imageObject,
                        matrix.A,
                        matrix.B,
                        matrix.C,
                        matrix.D,
                        matrix.E,
                        matrix.F) == 0)
                {
                    throw new InvalidOperationException("PDFium no pudo ubicar la marca de agua en la página.");
                }

                if (PdfiumNative.FPDFPage_InsertObject(page, imageObject) == 0)
                    throw new InvalidOperationException("PDFium no pudo insertar la marca de agua en la página.");
                inserted = true;
            }
            finally
            {
                if (pages != IntPtr.Zero)
                    Marshal.FreeHGlobal(pages);
                if (!inserted)
                    PdfiumNative.FPDFPageObj_Destroy(imageObject);
            }

            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException($"PDFium no pudo generar el contenido de la página {placement.PageIndex + 1}.");
        }
        finally
        {
            PdfiumNative.FPDF_ClosePage(page);
        }
    }

    private static void SaveDocument(IntPtr document, string path)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var context = new NativeFileWriteContext(stream);
        var contextHandle = GCHandle.Alloc(context);
        IntPtr nativeWrite = IntPtr.Zero;
        PdfiumNative.FileWriteBlock callback = WriteBlock;
        try
        {
            var fileWrite = new PdfiumNative.ExtendedFileWrite
            {
                Version = 1,
                WriteBlock = Marshal.GetFunctionPointerForDelegate(callback),
                Context = GCHandle.ToIntPtr(contextHandle)
            };
            nativeWrite = Marshal.AllocHGlobal(Marshal.SizeOf<PdfiumNative.ExtendedFileWrite>());
            Marshal.StructureToPtr(fileWrite, nativeWrite, false);
            if (PdfiumNative.FPDF_SaveAsCopy(document, nativeWrite, 0u) == 0)
                throw context.Error ?? new InvalidOperationException("PDFium no pudo guardar la copia temporal.");
            if (context.Error is not null)
                throw context.Error;
            stream.Flush(flushToDisk: true);
        }
        finally
        {
            GC.KeepAlive(callback);
            if (nativeWrite != IntPtr.Zero)
                Marshal.FreeHGlobal(nativeWrite);
            if (contextHandle.IsAllocated)
                contextHandle.Free();
        }
    }

    private static int WriteBlock(IntPtr self, IntPtr data, uint size)
    {
        try
        {
            var writeInfo = Marshal.PtrToStructure<PdfiumNative.ExtendedFileWrite>(self);
            var handle = GCHandle.FromIntPtr(writeInfo.Context);
            if (handle.Target is not NativeFileWriteContext context || size > int.MaxValue)
                return 0;

            var buffer = new byte[checked((int)size)];
            Marshal.Copy(data, buffer, 0, buffer.Length);
            context.Stream.Write(buffer, 0, buffer.Length);
            return 1;
        }
        catch (Exception exception)
        {
            try
            {
                var writeInfo = Marshal.PtrToStructure<PdfiumNative.ExtendedFileWrite>(self);
                var handle = GCHandle.FromIntPtr(writeInfo.Context);
                if (handle.Target is NativeFileWriteContext context)
                    context.Error = exception;
            }
            catch
            {
            }
            return 0;
        }
    }

    private sealed class NativeFileWriteContext(FileStream stream)
    {
        internal FileStream Stream { get; } = stream;
        internal Exception? Error { get; set; }
    }
}
