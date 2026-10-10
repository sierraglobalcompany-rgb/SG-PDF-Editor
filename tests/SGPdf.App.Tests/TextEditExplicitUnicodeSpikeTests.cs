using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

/// <summary>
/// Throwaway/characterization spike for the F7 blocked font-fallback gate.
/// This is test-only evidence; it is not product code.
/// </summary>
public sealed class TextEditExplicitUnicodeSpikeTests
{
    [Fact]
    public void PinnedPdfium_CidType2WithExplicitMaps_ReopensExactSpanishUnicode()
    {
        var library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();
        try
        {
            Assert.True(
                NativeLibrary.TryGetExport(library, "FPDFText_LoadCidType2Font", out _),
                "El pdfium.dll pinneado no exporta FPDFText_LoadCidType2Font.");
        }
        finally
        {
            NativeLibrary.Free(library);
        }

        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var fontPath = TextEditNativeCharacterizationHarness.ResolveWindowsFixtureTtf();
        var output = fixture.NewOutputPath("cid-type2-explicit-unicode.pdf");
        const string expected = "NIÑO áé";

        ExplicitUnicodeCidType2Probe.ReplaceFirstTextAndSave(
            fixture.Path,
            output,
            fontPath,
            expected);

        var after = TextEditNativeCharacterizationHarness.InspectFirstText(output);
        Assert.Equal(expected, after.Text);

        using var session = PdfDocumentSession.Open(output);
        var rendered = session.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
        Assert.NotEmpty(rendered.Pixels);
    }

    private static class ExplicitUnicodeCidType2Probe
    {
        private const int TextObjectType = 1;
        private const string Library = "pdfium";

        internal static void ReplaceFirstTextAndSave(
            string sourcePath,
            string destinationPath,
            string fontPath,
            string text)
        {
            var fontBytes = File.ReadAllBytes(fontPath);
            if (fontBytes.Length == 0)
                throw new InvalidDataException("The spike TTF is empty.");

            var (toUnicodeCMap, cidToGidMap) = BuildExplicitMaps(fontPath, text);

            PdfiumRuntime.EnsureInitialized();
            PdfiumRuntime.NativeGate.Wait();
            IntPtr document = IntPtr.Zero;
            IntPtr page = IntPtr.Zero;
            IntPtr font = IntPtr.Zero;
            IntPtr replacement = IntPtr.Zero;
            IntPtr original = IntPtr.Zero;
            IntPtr fontBuffer = IntPtr.Zero;
            IntPtr cidToGidBuffer = IntPtr.Zero;
            var originalRemoved = false;
            var replacementInserted = false;
            try
            {
                document = PdfiumNative.FPDF_LoadDocument(sourcePath, null);
                if (document == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium could not open the explicit-Unicode spike source.");

                page = PdfiumNative.FPDF_LoadPage(document, 0);
                if (page == IntPtr.Zero)
                    throw new InvalidOperationException("PDFium could not load the explicit-Unicode spike page.");

                var (originalIndex, originalHandle) = FindFirstTextObject(page);
                original = originalHandle;

                if (PdfiumNative.FPDFPageObj_GetMatrix(original, out var originalMatrix) == 0)
                    throw new InvalidOperationException("Could not read the original text matrix.");
                if (FPDFTextObj_GetFontSize(original, out var fontSize) == 0 || fontSize <= 0)
                    throw new InvalidOperationException("Could not read the original text font size.");
                if (FPDFPageObj_GetFillColor(original, out var red, out var green, out var blue, out var alpha) == 0)
                    throw new InvalidOperationException("Could not read the original text fill color.");

                fontBuffer = Marshal.AllocHGlobal(fontBytes.Length);
                Marshal.Copy(fontBytes, 0, fontBuffer, fontBytes.Length);
                cidToGidBuffer = Marshal.AllocHGlobal(cidToGidMap.Length);
                Marshal.Copy(cidToGidMap, 0, cidToGidBuffer, cidToGidMap.Length);

                font = FPDFText_LoadCidType2Font(
                    document,
                    fontBuffer,
                    checked((uint)fontBytes.Length),
                    toUnicodeCMap,
                    cidToGidBuffer,
                    checked((uint)cidToGidMap.Length));
                if (font == IntPtr.Zero)
                    throw new InvalidOperationException("FPDFText_LoadCidType2Font rejected the explicit maps.");

                replacement = FPDFPageObj_CreateTextObj(document, font, fontSize);
                if (replacement == IntPtr.Zero)
                    throw new InvalidOperationException("FPDFPageObj_CreateTextObj failed with the CID Type2 font.");
                if (FPDFText_SetText(replacement, text) == 0)
                    throw new InvalidOperationException("FPDFText_SetText failed with the explicit ToUnicode font.");
                if (PdfiumNative.FPDFPageObj_SetFillColor(replacement, red, green, blue, alpha) == 0)
                    throw new InvalidOperationException("Could not copy fill color in the explicit-Unicode spike.");
                if (PdfiumNative.FPDFPageObj_SetMatrix(replacement, ref originalMatrix) == 0)
                    throw new InvalidOperationException("Could not copy matrix in the explicit-Unicode spike.");

                if (PdfiumNative.FPDFPage_RemoveObject(page, original) == 0)
                    throw new InvalidOperationException("Could not remove original text in the explicit-Unicode spike.");
                originalRemoved = true;

                if (PdfiumNative.FPDFPage_InsertObjectAtIndex(page, replacement, (nuint)originalIndex) == 0)
                    throw new InvalidOperationException("Could not insert explicit-Unicode text at the original index.");
                replacementInserted = true;

                PdfiumNative.FPDFPageObj_Destroy(original);
                original = IntPtr.Zero;

                if (PdfiumNative.FPDFPage_GenerateContent(page) == 0)
                    throw new InvalidOperationException("FPDFPage_GenerateContent failed in the explicit-Unicode spike.");

                SaveDocument(document, destinationPath);
            }
            finally
            {
                if (originalRemoved && original != IntPtr.Zero)
                    PdfiumNative.FPDFPageObj_Destroy(original);
                if (!replacementInserted && replacement != IntPtr.Zero)
                    PdfiumNative.FPDFPageObj_Destroy(replacement);
                if (font != IntPtr.Zero)
                    FPDFFont_Close(font);
                if (cidToGidBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(cidToGidBuffer);
                if (fontBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(fontBuffer);
                if (page != IntPtr.Zero)
                    PdfiumNative.FPDF_ClosePage(page);
                if (document != IntPtr.Zero)
                    PdfiumNative.FPDF_CloseDocument(document);
                PdfiumRuntime.NativeGate.Release();
            }
        }

        private static (string ToUnicodeCMap, byte[] CidToGidMap) BuildExplicitMaps(
            string fontPath,
            string text)
        {
            var uniqueCharacters = new string(text.Distinct().ToArray());
            using var typeface = SKTypeface.FromFile(fontPath)
                ?? throw new InvalidDataException("Skia could not load the spike TTF.");
            using var skFont = new SKFont(typeface);
            var glyphs = skFont.GetGlyphs(uniqueCharacters);

            Assert.Equal(uniqueCharacters.Length, glyphs.Length);
            Assert.DoesNotContain((ushort)0, glyphs);

            var cidToGidMap = new byte[(glyphs.Length + 1) * 2];
            var cmap = new StringBuilder();
            cmap.AppendLine("/CIDInit /ProcSet findresource begin");
            cmap.AppendLine("12 dict begin");
            cmap.AppendLine("begincmap");
            cmap.AppendLine("/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> def");
            cmap.AppendLine("/CMapName /Adobe-Identity-H def");
            cmap.AppendLine("/CMapType 2 def");
            cmap.AppendLine("1 begincodespacerange");
            cmap.AppendLine("<0000> <FFFF>");
            cmap.AppendLine("endcodespacerange");
            cmap.AppendLine($"{uniqueCharacters.Length} beginbfchar");

            for (var i = 0; i < uniqueCharacters.Length; i++)
            {
                var cid = i + 1;
                var glyphId = glyphs[i];
                cidToGidMap[cid * 2] = checked((byte)(glyphId >> 8));
                cidToGidMap[(cid * 2) + 1] = checked((byte)(glyphId & 0xFF));
                cmap.Append('<').Append(cid.ToString("X4")).Append("> <")
                    .Append(((int)uniqueCharacters[i]).ToString("X4")).AppendLine(">");
            }

            cmap.AppendLine("endbfchar");
            cmap.AppendLine("endcmap");
            cmap.AppendLine("CMapName currentdict /CMap defineresource pop");
            cmap.AppendLine("end");
            cmap.AppendLine("end");

            return (cmap.ToString(), cidToGidMap);
        }

        private static (int Index, IntPtr Handle) FindFirstTextObject(IntPtr page)
        {
            var count = PdfiumNative.FPDFPage_CountObjects(page);
            if (count < 0)
                throw new InvalidOperationException("PDFium returned an invalid page-object count.");

            for (var index = 0; index < count; index++)
            {
                var handle = PdfiumNative.FPDFPage_GetObject(page, index);
                if (handle != IntPtr.Zero && PdfiumNative.FPDFPageObj_GetType(handle) == TextObjectType)
                    return (index, handle);
            }

            throw new InvalidOperationException("The spike PDF contains no top-level text object.");
        }

        private static void SaveDocument(IntPtr document, string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            var context = new NativeFileWriteContext(stream);
            var contextHandle = GCHandle.Alloc(context);
            IntPtr nativeWrite = IntPtr.Zero;
            PdfiumNative.FileWriteBlock callback = WriteBlock;
            try
            {
                var fileWrite = new PdfiumNative.ExtendedFileWrite
                {
                    Version = 1,
                    WriteBlock = Marshal.GetFunctionPointerForDelegate(callback),
                    Context = GCHandle.ToIntPtr(contextHandle)
                };
                nativeWrite = Marshal.AllocHGlobal(Marshal.SizeOf<PdfiumNative.ExtendedFileWrite>());
                Marshal.StructureToPtr(fileWrite, nativeWrite, false);
                if (PdfiumNative.FPDF_SaveAsCopy(document, nativeWrite, 0u) == 0)
                    throw context.Error ?? new InvalidOperationException("PDFium explicit-Unicode spike save failed.");
                if (context.Error is not null)
                    throw context.Error;
                stream.Flush(flushToDisk: true);
            }
            finally
            {
                GC.KeepAlive(callback);
                if (nativeWrite != IntPtr.Zero)
                    Marshal.FreeHGlobal(nativeWrite);
                if (contextHandle.IsAllocated)
                    contextHandle.Free();
            }
        }

        private static int WriteBlock(IntPtr self, IntPtr data, uint size)
        {
            try
            {
                var fileWrite = Marshal.PtrToStructure<PdfiumNative.ExtendedFileWrite>(self);
                var handle = GCHandle.FromIntPtr(fileWrite.Context);
                if (handle.Target is not NativeFileWriteContext context || size > int.MaxValue)
                    return 0;
                var buffer = new byte[(int)size];
                Marshal.Copy(data, buffer, 0, buffer.Length);
                context.Stream.Write(buffer, 0, buffer.Length);
                return 1;
            }
            catch (Exception ex)
            {
                try
                {
                    var fileWrite = Marshal.PtrToStructure<PdfiumNative.ExtendedFileWrite>(self);
                    var handle = GCHandle.FromIntPtr(fileWrite.Context);
                    if (handle.Target is NativeFileWriteContext context)
                        context.Error = ex;
                }
                catch
                {
                }
                return 0;
            }
        }

        private sealed class NativeFileWriteContext
        {
            internal NativeFileWriteContext(FileStream stream) => Stream = stream;
            internal FileStream Stream { get; }
            internal Exception? Error { get; set; }
        }

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        private static extern int FPDFTextObj_GetFontSize(IntPtr textObject, out float size);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        private static extern int FPDFPageObj_GetFillColor(
            IntPtr pageObject,
            out uint red,
            out uint green,
            out uint blue,
            out uint alpha);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr FPDFText_LoadCidType2Font(
            IntPtr document,
            IntPtr fontData,
            uint fontDataSize,
            [MarshalAs(UnmanagedType.LPStr)] string toUnicodeCMap,
            IntPtr cidToGidMapData,
            uint cidToGidMapDataSize);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr FPDFPageObj_CreateTextObj(
            IntPtr document,
            IntPtr font,
            float fontSize);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        private static extern int FPDFText_SetText(
            IntPtr textObject,
            [MarshalAs(UnmanagedType.LPWStr)] string text);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        private static extern void FPDFFont_Close(IntPtr font);
    }
}
