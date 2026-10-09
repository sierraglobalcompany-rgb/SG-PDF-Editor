using System.IO;
using System.Runtime.InteropServices;
using SGPdf.App.Features.Organize;

namespace SGPdf.App.Pdf;

internal sealed class PdfOrganizeWriter
{
    private readonly Func<IntPtr, IntPtr, uint, int> _saveAsCopy;
    private readonly Action<string, IReadOnlyList<OrganizeExpectedPage>, CancellationToken> _validateOutput;
    private readonly Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult> _preflight;

    internal PdfOrganizeWriter(
        Func<IntPtr, IntPtr, uint, int>? saveAsCopyOverride = null,
        Action<string, IReadOnlyList<OrganizeExpectedPage>, CancellationToken>? validateOutputOverride = null,
        Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>? preflightOverride = null)
    {
        _saveAsCopy = saveAsCopyOverride ?? PdfiumNative.FPDF_SaveAsCopy;
        _validateOutput = validateOutputOverride ?? ValidateOutput;
        _preflight = preflightOverride ?? InspectPreflight;
    }

    internal void SaveAsCopy(
        OrganizePlan plan,
        string activeSourcePath,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeSourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        cancellationToken.ThrowIfCancellationRequested();
        var activeFullPath = Path.GetFullPath(activeSourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);

        if (string.Equals(activeFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Guardar como requiere una ruta diferente al PDF activo.", nameof(destinationPath));

        if (!plan.Sources.Any(source =>
                string.Equals(source.Path, activeFullPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("El PDF activo no pertenece al plan de organización.", nameof(activeSourcePath));
        }

        if (plan.Sources.Any(source =>
                string.Equals(source.Path, destinationFullPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Guardar como no puede sobrescribir ningún PDF de origen.", nameof(destinationPath));
        }

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath)
            ?? throw new ArgumentException("La ruta de destino no tiene directorio.", nameof(destinationPath));
        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException(destinationDirectory);

        foreach (var source in plan.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!source.MatchesCurrentFile())
            {
                throw new IOException(
                    $"El PDF de origen cambió o ya no está disponible: {source.Path}");
            }
        }

        PreflightSources(plan, warningsConfirmed, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var destinationFileName = Path.GetFileName(destinationFullPath);
        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{destinationFileName}.{Guid.NewGuid():N}.sgpdf.tmp");

        try
        {
            var expectedPages = WriteTemporaryCopy(plan, temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _validateOutput(temporaryPath, expectedPages, cancellationToken);
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

    private void PreflightSources(
        OrganizePlan plan,
        bool warningsConfirmed,
        CancellationToken cancellationToken)
    {
        foreach (var source in plan.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var session = PdfDocumentSession.Open(source.Path);
                if (session.PageCount != source.OriginalPageCount)
                    throw new InvalidOperationException("El PDF de origen cambió su cantidad de páginas.");

                var result = _preflight(session, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (!result.CanProceed)
                    throw new InvalidOperationException("El PDF contiene una estructura bloqueada para ORGANIZAR.");

                if (result.RequiresWarningConfirmation && !warningsConfirmed)
                {
                    throw new InvalidOperationException(
                        "El PDF contiene estructuras cuya preservación no está demostrada y requiere confirmación explícita.");
                }
            }
            catch (PdfDocumentOpenException ex)
                when (ex.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
            {
                throw new InvalidOperationException(
                    "Los PDF protegidos con contraseña no se pueden materializar en ORGANIZAR.",
                    ex);
            }
        }
    }

    private IReadOnlyList<OrganizeExpectedPage> WriteTemporaryCopy(
        OrganizePlan plan,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);

        var sourceDocuments = new Dictionary<Guid, IntPtr>();
        IntPtr destinationDocument = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var source in plan.Sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = PdfiumNative.FPDF_LoadDocument(source.Path, null);
                if (document == IntPtr.Zero)
                {
                    var error = PdfiumNative.FPDF_GetLastError();
                    throw new InvalidOperationException(
                        $"PDFium no pudo abrir un origen para ORGANIZAR. Error: {error}.");
                }

                if (PdfiumNative.FPDF_GetPageCount(document) != source.OriginalPageCount)
                {
                    PdfiumNative.FPDF_CloseDocument(document);
                    throw new InvalidOperationException("El PDF de origen cambió su cantidad de páginas.");
                }

                sourceDocuments.Add(source.SourceId, document);
            }

            destinationDocument = PdfiumNative.FPDF_CreateNewDocument();
            if (destinationDocument == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo crear el PDF de destino.");

            var expectedPages = new List<OrganizeExpectedPage>(plan.Pages.Count);
            for (var outputPageIndex = 0; outputPageIndex < plan.Pages.Count; outputPageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pagePlan = plan.Pages[outputPageIndex];
                if (!sourceDocuments.TryGetValue(pagePlan.SourceId, out var sourceDocument))
                    throw new InvalidOperationException("El plan referencia un origen que no está abierto.");

                if (PdfiumNative.FPDF_GetPageSizeByIndexF(
                        sourceDocument,
                        pagePlan.SourcePageIndex,
                        out var sourceSize) == 0)
                {
                    throw new InvalidOperationException(
                        $"PDFium no pudo consultar el tamaño de la página de origen {pagePlan.SourcePageIndex + 1}.");
                }

                var width = (double)sourceSize.Width;
                var height = (double)sourceSize.Height;
                if (!double.IsFinite(width) || width <= 0d ||
                    !double.IsFinite(height) || height <= 0d)
                {
                    throw new InvalidOperationException("PDFium devolvió un tamaño de página inválido.");
                }

                var sourcePage = PdfiumNative.FPDF_LoadPage(sourceDocument, pagePlan.SourcePageIndex);
                if (sourcePage == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium no pudo cargar una página de origen.");

                int sourceRotation;
                try
                {
                    sourceRotation = PdfiumNative.FPDFPage_GetRotation(sourcePage);
                    if (sourceRotation is < 0 or > 3)
                        throw new InvalidOperationException("PDFium devolvió una rotación de origen inválida.");
                }
                finally
                {
                    PdfiumNative.FPDF_ClosePage(sourcePage);
                }

                var sourcePageIndex = new[] { pagePlan.SourcePageIndex };
                if (PdfiumNative.FPDF_ImportPagesByIndex(
                        destinationDocument,
                        sourceDocument,
                        sourcePageIndex,
                        1u,
                        outputPageIndex) == 0)
                {
                    throw new InvalidOperationException(
                        $"PDFium no pudo importar la página de salida {outputPageIndex + 1}.");
                }

                var absoluteRotation = (sourceRotation + pagePlan.RotationDeltaQuarterTurns) & 3;
                var importedPage = PdfiumNative.FPDF_LoadPage(destinationDocument, outputPageIndex);
                if (importedPage == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium no pudo reabrir una página importada.");

                try
                {
                    PdfiumNative.FPDFPage_SetRotation(importedPage, absoluteRotation);
                }
                finally
                {
                    PdfiumNative.FPDF_ClosePage(importedPage);
                }

                var swapsVisibleAxes = (pagePlan.RotationDeltaQuarterTurns & 1) != 0;
                expectedPages.Add(new OrganizeExpectedPage(
                    swapsVisibleAxes
                        ? new PdfPageSize(height, width)
                        : new PdfPageSize(width, height),
                    absoluteRotation));
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveDocument(destinationDocument, temporaryPath);
            return expectedPages.AsReadOnly();
        }
        finally
        {
            if (destinationDocument != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(destinationDocument);

            foreach (var sourceDocument in sourceDocuments.Values)
            {
                if (sourceDocument != IntPtr.Zero)
                    PdfiumNative.FPDF_CloseDocument(sourceDocument);
            }

            PdfiumRuntime.NativeGate.Release();
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
                throw context.Error ?? new InvalidOperationException("PDFium no pudo guardar el PDF reorganizado.");
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

    private static OrganizePreflightResult InspectPreflight(
        PdfDocumentSession session,
        CancellationToken cancellationToken)
        => new OrganizePreflightInspector().Inspect(session, cancellationToken);

    private static void ValidateOutput(
        string path,
        IReadOnlyList<OrganizeExpectedPage> expectedPages,
        CancellationToken cancellationToken)
        => new OrganizeOutputValidator().Validate(path, expectedPages, cancellationToken);

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

    private sealed class FileWriteContext
    {
        internal FileWriteContext(FileStream stream) => Stream = stream;
        internal FileStream Stream { get; }
        internal Exception? Error { get; set; }
    }
}
