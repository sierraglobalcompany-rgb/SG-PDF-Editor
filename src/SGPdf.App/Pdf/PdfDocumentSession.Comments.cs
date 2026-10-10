using System.Runtime.InteropServices;
using SGPdf.App.Features.Comments;

namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    internal IReadOnlyList<PdfCommentInfo> GetCommentsOnPage(
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
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para consultar comentarios.");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = PdfiumNative.FPDFPage_GetAnnotCount(page);
                if (count < 0)
                    throw new InvalidOperationException($"PDFium devolvió un conteo de anotaciones inválido en la página {pageIndex + 1}.");
                if (count == 0)
                    return Array.Empty<PdfCommentInfo>();

                var comments = new List<PdfCommentInfo>(count);
                for (var annotationIndex = 0; annotationIndex < count; annotationIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var annotation = PdfiumNative.FPDFPage_GetAnnot(page, annotationIndex);
                    if (annotation == IntPtr.Zero)
                        continue;

                    try
                    {
                        comments.Add(ReadCommentSnapshot(pageIndex, annotationIndex, annotation));
                    }
                    finally
                    {
                        PdfiumNative.FPDFPage_CloseAnnot(annotation);
                    }
                }

                return comments;
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static PdfCommentInfo ReadCommentSnapshot(
        int pageIndex,
        int annotationIndex,
        IntPtr annotation)
    {
        var nativeSubtype = PdfiumNative.FPDFAnnot_GetSubtype(annotation);
        var subtype = MapSubtype(nativeSubtype);
        var rect = ReadRect(annotation);
        var color = ReadColor(annotation);
        var quads = subtype is CommentSubtype.Highlight or CommentSubtype.Underline or CommentSubtype.Strikeout
            ? ReadQuads(annotation)
            : Array.Empty<CommentQuad>();
        var contents = subtype == CommentSubtype.Text
            ? ReadString(annotation, "Contents")
            : string.Empty;
        var strokes = subtype == CommentSubtype.Ink
            ? ReadInkStrokes(annotation)
            : Array.Empty<CommentStroke>();
        var borderWidth = subtype is CommentSubtype.Square or CommentSubtype.Circle
            ? ReadBorderWidth(annotation)
            : null;

        return new PdfCommentInfo(
            pageIndex,
            annotationIndex,
            nativeSubtype,
            subtype,
            IsEditable: subtype.HasValue && rect.HasValue,
            rect,
            color,
            quads,
            contents,
            strokes,
            borderWidth);
    }

    private static CommentSubtype? MapSubtype(int subtype) => subtype switch
    {
        PdfiumNative.FPDF_ANNOT_TEXT => CommentSubtype.Text,
        PdfiumNative.FPDF_ANNOT_SQUARE => CommentSubtype.Square,
        PdfiumNative.FPDF_ANNOT_CIRCLE => CommentSubtype.Circle,
        PdfiumNative.FPDF_ANNOT_HIGHLIGHT => CommentSubtype.Highlight,
        PdfiumNative.FPDF_ANNOT_UNDERLINE => CommentSubtype.Underline,
        PdfiumNative.FPDF_ANNOT_STRIKEOUT => CommentSubtype.Strikeout,
        PdfiumNative.FPDF_ANNOT_INK => CommentSubtype.Ink,
        _ => null
    };

    private static CommentRect? ReadRect(IntPtr annotation)
    {
        if (PdfiumNative.FPDFAnnot_GetRect(annotation, out var rect) == 0)
            return null;

        var left = (double)rect.Left;
        var bottom = (double)rect.Bottom;
        var right = (double)rect.Right;
        var top = (double)rect.Top;
        if (!double.IsFinite(left) || !double.IsFinite(bottom) ||
            !double.IsFinite(right) || !double.IsFinite(top) ||
            right <= left || top <= bottom)
        {
            return null;
        }

        return new CommentRect(left, bottom, right, top);
    }

    private static CommentColor? ReadColor(IntPtr annotation)
    {
        if (PdfiumNative.FPDFAnnot_GetColor(
                annotation,
                PdfiumNative.FPDFANNOT_COLORTYPE_Color,
                out var red,
                out var green,
                out var blue,
                out var alpha) == 0)
        {
            return null;
        }

        if (red > byte.MaxValue || green > byte.MaxValue ||
            blue > byte.MaxValue || alpha > byte.MaxValue)
        {
            return null;
        }

        return new CommentColor((byte)red, (byte)green, (byte)blue, (byte)alpha);
    }

    private static IReadOnlyList<CommentQuad> ReadQuads(IntPtr annotation)
    {
        var count = PdfiumNative.FPDFAnnot_CountAttachmentPoints(annotation);
        if (count == 0)
            return Array.Empty<CommentQuad>();
        if (count > int.MaxValue)
            throw new InvalidOperationException("La anotación contiene demasiados quadpoints.");

        var quads = new List<CommentQuad>((int)count);
        for (nuint index = 0; index < count; index++)
        {
            if (PdfiumNative.FPDFAnnot_GetAttachmentPoints(annotation, index, out var quad) == 0)
                continue;

            if (!TryCreatePoint(quad.X1, quad.Y1, out var p1) ||
                !TryCreatePoint(quad.X2, quad.Y2, out var p2) ||
                !TryCreatePoint(quad.X3, quad.Y3, out var p3) ||
                !TryCreatePoint(quad.X4, quad.Y4, out var p4))
            {
                continue;
            }

            quads.Add(new CommentQuad(p1, p2, p3, p4));
        }

        return quads;
    }

    private static IReadOnlyList<CommentStroke> ReadInkStrokes(IntPtr annotation)
    {
        var pathCount = PdfiumNative.FPDFAnnot_GetInkListCount(annotation);
        if (pathCount == 0)
            return Array.Empty<CommentStroke>();

        var strokes = new List<CommentStroke>(checked((int)pathCount));
        for (uint pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            var required = PdfiumNative.FPDFAnnot_GetInkListPath(annotation, pathIndex, null!, 0);
            if (required < 2 || required > int.MaxValue)
                continue;

            var nativePoints = new PdfiumNative.PointF[(int)required];
            var copied = PdfiumNative.FPDFAnnot_GetInkListPath(annotation, pathIndex, nativePoints, required);
            if (copied != required)
                continue;

            var points = new List<CommentPoint>(nativePoints.Length);
            var valid = true;
            foreach (var nativePoint in nativePoints)
            {
                if (!TryCreatePoint(nativePoint.X, nativePoint.Y, out var point))
                {
                    valid = false;
                    break;
                }
                points.Add(point);
            }

            if (valid && points.Count >= 2)
                strokes.Add(new CommentStroke(points));
        }

        return strokes;
    }

    private static double? ReadBorderWidth(IntPtr annotation)
    {
        if (PdfiumNative.FPDFAnnot_GetBorder(
                annotation,
                out _,
                out _,
                out var borderWidth) == 0)
        {
            return null;
        }

        var width = (double)borderWidth;
        return double.IsFinite(width) && width >= 0d ? width : null;
    }

    private static string ReadString(IntPtr annotation, string key)
    {
        var required = PdfiumNative.FPDFAnnot_GetStringValue(annotation, key, IntPtr.Zero, 0);
        if (required == 0)
            return string.Empty;
        if (required > int.MaxValue)
            throw new InvalidOperationException($"El string '{key}' de la anotación es demasiado grande.");

        var buffer = Marshal.AllocHGlobal((int)required);
        try
        {
            var copied = PdfiumNative.FPDFAnnot_GetStringValue(annotation, key, buffer, required);
            if (copied != required)
                return string.Empty;
            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryCreatePoint(float x, float y, out CommentPoint point)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
        {
            point = default;
            return false;
        }

        point = new CommentPoint(x, y);
        return true;
    }
}
