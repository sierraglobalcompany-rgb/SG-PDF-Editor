using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using PdfSharp.Pdf;
using SGPdf.App.Features.Forms;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfSharpFontGateCollection.Name)]
public sealed class PdfAcroFormFormFillCapabilityTests
{
    private const string FieldName = "SGPDF_FormFillGate";
    private const string OriginalValue = "ORIGINAL";
    private const string UpdatedValue = "UPDATED BY SG PDF";

    private static readonly string[] RequiredExports =
    {
        "FPDFDOC_InitFormFillEnvironment",
        "FPDFDOC_ExitFormFillEnvironment",
        "FORM_OnAfterLoadPage",
        "FORM_OnBeforeClosePage",
        "FPDF_FFLDraw"
    };

    [Fact]
    public void PinnedPdfium_ExportsMinimalFormFillRenderFunctions()
    {
        IntPtr library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();
        try
        {
            string[] missing = RequiredExports
                .Where(name => !NativeLibrary.TryGetExport(library, name, out _))
                .ToArray();

            Assert.True(
                missing.Length == 0,
                $"El pdfium.dll pinneado no exporta el FormFill mínimo requerido por F12.3 Task 4.1: {string.Join(", ", missing)}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [Fact]
    public void MinimalFormFill_RendersPersistedTextWidgetThatOrdinaryRenderOmits()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-3-formfill-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string sourcePath = Path.Combine(directory, "source.pdf");
            string destinationPath = Path.Combine(directory, "output.pdf");
            CreateTextFieldFixture(sourcePath);

            PdfRenderedPage sourceOrdinary;
            using (var sourceSession = PdfDocumentSession.Open(sourcePath))
                sourceOrdinary = sourceSession.RenderPage(0, 72d);

            byte[] sourceFormFill = RenderWithMinimalFormFill(sourcePath);

            new PdfAcroFormSaveService(beforeValidation: null).Save(
                sourcePath,
                destinationPath,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [FieldName] = UpdatedValue
                });

            PdfRenderedPage destinationOrdinary;
            using (var destinationSession = PdfDocumentSession.Open(destinationPath))
                destinationOrdinary = destinationSession.RenderPage(0, 72d);

            byte[] destinationFormFill = RenderWithMinimalFormFill(destinationPath);

            Assert.Equal(sourceOrdinary.PixelWidth, destinationOrdinary.PixelWidth);
            Assert.Equal(sourceOrdinary.PixelHeight, destinationOrdinary.PixelHeight);
            Assert.Equal(sourceOrdinary.Pixels, destinationOrdinary.Pixels);

            Assert.NotEqual(Hash(sourceOrdinary.Pixels), Hash(sourceFormFill));
            Assert.NotEqual(Hash(destinationOrdinary.Pixels), Hash(destinationFormFill));
            Assert.NotEqual(Hash(sourceFormFill), Hash(destinationFormFill));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static byte[] RenderWithMinimalFormFill(string path)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();

        IntPtr document = IntPtr.Zero;
        IntPtr formHandle = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        IntPtr bitmap = IntPtr.Zero;

        SetTimerDelegate setTimer = static (_, _, _) => 1;
        KillTimerDelegate killTimer = static (_, _) => { };
        GetPageDelegate getPage = (_, _, pageIndex) => pageIndex == 0 ? page : IntPtr.Zero;

        try
        {
            document = PdfiumNative.FPDF_LoadDocument(path, null);
            if (document == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"PDFium no pudo abrir el fixture FormFill. Error {PdfiumNative.FPDF_GetLastError()}.");
            }

            var info = new FormFillInfo
            {
                Version = 1,
                Release = IntPtr.Zero,
                FFI_Invalidate = IntPtr.Zero,
                FFI_OutputSelectedRect = IntPtr.Zero,
                FFI_SetCursor = IntPtr.Zero,
                FFI_SetTimer = Marshal.GetFunctionPointerForDelegate(setTimer),
                FFI_KillTimer = Marshal.GetFunctionPointerForDelegate(killTimer),
                FFI_GetLocalTime = IntPtr.Zero,
                FFI_OnChange = IntPtr.Zero,
                FFI_GetPage = Marshal.GetFunctionPointerForDelegate(getPage),
                FFI_GetCurrentPage = IntPtr.Zero,
                FFI_GetRotation = IntPtr.Zero,
                FFI_ExecuteNamedAction = IntPtr.Zero,
                FFI_SetTextFieldFocus = IntPtr.Zero,
                FFI_DoURIAction = IntPtr.Zero,
                FFI_DoGoToAction = IntPtr.Zero,
                JsPlatform = IntPtr.Zero
            };

            formHandle = FormFillNative.FPDFDOC_InitFormFillEnvironment(document, ref info);
            if (formHandle == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo inicializar el FormFill mínimo.");

            page = PdfiumNative.FPDF_LoadPage(document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo cargar la página del fixture FormFill.");

            FormFillNative.FORM_OnAfterLoadPage(page, formHandle);

            int pixelWidth = Math.Max(1, checked((int)Math.Ceiling(PdfiumNative.FPDF_GetPageWidthF(page))));
            int pixelHeight = Math.Max(1, checked((int)Math.Ceiling(PdfiumNative.FPDF_GetPageHeightF(page))));
            bitmap = PdfiumNative.FPDFBitmap_Create(pixelWidth, pixelHeight, 1);
            if (bitmap == IntPtr.Zero)
                throw new InvalidOperationException("PDFium no pudo crear el bitmap del capability gate FormFill.");

            if (PdfiumNative.FPDFBitmap_FillRect(bitmap, 0, 0, pixelWidth, pixelHeight, 0xFFFFFFFF) == 0)
                throw new InvalidOperationException("PDFium no pudo limpiar el bitmap del capability gate FormFill.");

            PdfiumNative.FPDF_RenderPageBitmap(
                bitmap,
                page,
                0,
                0,
                pixelWidth,
                pixelHeight,
                0,
                0);

            FormFillNative.FPDF_FFLDraw(
                formHandle,
                bitmap,
                page,
                0,
                0,
                pixelWidth,
                pixelHeight,
                0,
                0);

            int stride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
            IntPtr buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
            if (stride <= 0 || buffer == IntPtr.Zero)
                throw new InvalidOperationException("PDFium devolvió un bitmap FormFill inválido.");

            var pixels = new byte[checked(stride * pixelHeight)];
            Marshal.Copy(buffer, pixels, 0, pixels.Length);
            return pixels;
        }
        finally
        {
            if (page != IntPtr.Zero && formHandle != IntPtr.Zero)
                FormFillNative.FORM_OnBeforeClosePage(page, formHandle);
            if (bitmap != IntPtr.Zero)
                PdfiumNative.FPDFBitmap_Destroy(bitmap);
            if (page != IntPtr.Zero)
                PdfiumNative.FPDF_ClosePage(page);
            if (formHandle != IntPtr.Zero)
                FormFillNative.FPDFDOC_ExitFormFillEnvironment(formHandle);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);

            GC.KeepAlive(setTimer);
            GC.KeepAlive(killTimer);
            GC.KeepAlive(getPage);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static string Hash(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes));

    private static void CreateTextFieldFixture(string path)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();
        page.Width = PdfSharp.Drawing.XUnit.FromPoint(400d);
        page.Height = PdfSharp.Drawing.XUnit.FromPoint(300d);

        var acroForm = new PdfDictionary(document);
        var fields = new PdfArray(document);
        acroForm.Elements["/Fields"] = fields;
        acroForm.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
        AddHelveticaResources(acroForm, document);
        document.Internals.AddObject(acroForm);
        document.Internals.Catalog.Elements["/AcroForm"] = acroForm.Reference;

        var field = new PdfDictionary(document);
        field.Elements.SetName("/FT", "/Tx");
        field.Elements.SetName("/Subtype", "/Widget");
        field.Elements.SetString("/T", FieldName);
        field.Elements.SetString("/V", OriginalValue);
        field.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
        field.Elements["/Rect"] = CreateNumberArray(document, 40d, 180d, 360d, 220d);
        field.Elements["/AP"] = CreateAppearanceDictionary(document, OriginalValue);
        document.Internals.AddObject(field);
        fields.Elements.Add(field.Reference!);

        var annots = new PdfArray(document);
        annots.Elements.Add(field.Reference!);
        page.Elements["/Annots"] = annots;

        document.Save(path);
    }

    private static PdfDictionary CreateAppearanceDictionary(PdfDocument document, string text)
    {
        var appearance = new PdfDictionary(document);
        appearance.Elements.SetName("/Type", "/XObject");
        appearance.Elements.SetName("/Subtype", "/Form");
        appearance.Elements.SetInteger("/FormType", 1);
        appearance.Elements["/BBox"] = CreateNumberArray(document, 0d, 0d, 320d, 40d);
        AddHelveticaResources(appearance, document);
        appearance.CreateStream(Encoding.ASCII.GetBytes(
            $"q 1 1 1 rg 0 0 320 40 re f 0 0 0 RG 1 w 0.5 0.5 319 39 re S BT /Helv 14 Tf 0 g 6 13 Td ({text}) Tj ET Q\n"));
        document.Internals.AddObject(appearance);

        var dictionary = new PdfDictionary(document);
        dictionary.Elements["/N"] = appearance.Reference;
        return dictionary;
    }

    private static void AddHelveticaResources(PdfDictionary owner, PdfDocument document)
    {
        var resources = new PdfDictionary(document);
        var fonts = new PdfDictionary(document);
        var font = new PdfDictionary(document);
        font.Elements.SetName("/Type", "/Font");
        font.Elements.SetName("/Subtype", "/Type1");
        font.Elements.SetName("/BaseFont", "/Helvetica");
        document.Internals.AddObject(font);
        fonts.Elements["/Helv"] = font.Reference;
        resources.Elements["/Font"] = fonts;
        owner.Elements["/Resources"] = resources;
        if (owner.Elements.ContainsKey("/Fields"))
            owner.Elements["/DR"] = resources;
    }

    private static PdfArray CreateNumberArray(PdfDocument document, params double[] values)
    {
        var array = new PdfArray(document);
        foreach (double value in values)
            array.Elements.Add(new PdfReal(value));
        return array;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetTimerDelegate(IntPtr formInfo, int elapsedMilliseconds, IntPtr timerCallback);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void KillTimerDelegate(IntPtr formInfo, int timerId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr GetPageDelegate(IntPtr formInfo, IntPtr document, int pageIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct FormFillInfo
    {
        internal int Version;
        internal IntPtr Release;
        internal IntPtr FFI_Invalidate;
        internal IntPtr FFI_OutputSelectedRect;
        internal IntPtr FFI_SetCursor;
        internal IntPtr FFI_SetTimer;
        internal IntPtr FFI_KillTimer;
        internal IntPtr FFI_GetLocalTime;
        internal IntPtr FFI_OnChange;
        internal IntPtr FFI_GetPage;
        internal IntPtr FFI_GetCurrentPage;
        internal IntPtr FFI_GetRotation;
        internal IntPtr FFI_ExecuteNamedAction;
        internal IntPtr FFI_SetTextFieldFocus;
        internal IntPtr FFI_DoURIAction;
        internal IntPtr FFI_DoGoToAction;
        internal IntPtr JsPlatform;
    }

    private static class FormFillNative
    {
        private const string Library = "pdfium";

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr FPDFDOC_InitFormFillEnvironment(
            IntPtr document,
            ref FormFillInfo formInfo);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern void FPDFDOC_ExitFormFillEnvironment(IntPtr formHandle);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern void FORM_OnAfterLoadPage(IntPtr page, IntPtr formHandle);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern void FORM_OnBeforeClosePage(IntPtr page, IntPtr formHandle);

        [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
        internal static extern void FPDF_FFLDraw(
            IntPtr formHandle,
            IntPtr bitmap,
            IntPtr page,
            int startX,
            int startY,
            int sizeX,
            int sizeY,
            int rotate,
            int flags);
    }
}
