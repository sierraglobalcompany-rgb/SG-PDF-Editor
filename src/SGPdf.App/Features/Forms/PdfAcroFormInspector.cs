using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace SGPdf.App.Features.Forms;

internal static class PdfAcroFormInspector
{
    internal static IReadOnlyList<PdfAcroFormTextFieldInfo> InspectTextFields(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(pdfPath));

        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("No se encontró el archivo PDF.", pdfPath);

        using PdfDocument document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
        PdfDictionary? acroForm = document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
        PdfArray? fields = acroForm?.Elements.GetArray("/Fields");
        if (fields is null)
            return Array.Empty<PdfAcroFormTextFieldInfo>();

        var result = new List<PdfAcroFormTextFieldInfo>();
        for (var index = 0; index < fields.Elements.Count; index++)
        {
            PdfDictionary? field = fields.Elements.GetDictionary(index);
            if (field is not null)
                CollectTextFields(field, inheritedFieldType: null, parentName: null, result);
        }

        return result;
    }

    private static void CollectTextFields(
        PdfDictionary field,
        string? inheritedFieldType,
        string? parentName,
        List<PdfAcroFormTextFieldInfo> result)
    {
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
                    CollectTextFields(child, fieldType, fullName, result);
            }

            return;
        }

        if (fieldType != "/Tx" || string.IsNullOrWhiteSpace(fullName))
            return;

        result.Add(new PdfAcroFormTextFieldInfo(
            fullName,
            field.Elements.GetString("/V")));
    }
}
