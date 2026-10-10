using System.Runtime.InteropServices;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal static class CommentNativeCharacterizationHarness
{
    internal const string NoteContents = "Nota NIÑO áé";

    internal static readonly PdfiumNative.QuadPointsF MarkupQuad = new()
    {
        X1 = 60f,
        Y1 = 330f,
        X2 = 180f,
        Y2 = 330f,
        X3 = 60f,
        Y3 = 310f,
        X4 = 180f,
        Y4 = 310f
    };

    internal static readonly PdfiumNative.PointF[] InkStroke =
    {
        new() { X = 70f, Y = 170f },
        new() { X = 105f, Y = 205f },
        new() { X = 145f, Y = 180f }
    };

    internal static NativeCommentFixture CreateFixture()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f8-native-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "source.pdf");
        var output = Path.Combine(directory, "annotations.pdf");

        using (var document = new PdfDocument())
        {
            document.AddPage();
            document.Save(source);
        }

        return new NativeCommentFixture(directory, source, output);
    }

    internal static void CreateSevenAnnotationsAndSave(string sourcePath, string outputPath)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException($"PDFium no pudo abrir fixture F8. Error {PdfiumNative.FPDF_GetLastError()}.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo cargar la página fixture F8.");

            CreateMarkup(page, PdfiumNative.FPDF_ANNOT_HIGHLIGHT, CreateRect(55f, 335f, 185f, 305f), 255, 220, 0, 160);
            CreateMarkup(page, PdfiumNative.FPDF_ANNOT_UNDERLINE, CreateRect(55f, 295f, 185f, 265f), 0, 95, 200, 210);
            CreateMarkup(page, PdfiumNative.FPDF_ANNOT_STRIKEOUT, CreateRect(55f, 255f, 185f, 225f), 210, 30, 30, 230);
            CreateNote(page);
            CreateInk(page);
            CreateShape(page, PdfiumNative.FPDF_ANNOT_SQUARE, CreateRect(220f, 340f, 320f, 260f), 20, 80, 210);
            CreateShape(page, PdfiumNative.FPDF_ANNOT_CIRCLE, CreateRect(220f, 230f, 320f, 150f), 20, 150, 70);

            PdfiumNative.FPDF_ClosePage(page);
            page = IntPtr.Zero;
            SaveDocument(document, outputPath);
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

    internal static IReadOnlyList<NativeCommentSnapshot> Inspect(string path)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(path, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException($"PDFium no pudo reabrir fixture F8. Error {PdfiumNative.FPDF_GetLastError()}.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo cargar la página reabierta F8.");

            var count = PdfiumNative.FPDFPage_GetAnnotCount(page);
            if (count < 0)
                throw new InvalidOperationException("PDFium devolvió un conteo de anotaciones inválido.");

            var snapshots = new List<NativeCommentSnapshot>(count);
            for (var index = 0; index < count; index++)
            {
                var annot = PdfiumNative.FPDFPage_GetAnnot(page, index);
                if (annot == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo obtener anotación {index}.");

                try
                {
                    snapshots.Add(ReadSnapshot(page, annot, index));
                }
                finally
                {
                    PdfiumNative.FPDFPage_CloseAnnot(annot);
                }
            }

            return snapshots;
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

    private static void CreateMarkup(
        IntPtr page,
        int subtype,
        PdfiumNative.RectF rect,
        uint red,
        uint green,
        uint blue,
        uint alpha)
    {
        var annot = CreateAnnotation(page, subtype);
        try
        {
            SetRect(annot, rect);
            SetColor(annot, red, green, blue, alpha);
            var quad = MarkupQuad;
            if (PdfiumNative.FPDFAnnot_AppendAttachmentPoints(annot, ref quad) == 0)
                throw new InvalidOperationException($"No se pudieron escribir quadpoints para subtipo {subtype}.");
        }
        finally
        {
            PdfiumNative.FPDFPage_CloseAnnot(annot);
        }
    }

    private static void CreateNote(IntPtr page)
    {
        var annot = CreateAnnotation(page, PdfiumNative.FPDF_ANNOT_TEXT);
        try
        {
            SetRect(annot, CreateRect(350f, 340f, 375f, 315f));
            SetColor(annot, 255, 210, 0, 255);
            if (PdfiumNative.FPDFAnnot_SetStringValue(annot, "Contents", NoteContents) == 0)
                throw new InvalidOperationException("No se pudo escribir Contents Unicode en nota F8.");
        }
        finally
        {
            PdfiumNative.FPDFPage_CloseAnnot(annot);
        }
    }

    private static void CreateInk(IntPtr page)
    {
        var annot = CreateAnnotation(page, PdfiumNative.FPDF_ANNOT_INK);
        try
        {
            SetRect(annot, CreateRect(60f, 215f, 155f, 160f));
            SetColor(annot, 190, 20, 160, 220);
            var stroke = InkStroke.ToArray();
            var pathIndex = PdfiumNative.FPDFAnnot_AddInkStroke(annot, stroke, (nuint)stroke.Length);
            if (pathIndex != 0)
                throw new InvalidOperationException($"FPDFAnnot_AddInkStroke devolvió índice inesperado {pathIndex}.");
        }
        finally
        {
            PdfiumNative.FPDFPage_CloseAnnot(annot);
        }
    }

    private static void CreateShape(
        IntPtr page,
        int subtype,
        PdfiumNative.RectF rect,
        uint red,
        uint green,
        uint blue)
    {
        var annot = CreateAnnotation(page, subtype);
        try
        {
            SetRect(annot, rect);
            SetColor(annot, red, green, blue, 255);
            if (PdfiumNative.FPDFAnnot_SetBorder(annot, 0f, 0f, 2.5f) == 0)
                throw new InvalidOperationException($"No se pudo escribir borde para subtipo {subtype}.");
        }
        finally
        {
            PdfiumNative.FPDFPage_CloseAnnot(annot);
        }
    }

    private static IntPtr CreateAnnotation(IntPtr page, int subtype)
    {
        if (PdfiumNative.FPDFAnnot_IsSupportedSubtype(subtype) == 0)
            throw new InvalidOperationException($"El runtime pinneado no soporta crear subtipo {subtype}.");

        var annot = PdfiumNative.FPDFPage_CreateAnnot(page, subtype);
        if (annot == IntPtr.Zero)
            throw new InvalidOperationException($"PDFium no pudo crear subtipo {subtype}.");
        if (PdfiumNative.FPDFPage_GetAnnotIndex(page, annot) < 0)
        {
            PdfiumNative.FPDFPage_CloseAnnot(annot);
            throw new InvalidOperationException($"PDFium creó subtipo {subtype} pero no pudo resolver su índice.");
        }
        return annot;
    }

    private static void SetRect(IntPtr annot, PdfiumNative.RectF rect)
    {
        if (PdfiumNative.FPDFAnnot_SetRect(annot, ref rect) == 0)
            throw new InvalidOperationException("No se pudo escribir rect de anotación F8.");
    }

    private static void SetColor(IntPtr annot, uint red, uint green, uint blue, uint alpha)
    {
        if (PdfiumNative.FPDFAnnot_SetColor(
                annot,
                PdfiumNative.FPDFANNOT_COLORTYPE_Color,
                red,
                green,
                blue,
                alpha) == 0)
        {
            throw new InvalidOperationException("No se pudo escribir color de anotación F8.");
        }
    }

    private static NativeCommentSnapshot ReadSnapshot(IntPtr page, IntPtr annot, int index)
    {
        var resolvedIndex = PdfiumNative.FPDFPage_GetAnnotIndex(page, annot);
        if (resolvedIndex != index)
            throw new InvalidOperationException($"Índice de anotación inestable: esperado {index}, obtenido {resolvedIndex}.");

        var subtype = PdfiumNative.FPDFAnnot_GetSubtype(annot);
        if (PdfiumNative.FPDFAnnot_GetRect(annot, out var rect) == 0)
            throw new InvalidOperationException($"No se pudo leer rect del subtipo {subtype}.");
        if (PdfiumNative.FPDFAnnot_GetColor(
                annot,
                PdfiumNative.FPDFANNOT_COLORTYPE_Color,
                out var red,
                out var green,
                out var blue,
                out var alpha) == 0)
        {
            throw new InvalidOperationException($"No se pudo leer color del subtipo {subtype}.");
        }

        var quads = ReadQuads(annot);
        var contents = subtype == PdfiumNative.FPDF_ANNOT_TEXT ? ReadString(annot, "Contents") : string.Empty;
        var inkPaths = subtype == PdfiumNative.FPDF_ANNOT_INK ? ReadInkPaths(annot) : Array.Empty<PdfiumNative.PointF[]>();
        NativeBorder? border = null;
        if (subtype is PdfiumNative.FPDF_ANNOT_SQUARE or PdfiumNative.FPDF_ANNOT_CIRCLE)
        {
            if (PdfiumNative.FPDFAnnot_GetBorder(annot, out var horizontal, out var vertical, out var width) == 0)
                throw new InvalidOperationException($"No se pudo leer borde del subtipo {subtype}.");
            border = new NativeBorder(horizontal, vertical, width);
        }

        return new NativeCommentSnapshot(
            index,
            subtype,
            rect,
            new NativeColor(red, green, blue, alpha),
            quads,
            contents,
            inkPaths,
            border);
    }

    private static PdfiumNative.QuadPointsF[] ReadQuads(IntPtr annot)
    {
        var count = PdfiumNative.FPDFAnnot_CountAttachmentPoints(annot);
        if (count > int.MaxValue)
            throw new InvalidOperationException("Demasiados quadpoints en caracterización F8.");

        var result = new PdfiumNative.QuadPointsF[(int)count];
        for (nuint index = 0; index < count; index++)
        {
            if (PdfiumNative.FPDFAnnot_GetAttachmentPoints(annot, index, out result[(int)index]) == 0)
                throw new InvalidOperationException($"No se pudo leer quadpoint {index}.");
        }
        return result;
    }

    private static PdfiumNative.PointF[][] ReadInkPaths(IntPtr annot)
    {
        var pathCount = PdfiumNative.FPDFAnnot_GetInkListCount(annot);
        var paths = new PdfiumNative.PointF[pathCount][];
        for (uint pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            var required = PdfiumNative.FPDFAnnot_GetInkListPath(annot, pathIndex, null!, 0);
            if (required == 0)
                throw new InvalidOperationException($"Ink path {pathIndex} no devolvió puntos.");
            var points = new PdfiumNative.PointF[required];
            var copied = PdfiumNative.FPDFAnnot_GetInkListPath(annot, pathIndex, points, required);
            if (copied != required)
                throw new InvalidOperationException($"Ink path {pathIndex} devolvió {copied}/{required} puntos.");
            paths[pathIndex] = points;
        }
        return paths;
    }

    private static string ReadString(IntPtr annot, string key)
    {
        var required = PdfiumNative.FPDFAnnot_GetStringValue(annot, key, IntPtr.Zero, 0);
        if (required == 0 || required > int.MaxValue)
            throw new InvalidOperationException($"No se pudo medir string '{key}' de anotación F8.");

        var buffer = Marshal.AllocHGlobal((int)required);
        try
        {
            var copied = PdfiumNative.FPDFAnnot_GetStringValue(annot, key, buffer, required);
            if (copied != required)
                throw new InvalidOperationException($"String '{key}' devolvió {copied}/{required} bytes.");
            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static PdfiumNative.RectF CreateRect(float left, float top, float right, float bottom)
        => new() { Left = left, Top = top, Right = right, Bottom = bottom };

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
                throw context.Error ?? new InvalidOperationException("PDFium F8 characterization save failed.");
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

internal sealed record NativeCommentSnapshot(
    int Index,
    int Subtype,
    PdfiumNative.RectF Rect,
    NativeColor Color,
    IReadOnlyList<PdfiumNative.QuadPointsF> Quads,
    string Contents,
    IReadOnlyList<PdfiumNative.PointF[]> InkPaths,
    NativeBorder? Border);

internal readonly record struct NativeColor(uint Red, uint Green, uint Blue, uint Alpha);
internal readonly record struct NativeBorder(float HorizontalRadius, float VerticalRadius, float Width);

internal sealed class NativeCommentFixture : IDisposable
{
    internal NativeCommentFixture(string directoryPath, string sourcePath, string outputPath)
    {
        DirectoryPath = directoryPath;
        SourcePath = sourcePath;
        OutputPath = outputPath;
    }

    internal string DirectoryPath { get; }
    internal string SourcePath { get; }
    internal string OutputPath { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}