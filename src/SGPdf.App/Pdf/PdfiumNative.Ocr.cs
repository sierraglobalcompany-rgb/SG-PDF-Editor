using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

internal static partial class PdfiumNative
{
    internal const int FPDF_TEXTRENDERMODE_INVISIBLE = 3;

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFTextObj_SetTextRenderMode(IntPtr textObject, int renderMode);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern int FPDFText_CountChars(IntPtr textPage);
}
