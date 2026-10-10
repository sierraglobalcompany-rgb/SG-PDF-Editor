using System.Runtime.InteropServices;
using System.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal sealed class TextEditPdfFixture : IDisposable
{
    internal TextEditPdfFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    internal string NewOutputPath(string fileName) => System.IO.Path.Combine(DirectoryPath, fileName);

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal readonly record struct NativeTextSnapshot(
    int PageObjectIndex,
    string Text,
    float FontSize,
    string FontName,
    uint Red,
    uint Green,
    uint Blue,
    uint Alpha,
    PdfObjectMatrix Matrix,
    int RenderMode);

internal static class TextEditNativeCharacterizationHarness
{
    private const int TextObjectType = 1;
    private const int TrueTypeFont = 2;

    internal static TextEditPdfFixture CreateSimpleTextPdf()
    {
        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sgpdf-f7-text-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "source.pdf");
        WriteMinimalHelveticaPdf(path, "CASA 123");
        return new TextEditPdfFixture(directory, path);
    }

    internal static NativeTextSnapshot InspectFirstText(string path)
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
                throw new InvalidOperationException("PDFium could not open the F7 text characterization source.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F7 characterization page.");

            textPage = PdfiumNative.FPDFText_LoadPage(page);
            if (textPage == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not create a text page for F7 characterization.");

            var (index, textObject) = FindFirstTextObject(page);
            return ReadSnapshot(index, textObject, textPage);
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

    internal static void SetFirstTextAndSave(string sourcePath, string destinationPath, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open the F7 in-place source.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F7 in-place page.");

            var (_, textObject) = FindFirstTextObject(page);
            if (NativeText.FPDFText_SetText(textObject, text) == 0)
                throw new InvalidOperationException("FPDFText_SetText failed in the pinned runtime.");

            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException("FPDFPage_GenerateContent failed after setting text.");

            SaveDocument(document, destinationPath);
        }
        finally
        {
            if (page != IntPtr.Zero)
                PdfiumNative.FPDF_ClosePage(page);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal static void ReplaceFirstTextWithTtfAndSave(
        string sourcePath,
        string destinationPath,
        string fontPath,
        string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var fontBytes = File.ReadAllBytes(fontPath);
        if (fontBytes.Length == 0)
            throw new InvalidDataException("The characterization TTF is empty.");

        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        IntPtr font = IntPtr.Zero;
        IntPtr replacement = IntPtr.Zero;
        IntPtr fontBuffer = IntPtr.Zero;
        var replacementInserted = false;
        var originalRemoved = false;
        IntPtr original = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open the F7 fallback source.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load the F7 fallback page.");

            var (originalIndex, originalHandle) = FindFirstTextObject(page);
            original = originalHandle;
            if (PdfiumNative.FPDFPageObj_GetMatrix(original, out var originalMatrix) == 0)
                throw new InvalidOperationException("Could not read the original text matrix.");
            if (NativeText.FPDFTextObj_GetFontSize(original, out var fontSize) == 0 || fontSize <= 0)
                throw new InvalidOperationException("Could not read the original text font size.");
            if (NativeText.FPDFPageObj_GetFillColor(original, out var red, out var green, out var blue, out var alpha) == 0)
                throw new InvalidOperationException("Could not read the original text fill color.");

            fontBuffer = Marshal.AllocHGlobal(fontBytes.Length);
            Marshal.Copy(fontBytes, 0, fontBuffer, fontBytes.Length);
            font = NativeText.FPDFText_LoadFont(
                document,
                fontBuffer,
                checked((uint)fontBytes.Length),
                TrueTypeFont,
                cid: 1);
            if (font == IntPtr.Zero)
                throw new InvalidOperationException("FPDFText_LoadFont rejected the Windows fixture TTF.");

            replacement = NativeText.FPDFPageObj_CreateTextObj(document, font, fontSize);
            if (replacement == IntPtr.Zero)
                throw new InvalidOperationException("FPDFPageObj_CreateTextObj failed with the loaded TTF.");
            if (NativeText.FPDFText_SetText(replacement, text) == 0)
                throw new InvalidOperationException("FPDFText_SetText failed on the replacement text object.");
            if (PdfiumNative.FPDFPageObj_SetFillColor(replacement, red, green, blue, alpha) == 0)
                throw new InvalidOperationException("Could not copy the fill color to the replacement text object.");
            if (PdfiumNative.FPDFPageObj_SetMatrix(replacement, ref originalMatrix) == 0)
                throw new InvalidOperationException("Could not copy the matrix to the replacement text object.");

            if (PdfiumNative.FPDFPage_RemoveObject(page, original) == 0)
                throw new InvalidOperationException("Could not remove the original text object.");
            originalRemoved = true;

            if (PdfiumNative.FPDFPage_InsertObjectAtIndex(page, replacement, (nuint)originalIndex) == 0)
                throw new InvalidOperationException("Could not insert replacement text at the original object index.");
            replacementInserted = true;

            PdfiumNative.FPDFPageObj_Destroy(original);
            original = IntPtr.Zero;

            if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                throw new InvalidOperationException("FPDFPage_GenerateContent failed after TTF replacement.");

            SaveDocument(document, destinationPath);
        }
        finally
        {
            if (originalRemoved && original != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(original);
            if (!replacementInserted && replacement != IntPtr.Zero)
                PdfiumNative.FPDFPageObj_Destroy(replacement);
            if (font != IntPtr.Zero)
                NativeText.FPDFFont_Close(font);
            if (fontBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(fontBuffer);
            if (page != IntPtr.Zero)
                PdfiumNative.FPDF_ClosePage(page);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal static string ResolveWindowsFixtureTtf()
    {
        var fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var candidates = new[]
        {
            System.IO.Path.Combine(fonts, "arial.ttf"),
            System.IO.Path.Combine(fonts, "segoeui.ttf"),
            System.IO.Path.Combine(fonts, "tahoma.ttf")
        };
        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("No Windows TTF fixture was available for the F7 capability gate.");
    }

    private static (int Index, IntPtr Handle) FindFirstTextObject(IntPtr page)
    {
        var count = PdfiumNative.FPDFPage_CountObjects(page);
        if (count < 0)
            throw new InvalidOperationException("PDFium returned an invalid page-object count.");

        for (var index = 0; index < count; index++)
        {
            var handle = PdfiumNative.FPDFPage_GetObject(page, index);
            if (handle != IntPtr.Zero && PdfiumNative.FPDFPageObj_GetType(handle) == TextObjectType)
                return (index, handle);
        }

        throw new InvalidOperationException("The characterization PDF contains no top-level text object.");
    }

    private static NativeTextSnapshot ReadSnapshot(int index, IntPtr textObject, IntPtr textPage)
    {
        var text = ReadText(textObject, textPage);
        if (NativeText.FPDFTextObj_GetFontSize(textObject, out var fontSize) == 0)
            throw new InvalidOperationException("Could not read text font size.");
        var font = NativeText.FPDFTextObj_GetFont(textObject);
        if (font == IntPtr.Zero)
            throw new InvalidOperationException("Could not resolve the text font.");
        var fontName = ReadBaseFontName(font);
        if (NativeText.FPDFPageObj_GetFillColor(textObject, out var red, out var green, out var blue, out var alpha) == 0)
            throw new InvalidOperationException("Could not read text fill color.");
        if (PdfiumNative.FPDFPageObj_GetMatrix(textObject, out var matrix) == 0)
            throw new InvalidOperationException("Could not read text matrix.");
        var renderMode = NativeText.FPDFTextObj_GetTextRenderMode(textObject);
        if (renderMode < 0)
            throw new InvalidOperationException("Could not read text render mode.");

        return new NativeTextSnapshot(
            index,
            text,
            fontSize,
            fontName,
            red,
            green,
            blue,
            alpha,
            new PdfObjectMatrix(matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F),
            renderMode);
    }

    private static string ReadText(IntPtr textObject, IntPtr textPage)
    {
        var required = NativeText.FPDFTextObj_GetText(textObject, textPage, IntPtr.Zero, 0);
        if (required == 0)
            throw new InvalidOperationException("FPDFTextObj_GetText returned no text.");

        var bytes = checked((int)required * sizeof(ushort));
        var buffer = Marshal.AllocHGlobal(bytes);
        try
        {
            var copied = NativeText.FPDFTextObj_GetText(textObject, textPage, buffer, required);
            if (copied == 0 || copied > required)
                throw new InvalidOperationException("FPDFTextObj_GetText returned an invalid length.");
            var raw = new short[copied];
            Marshal.Copy(buffer, raw, 0, checked((int)copied));
            var chars = raw
                .TakeWhile(value => value != 0)
                .Select(value => (char)(ushort)value)
                .ToArray();
            return new string(chars);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string ReadBaseFontName(IntPtr font)
    {
        var required = NativeText.FPDFFont_GetBaseFontName(font, IntPtr.Zero, 0);
        if (required == 0 || required > int.MaxValue)
            throw new InvalidOperationException("FPDFFont_GetBaseFontName returned an invalid length.");

        var buffer = Marshal.AllocHGlobal(checked((int)required));
        try
        {
            var copied = NativeText.FPDFFont_GetBaseFontName(font, buffer, required);
            if (copied == 0 || copied > required)
                throw new InvalidOperationException("FPDFFont_GetBaseFontName failed.");
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

    private static void SaveDocument(IntPtr document, string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
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
                throw context.Error ?? new InvalidOperationException("PDFium F7 characterization save failed.");
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
            throw new ArgumentException("Minimal characterization fixture accepts plain ASCII text only.", nameof(text));

        var content = $"BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 72 300 Tm\n({text}) Tj\nET\n";
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

    private static class NativeText
    {
        private const string Library = "pdfium";

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern uint FPDFTextObj_GetText(
            IntPtr textObject,
            IntPtr textPage,
            IntPtr buffer,
            uint length);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FPDFTextObj_GetFontSize(IntPtr textObject, out float size);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr FPDFTextObj_GetFont(IntPtr textObject);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern nuint FPDFFont_GetBaseFontName(IntPtr font, IntPtr buffer, nuint length);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FPDFTextObj_GetTextRenderMode(IntPtr textObject);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FPDFPageObj_GetFillColor(
            IntPtr pageObject,
            out uint red,
            out uint green,
            out uint blue,
            out uint alpha);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FPDFText_SetText(
            IntPtr textObject,
            [MarshalAs(UnmanagedType.LPWStr)] string text);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr FPDFText_LoadFont(
            IntPtr document,
            IntPtr data,
            uint size,
            int fontType,
            int cid);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern void FPDFFont_Close(IntPtr font);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr FPDFPageObj_CreateTextObj(
            IntPtr document,
            IntPtr font,
            float fontSize);
    }
}
