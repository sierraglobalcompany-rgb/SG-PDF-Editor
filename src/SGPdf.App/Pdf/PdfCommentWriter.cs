using System.IO;
using System.Runtime.InteropServices;
using SGPdf.App.Features.Comments;

namespace SGPdf.App.Pdf;

internal sealed class PdfCommentWriter
{
    internal void SaveAsCopy(
        CommentWorkspace workspace,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("La ruta de destino es obligatoria.", nameof(destinationPath));

        cancellationToken.ThrowIfCancellationRequested();
        var sourcePath = Path.GetFullPath(workspace.SourcePath);
        var normalizedDestination = Path.GetFullPath(destinationPath);
        if (string.Equals(sourcePath, normalizedDestination, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Guardar como requiere un archivo de destino diferente del PDF original.");

        var destinationDirectory = Path.GetDirectoryName(normalizedDestination);
        if (string.IsNullOrWhiteSpace(destinationDirectory) || !Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException("La carpeta de destino no existe.");
        if (workspace.SourceOpenedWithPassword)
            throw new InvalidOperationException("Los PDF abiertos con contraseña están bloqueados para comentarios en esta versión.");
        if (!workspace.SourceMatchesCurrentFile)
            throw new IOException("El PDF fuente cambió desde que se inició el modo COMENTAR.");

        var editedStates = workspace.EditedStates;
        if (editedStates.Count == 0)
            throw new InvalidOperationException("No hay cambios de comentarios para guardar.");

        ValidateSupportedScope(editedStates);
        var resolvedExisting = ResolveExistingBeforeMutation(workspace, editedStates, cancellationToken);
        var temporaryPath = CreateTemporaryPath(normalizedDestination);

        try
        {
            WriteTemporary(sourcePath, temporaryPath, editedStates, resolvedExisting, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            PdfCommentOutputValidator.Validate(temporaryPath, workspace, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            PublishTemporary(temporaryPath, normalizedDestination);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static IReadOnlyList<ResolvedExistingComment> ResolveExistingBeforeMutation(
        CommentWorkspace workspace,
        IReadOnlyList<CommentState> editedStates,
        CancellationToken cancellationToken)
    {
        var existing = editedStates.Where(state => !state.IsNew).ToArray();
        if (existing.Length == 0)
        {
            using var sourceSession = PdfDocumentSession.Open(workspace.SourcePath);
            if (sourceSession.GetCryptographicSignatureCount() > 0)
                throw new InvalidOperationException("El PDF contiene una firma criptográfica y no puede modificarse en COMENTAR.");
            return Array.Empty<ResolvedExistingComment>();
        }

        using var session = PdfDocumentSession.Open(workspace.SourcePath);
        if (session.GetCryptographicSignatureCount() > 0)
            throw new InvalidOperationException("El PDF contiene una firma criptográfica y no puede modificarse en COMENTAR.");

        var resolved = new List<ResolvedExistingComment>(existing.Length);
        foreach (var pageGroup in existing.GroupBy(state => state.PageIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var comments = session.GetCommentsOnPage(pageGroup.Key, cancellationToken);
            foreach (var state in pageGroup)
            {
                if (!workspace.TryGetSourceIdentity(state.Key, out var identity))
                    throw new InvalidOperationException("Falta la identidad estable del comentario existente; se abortó el guardado.");
                if (!workspace.TryGetOriginalBaseline(state.Key, out var original))
                    throw new InvalidOperationException("Falta el baseline original del comentario existente.");

                var sourceComment = CommentIdentity.Resolve(identity, comments);
                if (sourceComment is null)
                    throw new InvalidOperationException("La identidad del comentario existente ya no puede resolverse de forma segura.");
                if (sourceComment.Subtype != state.Subtype)
                    throw new InvalidOperationException("El subtipo del comentario fuente ya no coincide con el plan de edición.");

                ValidateExistingMutation(original, state);
                resolved.Add(new ResolvedExistingComment(
                    state.PageIndex,
                    sourceComment.AnnotationIndex,
                    state,
                    original));
            }
        }

        return resolved;
    }

    private static void ValidateExistingMutation(CommentState original, CommentState current)
    {
        if (original.PageIndex != current.PageIndex || original.Subtype != current.Subtype)
            throw new InvalidOperationException("La identidad lógica del comentario cambió durante la edición.");

        switch (original.Subtype)
        {
            case CommentSubtype.Highlight:
            case CommentSubtype.Underline:
            case CommentSubtype.Strikeout:
                if (original.Rect != current.Rect ||
                    !original.QuadPoints.SequenceEqual(current.QuadPoints) ||
                    !string.Equals(original.Contents, current.Contents, StringComparison.Ordinal) ||
                    !InkStrokesEqual(original.InkStrokes, current.InkStrokes) ||
                    !Nullable.Equals(original.BorderWidth, current.BorderWidth))
                {
                    throw new NotSupportedException("El marcado de texto solo permite cambiar color/opacidad o eliminar.");
                }
                break;

            case CommentSubtype.Text:
                if (!original.QuadPoints.SequenceEqual(current.QuadPoints) ||
                    !InkStrokesEqual(original.InkStrokes, current.InkStrokes) ||
                    !Nullable.Equals(original.BorderWidth, current.BorderWidth))
                {
                    throw new NotSupportedException("La nota TEXT solo permite editar contenido, posición, color o eliminar.");
                }
                break;

            default:
                throw new NotSupportedException("Task 6 solo materializa marcado de texto y notas TEXT.");
        }
    }

    private static bool InkStrokesEqual(
        IReadOnlyList<CommentStroke> left,
        IReadOnlyList<CommentStroke> right)
    {
        if (left.Count != right.Count)
            return false;
        for (var index = 0; index < left.Count; index++)
        {
            if (!left[index].Points.SequenceEqual(right[index].Points))
                return false;
        }
        return true;
    }

    private static void ValidateSupportedScope(IReadOnlyList<CommentState> states)
    {
        foreach (var state in states)
        {
            if (state.Subtype is not (CommentSubtype.Highlight or CommentSubtype.Underline or CommentSubtype.Strikeout or CommentSubtype.Text))
                throw new NotSupportedException("Task 6 solo guarda marcado de texto y notas TEXT.");
            if (state.IsNew && state.Deleted)
                throw new InvalidOperationException("Un comentario nuevo eliminado no debe llegar al plan de materialización.");

            if (state.Subtype is CommentSubtype.Highlight or CommentSubtype.Underline or CommentSubtype.Strikeout)
            {
                if (state.IsNew && state.QuadPoints.Count == 0)
                    throw new InvalidOperationException("Un marcado de texto nuevo requiere al menos un quadpoint.");
                continue;
            }

            if (state.QuadPoints.Count != 0 || state.InkStrokes.Count != 0 || state.BorderWidth is not null)
                throw new InvalidOperationException("Una nota TEXT no puede contener quadpoints, trazos Ink ni borde en Task 6.");
        }
    }

    private static void WriteTemporary(
        string sourcePath,
        string temporaryPath,
        IReadOnlyList<CommentState> states,
        IReadOnlyList<ResolvedExistingComment> resolvedExisting,
        CancellationToken cancellationToken)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException($"PDFium no pudo abrir el PDF fuente para comentarios. Error {PdfiumNative.FPDF_GetLastError()}.");

            try
            {
                var pageCount = PdfiumNative.FPDF_GetPageCount(document);
                if (pageCount <= 0)
                    throw new InvalidOperationException("El PDF fuente no contiene páginas materializables.");

                var pages = states.Select(state => state.PageIndex)
                    .Distinct()
                    .OrderBy(index => index)
                    .ToArray();

                foreach (var pageIndex in pages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (pageIndex < 0 || pageIndex >= pageCount)
                        throw new InvalidOperationException("El plan de comentarios contiene una página fuera de rango.");

                    var page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
                    if (page == IntPtr.Zero)
                        throw new InvalidOperationException($"PDFium no pudo cargar la página {pageIndex + 1} para guardar comentarios.");

                    try
                    {
                        ApplyExistingUpdates(
                            page,
                            resolvedExisting.Where(item => item.PageIndex == pageIndex && !item.State.Deleted).ToArray(),
                            cancellationToken);
                        ApplyExistingDeletes(
                            page,
                            resolvedExisting.Where(item => item.PageIndex == pageIndex && item.State.Deleted).ToArray(),
                            cancellationToken);
                        ApplyNewComments(
                            page,
                            states.Where(state => state.PageIndex == pageIndex && state.IsNew && !state.Deleted).ToArray(),
                            cancellationToken);
                    }
                    finally
                    {
                        PdfiumNative.FPDF_ClosePage(page);
                    }
                }

                SaveDocument(document, temporaryPath);
            }
            finally
            {
                PdfiumNative.FPDF_CloseDocument(document);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static void ApplyExistingUpdates(
        IntPtr page,
        IReadOnlyList<ResolvedExistingComment> updates,
        CancellationToken cancellationToken)
    {
        foreach (var update in updates.OrderBy(item => item.AnnotationIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var annotation = PdfiumNative.FPDFPage_GetAnnot(page, update.AnnotationIndex);
            if (annotation == IntPtr.Zero)
                throw new InvalidOperationException("No se pudo resolver la anotación existente antes de modificarla.");

            try
            {
                var expectedSubtype = MapSubtype(update.State.Subtype);
                if (PdfiumNative.FPDFAnnot_GetSubtype(annotation) != expectedSubtype)
                    throw new InvalidOperationException("La anotación existente ya no corresponde al subtipo esperado.");

                if (update.State.Subtype == CommentSubtype.Text)
                {
                    SetRect(annotation, update.State.Rect);
                    SetColor(annotation, update.State.Color);
                    SetContents(annotation, update.State.Contents ?? string.Empty);
                }
                else
                {
                    SetColor(annotation, update.State.Color);
                }
            }
            finally
            {
                PdfiumNative.FPDFPage_CloseAnnot(annotation);
            }
        }
    }

    private static void ApplyExistingDeletes(
        IntPtr page,
        IReadOnlyList<ResolvedExistingComment> deletes,
        CancellationToken cancellationToken)
    {
        foreach (var deletion in deletes.OrderByDescending(item => item.AnnotationIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (PdfiumNative.FPDFPage_RemoveAnnot(page, deletion.AnnotationIndex) == 0)
                throw new InvalidOperationException("PDFium no pudo eliminar la anotación seleccionada.");
        }
    }

    private static void ApplyNewComments(
        IntPtr page,
        IReadOnlyList<CommentState> additions,
        CancellationToken cancellationToken)
    {
        foreach (var state in additions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var annotation = PdfiumNative.FPDFPage_CreateAnnot(page, MapSubtype(state.Subtype));
            if (annotation == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo crear la anotación.");

            try
            {
                SetRect(annotation, state.Rect);
                SetColor(annotation, state.Color);

                if (state.Subtype == CommentSubtype.Text)
                {
                    SetContents(annotation, state.Contents ?? string.Empty);
                    continue;
                }

                foreach (var quad in state.QuadPoints)
                {
                    var nativeQuad = new PdfiumNative.QuadPointsF
                    {
                        X1 = checked((float)quad.P1.X),
                        Y1 = checked((float)quad.P1.Y),
                        X2 = checked((float)quad.P2.X),
                        Y2 = checked((float)quad.P2.Y),
                        X3 = checked((float)quad.P3.X),
                        Y3 = checked((float)quad.P3.Y),
                        X4 = checked((float)quad.P4.X),
                        Y4 = checked((float)quad.P4.Y)
                    };
                    if (PdfiumNative.FPDFAnnot_AppendAttachmentPoints(annotation, ref nativeQuad) == 0)
                        throw new InvalidOperationException("PDFium no pudo aplicar los quadpoints del comentario.");
                }
            }
            finally
            {
                PdfiumNative.FPDFPage_CloseAnnot(annotation);
            }
        }
    }

    private static void SetRect(IntPtr annotation, CommentRect rect)
    {
        var nativeRect = new PdfiumNative.RectF
        {
            Left = checked((float)rect.Left),
            Bottom = checked((float)rect.Bottom),
            Right = checked((float)rect.Right),
            Top = checked((float)rect.Top)
        };
        if (PdfiumNative.FPDFAnnot_SetRect(annotation, ref nativeRect) == 0)
            throw new InvalidOperationException("PDFium no pudo aplicar los límites del comentario.");
    }

    private static void SetColor(IntPtr annotation, CommentColor color)
    {
        if (PdfiumNative.FPDFAnnot_SetColor(
                annotation,
                PdfiumNative.FPDFANNOT_COLORTYPE_Color,
                color.Red,
                color.Green,
                color.Blue,
                color.Alpha) == 0)
        {
            throw new InvalidOperationException("PDFium no pudo aplicar el color del comentario.");
        }
    }

    private static void SetContents(IntPtr annotation, string contents)
    {
        if (PdfiumNative.FPDFAnnot_SetStringValue(annotation, "Contents", contents) == 0)
            throw new InvalidOperationException("PDFium no pudo aplicar el contenido Unicode de la nota.");
    }

    private static int MapSubtype(CommentSubtype subtype) => subtype switch
    {
        CommentSubtype.Highlight => PdfiumNative.FPDF_ANNOT_HIGHLIGHT,
        CommentSubtype.Underline => PdfiumNative.FPDF_ANNOT_UNDERLINE,
        CommentSubtype.Strikeout => PdfiumNative.FPDF_ANNOT_STRIKEOUT,
        CommentSubtype.Text => PdfiumNative.FPDF_ANNOT_TEXT,
        _ => throw new NotSupportedException("El subtipo aún no pertenece al writer de Task 6.")
    };

    private static string CreateTemporaryPath(string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath)
            ?? throw new DirectoryNotFoundException("No se pudo resolver la carpeta de destino.");
        var fileName = Path.GetFileName(destinationPath);
        return Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.sgpdf.tmp");
    }

    private static void PublishTemporary(string temporaryPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
            File.Replace(temporaryPath, destinationPath, null, ignoreMetadataErrors: true);
        else
            File.Move(temporaryPath, destinationPath);
    }

    private static void SaveDocument(IntPtr document, string temporaryPath)
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

            if (PdfiumNative.FPDF_SaveAsCopy(document, nativeWrite, 0u) == 0)
                throw context.Error ?? new InvalidOperationException("PDFium no pudo guardar el PDF con comentarios.");
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

    private sealed record ResolvedExistingComment(
        int PageIndex,
        int AnnotationIndex,
        CommentState State,
        CommentState Original);

    private sealed class FileWriteContext
    {
        internal FileWriteContext(FileStream stream) => Stream = stream;
        internal FileStream Stream { get; }
        internal Exception? Error { get; set; }
    }
}
