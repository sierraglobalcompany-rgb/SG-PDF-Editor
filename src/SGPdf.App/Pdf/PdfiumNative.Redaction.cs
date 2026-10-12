using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

internal static partial class PdfiumNative
{
    internal const int FPDF_PAGEOBJ_PATH = 2;
    internal const int FPDF_FILLMODE_WINDING = 2;

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFPageObj_CreateNewRect(
        float x,
        float y,
        float w,
        float h);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFPath_SetDrawMode(
        IntPtr path,
        int fillMode,
        int stroke);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFPath_GetDrawMode(
        IntPtr path,
        out int fillMode,
        out int stroke);
}
