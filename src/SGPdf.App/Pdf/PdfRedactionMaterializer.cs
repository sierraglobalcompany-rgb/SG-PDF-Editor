using System.IO;
using System.Runtime.InteropServices;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Pdf;

internal sealed class PdfRedactionMaterializer
{
    private const double MatrixTolerance = 0.05d;
    private const double BoundsTolerance = 0.10d;

    internal void Materialize(
        string sourcePath,
        IReadOnlyList<PdfTextObjectInfo> targets,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (targets.Count == 0)
            throw new ArgumentException("La redacción no contiene objetivos.", nameof(targets));
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("La materialización de redacción requiere otra ruta de salida.", nameof(outputPath));

        cancellationToken.ThrowIfCancellationRequested();
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);

        IntPtr document = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException($"PDFium no pudo abrir la copia staging. Error: {PdfiumNative.FPDF_GetLastError()}.");

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageCount <= 0)
                throw new InvalidOperationException("PDFium devolvió una cantidad de páginas inválida para redactar.");

            foreach (var group in targets.GroupBy(target => target.Key.PageIndex).OrderBy(group => group.Key))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (group.Key < 0 || group.Key >= pageCount)
                    throw new InvalidOperationException("La redacción referencia una página inválida.");

                var page = PdfiumNative.FPDF_LoadPage(document, group.Key);
                if (page == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la página {group.Key + 1} para redactar.");

                IntPtr textPage = IntPtr.Zero;
                try
                {
                    textPage = PdfiumNative.FPDFText_LoadPage(page);
                    if (textPage == IntPtr.Zero)
                        throw new InvalidOperationException("PDFium no pudo cargar la capa de texto antes de redactar.");

                    var resolved = ResolveAllBeforeMutation(
                        page,
                        textPage,
                        group.OrderBy(target => target.Key.PageObjectIndex).ToArray(),
                        cancellationToken);
                    ApplyRedactions(page, resolved, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                        throw new InvalidOperationException("PDFium no pudo regenerar el contenido de la página redactada.");
                }
                finally
                {
                    if (textPage != IntPtr.Zero)
                        PdfiumNative.FPDFText_ClosePage(textPage);
                    PdfiumNative.FPDF_ClosePage(page);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveDocument(document, outputPath);
        }
        finally
        {
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static IReadOnlyList<ResolvedTarget> ResolveAllBeforeMutation(
        IntPtr page,
        IntPtr textPage,
        IReadOnlyList<PdfTextObjectInfo> targets,
        CancellationToken cancellationToken)
    {
        var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
        if (objectCount < 0)
            throw new InvalidOperationException("PDFium devolvió una cantidad inválida de objetos antes de redactar.");

        var seen = new HashSet<int>();
        var resolved = new List<ResolvedTarget>(targets.Count);
        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = target.Key.PageObjectIndex;
            if (index < 0 || index >= objectCount || !seen.Add(index))
                throw new InvalidOperationException("El ordinal del texto ya no es válido o está duplicado para redactar.");

            var handle = PdfiumNative.FPDFPage_GetObject(page, index);
            if (handle == IntPtr.Zero || PdfiumNative.FPDFPageObj_GetType(handle) != PdfiumNative.FPDF_PAGEOBJ_TEXT)
                throw new InvalidOperationException("El objeto staging ya no corresponde a texto redactable.");

            ValidateSnapshot(handle, textPage, target);
            resolved.Add(new ResolvedTarget(target, handle));
        }

        return resolved;
    }

    private static void ApplyRedactions(
        IntPtr page,
        IReadOnlyList<ResolvedTarget> targets,
        CancellationToken cancellationToken)
    {
        var prepared = new List<PreparedRectangle>(targets.Count);
        try
        {
            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bounds = target.Target.Bounds;
                var rectangle = PdfiumNative.FPDFPageObj_CreateNewRect(
                    checked((float)bounds.Left),
                    checked((float)bounds.Bottom),
                    checked((float)(bounds.Right - bounds.Left)),
                    checked((float)(bounds.Top - bounds.Bottom)));
                if (rectangle == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium no pudo crear la marca visual de redacción.");

                var item = new PreparedRectangle(target, rectangle);
                prepared.Add(item);
                if (PdfiumNative.FPDFPageObj_SetFillColor(rectangle, 0u, 0u, 0u, 255u) == 0)
                    throw new InvalidOperationException("PDFium no pudo asignar el relleno negro a la redacción.");
                if (PdfiumNative.FPDFPath_SetDrawMode(
                        rectangle,
                        PdfiumNative.FPDF_FILLMODE_WINDING,
                        0) == 0)
                {
                    throw new InvalidOperationException("PDFium no pudo activar el relleno del rectángulo de redacción.");
                }
            }

            foreach (var item in prepared)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (PdfiumNative.FPDFPage_RemoveObject(page, item.Target.Handle) == 0)
                    throw new InvalidOperationException("PDFium no pudo eliminar el texto subyacente de la redacción.");

                PdfiumNative.FPDFPageObj_Destroy(item.Target.Handle);
                item.TextDestroyed = true;

                cancellationToken.ThrowIfCancellationRequested();
                if (PdfiumNative.FPDFPage_InsertObject(page, item.Rectangle) == 0)
                    throw new InvalidOperationException("PDFium no pudo insertar la marca visual de redacción.");
                item.RectangleInserted = true;
            }
        }
        finally
        {
            foreach (var item in prepared)
            {
                if (!item.RectangleInserted && item.Rectangle != IntPtr.Zero)
                    PdfiumNative.FPDFPageObj_Destroy(item.Rectangle);
            }
        }
    }

    private static void ValidateSnapshot(
        IntPtr handle,
        IntPtr textPage,
        PdfTextObjectInfo expected)
    {
        var text = ReadText(handle, textPage);
        if (!string.Equals(text, expected.Text, StringComparison.Ordinal))
            throw new InvalidOperationException("El texto staging ya no coincide con el snapshot de redacción.");

        if (PdfiumNative.FPDFPageObj_GetBounds(handle, out var left, out var bottom, out var right, out var top) == 0 ||
            !NearlyEqual(left, expected.Bounds.Left, BoundsTolerance) ||
            !NearlyEqual(bottom, expected.Bounds.Bottom, BoundsTolerance) ||
            !NearlyEqual(right, expected.Bounds.Right, BoundsTolerance) ||
            !NearlyEqual(top, expected.Bounds.Top, BoundsTolerance))
        {
            throw new InvalidOperationException("La geometría staging ya no coincide con el snapshot de redacción.");
        }

        if (PdfiumNative.FPDFPageObj_GetMatrix(handle, out var matrix) == 0 ||
            !NearlyEqual(matrix.A, expected.Matrix.A, MatrixTolerance) ||
            !NearlyEqual(matrix.B, expected.Matrix.B, MatrixTolerance) ||
            !NearlyEqual(matrix.C, expected.Matrix.C, MatrixTolerance) ||
            !NearlyEqual(matrix.D, expected.Matrix.D, MatrixTolerance) ||
            !NearlyEqual(matrix.E, expected.Matrix.E, MatrixTolerance) ||
            !NearlyEqual(matrix.F, expected.Matrix.F, MatrixTolerance))
        {
            throw new InvalidOperationException("La transformación staging ya no coincide con el snapshot de redacción.");
        }

        if (PdfiumNative.FPDFTextObj_GetTextRenderMode(handle) != expected.TextRenderMode)
            throw new InvalidOperationException("El modo de render staging ya no coincide con el snapshot de redacción.");
    }

    private static string ReadText(IntPtr textObject, IntPtr textPage)
    {
        var required = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, IntPtr.Zero, 0);
        if (required == 0)
            throw new InvalidOperationException("PDFium no pudo leer el texto staging para redactar.");

        var buffer = Marshal.AllocHGlobal(checked((int)required * sizeof(ushort)));
        try
        {
            var copied = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, buffer, required);
            if (copied == 0 || copied > required)
                throw new InvalidOperationException("PDFium devolvió una longitud inválida al validar la redacción.");

            var raw = new short[copied];
            Marshal.Copy(buffer, raw, 0, checked((int)copied));
            return new string(raw
                .TakeWhile(value => value != 0)
                .Select(value => (char)(ushort)value)
                .ToArray());
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool NearlyEqual(double left, double right, double tolerance)
        => double.IsFinite(left) && double.IsFinite(right) && Math.Abs(left - right) <= tolerance;

    private static void SaveDocument(IntPtr document, string outputPath)
    {
        using var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
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

            if (PdfiumNative.FPDF_SaveAsCopy(document, nativeWrite, 0u) == 0)
                throw context.Error ?? new InvalidOperationException("PDFium no pudo guardar la copia redactada.");
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

    private sealed record ResolvedTarget(PdfTextObjectInfo Target, IntPtr Handle);

    private sealed class PreparedRectangle
    {
        internal PreparedRectangle(ResolvedTarget target, IntPtr rectangle)
        {
            Target = target;
            Rectangle = rectangle;
        }

        internal ResolvedTarget Target { get; }
        internal IntPtr Rectangle { get; }
        internal bool RectangleInserted { get; set; }
        internal bool TextDestroyed { get; set; }
    }

    private sealed class FileWriteContext
    {
        internal FileWriteContext(FileStream stream) => Stream = stream;
        internal Exception? Error { get; set; }
        internal FileStream Stream { get; }
    }
}
