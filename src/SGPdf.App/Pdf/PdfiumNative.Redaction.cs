using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

internal static partial class PdfiumNative
{
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDFPageObj_CreateNewRect(
        float x,
        float y,
        float w,
        float h);
}
