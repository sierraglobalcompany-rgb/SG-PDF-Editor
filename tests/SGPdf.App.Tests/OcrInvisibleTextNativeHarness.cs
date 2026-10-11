using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal sealed class OcrInvisibleTextCapabilitySnapshot : IDisposable
{
    internal required string DirectoryPath { get; init; }
    internal required string SourcePath { get; init; }
    internal required string OutputPath { get; init; }
    internal required byte[] SourceBytesBefore { get; init; }
    internal required byte[] SourceBytesAfter { get; init; }
    internal required PdfRenderedPage SourceRender { get; init; }
    internal required PdfRenderedPage OutputRender { get; init; }
    internal required string ExtractedText { get; init; }
    internal required int RenderMode { get; init; }
    internal required IReadOnlyList<PdfTextMatch> NinoMatches { get; init; }
    internal required IReadOnlyList<PdfTextMatch> BogotaMatches { get; init; }
    internal required IReadOnlyList<PdfTextMatch> NumberMatches { get; init; }
    internal required IReadOnlyList<PdfTextRect> TextRects { get; init; }
    internal required bool TextGeometryIntersectsExpectedVisibleRegion { get; init; }
    internal required bool SourceHadSearchableText { get; init; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal static class OcrInvisibleTextNativeHarness
{
    internal const string SearchableText =
        "Factura número 12345 — NIÑO Bogotá, información útil. Total: $98.765,43";

    private const double PageWidth = 300d;
    private const double PageHeight = 400d;
    private const float HiddenFontSize = 18f;
    private static readonly PdfTextRect ExpectedVisibleRegion = new(28d, 175d, 295d, 255d);

    internal static OcrInvisibleTextCapabilitySnapshot Characterize(bool rotate90)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"sgpdf-f10-invisible-text-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var sourcePath = Path.Combine(directory, rotate90 ? "source-rot90.pdf" : "source.pdf");
            var outputPath = Path.Combine(directory, rotate90 ? "output-rot90.pdf" : "output.pdf");
            WriteImageOnlyPdf(sourcePath, rotate90);

            var sourceBytesBefore = File.ReadAllBytes(sourcePath);
            PdfRenderedPage sourceRender;
            bool sourceHadSearchableText;
            using (var sourceSession = PdfDocumentSession.Open(sourcePath))
            {
                sourceRender = sourceSession.RenderPage(0, 72d);
                sourceHadSearchableText =
                    sourceSession.FindTextOnPage(0, "NIÑO").Count != 0 ||
                    sourceSession.FindTextOnPage(0, "Bogotá").Count != 0 ||
                    sourceSession.FindTextOnPage(0, "12345").Count != 0;
            }

            WriteInvisibleTextCopy(sourcePath, outputPath);
            var sourceBytesAfter = File.ReadAllBytes(sourcePath);

            var renderMode = ReadInsertedTextRenderMode(outputPath);
            PdfRenderedPage outputRender;
            string extractedText;
            IReadOnlyList<PdfTextMatch> ninoMatches;
            IReadOnlyList<PdfTextMatch> bogotaMatches;
            IReadOnlyList<PdfTextMatch> numberMatches;
            IReadOnlyList<PdfTextRect> textRects;
            using (var outputSession = PdfDocumentSession.Open(outputPath))
            {
                outputRender = outputSession.RenderPage(0, 72d);
                extractedText = outputSession.GetTextRange(0, 0, SearchableText.Length + 16);
                ninoMatches = outputSession.FindTextOnPage(0, "NIÑO");
                bogotaMatches = outputSession.FindTextOnPage(0, "Bogotá");
                numberMatches = outputSession.FindTextOnPage(0, "12345");
                textRects = ninoMatches.SelectMany(match => match.Rects).ToArray();
            }

            return new OcrInvisibleTextCapabilitySnapshot
            {
                DirectoryPath = directory,
                SourcePath = sourcePath,
                OutputPath = outputPath,
                SourceBytesBefore = sourceBytesBefore,
                SourceBytesAfter = sourceBytesAfter,
                SourceRender = sourceRender,
                OutputRender = outputRender,
                ExtractedText = extractedText,
                RenderMode = renderMode,
                NinoMatches = ninoMatches,
                BogotaMatches = bogotaMatches,
                NumberMatches = numberMatches,
                TextRects = textRects,
                TextGeometryIntersectsExpectedVisibleRegion = textRects.Any(IntersectsExpectedVisibleRegion),
                SourceHadSearchableText = sourceHadSearchableText
            };
        }
        catch
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
            throw;
        }
    }

    private static bool IntersectsExpectedVisibleRegion(PdfTextRect rect) =>
        rect.Right > ExpectedVisibleRegion.Left &&
        rect.Left < ExpectedVisibleRegion.Right &&
        rect.Top > ExpectedVisibleRegion.Bottom &&
        rect.Bottom < ExpectedVisibleRegion.Top;

    private static void WriteInvisibleTextCopy(string sourcePath, string destinationPath)
    {
        var map = CidType2FontMapBuilder.Build(new[] { SearchableText });
        var fontBytes = FallbackFontAsset.LoadBytes();

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        IntPtr font = IntPtr.Zero;
        IntPtr fontBuffer = IntPtr.Zero;
        IntPtr cidToGidBuffer = IntPtr.Zero;
        IntPtr textObject = IntPtr.Zero;
        var inserted = false;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open the F10 image-only source.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F10 source page.");

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
                throw new InvalidOperationException("PDFium could not load the pinned CID Type2 font for F10.");

            textObject = PdfiumNative.FPDFPageObj_CreateTextObj(document, font, HiddenFontSize);
            if (textObject == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not create the F10 OCR text object.");

            if (PdfiumNative.FPDFText_SetText(textObject, SearchableText) == 0)
                throw new InvalidOperationException("PDFium could not assign Unicode OCR text.");
            if (PdfiumNative.FPDFTextObj_SetTextRenderMode(
                    textObject,
                    PdfiumNative.FPDF_TEXTRENDERMODE_INVISIBLE) == 0)
            {
                throw new InvalidOperationException("PDFium rejected true invisible text render mode.");
            }

            var matrix = new PdfiumNative.Matrix
            {
                A = 1f,
                B = 0f,
                C = 0f,
                D = 1f,
                E = 34f,
                F = 214f
            };
            if (PdfiumNative.FPDFPageObj_SetMatrix(textObject, ref matrix) == 0)
                throw new InvalidOperationException("PDFium could not position the F10 invisible text object.");

            if (PdfiumNative.FPDFPage_InsertObject(page, textObject) == 0)
                throw new InvalidOperationException("PDFium could not insert the F10 invisible text object.");
            inserted = true;

            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException("PDFium could not regenerate content after F10 invisible text insertion.");

            SaveDocument(document, destinationPath);
        }
        finally
        {
            if (!inserted && textObject != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(textObject);
            if (font != IntPtr.Zero)
                PdfiumNative.FPDFFont_Close(font);
            if (cidToGidBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(cidToGidBuffer);
            if (fontBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(fontBuffer);
            if (page != IntPtr.Zero)
                PdfiumNative.FPDF_ClosePage(page);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static int ReadInsertedTextRenderMode(string path)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        IntPtr textPage = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(path, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not reopen the F10 output.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not reopen the F10 output page.");

            textPage = PdfiumNative.FPDFText_LoadPage(page);
            if (textPage == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F10 output text page.");

            var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
            for (var index = 0; index < objectCount; index++)
            {
                var pageObject = PdfiumNative.FPDFPage_GetObject(page, index);
                if (pageObject == IntPtr.Zero ||
                    PdfiumNative.FPDFPageObj_GetType(pageObject) != PdfiumNative.FPDF_PAGEOBJ_TEXT)
                {
                    continue;
                }

                var text = ReadTextObject(pageObject, textPage);
                if (text.Contains("NIÑO", StringComparison.Ordinal))
                    return PdfiumNative.FPDFTextObj_GetTextRenderMode(pageObject);
            }

            throw new InvalidOperationException("The inserted F10 invisible text object was not found after reopen.");
        }
        finally
        {
            if (textPage != IntPtr.Zero)
                PdfiumNative.FPDFText_ClosePage(textPage);
            if (page != IntPtr.Zero)
                PdfiumNative.FPDF_ClosePage(page);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static string ReadTextObject(IntPtr textObject, IntPtr textPage)
    {
        var required = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, IntPtr.Zero, 0);
        if (required == 0)
            return string.Empty;

        var buffer = Marshal.AllocHGlobal(checked((int)required * sizeof(ushort)));
        try
        {
            var copied = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, buffer, required);
            if (copied == 0 || copied > required)
                return string.Empty;

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

    private static void WriteImageOnlyPdf(string path, bool rotate90)
    {
        var jpeg = CreateVisibleScanJpeg();
        var content = Encoding.ASCII.GetBytes("q\n300 0 0 400 0 0 cm\n/Im0 Do\nQ\n");
        var rotateEntry = rotate90 ? " /Rotate 90" : string.Empty;

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        WriteAscii(stream, "%PDF-1.4\n%SGPDF-F10-IMAGE-ONLY\n");

        var offsets = new long[6];
        offsets[1] = stream.Position;
        WriteAscii(stream, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[2] = stream.Position;
        WriteAscii(stream, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets[3] = stream.Position;
        WriteAscii(
            stream,
            $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}]{rotateEntry} /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

        offsets[4] = stream.Position;
        WriteAscii(
            stream,
            $"4 0 obj\n<< /Type /XObject /Subtype /Image /Width 600 /Height 800 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpeg.Length} >>\nstream\n");
        stream.Write(jpeg, 0, jpeg.Length);
        WriteAscii(stream, "\nendstream\nendobj\n");

        offsets[5] = stream.Position;
        WriteAscii(stream, $"5 0 obj\n<< /Length {content.Length} >>\nstream\n");
        stream.Write(content, 0, content.Length);
        WriteAscii(stream, "endstream\nendobj\n");

        var xrefOffset = stream.Position;
        WriteAscii(stream, "xref\n0 6\n0000000000 65535 f \n");
        for (var index = 1; index <= 5; index++)
            WriteAscii(stream, $"{offsets[index]:0000000000} 00000 n \n");
        WriteAscii(stream, $"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
    }

    private static byte[] CreateVisibleScanJpeg()
    {
        var info = new SKImageInfo(600, 800, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var typeface = SKTypeface.Default;
        using var font = new SKFont(typeface, 30f);
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        canvas.DrawText("Factura número 12345 — NIÑO Bogotá,", 58f, 360f, SKTextAlign.Left, font, paint);
        canvas.DrawText("información útil. Total: $98.765,43", 58f, 420f, SKTextAlign.Left, font, paint);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        if (data is null)
            throw new InvalidOperationException("SkiaSharp could not encode the synthetic F10 scan fixture.");
        return data.ToArray();
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
                throw context.Error ?? new InvalidOperationException("PDFium F10 capability save failed.");
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

    private static void WriteAscii(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private sealed class NativeFileWriteContext
    {
        internal NativeFileWriteContext(FileStream stream) => Stream = stream;

        internal FileStream Stream { get; }

        internal Exception? Error { get; set; }
    }
}
