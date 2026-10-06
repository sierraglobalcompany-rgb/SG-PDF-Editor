using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

internal static class PdfiumNative
{
    private const string Library = "pdfium";

    internal const int FPDF_RENDER_READY = 0;
    internal const int FPDF_RENDER_TOBECONTINUED = 1;
    internal const int FPDF_RENDER_DONE = 2;
    internal const int FPDF_RENDER_FAILED = 3;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int NeedToPauseNowCallback(ref IfSdkPause pause);

    [StructLayout(LayoutKind.Sequential)]
    internal struct IfSdkPause
    {
        internal int Version;
        internal NeedToPauseNowCallback NeedToPauseNow;
        internal IntPtr User;
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
    internal static extern IntPtr FPDF_LoadPage(IntPtr document, int pageIndex);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDF_ClosePage(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern float FPDF_GetPageWidthF(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern float FPDF_GetPageHeightF(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

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
    internal static extern int FPDF_RenderPageBitmap_Start(
        IntPtr bitmap,
        IntPtr page,
        int startX,
        int startY,
        int sizeX,
        int sizeY,
        int rotate,
        int flags,
        ref IfSdkPause pause);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDF_RenderPage_Continue(
        IntPtr page,
        ref IfSdkPause pause);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern void FPDF_RenderPage_Close(IntPtr page);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDF_GetLastError();
}
