using System.Runtime.InteropServices;
using System.Text;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    internal IReadOnlyList<PdfTextObjectInfo> GetTextObjects(
        int pageIndex,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para inspeccionar texto.");

            IntPtr textPage = IntPtr.Zero;
            try
            {
                textPage = PdfiumNative.FPDFText_LoadPage(page);
                if (textPage == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la capa de texto de la página {pageIndex + 1}.");

                var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
                if (objectCount < 0)
                    throw new InvalidOperationException("PDFium devolvió una cantidad inválida de objetos de página.");

                var objects = new List<PdfTextObjectInfo>();
                for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var pageObject = PdfiumNative.FPDFPage_GetObject(page, objectIndex);
                    if (pageObject == IntPtr.Zero)
                        throw new InvalidOperationException($"PDFium no pudo resolver el objeto {objectIndex} de la página {pageIndex + 1}.");

                    if (PdfiumNative.FPDFPageObj_GetType(pageObject) != PdfiumNative.FPDF_PAGEOBJ_TEXT)
                        continue;

                    var snapshot = TryReadTextObjectSnapshot(
                        pageIndex,
                        objectIndex,
                        pageObject,
                        textPage);
                    if (snapshot is not null)
                        objects.Add(snapshot);
                }

                cancellationToken.ThrowIfCancellationRequested();
                return objects.AsReadOnly();
            }
            finally
            {
                if (textPage != IntPtr.Zero)
                    PdfiumNative.FPDFText_ClosePage(textPage);
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static PdfTextObjectInfo? TryReadTextObjectSnapshot(
        int pageIndex,
        int objectIndex,
        IntPtr pageObject,
        IntPtr textPage)
    {
        var text = ReadTextObjectText(pageObject, textPage);
        if (string.IsNullOrEmpty(text))
            return null;

        if (PdfiumNative.FPDFPageObj_GetMatrix(pageObject, out var matrix) == 0 ||
            PdfiumNative.FPDFPageObj_GetBounds(
                pageObject,
                out var left,
                out var bottom,
                out var right,
                out var top) == 0 ||
            PdfiumNative.FPDFPageObj_GetRotatedBounds(pageObject, out var quad) == 0 ||
            PdfiumNative.FPDFTextObj_GetFontSize(pageObject, out var fontSize) == 0 ||
            !float.IsFinite(fontSize) ||
            fontSize <= 0f)
        {
            return null;
        }

        var font = PdfiumNative.FPDFTextObj_GetFont(pageObject);
        if (font == IntPtr.Zero)
            return null;

        var fontName = ReadBaseFontName(font);
        if (string.IsNullOrWhiteSpace(fontName))
            return null;

        if (PdfiumNative.FPDFPageObj_GetFillColor(
                pageObject,
                out var red,
                out var green,
                out var blue,
                out var alpha) == 0)
        {
            return null;
        }

        var renderMode = PdfiumNative.FPDFTextObj_GetTextRenderMode(pageObject);
        if (renderMode < 0)
            return null;

        var managedMatrix = new PdfObjectMatrix(
            matrix.A,
            matrix.B,
            matrix.C,
            matrix.D,
            matrix.E,
            matrix.F);
        var managedBounds = new PdfObjectBounds(left, bottom, right, top);
        var managedQuad = new PdfTextObjectQuad(
            quad.X1,
            quad.Y1,
            quad.X2,
            quad.Y2,
            quad.X3,
            quad.Y3,
            quad.X4,
            quad.Y4);

        if (!IsValidTextGeometry(managedMatrix, managedBounds, managedQuad))
            return null;

        return new PdfTextObjectInfo(
            new TextObjectKey(pageIndex, objectIndex),
            text,
            managedMatrix,
            managedBounds,
            managedQuad,
            fontName,
            fontSize,
            new PdfTextFillColor(red, green, blue, alpha),
            renderMode);
    }

    private static string ReadTextObjectText(IntPtr textObject, IntPtr textPage)
    {
        var required = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, IntPtr.Zero, 0);
        if (required == 0 || required > int.MaxValue)
            return string.Empty;

        var charCapacity = checked((int)required);
        var buffer = Marshal.AllocHGlobal(checked(charCapacity * sizeof(ushort)));
        try
        {
            var copied = PdfiumNative.FPDFTextObj_GetText(textObject, textPage, buffer, required);
            if (copied == 0 || copied > required)
                return string.Empty;

            var raw = new short[checked((int)copied)];
            Marshal.Copy(buffer, raw, 0, raw.Length);
            var length = raw.Length;
            while (length > 0 && raw[length - 1] == 0)
                length--;

            var chars = new char[length];
            for (var index = 0; index < length; index++)
                chars[index] = (char)(ushort)raw[index];

            return new string(chars);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string ReadBaseFontName(IntPtr font)
    {
        var required = PdfiumNative.FPDFFont_GetBaseFontName(font, IntPtr.Zero, 0);
        if (required == 0 || required > (nuint)int.MaxValue)
            return string.Empty;

        var buffer = Marshal.AllocHGlobal(checked((int)required));
        try
        {
            var copied = PdfiumNative.FPDFFont_GetBaseFontName(font, buffer, required);
            if (copied == 0 || copied > required || copied > (nuint)int.MaxValue)
                return string.Empty;

            var bytes = new byte[checked((int)copied)];
            Marshal.Copy(buffer, bytes, 0, bytes.Length);
            var length = bytes.Length;
            while (length > 0 && bytes[length - 1] == 0)
                length--;

            return Encoding.UTF8.GetString(bytes, 0, length);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool IsValidTextGeometry(
        PdfObjectMatrix matrix,
        PdfObjectBounds bounds,
        PdfTextObjectQuad quad)
    {
        return double.IsFinite(matrix.A) &&
               double.IsFinite(matrix.B) &&
               double.IsFinite(matrix.C) &&
               double.IsFinite(matrix.D) &&
               double.IsFinite(matrix.E) &&
               double.IsFinite(matrix.F) &&
               double.IsFinite(bounds.Left) &&
               double.IsFinite(bounds.Bottom) &&
               double.IsFinite(bounds.Right) &&
               double.IsFinite(bounds.Top) &&
               bounds.Right > bounds.Left &&
               bounds.Top > bounds.Bottom &&
               double.IsFinite(quad.X1) &&
               double.IsFinite(quad.Y1) &&
               double.IsFinite(quad.X2) &&
               double.IsFinite(quad.Y2) &&
               double.IsFinite(quad.X3) &&
               double.IsFinite(quad.Y3) &&
               double.IsFinite(quad.X4) &&
               double.IsFinite(quad.Y4);
    }
}
