using System.Runtime.InteropServices;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Sign;

internal sealed class PdfVisualSignatureWriter
{
    private readonly Func<IntPtr, IntPtr, uint, int> _saveAsCopy;
    private readonly Action<string, int, IReadOnlyCollection<int>> _validateOutput;

    internal PdfVisualSignatureWriter(
        Func<IntPtr, IntPtr, uint, int>? saveAsCopyOverride = null,
        Action<string, int, IReadOnlyCollection<int>>? validateOutputOverride = null)
    {
        _saveAsCopy = saveAsCopyOverride ?? PdfiumNative.FPDF_SaveAsCopy;
        _validateOutput = validateOutputOverride ?? ValidateOutput;
    }

    internal void SaveAsCopy(
        string sourcePath,
        string destinationPath,
        IReadOnlyList<SignaturePlacement> placements,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(placements);
        if (placements.Count == 0)
            throw new ArgumentException("At least one visual signature placement is required.", nameof(placements));

        var sourceFullPath = Path.GetFullPath(sourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Guardar como requiere una ruta diferente al PDF original.", nameof(destinationPath));
        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException("No se encontró el PDF original.", sourceFullPath);

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath)
            ?? throw new ArgumentException("La ruta de destino no tiene directorio.", nameof(destinationPath));
        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException(destinationDirectory);

        var destinationFileName = Path.GetFileName(destinationFullPath);
        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{destinationFileName}.{Guid.NewGuid():N}.sgpdf.tmp");

        var affectedPages = placements.Select(placement => placement.PageIndex).Distinct().Order().ToArray();
        var sourcePageCount = 0;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourcePageCount = WriteTemporaryCopy(
                sourceFullPath,
                temporaryPath,
                placements,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _validateOutput(temporaryPath, sourcePageCount, affectedPages);
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(destinationFullPath))
                File.Replace(temporaryPath, destinationFullPath, null, ignoreMetadataErrors: true);
            else
                File.Move(temporaryPath, destinationFullPath);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private int WriteTemporaryCopy(
        string sourcePath,
        string temporaryPath,
        IReadOnlyList<SignaturePlacement> placements,
        CancellationToken cancellationToken)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);

        IntPtr document = IntPtr.Zero;
        var loadedPages = new Dictionary<int, IntPtr>();
        var nativeBitmaps = new List<IntPtr>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
            {
                var error = PdfiumNative.FPDF_GetLastError();
                throw new InvalidOperationException($"PDFium no pudo abrir el PDF para firmarlo. Error: {error}.");
            }

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageCount <= 0)
                throw new InvalidOperationException("El PDF no contiene páginas válidas.");

            foreach (var group in placements.GroupBy(placement => placement.PageIndex).OrderBy(group => group.Key))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (group.Key < 0 || group.Key >= pageCount)
                    throw new ArgumentOutOfRangeException(nameof(placements), "La firma apunta a una página inexistente.");

                var page = PdfiumNative.FPDF_LoadPage(document, group.Key);
                if (page == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la página {group.Key + 1} para firmarla.");
                loadedPages.Add(group.Key, page);

                var visibleBounds = GetVisiblePageBounds(page);
                foreach (var placement in group)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ValidatePlacement(placement, visibleBounds);
                    var bitmap = CreateBitmap(placement.Asset);
                    nativeBitmaps.Add(bitmap);
                    InsertImage(document, page, bitmap, placement.Bounds);
                }

                if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                    throw new InvalidOperationException($"PDFium no pudo regenerar el contenido de la página {group.Key + 1}.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveDocument(document, temporaryPath);
            return pageCount;
        }
        finally
        {
            foreach (var page in loadedPages.Values)
            {
                if (page != IntPtr.Zero)
                    PdfiumNative.FPDF_ClosePage(page);
            }

            foreach (var bitmap in nativeBitmaps)
            {
                if (bitmap != IntPtr.Zero)
                    PdfiumNative.FPDFBitmap_Destroy(bitmap);
            }

            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);

            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static PdfRect GetVisiblePageBounds(IntPtr page)
    {
        var pageWidth = PdfiumNative.FPDF_GetPageWidthF(page);
        var pageHeight = PdfiumNative.FPDF_GetPageHeightF(page);
        var deviceWidth = Math.Max(1, checked((int)Math.Ceiling(pageWidth)));
        var deviceHeight = Math.Max(1, checked((int)Math.Ceiling(pageHeight)));

        if (PdfiumNative.FPDF_DeviceToPage(
                page, 0, 0, deviceWidth, deviceHeight, 0, 0, 0,
                out var x0, out var y0) == 0 ||
            PdfiumNative.FPDF_DeviceToPage(
                page, 0, 0, deviceWidth, deviceHeight, 0, deviceWidth, deviceHeight,
                out var x1, out var y1) == 0)
        {
            throw new InvalidOperationException("PDFium no pudo determinar los límites visibles de la página.");
        }

        return new PdfRect(
            Math.Min(x0, x1),
            Math.Min(y0, y1),
            Math.Abs(x1 - x0),
            Math.Abs(y1 - y0));
    }

    private static void ValidatePlacement(SignaturePlacement placement, PdfRect pageBounds)
    {
        ArgumentNullException.ThrowIfNull(placement.Asset);
        var bounds = placement.Bounds;
        if (!double.IsFinite(bounds.Left) || !double.IsFinite(bounds.Bottom) ||
            !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) ||
            bounds.Width <= 0d || bounds.Height <= 0d)
        {
            throw new ArgumentException("La geometría de la firma no es válida.", nameof(placement));
        }

        const double tolerance = 0.01d;
        if (bounds.Left < pageBounds.Left - tolerance ||
            bounds.Bottom < pageBounds.Bottom - tolerance ||
            bounds.Right > pageBounds.Right + tolerance ||
            bounds.Top > pageBounds.Top + tolerance)
        {
            throw new ArgumentOutOfRangeException(nameof(placement), "La firma queda fuera de los límites de la página.");
        }
    }

    private static IntPtr CreateBitmap(SignatureAsset asset)
    {
        var bitmap = PdfiumNative.FPDFBitmap_CreateEx(
            asset.PixelWidth,
            asset.PixelHeight,
            PdfiumNative.FPDFBitmap_BGRA,
            IntPtr.Zero,
            0);
        if (bitmap == IntPtr.Zero)
            throw new InvalidOperationException("PDFium no pudo crear el bitmap de la firma.");

        try
        {
            var destination = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
            var destinationStride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
            if (destination == IntPtr.Zero || destinationStride < checked(asset.PixelWidth * 4))
                throw new InvalidOperationException("PDFium devolvió un bitmap de firma inválido.");

            var source = asset.BgraPixels.ToArray();
            var bytesPerRow = checked(asset.PixelWidth * 4);
            for (var row = 0; row < asset.PixelHeight; row++)
            {
                Marshal.Copy(
                    source,
                    checked(row * asset.Stride),
                    IntPtr.Add(destination, checked(row * destinationStride)),
                    bytesPerRow);
            }

            return bitmap;
        }
        catch
        {
            PdfiumNative.FPDFBitmap_Destroy(bitmap);
            throw;
        }
    }

    private static void InsertImage(IntPtr document, IntPtr page, IntPtr bitmap, PdfRect bounds)
    {
        var imageObject = PdfiumNative.FPDFPageObj_NewImageObj(document);
        if (imageObject == IntPtr.Zero)
            throw new InvalidOperationException("PDFium no pudo crear el objeto de imagen para la firma.");

        var ownershipTransferred = false;
        try
        {
            if (PdfiumNative.FPDFImageObj_SetBitmap(IntPtr.Zero, 0, imageObject, bitmap) == 0)
                throw new InvalidOperationException("PDFium no pudo asignar el bitmap de la firma.");

            if (PdfiumNative.FPDFImageObj_SetMatrix(
                    imageObject,
                    bounds.Width,
                    0d,
                    0d,
                    bounds.Height,
                    bounds.Left,
                    bounds.Bottom) == 0)
            {
                throw new InvalidOperationException("PDFium no pudo posicionar la firma.");
            }

            if (PdfiumNative.FPDFPage_InsertObject(page, imageObject) == 0)
            {
                // Current PDFium frees the object when insertion fails.
                imageObject = IntPtr.Zero;
                throw new InvalidOperationException("PDFium no pudo insertar la firma en la página.");
            }

            ownershipTransferred = true;
        }
        finally
        {
            if (!ownershipTransferred && imageObject != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(imageObject);
        }
    }

    private void SaveDocument(IntPtr document, string temporaryPath)
    {
        using var stream = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        var context = new FileWriteContext(stream);
        var handle = GCHandle.Alloc(context);
        IntPtr nativeWrite = IntPtr.Zero;
        PdfiumNative.FileWriteBlock? writeBlock = WriteBlock;
        try
        {
            var fileWrite = new PdfiumNative.ExtendedFileWrite
            {
                Version = 1,
                WriteBlock = Marshal.GetFunctionPointerForDelegate(writeBlock),
                Context = GCHandle.ToIntPtr(handle)
            };

            nativeWrite = Marshal.AllocHGlobal(Marshal.SizeOf<PdfiumNative.ExtendedFileWrite>());
            Marshal.StructureToPtr(fileWrite, nativeWrite, fDeleteOld: false);

            if (_saveAsCopy(document, nativeWrite, 0u) == 0)
                throw context.Error ?? new InvalidOperationException("PDFium no pudo guardar la copia firmada.");
            if (context.Error is not null)
                throw context.Error;

            stream.Flush(flushToDisk: true);
        }
        finally
        {
            GC.KeepAlive(writeBlock);
            if (nativeWrite != IntPtr.Zero)
                Marshal.FreeHGlobal(nativeWrite);
            if (handle.IsAllocated)
                handle.Free();
        }
    }

    private static int WriteBlock(IntPtr self, IntPtr data, uint size)
    {
        try
        {
            var fileWrite = Marshal.PtrToStructure<PdfiumNative.ExtendedFileWrite>(self);
            var handle = GCHandle.FromIntPtr(fileWrite.Context);
            var context = (FileWriteContext?)handle.Target;
            if (context is null || size > int.MaxValue)
                return 0;

            var buffer = new byte[(int)size];
            Marshal.Copy(data, buffer, 0, buffer.Length);
            context.Stream.Write(buffer, 0, buffer.Length);
            return 1;
        }
        catch (Exception ex)
        {
            try
            {
                var fileWrite = Marshal.PtrToStructure<PdfiumNative.ExtendedFileWrite>(self);
                var handle = GCHandle.FromIntPtr(fileWrite.Context);
                if (handle.Target is FileWriteContext context)
                    context.Error = ex;
            }
            catch
            {
                // The native callback must never throw across the ABI boundary.
            }
            return 0;
        }
    }

    private static void ValidateOutput(
        string path,
        int expectedPageCount,
        IReadOnlyCollection<int> affectedPages)
    {
        using var session = PdfDocumentSession.Open(path);
        if (session.PageCount != expectedPageCount)
            throw new InvalidDataException("La copia firmada cambió inesperadamente la cantidad de páginas.");

        foreach (var pageIndex in affectedPages)
            _ = session.RenderPage(pageIndex, 96d);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup must not hide the primary save failure.
        }
    }

    private sealed class FileWriteContext
    {
        internal FileWriteContext(FileStream stream) => Stream = stream;
        internal FileStream Stream { get; }
        internal Exception? Error { get; set; }
    }
}
