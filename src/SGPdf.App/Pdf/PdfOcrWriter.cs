using System.IO;
using System.Runtime.InteropServices;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Features.Utilities;

namespace SGPdf.App.Pdf;

internal sealed record PdfOcrWriteResult(bool Succeeded, string? Error)
{
    internal static PdfOcrWriteResult Success { get; } = new(true, null);

    internal static PdfOcrWriteResult Failure(string error) => new(false, error);
}

internal sealed class PdfOcrWriter
{
    private readonly Func<string, CancellationToken, bool> _validator;
    private readonly Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> _preflight;

    internal PdfOcrWriter()
    {
        var utilityPreflight = new PdfUtilityPreflight();
        _preflight = utilityPreflight.Inspect;
        _validator = ValidateReadableCopy;
    }

    internal PdfOcrWriter(
        Func<string, CancellationToken, bool> validator,
        Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> preflight)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
    }

    internal PdfOcrWriteResult SaveAsCopy(
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        IReadOnlyList<OcrTextLinePlacement> placements,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        if (placements.Count == 0)
            throw new ArgumentException("At least one OCR text placement is required.", nameof(placements));

        var sourceFullPath = Path.GetFullPath(sourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source and destination PDF paths must differ.", nameof(destinationPath));

        ValidatePlacements(placements);
        cancellationToken.ThrowIfCancellationRequested();

        if (!fingerprint.MatchesCurrentFile(sourceFullPath))
            return PdfOcrWriteResult.Failure("The source PDF changed after the OCR session started.");

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
                    return PdfOcrWriteResult.Failure("The source PDF changed before the OCR write began.");

                if (!session.IsTrueInvisibleTextRenderModeAvailableForOcr())
                    return PdfOcrWriteResult.Failure("PDFium true invisible text render mode is unavailable.");

                var preflight = _preflight(session, cancellationToken);
                if (!preflight.CanProceed)
                    return PdfOcrWriteResult.Failure(BuildPreflightError(preflight));

                var invalidPage = placements.FirstOrDefault(placement => placement.PageIndex >= session.PageCount);
                if (invalidPage is not null)
                    return PdfOcrWriteResult.Failure("An OCR placement targets a page outside the document.");

                var eligibility = new OcrPageEligibilityPolicy().Resolve(
                    session,
                    OcrPageScope.Automatic,
                    cancellationToken);
                var eligiblePages = eligibility.CandidatePageIndices.ToHashSet();
                if (placements.Any(placement => !eligiblePages.Contains(placement.PageIndex)))
                    return PdfOcrWriteResult.Failure("OCR cannot add a hidden text layer to a page that already contains meaningful native text.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            temporaryPath = CreateTemporaryPath(destinationFullPath);
            WriteTemporaryCopy(
                sourceFullPath,
                fingerprint,
                placements,
                temporaryPath,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (!_validator(temporaryPath, cancellationToken))
                return PdfOcrWriteResult.Failure("The saved OCR output failed validation.");

            cancellationToken.ThrowIfCancellationRequested();
            if (!fingerprint.MatchesCurrentFile(sourceFullPath))
                return PdfOcrWriteResult.Failure("The source PDF changed before the OCR output was published.");

            PublishTemporaryFile(temporaryPath, destinationFullPath);
            temporaryPath = null;
            return PdfOcrWriteResult.Success;
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
            return PdfOcrWriteResult.Failure(ex.Message);
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

    private static void ValidatePlacements(IReadOnlyList<OcrTextLinePlacement> placements)
    {
        foreach (var placement in placements)
        {
            if (placement.PageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(placements), "OCR page index cannot be negative.");
            ArgumentException.ThrowIfNullOrWhiteSpace(placement.Text);
            ValidateBounds(placement.Bounds, nameof(placements));
            if (placement.Words.Count == 0)
                throw new ArgumentException("Each OCR line must contain at least one word.", nameof(placements));

            foreach (var word in placement.Words)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(word.Text);
                ValidateBounds(word.Bounds, nameof(placements));
            }
        }
    }

    private static void ValidateBounds(PdfOcrRect bounds, string parameterName)
    {
        if (!double.IsFinite(bounds.Left) ||
            !double.IsFinite(bounds.Bottom) ||
            !double.IsFinite(bounds.Right) ||
            !double.IsFinite(bounds.Top) ||
            bounds.Right <= bounds.Left ||
            bounds.Top <= bounds.Bottom)
        {
            throw new ArgumentOutOfRangeException(parameterName, "OCR bounds must be finite and have positive width and height.");
        }
    }

    private static string BuildPreflightError(PdfEditPreflightResult preflight)
    {
        var blockers = preflight.Findings
            .Where(finding => finding.Severity == PdfEditFindingSeverity.Block)
            .Select(finding => finding.Message)
            .ToArray();
        return blockers.Length == 0
            ? "The PDF is not safe to materialize with the OCR writer."
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

    private static bool ValidateReadableCopy(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var session = PdfDocumentSession.Open(path);
        cancellationToken.ThrowIfCancellationRequested();
        return session.PageCount > 0;
    }

    private static void WriteTemporaryCopy(
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        IReadOnlyList<OcrTextLinePlacement> placements,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        if (!fingerprint.MatchesCurrentFile(sourcePath))
            throw new InvalidOperationException("The source PDF changed before native OCR mutation.");

        var words = placements
            .SelectMany(placement => placement.Words)
            .OrderBy(word => word.SourceOrder)
            .ToArray();
        var map = CidType2FontMapBuilder.Build(words.Select(word => word.Text));
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
                throw new InvalidOperationException("PDFium could not open the source PDF for OCR text insertion.");

            if (!fingerprint.MatchesCurrentFile(sourcePath))
                throw new InvalidOperationException("The source PDF changed while the OCR writer was opening it.");

            var pageCount = PdfiumNative.FPDF_GetPageCount(document);
            if (pageCount <= 0)
                throw new InvalidOperationException("PDFium returned an invalid page count for the source PDF.");
            if (placements.Any(placement => placement.PageIndex >= pageCount))
                throw new ArgumentOutOfRangeException(nameof(placements), "An OCR placement targets a page outside the document.");

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
                throw new InvalidOperationException("PDFium could not load the pinned CID Type2 fallback font for OCR.");

            foreach (var pageGroup in placements
                         .GroupBy(placement => placement.PageIndex)
                         .OrderBy(group => group.Key))
            {
                cancellationToken.ThrowIfCancellationRequested();
                WritePageWords(
                    document,
                    font,
                    pageGroup.Key,
                    pageGroup.OrderBy(placement => placement.SourceOrder).ToArray(),
                    cancellationToken);
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

    private static void WritePageWords(
        IntPtr document,
        IntPtr font,
        int pageIndex,
        IReadOnlyList<OcrTextLinePlacement> placements,
        CancellationToken cancellationToken)
    {
        var page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
        if (page == IntPtr.Zero)
            throw new InvalidOperationException($"PDFium could not load page {pageIndex} for OCR text insertion.");

        try
        {
            foreach (var placement in placements)
            {
                foreach (var word in placement.Words.OrderBy(item => item.SourceOrder))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InsertInvisibleWord(document, page, font, word);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException($"PDFium could not regenerate page {pageIndex} after OCR text insertion.");
        }
        finally
        {
            PdfiumNative.FPDF_ClosePage(page);
        }
    }

    private static void InsertInvisibleWord(
        IntPtr document,
        IntPtr page,
        IntPtr font,
        OcrTextWordPlacement word)
    {
        var fontSize = word.Bounds.Top - word.Bounds.Bottom;
        var textObject = PdfiumNative.FPDFPageObj_CreateTextObj(
            document,
            font,
            checked((float)fontSize));
        if (textObject == IntPtr.Zero)
            throw new InvalidOperationException("PDFium could not create an OCR text object.");

        var inserted = false;
        try
        {
            if (PdfiumNative.FPDFText_SetText(textObject, word.Text) == 0)
                throw new InvalidOperationException("PDFium could not assign Unicode OCR text.");

            if (!PdfiumNative.FPDFPageObj_SetRenderMode(textObject, 3))
                throw new InvalidOperationException("PDFium could not set true invisible render mode for OCR text.");

            var matrix = new PdfiumNative.Matrix
            {
                A = 1f,
                B = 0f,
                C = 0f,
                D = 1f,
                E = checked((float)word.Bounds.Left),
                F = checked((float)word.Bounds.Bottom)
            };
            if (PdfiumNative.FPDFPageObj_SetMatrix(textObject, ref matrix) == 0)
                throw new InvalidOperationException("PDFium could not position an OCR text object.");

            if (PdfiumNative.FPDFPage_InsertObject(page, textObject) == 0)
                throw new InvalidOperationException("PDFium could not insert an OCR text object.");
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
                throw context.Error ?? new InvalidOperationException("PDFium could not save the OCR copy.");
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
