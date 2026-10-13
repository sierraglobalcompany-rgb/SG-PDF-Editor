using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

internal static partial class PdfiumNative
{
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr FPDF_GetSignatureObject(IntPtr document, int index);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFSignatureObj_GetContents(
        IntPtr signature,
        IntPtr buffer,
        uint length);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFSignatureObj_GetByteRange(
        IntPtr signature,
        IntPtr buffer,
        uint length);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    internal static extern uint FPDFSignatureObj_GetSubFilter(
        IntPtr signature,
        IntPtr buffer,
        uint length);
}
