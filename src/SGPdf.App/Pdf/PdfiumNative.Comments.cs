using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

internal static partial class PdfiumNative
{
    internal const int FPDF_ANNOT_TEXT = 1;
    internal const int FPDF_ANNOT_SQUARE = 5;
    internal const int FPDF_ANNOT_CIRCLE = 6;
    internal const int FPDF_ANNOT_HIGHLIGHT = 9;
    internal const int FPDF_ANNOT_UNDERLINE = 10;
    internal const int FPDF_ANNOT_STRIKEOUT = 12;
    internal const int FPDF_ANNOT_INK = 15;

    internal const int FPDFANNOT_COLORTYPE_Color = 0;
    internal const int FPDFANNOT_COLORTYPE_InteriorColor = 1;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PointF
    {
        internal float X;
        internal float Y;
    }

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_IsSupportedSubtype(int subtype);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFPage_CreateAnnot(IntPtr page, int subtype);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFPage_GetAnnotCount(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFPage_GetAnnot(IntPtr page, int index);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFPage_GetAnnotIndex(IntPtr page, IntPtr annot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDFPage_CloseAnnot(IntPtr annot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFPage_RemoveAnnot(IntPtr page, int index);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_GetSubtype(IntPtr annot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_SetRect(IntPtr annot, ref RectF rect);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_GetRect(IntPtr annot, out RectF rect);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_SetColor(
        IntPtr annot,
        int colorType,
        uint red,
        uint green,
        uint blue,
        uint alpha);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_GetColor(
        IntPtr annot,
        int colorType,
        out uint red,
        out uint green,
        out uint blue,
        out uint alpha);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_AppendAttachmentPoints(
        IntPtr annot,
        ref QuadPointsF quadPoints);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern nuint FPDFAnnot_CountAttachmentPoints(IntPtr annot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_GetAttachmentPoints(
        IntPtr annot,
        nuint quadIndex,
        out QuadPointsF quadPoints);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_SetStringValue(
        IntPtr annot,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string key,
        [MarshalAs(UnmanagedType.LPWStr)] string value);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFAnnot_GetStringValue(
        IntPtr annot,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string key,
        IntPtr buffer,
        uint buflen);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_AddInkStroke(
        IntPtr annot,
        [In] PointF[] points,
        nuint pointCount);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFAnnot_GetInkListCount(IntPtr annot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFAnnot_GetInkListPath(
        IntPtr annot,
        uint pathIndex,
        [Out] PointF[] buffer,
        uint length);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_RemoveInkList(IntPtr annot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_SetBorder(
        IntPtr annot,
        float horizontalRadius,
        float verticalRadius,
        float borderWidth);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFAnnot_GetBorder(
        IntPtr annot,
        out float horizontalRadius,
        out float verticalRadius,
        out float borderWidth);
}