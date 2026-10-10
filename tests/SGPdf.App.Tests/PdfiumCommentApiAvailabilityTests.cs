using System.Reflection;
using System.Runtime.InteropServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfiumCommentApiAvailabilityTests
{
    internal static readonly string[] CriticalExports =
    {
        "FPDFAnnot_IsSupportedSubtype",
        "FPDFPage_CreateAnnot",
        "FPDFPage_GetAnnotCount",
        "FPDFPage_GetAnnot",
        "FPDFPage_GetAnnotIndex",
        "FPDFPage_CloseAnnot",
        "FPDFPage_RemoveAnnot",
        "FPDFAnnot_GetSubtype",
        "FPDFAnnot_SetRect",
        "FPDFAnnot_GetRect",
        "FPDFAnnot_SetColor",
        "FPDFAnnot_GetColor",
        "FPDFAnnot_AppendAttachmentPoints",
        "FPDFAnnot_CountAttachmentPoints",
        "FPDFAnnot_GetAttachmentPoints",
        "FPDFAnnot_SetStringValue",
        "FPDFAnnot_GetStringValue",
        "FPDFAnnot_AddInkStroke",
        "FPDFAnnot_GetInkListCount",
        "FPDFAnnot_GetInkListPath",
        "FPDFAnnot_RemoveInkList",
        "FPDFAnnot_SetBorder",
        "FPDFAnnot_GetBorder",
        "FPDF_SaveAsCopy"
    };

    private static readonly (string Name, int Value)[] RequiredSubtypeConstants =
    {
        ("FPDF_ANNOT_TEXT", 1),
        ("FPDF_ANNOT_SQUARE", 5),
        ("FPDF_ANNOT_CIRCLE", 6),
        ("FPDF_ANNOT_HIGHLIGHT", 9),
        ("FPDF_ANNOT_UNDERLINE", 10),
        ("FPDF_ANNOT_STRIKEOUT", 12),
        ("FPDF_ANNOT_INK", 15)
    };

    [Fact]
    public void PinnedPdfium_ExportsAllF8CriticalCommentFunctions()
    {
        var library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();
        try
        {
            var missing = CriticalExports
                .Where(name => !NativeLibrary.TryGetExport(library, name, out _))
                .ToArray();

            Assert.True(
                missing.Length == 0,
                $"El pdfium.dll pinneado no exporta APIs core requeridas por F8: {string.Join(", ", missing)}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [Fact]
    public void ProductionInterop_DeclaresF8CriticalCommentFunctions()
    {
        var methods = typeof(PdfiumNative)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = CriticalExports
            .Where(name => name != "FPDF_SaveAsCopy" && !methods.Contains(name))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"PdfiumNative todavía no declara APIs F8 requeridas: {string.Join(", ", missing)}");
    }

    [Fact]
    public void ProductionInterop_DeclaresExactF8SubtypeConstants()
    {
        var fields = typeof(PdfiumNative)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .ToDictionary(field => field.Name, StringComparer.Ordinal);

        foreach (var (name, expected) in RequiredSubtypeConstants)
        {
            Assert.True(fields.TryGetValue(name, out var field), $"Falta constante F8: {name}");
            Assert.Equal(expected, (int)field!.GetRawConstantValue()!);
        }
    }
}