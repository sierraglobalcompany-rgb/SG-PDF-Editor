using System.IO;
using System.Runtime.InteropServices;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Utilities;

namespace SGPdf.App.Pdf;

internal sealed record PdfTextOverlayWriteResult(bool Succeeded, string? Error)
{
    internal static PdfTextOverlayWriteResult Success { get; } = new(true, null);

    internal static PdfTextOverlayWriteResult Failure(string error) => new(false, error);
}

internal sealed class PdfTextOverlayWriter
{
    private readonly Func<string, CancellationToken, bool>? _validatorOverride;
    private readonly Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> _preflight;
    private readonly PdfTextOverlayOutputValidator _outputValidator = new();

    internal PdfTextOverlayWriter()
    {
        var utilityPreflight = new PdfUtilityPreflight();
        _preflight = utilityPreflight.Inspect;
    }

    internal PdfTextOverlayWriter(
        Func<string, CancellationToken, bool> validator,
        Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> preflight)
    {
        _validatorOverride = validator ?? throw new ArgumentNullException(nameof(validator));
        _preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
    }

    internal PdfTextOverlayWriteResult SaveAsCopy(
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        IReadOnlyList<TextOverlayPlacement> placements,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        if (placements.Count == 0)
            throw new ArgumentException("At least one text overlay placement is required.", nameof(placements));

        var sourceFullPath = Path.GetFullPath(sourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source and destination PDF paths must differ.", nameof(destinationPath));

        ValidatePlacements(placements);
        cancellationToken.ThrowIfCancellationRequested();

        if (!fingerprint.MatchesCurrentFile(sourceFullPath))
            return PdfTextOverlayWriteResult.Failure("The source PDF changed after the utility session started.");

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
        if (string.IsNullOrWhiteSpace(destinationDirectory) || !Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException("The destination directory does not exist.");

        string? temporaryPath = null;
        try
        {
            using (var session = PdfDocumentSession.Open(sourceFullPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!fingerprint.MatchesCurrentFile(sourceFullPath))
                    return PdfTextOverlayWriteResult.Failure("The source PDF changed before the utility write began.");

                var preflight = _preflight(session, cancellationToken);
                if (!preflight.CanProceed)
                    return PdfTextOverlayWriteResult.Failure(BuildPreflightError(preflight));
            }

            cancellationToken.ThrowIfCancellationRequested();
            temporaryPath = CreateTemporaryPath(destinationFullPath);
            WriteTemporaryCopy(sourceFullPath, fingerprint, placements, temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (_validatorOverride is not null)
            {
                if (!_validatorOverride(temporaryPath, cancellationToken))
                    return PdfTextOverlayWriteResult.Failure("The saved overlay output failed validation.");
            }
            else
            {
                var validation = _outputValidator.Validate(temporaryPath, placements, cancellationToken);
                if (!validation.IsValid)
                    return PdfTextOverlayWriteResult.Failure(validation.Error ?? "The saved overlay output failed validation.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            PublishTemporaryFile(temporaryPath, destinationFullPath);
            temporaryPath = null;
            return PdfTextOverlayWriteResult.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NotSupportedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PdfTextOverlayWriteResult.Failure(ex.Message);
        }
        finally
        {
            if (temporaryPath is not null && File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch
                {
                }
            }
        }
    }

    private static void ValidatePlacements(IReadOnlyList<TextOverlayPlacement> placements)
    {
        foreach (var placement in placements)
        {
            if (placement.PageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(placements), "Overlay page index cannot be negative.");
            ArgumentException.ThrowIfNullOrWhiteSpace(placement.Text);
            if (!double.IsFinite(placement.FontSize) || placement.FontSize <= 0d)
                throw new ArgumentOutOfRangeException(nameof(placements), "Overlay font size must be finite and greater than zero.");
            if (!double.IsFinite(placement.Opacity) || placement.Opacity < 0d || placement.Opacity > 1d)
                throw new ArgumentOutOfRangeException(nameof(placements), "Overlay opacity must be between zero and one.");
            if (!MatrixIsFinite(placement.Matrix))
                throw new ArgumentOutOfRangeException(nameof(placements), "Overlay matrix must contain finite values.");
        }
    }

    private static bool MatrixIsFinite(PdfObjectMatrix matrix) =>
        double.IsFinite(matrix.A) &&
        double.IsFinite(matrix.B) &&
        double.IsFinite(matrix.C) &&
        double.IsFinite(matrix.D) &&
        double.IsFinite(matrix.E) &&
        double.IsFinite(matrix.F);

    private static string BuildPreflightError(PdfEditPreflightResult preflight)
    {
        var blockers = preflight.Findings
            .Where(finding => finding.Severity == PdfEditFindingSeverity.Block)
            .Select(finding => finding.Message)
            .ToArray();
        return blockers.Length == 0
            ? "The PDF is not safe to materialize with the utility writer."
            : string.Join(" ", blockers);
    }

    private static string CreateTemporaryPath(string destinationFullPath)
    {
        var directory = Path.GetDirectoryName(destinationFullPath)!;
        var stem = Path.GetFileNameWithoutExtension(destinationFullPath);
        return Path.Combine(directory, $"{stem}.{Guid.NewGuid():N}.sgpdf-tmp.pdf");
    }

    private static void PublishTemporaryFile(string temporaryPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
            File.Replace(temporaryPath, destinationPath, destinationBackupFileName: null);
        else
            File.Move(temporaryPath, destinationPath);
    }

    private static void WriteTemporaryCopy(
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        IReadOnlyList<TextOverlayPlacement> placements,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        if (!fingerprint.MatchesCurrentFile(sourcePath))
            throw new InvalidOperationException("The source PDF changed before native mutation.");

        var map = CidType2FontMapBuilder.Build(placements.Select(placement => placement.Text));
        var fontBytes = FallbackFontAsset.LoadBytes();

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        IntPtr document = IntPtr.Zero;
        IntPtr font = IntPtr.Zero;
        IntPtr fontBuffer = IntPtr.Zero;
        IntPtr cidToGidBuffer = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open the source PDF for text overlays.");

            if (!fingerprint.MatchesCurrentFile(sourcePath))
                throw new InvalidOperationException("The source PDF changed while the utility writer was opening it.");

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageCount <= 0)
                throw new InvalidOperationException("PDFium returned an invalid page count for the source PDF.");
            if (placements.Any(placement => placement.PageIndex >= pageCount))
                throw new ArgumentOutOfRangeException(nameof(placements), "An overlay placement targets a page outside the document.");

            fontBuffer = Marshal.AllocHGlobal(fontBytes.Length);
            Marshal.Copy(fontBytes, 0, fontBuffer, fontBytes.Length);
            cidToGidBuffer = Marshal.AllocHGlobal(map.CidToGidMap.Length);
            Marshal.Copy(map.CidToGidMap, 0, cidToGidBuffer, map.CidToGidMap.Length);

            font = PdfiumNative.FPDFText_LoadCidType2Font(
                document,
                fontBuffer,
                checked((uint)fontBytes.Length),
                map.ToUnicodeCMap,
                cidToGidBuffer,
                checked((uint)map.CidToGidMap.Length));
            if (font == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the pinned CID Type2 fallback font.");

            foreach (var pageGroup in placements.GroupBy(placement => placement.PageIndex).OrderBy(group => group.Key))
            {
                cancellationToken.ThrowIfCancellationRequested();
                WritePageOverlays(document, font, pageGroup.Key, pageGroup.ToArray(), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveDocument(document, temporaryPath);
        }
        finally
        {
            if (font != IntPtr.Zero)
                PdfiumNative.FPDFFont_Close(font);
            if (cidToGidBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(cidToGidBuffer);
            if (fontBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(fontBuffer);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static void WritePageOverlays(
        IntPtr document,
        IntPtr font,
        int pageIndex,
        IReadOnlyList<TextOverlayPlacement> placements,
        CancellationToken cancellationToken)
    {
        var page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
        if (page == IntPtr.Zero)
            throw new InvalidOperationException($"PDFium could not load page {pageIndex} for text overlays.");

        try
        {
            foreach (var placement in placements
                         .Where(item => item.Layer == TextOverlayLayer.Background)
                         .Reverse())
            {
                cancellationToken.ThrowIfCancellationRequested();
                InsertTextObject(document, page, font, placement, background: true);
            }

            foreach (var placement in placements.Where(item => item.Layer == TextOverlayLayer.Foreground))
            {
                cancellationToken.ThrowIfCancellationRequested();
                InsertTextObject(document, page, font, placement, background: false);
            }

            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException($"PDFium could not regenerate page {pageIndex} after text overlays.");
        }
        finally
        {
            PdfiumNative.FPDF_ClosePage(page);
        }
    }

    private static void InsertTextObject(
        IntPtr document,
        IntPtr page,
        IntPtr font,
        TextOverlayPlacement placement,
        bool background)
    {
        var textObject = PdfiumNative.FPDFPageObj_CreateTextObj(
            document,
            font,
            checked((float)placement.FontSize));
        if (textObject == IntPtr.Zero)
            throw new InvalidOperationException("PDFium could not create a text overlay object.");

        var inserted = false;
        try
        {
            if (PdfiumNative.FPDFText_SetText(textObject, placement.Text) == 0)
                throw new InvalidOperationException("PDFium could not assign Unicode text to an overlay object.");

            var alpha = checked((uint)Math.Round(placement.Opacity * 255d, MidpointRounding.AwayFromZero));
            if (PdfiumNative.FPDFPageObj_SetFillColor(textObject, 0u, 0u, 0u, alpha) == 0)
                throw new InvalidOperationException("PDFium could not assign opacity to a text overlay object.");

            var matrix = new PdfiumNative.Matrix
            {
                A = checked((float)placement.Matrix.A),
                B = checked((float)placement.Matrix.B),
                C = checked((float)placement.Matrix.C),
                D = checked((float)placement.Matrix.D),
                E = checked((float)placement.Matrix.E),
                F = checked((float)placement.Matrix.F)
            };
            if (PdfiumNative.FPDFPageObj_SetMatrix(textObject, ref matrix) == 0)
                throw new InvalidOperationException("PDFium could not assign the text overlay matrix.");

            var insertedResult = background
                ? PdfiumNative.FPDFPage_InsertObjectAtIndex(page, textObject, 0)
                : PdfiumNative.FPDFPage_InsertObject(page, textObject);
            if (insertedResult == 0)
                throw new InvalidOperationException("PDFium could not insert a text overlay object.");
            inserted = true;
        }
        finally
        {
            if (!inserted)
                PdfiumNative.FPDFPageObj_Destroy(textObject);
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
                throw context.Error ?? new InvalidOperationException("PDFium could not save the text overlay copy.");
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
            var fileWrite = Marshal.PtrToStructure<PdfiumNative.ExtendedFileWrite>(self);
            var handle = GCHandle.FromIntPtr(fileWrite.Context);
            if (handle.Target is not NativeFileWriteContext context || size > int.MaxValue)
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
                if (handle.Target is NativeFileWriteContext context)
                    context.Error = ex;
            }
            catch
            {
            }

            return 0;
        }
    }

    private sealed class NativeFileWriteContext
    {
        internal NativeFileWriteContext(FileStream stream) => Stream = stream;

        internal FileStream Stream { get; }

        internal Exception? Error { get; set; }
    }
}
