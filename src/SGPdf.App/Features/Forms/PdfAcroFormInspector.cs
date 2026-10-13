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
            if (field is null || field.Elements.GetName("/FT") != "/Tx")
                continue;

            string name = field.Elements.GetString("/T");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            result.Add(new PdfAcroFormTextFieldInfo(
                name,
                field.Elements.GetString("/V")));
        }

        return result;
    }
}
