using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Forms;

internal sealed class PdfAcroFormOutputValidator
{
    internal void Validate(
        string sourcePath,
        string outputPath,
        IReadOnlyDictionary<string, string> expectedValues,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta del PDF fuente es obligatoria.", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("La ruta del PDF de salida es obligatoria.", nameof(outputPath));
        ArgumentNullException.ThrowIfNull(expectedValues);
        if (expectedValues.Count == 0)
            throw new ArgumentException("Debe indicar al menos un campo para validar.", nameof(expectedValues));

        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<string, FieldSnapshot> source = ReadFieldSnapshots(
            sourcePath,
            expectedValues.Keys,
            cancellationToken);
        IReadOnlyDictionary<string, FieldSnapshot> output = ReadFieldSnapshots(
            outputPath,
            expectedValues.Keys,
            cancellationToken);

        var requiresVisualChange = false;

        foreach ((string name, string expectedValue) in expectedValues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!source.TryGetValue(name, out FieldSnapshot? sourceField))
                throw new InvalidDataException($"El PDF fuente no contiene el campo de texto esperado '{name}'.");
            if (!output.TryGetValue(name, out FieldSnapshot? outputField))
                throw new InvalidDataException($"El PDF de salida no contiene el campo de texto esperado '{name}'.");

            if (!string.Equals(outputField.Value, expectedValue, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"El PDF de salida no conservó el valor esperado para el campo '{name}'.");
            }

            if (!string.Equals(sourceField.RectSignature, outputField.RectSignature, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"El widget del campo '{name}' cambió de posición o tamaño durante el guardado.");
            }

            if (outputField.AppearanceHash is null)
            {
                throw new InvalidDataException(
                    $"El campo '{name}' no conserva una apariencia normal /AP/N utilizable.");
            }

            if (!string.Equals(sourceField.Value, expectedValue, StringComparison.Ordinal))
            {
                requiresVisualChange = true;

                if (sourceField.AppearanceHash is not null
                    && string.Equals(
                        sourceField.AppearanceHash,
                        outputField.AppearanceHash,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"El campo '{name}' cambió de valor sin actualizar su apariencia normal /AP/N.");
                }
            }
        }

        if (!requiresVisualChange)
            return;

        cancellationToken.ThrowIfCancellationRequested();
        string sourceRenderHash = RenderFormFillHash(sourcePath, cancellationToken);
        string outputRenderHash = RenderFormFillHash(outputPath, cancellationToken);
        if (string.Equals(sourceRenderHash, outputRenderHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "El formulario cambió de valor persistido, pero PDFium FormFill no detectó un cambio visual.");
        }
    }

    private static IReadOnlyDictionary<string, FieldSnapshot> ReadFieldSnapshots(
        string path,
        IEnumerable<string> requestedNames,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró el PDF que debe validarse.", path);

        var requested = new HashSet<string>(requestedNames, StringComparer.Ordinal);
        var result = new Dictionary<string, FieldSnapshot>(StringComparer.Ordinal);

        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        PdfDictionary? acroForm = document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
        PdfArray? fields = acroForm?.Elements.GetArray("/Fields");
        if (fields is null)
            return result;

        for (var index = 0; index < fields.Elements.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PdfDictionary? field = fields.Elements.GetDictionary(index);
            if (field is not null)
            {
                CollectFieldSnapshots(
                    field,
                    inheritedFieldType: null,
                    parentName: null,
                    requested,
                    result,
                    cancellationToken);
            }
        }

        return result;
    }

    private static void CollectFieldSnapshots(
        PdfDictionary field,
        string? inheritedFieldType,
        string? parentName,
        HashSet<string> requestedNames,
        Dictionary<string, FieldSnapshot> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string ownFieldType = field.Elements.GetName("/FT");
        string? fieldType = string.IsNullOrEmpty(ownFieldType)
            ? inheritedFieldType
            : ownFieldType;

        string ownName = field.Elements.GetString("/T");
        string? fullName = string.IsNullOrWhiteSpace(ownName)
            ? parentName
            : string.IsNullOrWhiteSpace(parentName)
                ? ownName
                : $"{parentName}.{ownName}";

        PdfArray? kids = field.Elements.GetArray("/Kids");
        if (kids is not null && kids.Elements.Count > 0)
        {
            for (var index = 0; index < kids.Elements.Count; index++)
            {
                PdfDictionary? child = kids.Elements.GetDictionary(index);
                if (child is not null)
                {
                    CollectFieldSnapshots(
                        child,
                        fieldType,
                        fullName,
                        requestedNames,
                        result,
                        cancellationToken);
                }
            }

            return;
        }

        if (fieldType != "/Tx"
            || string.IsNullOrWhiteSpace(fullName)
            || !requestedNames.Contains(fullName))
        {
            return;
        }

        var snapshot = new FieldSnapshot(
            field.Elements.GetString("/V"),
            ReadRectSignature(field, fullName),
            ReadNormalAppearanceHash(field));

        if (!result.TryAdd(fullName, snapshot))
            throw new InvalidDataException($"El PDF contiene el campo duplicado '{fullName}'.");
    }

    private static string ReadRectSignature(PdfDictionary field, string fieldName)
    {
        PdfArray? rect = field.Elements.GetArray("/Rect");
        if (rect is null || rect.Elements.Count != 4)
            throw new InvalidDataException($"El campo '{fieldName}' no contiene un /Rect válido.");

        return string.Join("|", rect.Elements.Select(item => item.ToString()));
    }

    private static string? ReadNormalAppearanceHash(PdfDictionary field)
    {
        PdfDictionary? appearanceDictionary = field.Elements.GetDictionary("/AP");
        PdfDictionary? normalAppearance = appearanceDictionary?.Elements.GetDictionary("/N");
        byte[]? bytes = normalAppearance?.Stream?.UnfilteredValue;
        if (bytes is null || bytes.Length == 0)
            return null;

        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string RenderFormFillHash(string path, CancellationToken cancellationToken)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);

        IntPtr document = IntPtr.Zero;
        IntPtr formHandle = IntPtr.Zero;
        IntPtr currentPage = IntPtr.Zero;
        var currentPageIndex = -1;

        SetTimerDelegate setTimer = static (_, _, _) => 1;
        KillTimerDelegate killTimer = static (_, _) => { };
        GetPageDelegate getPage = (_, _, pageIndex) =>
            pageIndex == currentPageIndex ? currentPage : IntPtr.Zero;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            document = PdfiumNative.FPDF_LoadDocument(path, null);
            if (document == IntPtr.Zero)
            {
                throw new InvalidDataException(
                    $"PDFium no pudo abrir el PDF para validar el formulario. Error {PdfiumNative.FPDF_GetLastError()}.");
            }

            var info = new FormFillInfo
            {
                Version = 1,
                FFI_SetTimer = Marshal.GetFunctionPointerForDelegate(setTimer),
                FFI_KillTimer = Marshal.GetFunctionPointerForDelegate(killTimer),
                FFI_GetPage = Marshal.GetFunctionPointerForDelegate(getPage)
            };

            formHandle = FormFillNative.FPDFDOC_InitFormFillEnvironment(document, ref info);
            if (formHandle == IntPtr.Zero)
                throw new InvalidDataException("PDFium no pudo inicializar FormFill para validar el formulario.");

            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            int pageCount = PdfiumNative.FPDF_GetPageCount(document);
            hash.AppendData(BitConverter.GetBytes(pageCount));

            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentPageIndex = pageIndex;
                currentPage = PdfiumNative.FPDF_LoadPage(document, pageIndex);
                if (currentPage == IntPtr.Zero)
                    throw new InvalidDataException($"PDFium no pudo cargar la página {pageIndex + 1} para validar FormFill.");

                IntPtr bitmap = IntPtr.Zero;
                var afterLoadCalled = false;
                try
                {
                    FormFillNative.FORM_OnAfterLoadPage(currentPage, formHandle);
                    afterLoadCalled = true;

                    int pixelWidth = Math.Max(
                        1,
                        checked((int)Math.Ceiling(PdfiumNative.FPDF_GetPageWidthF(currentPage))));
                    int pixelHeight = Math.Max(
                        1,
                        checked((int)Math.Ceiling(PdfiumNative.FPDF_GetPageHeightF(currentPage))));

                    bitmap = PdfiumNative.FPDFBitmap_Create(pixelWidth, pixelHeight, 1);
                    if (bitmap == IntPtr.Zero)
                        throw new InvalidDataException("PDFium no pudo crear el bitmap de validación FormFill.");

                    if (PdfiumNative.FPDFBitmap_FillRect(
                            bitmap,
                            0,
                            0,
                            pixelWidth,
                            pixelHeight,
                            0xFFFFFFFF) == 0)
                    {
                        throw new InvalidDataException("PDFium no pudo limpiar el bitmap de validación FormFill.");
                    }

                    PdfiumNative.FPDF_RenderPageBitmap(
                        bitmap,
                        currentPage,
                        0,
                        0,
                        pixelWidth,
                        pixelHeight,
                        0,
                        0);

                    FormFillNative.FPDF_FFLDraw(
                        formHandle,
                        bitmap,
                        currentPage,
                        0,
                        0,
                        pixelWidth,
                        pixelHeight,
                        0,
                        0);

                    int stride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
                    IntPtr buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
                    if (stride <= 0 || buffer == IntPtr.Zero)
                        throw new InvalidDataException("PDFium devolvió un bitmap FormFill inválido.");

                    var pixels = new byte[checked(stride * pixelHeight)];
                    Marshal.Copy(buffer, pixels, 0, pixels.Length);

                    hash.AppendData(BitConverter.GetBytes(pageIndex));
                    hash.AppendData(BitConverter.GetBytes(pixelWidth));
                    hash.AppendData(BitConverter.GetBytes(pixelHeight));
                    hash.AppendData(BitConverter.GetBytes(stride));
                    hash.AppendData(pixels);
                }
                finally
                {
                    if (afterLoadCalled)
                        FormFillNative.FORM_OnBeforeClosePage(currentPage, formHandle);
                    if (bitmap != IntPtr.Zero)
                        PdfiumNative.FPDFBitmap_Destroy(bitmap);
                    PdfiumNative.FPDF_ClosePage(currentPage);
                    currentPage = IntPtr.Zero;
                    currentPageIndex = -1;
                }
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }
        finally
        {
            if (currentPage != IntPtr.Zero)
            {
                if (formHandle != IntPtr.Zero)
                    FormFillNative.FORM_OnBeforeClosePage(currentPage, formHandle);
                PdfiumNative.FPDF_ClosePage(currentPage);
            }
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

    private sealed record FieldSnapshot(
        string Value,
        string RectSignature,
        string? AppearanceHash);

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
