using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

internal static class PdfiumNative
{
    private const string Library = "pdfium";
    internal const int FPDFBitmap_BGRA = 4;
    internal const uint PDFACTION_UNSUPPORTED = 0;
    internal const uint PDFACTION_GOTO = 1;
    internal const uint PDFACTION_REMOTEGOTO = 2;
    internal const uint PDFACTION_URI = 3;
    internal const uint PDFACTION_LAUNCH = 4;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int FileWriteBlock(IntPtr self, IntPtr data, uint size);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ExtendedFileWrite
    {
        internal int Version;
        internal IntPtr WriteBlock;
        internal IntPtr Context;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SizeF
    {
        internal float Width;
        internal float Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RectF
    {
        internal float Left;
        internal float Top;
        internal float Right;
        internal float Bottom;
    }

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDF_InitLibrary();

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDF_DestroyLibrary();

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDF_LoadDocument(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string filePath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? password);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDF_CloseDocument(IntPtr document);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDF_GetPageCount(IntPtr document);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDF_GetPageSizeByIndexF(IntPtr document, int pageIndex, out SizeF size);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDF_LoadPage(IntPtr document, int pageIndex);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDF_ClosePage(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern float FPDF_GetPageWidthF(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern float FPDF_GetPageHeightF(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFText_LoadPage(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDFText_ClosePage(IntPtr textPage);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFText_FindStart(
        IntPtr textPage,
        [MarshalAs(UnmanagedType.LPWStr)] string findWhat,
        uint flags,
        int startIndex);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_FindNext(IntPtr search);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_FindPrev(IntPtr search);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDFText_FindClose(IntPtr search);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_GetSchResultIndex(IntPtr search);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_GetSchCount(IntPtr search);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_GetCharIndexAtPos(
        IntPtr textPage,
        double x,
        double y,
        double xTolerance,
        double yTolerance);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_GetText(
        IntPtr textPage,
        int startIndex,
        int count,
        [Out] ushort[] result);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_CountRects(IntPtr textPage, int startIndex, int count);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_GetRect(
        IntPtr textPage,
        int rectIndex,
        out double left,
        out double top,
        out double right,
        out double bottom);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBookmark_GetFirstChild(IntPtr document, IntPtr bookmark);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBookmark_GetNextSibling(IntPtr document, IntPtr bookmark);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFBookmark_GetTitle(IntPtr bookmark, IntPtr buffer, uint buflen);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBookmark_GetDest(IntPtr document, IntPtr bookmark);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBookmark_GetAction(IntPtr bookmark);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFAction_GetType(IntPtr action);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFAction_GetDest(IntPtr document, IntPtr action);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFAction_GetURIPath(IntPtr document, IntPtr action, IntPtr buffer, uint buflen);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFDest_GetDestPageIndex(IntPtr document, IntPtr dest);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFLink_Enumerate(IntPtr page, ref int startPos, out IntPtr linkAnnot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFLink_GetAnnotRect(IntPtr linkAnnot, out RectF rect);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFLink_GetDest(IntPtr document, IntPtr linkAnnot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFLink_GetAction(IntPtr linkAnnot);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDF_GetSignatureCount(IntPtr document);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBitmap_CreateEx(
        int width,
        int height,
        int format,
        IntPtr firstScan,
        int stride);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFBitmap_FillRect(
        IntPtr bitmap,
        int left,
        int top,
        int width,
        int height,
        uint color);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFBitmap_GetStride(IntPtr bitmap);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDF_RenderPageBitmap(
        IntPtr bitmap,
        IntPtr page,
        int startX,
        int startY,
        int sizeX,
        int sizeY,
        int rotate,
        int flags);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDF_DeviceToPage(
        IntPtr page,
        int startX,
        int startY,
        int sizeX,
        int sizeY,
        int rotate,
        int deviceX,
        int deviceY,
        out double pageX,
        out double pageY);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFPageObj_NewImageObj(IntPtr document);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFImageObj_SetBitmap(
        IntPtr pages,
        int count,
        IntPtr imageObject,
        IntPtr bitmap);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFImageObj_SetMatrix(
        IntPtr imageObject,
        double a,
        double b,
        double c,
        double d,
        double e,
        double f);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFPage_InsertObject(IntPtr page, IntPtr pageObject);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDFPageObj_Destroy(IntPtr pageObject);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFPage_GenerateContent(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDF_SaveAsCopy(IntPtr document, IntPtr fileWrite, uint flags);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDF_GetLastError();
}
