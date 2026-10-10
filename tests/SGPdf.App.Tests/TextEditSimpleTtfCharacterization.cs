using System.Runtime.InteropServices;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal static class TextEditSimpleTtfCharacterization
{
    private const int TextObjectType = 1;
    private const int TrueTypeFont = 2;

    internal static void ReplaceFirstTextAndSave(
        string sourcePath,
        string destinationPath,
        string fontPath,
        string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var fontBytes = File.ReadAllBytes(fontPath);
        if (fontBytes.Length == 0)
            throw new InvalidDataException("The characterization TTF is empty.");

        ImageEditNativeCharacterizationHarness.MutateAndSave(
            sourcePath,
            destinationPath,
            context =>
            {
                var original = context.GetObjects().First(item => item.Type == TextObjectType);
                if (PdfiumNative.FPDFPageObj_GetMatrix(original.Handle, out var matrix) == 0)
                    throw new InvalidOperationException("Could not read the original text matrix.");
                if (NativeText.FPDFTextObj_GetFontSize(original.Handle, out var fontSize) == 0 || fontSize <= 0)
                    throw new InvalidOperationException("Could not read the original text font size.");
                if (NativeText.FPDFPageObj_GetFillColor(
                        original.Handle,
                        out var red,
                        out var green,
                        out var blue,
                        out var alpha) == 0)
                {
                    throw new InvalidOperationException("Could not read the original text fill color.");
                }

                IntPtr fontBuffer = IntPtr.Zero;
                IntPtr font = IntPtr.Zero;
                IntPtr replacement = IntPtr.Zero;
                var originalRemoved = false;
                var replacementInserted = false;
                try
                {
                    fontBuffer = Marshal.AllocHGlobal(fontBytes.Length);
                    Marshal.Copy(fontBytes, 0, fontBuffer, fontBytes.Length);
                    font = NativeText.FPDFText_LoadFont(
                        context.Document,
                        fontBuffer,
                        checked((uint)fontBytes.Length),
                        TrueTypeFont,
                        cid: 0);
                    if (font == IntPtr.Zero)
                        throw new InvalidOperationException("FPDFText_LoadFont rejected the simple TrueType fixture.");

                    replacement = NativeText.FPDFPageObj_CreateTextObj(context.Document, font, fontSize);
                    if (replacement == IntPtr.Zero)
                        throw new InvalidOperationException("FPDFPageObj_CreateTextObj failed with the simple TrueType font.");
                    if (NativeText.FPDFText_SetText(replacement, text) == 0)
                        throw new InvalidOperationException("FPDFText_SetText failed on the simple TrueType replacement.");
                    if (PdfiumNative.FPDFPageObj_SetFillColor(replacement, red, green, blue, alpha) == 0)
                        throw new InvalidOperationException("Could not copy fill color to the simple TrueType replacement.");
                    if (PdfiumNative.FPDFPageObj_SetMatrix(replacement, ref matrix) == 0)
                        throw new InvalidOperationException("Could not copy matrix to the simple TrueType replacement.");

                    if (PdfiumNative.FPDFPage_RemoveObject(context.Page, original.Handle) == 0)
                        throw new InvalidOperationException("Could not remove the original text object.");
                    originalRemoved = true;

                    if (PdfiumNative.FPDFPage_InsertObjectAtIndex(
                            context.Page,
                            replacement,
                            (nuint)original.Index) == 0)
                    {
                        throw new InvalidOperationException("Could not insert the simple TrueType replacement at the original index.");
                    }
                    replacementInserted = true;

                    PdfiumNative.FPDFPageObj_Destroy(original.Handle);
                    originalRemoved = false;
                }
                finally
                {
                    if (originalRemoved)
                        PdfiumNative.FPDFPageObj_Destroy(original.Handle);
                    if (!replacementInserted && replacement != IntPtr.Zero)
                        PdfiumNative.FPDFPageObj_Destroy(replacement);
                    if (font != IntPtr.Zero)
                        NativeText.FPDFFont_Close(font);
                    if (fontBuffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(fontBuffer);
                }
            });
    }

    private static class NativeText
    {
        private const string Library = "pdfium";

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FPDFTextObj_GetFontSize(IntPtr textObject, out float size);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FPDFPageObj_GetFillColor(
            IntPtr pageObject,
            out uint red,
            out uint green,
            out uint blue,
            out uint alpha);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr FPDFText_LoadFont(
            IntPtr document,
            IntPtr data,
            uint size,
            int fontType,
            int cid);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr FPDFPageObj_CreateTextObj(
            IntPtr document,
            IntPtr font,
            float fontSize);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FPDFText_SetText(
            IntPtr textObject,
            [MarshalAs(UnmanagedType.LPWStr)] string text);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern void FPDFFont_Close(IntPtr font);
    }
}
