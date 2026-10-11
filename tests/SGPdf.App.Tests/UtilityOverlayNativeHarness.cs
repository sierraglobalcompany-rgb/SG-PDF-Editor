using System.Runtime.InteropServices;
using System.Text;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal sealed class UtilityOverlayFixture : IDisposable
{
    internal UtilityOverlayFixture(string directoryPath, string sourcePath)
    {
        DirectoryPath = directoryPath;
        SourcePath = sourcePath;
    }

    internal string DirectoryPath { get; }
    internal string SourcePath { get; }

    internal string Output(string fileName) => Path.Combine(DirectoryPath, fileName);

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal readonly record struct UtilityOverlayObjectSnapshot(
    int Index,
    int Type,
    string? Text,
    uint Red,
    uint Green,
    uint Blue,
    uint Alpha,
    PdfObjectMatrix Matrix);

internal sealed record UtilityOverlayResult(
    IReadOnlyList<UtilityOverlayObjectSnapshot> Objects,
    int RenderPixelWidth,
    int RenderPixelHeight);

internal static class UtilityOverlayNativeHarness
{
    internal const string BaseText = "BASE";
    internal const uint OverlayRed = 35;
    internal const uint OverlayGreen = 90;
    internal const uint OverlayBlue = 200;
    internal const uint OverlayAlpha = 96;
    internal const uint ImageAlpha = 112;

    private const int TextObjectType = 1;
    private const int ImageObjectType = 3;

    internal static readonly PdfObjectMatrix BackTextMatrix =
        RotationMatrix(degrees: 24d, x: 42d, y: 210d);

    internal static readonly PdfObjectMatrix FrontTextMatrix =
        RotationMatrix(degrees: -18d, x: 74d, y: 126d);

    internal static readonly PdfObjectMatrix BackImageMatrix =
        new(70d, 0d, 0d, 42d, 38d, 190d);

    internal static readonly PdfObjectMatrix FrontImageMatrix =
        new(54d, 0d, 0d, 32d, 116d, 102d);

    internal static UtilityOverlayFixture CreateSourcePdf()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"sgpdf-f9-overlay-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var sourcePath = Path.Combine(directory, "source.pdf");
        WriteMinimalHelveticaPdf(sourcePath, BaseText);
        return new UtilityOverlayFixture(directory, sourcePath);
    }

    internal static UtilityOverlayResult CreateTextOverlayCopy(
        string sourcePath,
        string destinationPath,
        string overlayText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayText);

        var map = CidType2FontMapBuilder.Build(new[] { overlayText });
        var fontBytes = FallbackFontAsset.LoadBytes();

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        IntPtr font = IntPtr.Zero;
        IntPtr fontBuffer = IntPtr.Zero;
        IntPtr cidToGidBuffer = IntPtr.Zero;
        IntPtr back = IntPtr.Zero;
        IntPtr front = IntPtr.Zero;
        var backInserted = false;
        var frontInserted = false;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open the F9 text-overlay source.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F9 text-overlay page.");

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
                throw new InvalidOperationException("PDFium could not load the pinned CID Type2 font for F9.");

            back = CreateTextObject(document, font, overlayText, BackTextMatrix);
            if (PdfiumNative.FPDFPage_InsertObjectAtIndex(page, back, 0) == 0)
                throw new InvalidOperationException("PDFium could not insert the background text overlay at index 0.");
            backInserted = true;

            front = CreateTextObject(document, font, overlayText, FrontTextMatrix);
            if (PdfiumNative.FPDFPage_InsertObject(page, front) == 0)
                throw new InvalidOperationException("PDFium could not append the foreground text overlay.");
            frontInserted = true;

            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException("PDFium could not regenerate page content after text overlays.");

            SaveDocument(document, destinationPath);
        }
        finally
        {
            if (!frontInserted && front != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(front);
            if (!backInserted && back != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(back);
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

        return InspectOutput(destinationPath);
    }

    internal static UtilityOverlayResult CreateImageOverlayCopy(
        string sourcePath,
        string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        IntPtr back = IntPtr.Zero;
        IntPtr front = IntPtr.Zero;
        var backInserted = false;
        var frontInserted = false;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open the F9 image-overlay source.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F9 image-overlay page.");

            back = CreateImageObject(document, page, BackImageMatrix, 0xFF4A90E2u);
            if (PdfiumNative.FPDFPage_InsertObjectAtIndex(page, back, 0) == 0)
                throw new InvalidOperationException("PDFium could not insert the background image overlay at index 0.");
            backInserted = true;

            front = CreateImageObject(document, page, FrontImageMatrix, 0xFFE67E22u);
            if (PdfiumNative.FPDFPage_InsertObject(page, front) == 0)
                throw new InvalidOperationException("PDFium could not append the foreground image overlay.");
            frontInserted = true;

            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException("PDFium could not regenerate page content after image overlays.");

            SaveDocument(document, destinationPath);
        }
        finally
        {
            if (!frontInserted && front != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(front);
            if (!backInserted && back != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(back);
            if (page != IntPtr.Zero)
                PdfiumNative.FPDF_ClosePage(page);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }

        return InspectOutput(destinationPath);
    }

    private static IntPtr CreateTextObject(
        IntPtr document,
        IntPtr font,
        string text,
        PdfObjectMatrix matrix)
    {
        var textObject = PdfiumNative.FPDFPageObj_CreateTextObj(document, font, 28f);
        if (textObject == IntPtr.Zero)
            throw new InvalidOperationException("PDFium could not create an F9 text overlay object.");

        try
        {
            if (PdfiumNative.FPDFText_SetText(textObject, text) == 0)
                throw new InvalidOperationException("PDFium could not assign Unicode text to an F9 overlay.");
            if (PdfiumNative.FPDFPageObj_SetFillColor(
                    textObject,
                    OverlayRed,
                    OverlayGreen,
                    OverlayBlue,
                    OverlayAlpha) == 0)
            {
                throw new InvalidOperationException("PDFium could not assign RGBA fill to an F9 text overlay.");
            }
            if (SetMatrix(textObject, matrix) == 0)
                throw new InvalidOperationException("PDFium could not assign the F9 text overlay matrix.");
            return textObject;
        }
        catch
        {
            PdfiumNative.FPDFPageObj_Destroy(textObject);
            throw;
        }
    }

    private static IntPtr CreateImageObject(
        IntPtr document,
        IntPtr page,
        PdfObjectMatrix matrix,
        uint argb)
    {
        var imageObject = PdfiumNative.FPDFPageObj_NewImageObj(document);
        if (imageObject == IntPtr.Zero)
            throw new InvalidOperationException("PDFium could not create an F9 image overlay object.");

        try
        {
            SetTinyBitmap(page, imageObject, argb);
            if (PdfiumNative.FPDFImageObj_SetMatrix(
                    imageObject,
                    matrix.A,
                    matrix.B,
                    matrix.C,
                    matrix.D,
                    matrix.E,
                    matrix.F) == 0)
            {
                throw new InvalidOperationException("PDFium could not assign the F9 image overlay matrix.");
            }
            if (PdfiumNative.FPDFPageObj_SetFillColor(imageObject, 255u, 255u, 255u, ImageAlpha) == 0)
                throw new InvalidOperationException("PDFium could not assign opacity to an F9 image overlay.");
            return imageObject;
        }
        catch
        {
            PdfiumNative.FPDFPageObj_Destroy(imageObject);
            throw;
        }
    }

    private static void SetTinyBitmap(IntPtr page, IntPtr imageObject, uint argb)
    {
        var bitmap = PdfiumNative.FPDFBitmap_Create(8, 8, 1);
        if (bitmap == IntPtr.Zero)
            throw new InvalidOperationException("PDFium could not create the F9 image-overlay bitmap.");

        IntPtr pages = IntPtr.Zero;
        try
        {
            if (PdfiumNative.FPDFBitmap_FillRect(bitmap, 0, 0, 8, 8, argb) == 0)
                throw new InvalidOperationException("PDFium could not fill the F9 image-overlay bitmap.");

            pages = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(pages, page);
            if (PdfiumNative.FPDFImageObj_SetBitmap(pages, 1, imageObject, bitmap) == 0)
                throw new InvalidOperationException("PDFium could not assign the F9 image-overlay bitmap.");
        }
        finally
        {
            if (pages != IntPtr.Zero)
                Marshal.FreeHGlobal(pages);
            PdfiumNative.FPDFBitmap_Destroy(bitmap);
        }
    }

    private static UtilityOverlayResult InspectOutput(string path)
    {
        var objects = new List<UtilityOverlayObjectSnapshot>();

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        IntPtr textPage = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(path, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not reopen the F9 overlay output.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not reopen the F9 overlay page.");

            textPage = PdfiumNative.FPDFText_LoadPage(page);
            if (textPage == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F9 output text page.");

            var count = PdfiumNative.FPDFPage_CountObjects(page);
            if (count < 0)
                throw new InvalidOperationException("PDFium returned an invalid F9 output object count.");

            for (var index = 0; index < count; index++)
            {
                var handle = PdfiumNative.FPDFPage_GetObject(page, index);
                if (handle == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium could not resolve F9 output object {index}.");

                var type = PdfiumNative.FPDFPageObj_GetType(handle);
                if (PdfiumNative.FPDFPageObj_GetMatrix(handle, out var nativeMatrix) == 0)
                    throw new InvalidOperationException("PDFium could not read an F9 output object matrix.");
                if (PdfiumNative.FPDFPageObj_GetFillColor(
                        handle,
                        out var red,
                        out var green,
                        out var blue,
                        out var alpha) == 0)
                {
                    throw new InvalidOperationException("PDFium could not read an F9 output object fill color.");
                }

                var text = type == TextObjectType ? ReadText(handle, textPage) : null;
                objects.Add(new UtilityOverlayObjectSnapshot(
                    index,
                    type,
                    text,
                    red,
                    green,
                    blue,
                    alpha,
                    new PdfObjectMatrix(
                        nativeMatrix.A,
                        nativeMatrix.B,
                        nativeMatrix.C,
                        nativeMatrix.D,
                        nativeMatrix.E,
                        nativeMatrix.F)));
            }
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

        using var session = PdfDocumentSession.Open(path);
        var rendered = session.RenderPage(0, 36d);
        return new UtilityOverlayResult(objects.AsReadOnly(), rendered.PixelWidth, rendered.PixelHeight);
    }

    private static string ReadText(IntPtr textObject, IntPtr textPage)
    {
        var required = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, IntPtr.Zero, 0);
        if (required == 0)
            throw new InvalidOperationException("PDFium returned no text for an F9 text object.");

        var buffer = Marshal.AllocHGlobal(checked((int)required * sizeof(ushort)));
        try
        {
            var copied = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, buffer, required);
            if (copied == 0 || copied > required)
                throw new InvalidOperationException("PDFium returned an invalid F9 text length.");

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

    private static PdfObjectMatrix RotationMatrix(double degrees, double x, double y)
    {
        var radians = degrees * Math.PI / 180d;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new PdfObjectMatrix(cos, sin, -sin, cos, x, y);
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
                throw context.Error ?? new InvalidOperationException("PDFium F9 characterization save failed.");
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

    private static void WriteMinimalHelveticaPdf(string path, string text)
    {
        if (text.Any(ch => ch is '(' or ')' or '\\' || ch > 0x7f))
            throw new ArgumentException("F9 minimal fixture accepts ASCII base text only.", nameof(text));

        var content = $"BT\n/F1 20 Tf\n0 0 0 rg\n1 0 0 1 92 150 Tm\n({text}) Tj\nET\n";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
        };

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
        {
            NewLine = "\n"
        };
        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            writer.Flush();
        }

        var xrefOffset = stream.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();
    }

    private sealed class NativeFileWriteContext
    {
        internal NativeFileWriteContext(FileStream stream) => Stream = stream;
        internal FileStream Stream { get; }
        internal Exception? Error { get; set; }
    }
}
