using SGPdf.App.Features.Edit;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Pdf;

internal sealed class PdfEditWriter
{
    private const double MatrixTolerance = 0.05d;
    private const double BoundsTolerance = 0.10d;
    private const double FontSizeTolerance = 0.01d;

    private readonly Func<IntPtr, IntPtr, uint, int> _saveAsCopy;
    private readonly Action<string, ImageEditWorkspace, TextEditWorkspace?, CancellationToken> _validateOutput;
    private readonly Func<string, int> _signatureCount;
    private readonly Func<IntPtr, PdfObjectMatrix, int> _setMatrix;
    private readonly Func<IntPtr, int> _generateContent;
    private readonly Func<IntPtr, IntPtr, uint, string, IntPtr, uint, IntPtr> _loadCidType2Font;
    private readonly Func<IntPtr, IntPtr, int> _removeObject;

    internal PdfEditWriter(
        Func<IntPtr, IntPtr, uint, int>? saveAsCopyOverride = null,
        Action<string, ImageEditWorkspace, CancellationToken>? validateOutputOverride = null,
        Func<string, int>? signatureCountOverride = null,
        Func<IntPtr, PdfObjectMatrix, int>? setMatrixOverride = null)
    {
        _saveAsCopy = saveAsCopyOverride ?? PdfiumNative.FPDF_SaveAsCopy;
        _validateOutput = validateOutputOverride is null
            ? ValidateOutput
            : (path, imageWorkspace, _, cancellationToken) =>
                validateOutputOverride(path, imageWorkspace, cancellationToken);
        _signatureCount = signatureCountOverride ?? GetSignatureCount;
        _setMatrix = setMatrixOverride ?? SetMatrix;
        _generateContent = PdfiumNative.FPDFPage_GenerateContent;
        _loadCidType2Font = PdfiumNative.FPDFText_LoadCidType2Font;
        _removeObject = PdfiumNative.FPDFPage_RemoveObject;
    }

    internal PdfEditWriter(
        Func<IntPtr, IntPtr, uint, int>? saveAsCopyOverride,
        Action<string, ImageEditWorkspace, CancellationToken>? validateOutputOverride,
        Func<string, int>? signatureCountOverride,
        Func<IntPtr, PdfObjectMatrix, int>? setMatrixOverride,
        Func<IntPtr, int>? generateContentOverride)
        : this(saveAsCopyOverride, validateOutputOverride, signatureCountOverride, setMatrixOverride)
    {
        _generateContent = generateContentOverride ?? PdfiumNative.FPDFPage_GenerateContent;
    }

    internal PdfEditWriter(
        Func<IntPtr, IntPtr, uint, int>? saveAsCopyOverride,
        Action<string, ImageEditWorkspace, CancellationToken>? validateOutputOverride,
        Func<string, int>? signatureCountOverride,
        Func<IntPtr, PdfObjectMatrix, int>? setMatrixOverride,
        Func<IntPtr, int>? generateContentOverride,
        Func<IntPtr, IntPtr, uint, string, IntPtr, uint, IntPtr>? loadCidType2FontOverride,
        Func<IntPtr, IntPtr, int>? removeObjectOverride)
        : this(
            saveAsCopyOverride,
            validateOutputOverride,
            signatureCountOverride,
            setMatrixOverride,
            generateContentOverride)
    {
        _loadCidType2Font = loadCidType2FontOverride ?? PdfiumNative.FPDFText_LoadCidType2Font;
        _removeObject = removeObjectOverride ?? PdfiumNative.FPDFPage_RemoveObject;
    }

    internal void SaveAsCopy(
        ImageEditWorkspace workspace,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken = default)
        => SaveAsCopyCore(workspace, null, destinationPath, warningsConfirmed, cancellationToken);

    internal void SaveAsCopy(
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageWorkspace);
        ArgumentNullException.ThrowIfNull(textWorkspace);
        EnsureCombinedWorkspaceIdentity(imageWorkspace, textWorkspace);
        SaveAsCopyCore(imageWorkspace, textWorkspace, destinationPath, warningsConfirmed, cancellationToken);
    }

    private void SaveAsCopyCore(
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace? textWorkspace,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(imageWorkspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        cancellationToken.ThrowIfCancellationRequested();
        var sourcePath = Path.GetFullPath(imageWorkspace.SourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourcePath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Guardar como requiere una ruta diferente al PDF fuente.", nameof(destinationPath));

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath)
            ?? throw new ArgumentException("La ruta de destino no tiene directorio.", nameof(destinationPath));
        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException(destinationDirectory);

        if (imageWorkspace.SourceOpenedWithPassword || textWorkspace?.SourceOpenedWithPassword == true)
            throw new InvalidOperationException("Los PDF abiertos con contraseña no se pueden materializar en EDITAR.");

        if (!imageWorkspace.SourceFingerprint.MatchesCurrentFile(sourcePath))
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
            throw new InvalidOperationException("Los PDF con firma criptográfica están bloqueados para edición.");

        cancellationToken.ThrowIfCancellationRequested();
        using (var preflightSession = PdfDocumentSession.Open(sourcePath))
        {
            var preflight = new PdfEditPreflightInspector((_, _) => 0)
                .Inspect(preflightSession, cancellationToken);
            if (!preflight.CanProceed)
                throw new InvalidOperationException("El PDF no superó el preflight de preservación para EDITAR.");
            if (preflight.RequiresWarningConfirmation && !warningsConfirmed)
                throw new InvalidOperationException("El PDF requiere confirmación explícita de las advertencias de preservación.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var destinationFileName = Path.GetFileName(destinationFullPath);
        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{destinationFileName}.{Guid.NewGuid():N}.sgpdf.tmp");

        try
        {
            WriteTemporaryCopy(imageWorkspace, textWorkspace, temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _validateOutput(temporaryPath, imageWorkspace, textWorkspace, cancellationToken);
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

    private static void EnsureCombinedWorkspaceIdentity(
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace)
    {
        var imagePath = Path.GetFullPath(imageWorkspace.SourcePath);
        var textPath = Path.GetFullPath(textWorkspace.SourcePath);
        if (!string.Equals(imagePath, textPath, StringComparison.OrdinalIgnoreCase) ||
            imageWorkspace.SourceFingerprint != textWorkspace.SourceFingerprint)
        {
            throw new InvalidOperationException(
                "Los workspaces de imagen y texto deben pertenecer al mismo PDF fuente y al mismo fingerprint.");
        }
    }

    private void WriteTemporaryCopy(
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace? textWorkspace,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        var allTextStates = textWorkspace?.EditedStates ?? Array.Empty<TextEditState>();
        var fallbackStates = allTextStates
            .Where(state => state.FontStrategy == TextFontStrategy.FallbackTtf)
            .ToArray();
        CidType2FontMapPlan? fallbackPlan = null;
        byte[]? fallbackFontBytes = null;
        if (fallbackStates.Length > 0)
        {
            fallbackPlan = CidType2FontMapBuilder.Build(fallbackStates.Select(state => state.Text));
            fallbackFontBytes = FallbackFontAsset.LoadBytes();
        }

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);

        IntPtr document = IntPtr.Zero;
        IntPtr fallbackFont = IntPtr.Zero;
        IntPtr fallbackFontBuffer = IntPtr.Zero;
        IntPtr cidToGidBuffer = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            document = PdfiumNative.FPDF_LoadDocument(imageWorkspace.SourcePath, null);
            if (document == IntPtr.Zero)
            {
                var error = PdfiumNative.FPDF_GetLastError();
                throw new InvalidOperationException($"PDFium no pudo reabrir el PDF fuente. Error: {error}.");
            }

            if (fallbackPlan is not null && fallbackFontBytes is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                fallbackFontBuffer = Marshal.AllocHGlobal(fallbackFontBytes.Length);
                Marshal.Copy(fallbackFontBytes, 0, fallbackFontBuffer, fallbackFontBytes.Length);

                cidToGidBuffer = Marshal.AllocHGlobal(fallbackPlan.CidToGidMap.Length);
                Marshal.Copy(
                    fallbackPlan.CidToGidMap,
                    0,
                    cidToGidBuffer,
                    fallbackPlan.CidToGidMap.Length);

                fallbackFont = _loadCidType2Font(
                    document,
                    fallbackFontBuffer,
                    checked((uint)fallbackFontBytes.Length),
                    fallbackPlan.ToUnicodeCMap,
                    cidToGidBuffer,
                    checked((uint)fallbackPlan.CidToGidMap.Length));
                if (fallbackFont == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium no pudo cargar la fuente fallback CID Type2.");
            }

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageCount <= 0)
                throw new InvalidOperationException("PDFium devolvió una cantidad de páginas inválida.");

            var imageGroups = imageWorkspace.EditedStates
                .GroupBy(state => state.ObjectRef.Key.PageIndex)
                .ToDictionary(group => group.Key, group => group.ToArray());
            var textGroups = allTextStates
                .GroupBy(state => state.Key.PageIndex)
                .ToDictionary(group => group.Key, group => group.ToArray());
            var editedPages = imageGroups.Keys
                .Concat(textGroups.Keys)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();

            foreach (var pageIndex in editedPages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (pageIndex < 0 || pageIndex >= pageCount)
                    throw new InvalidOperationException("El plan de edición referencia una página inválida.");

                var page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
                if (page == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la página {pageIndex + 1} para materializar cambios.");

                IntPtr textPage = IntPtr.Zero;
                try
                {
                    imageGroups.TryGetValue(pageIndex, out var imageStates);
                    textGroups.TryGetValue(pageIndex, out var textStates);
                    imageStates ??= Array.Empty<ImageEditState>();
                    textStates ??= Array.Empty<TextEditState>();

                    if (textStates.Length > 0)
                    {
                        textPage = PdfiumNative.FPDFText_LoadPage(page);
                        if (textPage == IntPtr.Zero)
                            throw new InvalidOperationException($"PDFium no pudo cargar la capa de texto de la página {pageIndex + 1}.");
                    }

                    // Todos los handles de ambos tipos se resuelven antes de la primera mutación.
                    var resolvedImages = ResolveImagesBeforeMutation(page, imageStates, cancellationToken);
                    var resolvedText = ResolveTextBeforeMutation(page, textPage, textStates, cancellationToken);

                    ApplyImageChanges(page, resolvedImages, cancellationToken);
                    ApplyOriginalFontTextChanges(resolvedText, cancellationToken);
                    ApplyFallbackTextChanges(
                        document,
                        page,
                        resolvedText,
                        fallbackFont,
                        cancellationToken);
                    ApplyImageDeletesAndReordering(page, resolvedImages, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    if (_generateContent(page) == 0)
                        throw new InvalidOperationException("PDFium no pudo regenerar el contenido de una página editada.");
                }
                finally
                {
                    if (textPage != IntPtr.Zero)
                        PdfiumNative.FPDFText_ClosePage(textPage);
                    PdfiumNative.FPDF_ClosePage(page);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveDocument(document, temporaryPath);
        }
        finally
        {
            if (fallbackFont != IntPtr.Zero)
                PdfiumNative.FPDFFont_Close(fallbackFont);
            if (cidToGidBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(cidToGidBuffer);
            if (fallbackFontBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(fallbackFontBuffer);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private void ApplyImageChanges(
        IntPtr page,
        IReadOnlyList<ResolvedImage> resolved,
        CancellationToken cancellationToken)
    {
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
    }

    private static void ApplyOriginalFontTextChanges(
        IReadOnlyList<ResolvedText> resolved,
        CancellationToken cancellationToken)
    {
        foreach (var item in resolved.Where(item => item.State.FontStrategy == TextFontStrategy.OriginalFont))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = item.State;
            var original = state.Original;

            if (!NearlyEqual(state.FontSize, original.FontSize, FontSizeTolerance))
                throw new InvalidOperationException("La ruta OriginalFont no puede cambiar el tamaño de fuente en Texto V1.");

            if (!string.Equals(state.Text, original.Text, StringComparison.Ordinal) &&
                PdfiumNative.FPDFText_SetText(item.Handle, state.Text) == 0)
            {
                throw new InvalidOperationException("PDFium no pudo aplicar el texto editado usando la fuente original.");
            }

            if (state.FillColor != original.FillColor &&
                PdfiumNative.FPDFPageObj_SetFillColor(
                    item.Handle,
                    state.FillColor.Red,
                    state.FillColor.Green,
                    state.FillColor.Blue,
                    state.FillColor.Alpha) == 0)
            {
                throw new InvalidOperationException("PDFium no pudo aplicar el color editado al texto.");
            }
        }
    }

    private void ApplyFallbackTextChanges(
        IntPtr document,
        IntPtr page,
        IReadOnlyList<ResolvedText> resolved,
        IntPtr fallbackFont,
        CancellationToken cancellationToken)
    {
        var fallbackItems = resolved
            .Where(item => item.State.FontStrategy == TextFontStrategy.FallbackTtf)
            .OrderBy(item => item.State.Key.PageObjectIndex)
            .ToArray();
        if (fallbackItems.Length == 0)
            return;
        if (fallbackFont == IntPtr.Zero)
            throw new InvalidOperationException("La fuente fallback CID Type2 no está disponible.");

        foreach (var item in fallbackItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = item.State;
            var replacement = PdfiumNative.FPDFPageObj_CreateTextObj(
                document,
                fallbackFont,
                checked((float)state.FontSize));
            if (replacement == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo crear el objeto de texto fallback.");

            var originalRemoved = false;
            var replacementInserted = false;
            try
            {
                if (PdfiumNative.FPDFText_SetText(replacement, state.Text) == 0)
                    throw new InvalidOperationException("PDFium no pudo asignar Unicode al objeto de texto fallback.");

                if (PdfiumNative.FPDFPageObj_SetFillColor(
                        replacement,
                        state.FillColor.Red,
                        state.FillColor.Green,
                        state.FillColor.Blue,
                        state.FillColor.Alpha) == 0)
                {
                    throw new InvalidOperationException("PDFium no pudo copiar el color al texto fallback.");
                }

                if (_setMatrix(replacement, state.Original.Matrix) == 0)
                    throw new InvalidOperationException("PDFium no pudo copiar la matriz al texto fallback.");

                if (_removeObject(page, item.Handle) == 0)
                    throw new InvalidOperationException("PDFium no pudo retirar el texto original para reemplazarlo.");
                originalRemoved = true;

                // Gate explícito para abortar de forma segura entre remove e insert.
                cancellationToken.ThrowIfCancellationRequested();

                var targetIndex = state.Key.PageObjectIndex;
                var remainingCount = PdfiumNative.FPDFPage_CountObjects(page);
                if (remainingCount < 0 || targetIndex < 0 || targetIndex > remainingCount)
                    throw new InvalidOperationException("El índice original del texto ya no es válido para el reemplazo.");

                if (PdfiumNative.FPDFPage_InsertObjectAtIndex(page, replacement, (nuint)targetIndex) == 0)
                    throw new InvalidOperationException("PDFium no pudo insertar el texto fallback en su ordinal original.");
                replacementInserted = true;

                PdfiumNative.FPDFPageObj_Destroy(item.Handle);
                originalRemoved = false;
            }
            finally
            {
                if (originalRemoved)
                    PdfiumNative.FPDFPageObj_Destroy(item.Handle);
                if (!replacementInserted && replacement != IntPtr.Zero)
                    PdfiumNative.FPDFPageObj_Destroy(replacement);
            }
        }
    }

    private static void ApplyImageDeletesAndReordering(
        IntPtr page,
        IReadOnlyList<ResolvedImage> resolved,
        CancellationToken cancellationToken)
    {
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
    }

    private static IReadOnlyList<ResolvedImage> ResolveImagesBeforeMutation(
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

            ValidateOriginalImageObject(page, handle, original);
            result.Add(new ResolvedImage(state, handle));
        }

        return result;
    }

    private static IReadOnlyList<ResolvedText> ResolveTextBeforeMutation(
        IntPtr page,
        IntPtr textPage,
        IReadOnlyList<TextEditState> states,
        CancellationToken cancellationToken)
    {
        if (states.Count == 0)
            return Array.Empty<ResolvedText>();
        if (textPage == IntPtr.Zero)
            throw new InvalidOperationException("La capa de texto no está disponible para resolver objetos editados.");

        var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
        if (objectCount < 0)
            throw new InvalidOperationException("PDFium devolvió una cantidad inválida de objetos de página.");

        var result = new List<ResolvedText>(states.Count);
        foreach (var state in states.OrderBy(state => state.Key.PageObjectIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = state.Key.PageObjectIndex;
            if (index < 0 || index >= objectCount)
                throw new InvalidOperationException("No se pudo resolver el texto original por su ordinal.");

            var handle = PdfiumNative.FPDFPage_GetObject(page, index);
            if (handle == IntPtr.Zero || PdfiumNative.FPDFPageObj_GetType(handle) != PdfiumNative.FPDF_PAGEOBJ_TEXT)
                throw new InvalidOperationException("El objeto original ya no corresponde a texto editable.");

            ValidateOriginalTextObject(handle, textPage, state.Original);
            result.Add(new ResolvedText(state, handle));
        }

        return result;
    }

    private static void ValidateOriginalImageObject(IntPtr page, IntPtr handle, PdfImageObjectInfo original)
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

    private static void ValidateOriginalTextObject(
        IntPtr handle,
        IntPtr textPage,
        PdfTextObjectInfo original)
    {
        var actualText = ReadTextObjectText(handle, textPage);
        if (!string.Equals(actualText, original.Text, StringComparison.Ordinal))
            throw new InvalidOperationException("El texto fuente ya no coincide con el snapshot del plan de edición.");

        if (PdfiumNative.FPDFPageObj_GetMatrix(handle, out var matrix) == 0)
            throw new InvalidOperationException("PDFium no pudo validar la matriz original del texto.");
        var actualMatrix = new PdfObjectMatrix(matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F);
        if (!NearlyEqual(actualMatrix, original.Matrix))
            throw new InvalidOperationException("La matriz del texto fuente ya no coincide con el plan de edición.");

        if (PdfiumNative.FPDFPageObj_GetBounds(handle, out var left, out var bottom, out var right, out var top) == 0)
            throw new InvalidOperationException("PDFium no pudo validar los límites originales del texto.");
        if (!NearlyEqual(left, original.Bounds.Left, BoundsTolerance) ||
            !NearlyEqual(bottom, original.Bounds.Bottom, BoundsTolerance) ||
            !NearlyEqual(right, original.Bounds.Right, BoundsTolerance) ||
            !NearlyEqual(top, original.Bounds.Top, BoundsTolerance))
        {
            throw new InvalidOperationException("Los límites del texto fuente ya no coinciden con el plan de edición.");
        }

        if (PdfiumNative.FPDFTextObj_GetFontSize(handle, out var fontSize) == 0 ||
            !NearlyEqual(fontSize, original.FontSize, FontSizeTolerance))
        {
            throw new InvalidOperationException("El tamaño de fuente del texto ya no coincide con el plan de edición.");
        }

        var font = PdfiumNative.FPDFTextObj_GetFont(handle);
        if (font == IntPtr.Zero)
            throw new InvalidOperationException("PDFium no pudo resolver la fuente original del texto.");
        var fontName = ReadBaseFontName(font);
        if (!string.Equals(fontName, original.FontName, StringComparison.Ordinal))
            throw new InvalidOperationException("La fuente del texto ya no coincide con el plan de edición.");

        if (PdfiumNative.FPDFPageObj_GetFillColor(handle, out var red, out var green, out var blue, out var alpha) == 0 ||
            new PdfTextFillColor(red, green, blue, alpha) != original.FillColor)
        {
            throw new InvalidOperationException("El color original del texto ya no coincide con el plan de edición.");
        }

        var renderMode = PdfiumNative.FPDFTextObj_GetTextRenderMode(handle);
        if (renderMode != original.TextRenderMode)
            throw new InvalidOperationException("El modo de render del texto ya no coincide con el plan de edición.");
    }

    private static string ReadTextObjectText(IntPtr textObject, IntPtr textPage)
    {
        var required = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, IntPtr.Zero, 0);
        if (required == 0)
            throw new InvalidOperationException("PDFium no pudo leer el texto original del objeto.");

        var buffer = Marshal.AllocHGlobal(checked((int)required * sizeof(ushort)));
        try
        {
            var copied = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, buffer, required);
            if (copied == 0 || copied > required)
                throw new InvalidOperationException("PDFium devolvió una longitud inválida al leer el texto original.");

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

    private static string ReadBaseFontName(IntPtr font)
    {
        var required = PdfiumNative.FPDFFont_GetBaseFontName(font, IntPtr.Zero, 0);
        if (required == 0 || required > int.MaxValue)
            throw new InvalidOperationException("PDFium devolvió una longitud inválida para el nombre de fuente.");

        var buffer = Marshal.AllocHGlobal(checked((int)required));
        try
        {
            var copied = PdfiumNative.FPDFFont_GetBaseFontName(font, buffer, required);
            if (copied == 0 || copied > required)
                throw new InvalidOperationException("PDFium no pudo leer el nombre de la fuente original.");

            var bytes = new byte[checked((int)copied)];
            Marshal.Copy(buffer, bytes, 0, bytes.Length);
            var contentLength = bytes.Length > 0 && bytes[^1] == 0 ? bytes.Length - 1 : bytes.Length;
            return Encoding.UTF8.GetString(bytes, 0, contentLength);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
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

    private static void ValidateOutput(
        string path,
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace? textWorkspace,
        CancellationToken cancellationToken)
    {
        var validator = new PdfEditOutputValidator();
        if (textWorkspace is null)
            validator.Validate(path, imageWorkspace, cancellationToken);
        else
            validator.Validate(path, imageWorkspace, textWorkspace, cancellationToken);
    }

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
    private sealed record ResolvedText(TextEditState State, IntPtr Handle);

    private sealed class FileWriteContext
    {
        internal FileWriteContext(FileStream stream) => Stream = stream;
        internal Exception? Error { get; set; }
        internal FileStream Stream { get; }
    }
}
