using System.IO;
using System.Runtime.InteropServices;
using SGPdf.App.Features.Edit.Images;

namespace SGPdf.App.Pdf;

internal sealed class PdfImageEditWriter
{
    private const double MatrixTolerance = 0.05d;
    private const double BoundsTolerance = 0.10d;

    private readonly Func<IntPtr, IntPtr, uint, int> _saveAsCopy;
    private readonly Action<string, ImageEditWorkspace, CancellationToken> _validateOutput;
    private readonly Func<string, int> _signatureCount;
    private readonly Func<IntPtr, PdfObjectMatrix, int> _setMatrix;

    internal PdfImageEditWriter(
        Func<IntPtr, IntPtr, uint, int>? saveAsCopyOverride = null,
        Action<string, ImageEditWorkspace, CancellationToken>? validateOutputOverride = null,
        Func<string, int>? signatureCountOverride = null,
        Func<IntPtr, PdfObjectMatrix, int>? setMatrixOverride = null)
    {
        _saveAsCopy = saveAsCopyOverride ?? PdfiumNative.FPDF_SaveAsCopy;
        _validateOutput = validateOutputOverride ?? ValidateOutput;
        _signatureCount = signatureCountOverride ?? GetSignatureCount;
        _setMatrix = setMatrixOverride ?? SetMatrix;
    }

    internal void SaveAsCopy(
        ImageEditWorkspace workspace,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        _ = warningsConfirmed; // Task 8 owns non-blocking preservation warnings.

        cancellationToken.ThrowIfCancellationRequested();
        var sourcePath = Path.GetFullPath(workspace.SourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourcePath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Guardar como requiere una ruta diferente al PDF fuente.", nameof(destinationPath));

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath)
            ?? throw new ArgumentException("La ruta de destino no tiene directorio.", nameof(destinationPath));
        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException(destinationDirectory);

        if (workspace.SourceOpenedWithPassword)
            throw new InvalidOperationException("Los PDF abiertos con contraseña no se pueden materializar en EDITAR.");

        if (!workspace.SourceFingerprint.MatchesCurrentFile(sourcePath))
            throw new IOException("El PDF fuente cambió o ya no está disponible.");

        cancellationToken.ThrowIfCancellationRequested();
        int signatureCount;
        try
        {
            signatureCount = _signatureCount(sourcePath);
        }
        catch (PdfDocumentOpenException ex) when (ex.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
        {
            throw new InvalidOperationException("Los PDF protegidos con contraseña no se pueden materializar en EDITAR.", ex);
        }

        if (signatureCount < 0)
            throw new InvalidOperationException("No se pudo comprobar si el PDF contiene firmas criptográficas.");
        if (signatureCount > 0)
            throw new InvalidOperationException("Los PDF con firma criptográfica están bloqueados para edición de imágenes.");

        cancellationToken.ThrowIfCancellationRequested();
        var destinationFileName = Path.GetFileName(destinationFullPath);
        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{destinationFileName}.{Guid.NewGuid():N}.sgpdf.tmp");

        try
        {
            WriteTemporaryCopy(workspace, temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _validateOutput(temporaryPath, workspace, cancellationToken);
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

    private void WriteTemporaryCopy(
        ImageEditWorkspace workspace,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);

        IntPtr document = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            document = PdfiumNative.FPDF_LoadDocument(workspace.SourcePath, null);
            if (document == IntPtr.Zero)
            {
                var error = PdfiumNative.FPDF_GetLastError();
                throw new InvalidOperationException($"PDFium no pudo reabrir el PDF fuente. Error: {error}.");
            }

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageCount <= 0)
                throw new InvalidOperationException("PDFium devolvió una cantidad de páginas inválida.");

            foreach (var pageGroup in workspace.EditedStates.GroupBy(state => state.ObjectRef.Key.PageIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (pageGroup.Key < 0 || pageGroup.Key >= pageCount)
                    throw new InvalidOperationException("El plan de edición referencia una página inválida.");

                var page = PdfiumNative.FPDF_LoadPage(document, pageGroup.Key);
                if (page == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la página {pageGroup.Key + 1} para materializar imágenes.");

                try
                {
                    var resolved = ResolveAllBeforeMutation(page, pageGroup.ToArray(), cancellationToken);

                    foreach (var item in resolved)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (item.State.Deleted)
                            continue;

                        if (item.State.ReplacementAsset is not null)
                            SetReplacementBitmap(page, item.Handle, item.State.ReplacementAsset, cancellationToken);

                        if (!NearlyEqual(item.State.CurrentMatrix, item.State.ObjectRef.Original.Matrix) &&
                            _setMatrix(item.Handle, item.State.CurrentMatrix) == 0)
                        {
                            throw new InvalidOperationException("PDFium no pudo aplicar la matriz editada a una imagen.");
                        }

                        if (item.State.Opacity is byte alpha &&
                            PdfiumNative.FPDFPageObj_SetFillColor(
                                item.Handle,
                                255u,
                                255u,
                                255u,
                                alpha) == 0)
                        {
                            throw new InvalidOperationException("PDFium no pudo aplicar la opacidad editada a una imagen.");
                        }
                    }

                    foreach (var item in resolved.Where(item => item.State.Deleted))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (PdfiumNative.FPDFPage_RemoveObject(page, item.Handle) == 0)
                            throw new InvalidOperationException("PDFium no pudo eliminar una imagen de la página.");
                        PdfiumNative.FPDFPageObj_Destroy(item.Handle);
                    }

                    foreach (var item in resolved.Where(item => !item.State.Deleted && item.State.TargetObjectIndex is not null))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var target = item.State.TargetObjectIndex!.Value;
                        if (PdfiumNative.FPDFPage_RemoveObject(page, item.Handle) == 0)
                            throw new InvalidOperationException("PDFium no pudo retirar una imagen para reordenarla.");

                        var reinserted = false;
                        try
                        {
                            var remainingCount = PdfiumNative.FPDFPage_CountObjects(page);
                            if (remainingCount < 0 || target < 0 || target > remainingCount)
                                throw new InvalidOperationException("El índice de orden solicitado ya no es válido.");

                            if (PdfiumNative.FPDFPage_InsertObjectAtIndex(page, item.Handle, (nuint)target) == 0)
                                throw new InvalidOperationException("PDFium no pudo reinsertar la imagen en el orden solicitado.");
                            reinserted = true;
                        }
                        finally
                        {
                            if (!reinserted)
                                PdfiumNative.FPDFPageObj_Destroy(item.Handle);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                        throw new InvalidOperationException("PDFium no pudo regenerar el contenido de una página editada.");
                }
                finally
                {
                    PdfiumNative.FPDF_ClosePage(page);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveDocument(document, temporaryPath);
        }
        finally
        {
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static IReadOnlyList<ResolvedImage> ResolveAllBeforeMutation(
        IntPtr page,
        IReadOnlyList<ImageEditState> states,
        CancellationToken cancellationToken)
    {
        var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
        if (objectCount < 0)
            throw new InvalidOperationException("PDFium devolvió una cantidad inválida de objetos de página.");

        var result = new List<ResolvedImage>(states.Count);
        foreach (var state in states.OrderBy(state => state.ObjectRef.Key.PageObjectIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = state.ObjectRef.Original;
            var index = state.ObjectRef.Key.PageObjectIndex;
            if (index < 0 || index >= objectCount)
                throw new InvalidOperationException("No se pudo resolver la imagen original por su ordinal.");

            var handle = PdfiumNative.FPDFPage_GetObject(page, index);
            if (handle == IntPtr.Zero || PdfiumNative.FPDFPageObj_GetType(handle) != PdfiumNative.FPDF_PAGEOBJ_IMAGE)
                throw new InvalidOperationException("El objeto original ya no corresponde a una imagen editable.");

            ValidateOriginalObject(page, handle, original);
            result.Add(new ResolvedImage(state, handle));
        }

        return result;
    }

    private static void ValidateOriginalObject(IntPtr page, IntPtr handle, PdfImageObjectInfo original)
    {
        if (PdfiumNative.FPDFPageObj_GetMatrix(handle, out var matrix) == 0)
            throw new InvalidOperationException("PDFium no pudo validar la matriz original de una imagen.");
        var actualMatrix = new PdfObjectMatrix(matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F);
        if (!NearlyEqual(actualMatrix, original.Matrix))
            throw new InvalidOperationException("La matriz de la imagen fuente ya no coincide con el plan de edición.");

        if (PdfiumNative.FPDFPageObj_GetBounds(handle, out var left, out var bottom, out var right, out var top) == 0)
            throw new InvalidOperationException("PDFium no pudo validar los límites originales de una imagen.");
        if (!NearlyEqual(left, original.Bounds.Left, BoundsTolerance) ||
            !NearlyEqual(bottom, original.Bounds.Bottom, BoundsTolerance) ||
            !NearlyEqual(right, original.Bounds.Right, BoundsTolerance) ||
            !NearlyEqual(top, original.Bounds.Top, BoundsTolerance))
        {
            throw new InvalidOperationException("Los límites de la imagen fuente ya no coinciden con el plan de edición.");
        }

        if (PdfiumNative.FPDFImageObj_GetImageMetadata(handle, page, out var metadata) == 0)
            throw new InvalidOperationException("PDFium no pudo validar los metadatos originales de una imagen.");
        if (metadata.Width != original.Metadata.PixelWidth || metadata.Height != original.Metadata.PixelHeight)
            throw new InvalidOperationException("Las dimensiones de la imagen fuente ya no coinciden con el plan de edición.");
    }

    private static void SetReplacementBitmap(
        IntPtr page,
        IntPtr imageObject,
        ImageReplacementAsset asset,
        CancellationToken cancellationToken)
    {
        if (asset.PixelWidth <= 0 || asset.PixelHeight <= 0 || asset.Stride < asset.PixelWidth * 4)
            throw new InvalidDataException("El recurso de reemplazo tiene dimensiones inválidas.");

        var sourcePixels = asset.BgraPixels.ToArray();
        if (sourcePixels.Length < checked(asset.Stride * asset.PixelHeight))
            throw new InvalidDataException("El recurso de reemplazo no contiene suficientes píxeles.");

        var bitmap = PdfiumNative.FPDFBitmap_Create(asset.PixelWidth, asset.PixelHeight, 1);
        if (bitmap == IntPtr.Zero)
            throw new InvalidOperationException("PDFium no pudo crear el bitmap de reemplazo.");

        IntPtr pages = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nativeWidth = PdfiumNative.FPDFBitmap_GetWidth(bitmap);
            var nativeHeight = PdfiumNative.FPDFBitmap_GetHeight(bitmap);
            var nativeStride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
            var nativeBuffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
            if (nativeWidth != asset.PixelWidth || nativeHeight != asset.PixelHeight ||
                nativeStride < asset.PixelWidth * 4 || nativeBuffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("PDFium devolvió un bitmap de reemplazo inválido.");
            }

            var rowBytes = checked(asset.PixelWidth * 4);
            for (var y = 0; y < asset.PixelHeight; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Marshal.Copy(
                    sourcePixels,
                    y * asset.Stride,
                    IntPtr.Add(nativeBuffer, y * nativeStride),
                    rowBytes);
            }

            pages = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(pages, page);
            if (PdfiumNative.FPDFImageObj_SetBitmap(pages, 1, imageObject, bitmap) == 0)
                throw new InvalidOperationException("PDFium no pudo aplicar el bitmap de reemplazo a la imagen.");
        }
        finally
        {
            if (pages != IntPtr.Zero)
                Marshal.FreeHGlobal(pages);
            PdfiumNative.FPDFBitmap_Destroy(bitmap);
        }
    }

    private void SaveDocument(IntPtr document, string temporaryPath)
    {
        using var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
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
                throw context.Error ?? new InvalidOperationException("PDFium no pudo guardar el PDF editado.");
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
            if (handle.Target is not FileWriteContext context || size > int.MaxValue)
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
            }
            return 0;
        }
    }

    private static int GetSignatureCount(string path)
    {
        using var session = PdfDocumentSession.Open(path);
        return session.GetCryptographicSignatureCount();
    }

    private static int SetMatrix(IntPtr pageObject, PdfObjectMatrix matrix)
    {
        var native = new PdfiumNative.Matrix
        {
            A = checked((float)matrix.A),
            B = checked((float)matrix.B),
            C = checked((float)matrix.C),
            D = checked((float)matrix.D),
            E = checked((float)matrix.E),
            F = checked((float)matrix.F)
        };
        return PdfiumNative.FPDFPageObj_SetMatrix(pageObject, ref native);
    }

    private static void ValidateOutput(string path, ImageEditWorkspace workspace, CancellationToken cancellationToken)
        => new ImageEditOutputValidator().Validate(path, workspace, cancellationToken);

    private static bool NearlyEqual(PdfObjectMatrix left, PdfObjectMatrix right)
        => NearlyEqual(left.A, right.A, MatrixTolerance) &&
           NearlyEqual(left.B, right.B, MatrixTolerance) &&
           NearlyEqual(left.C, right.C, MatrixTolerance) &&
           NearlyEqual(left.D, right.D, MatrixTolerance) &&
           NearlyEqual(left.E, right.E, MatrixTolerance) &&
           NearlyEqual(left.F, right.F, MatrixTolerance);

    private static bool NearlyEqual(double left, double right, double tolerance)
        => double.IsFinite(left) && double.IsFinite(right) && Math.Abs(left - right) <= tolerance;

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private sealed record ResolvedImage(ImageEditState State, IntPtr Handle);

    private sealed class FileWriteContext
    {
        internal FileWriteContext(FileStream stream) => Stream = stream;
        internal FileStream Stream { get; }
        internal Exception? Error { get; set; }
    }
}
