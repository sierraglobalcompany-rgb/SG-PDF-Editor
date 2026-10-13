using PdfSharp.Pdf;
using SGPdf.App.Features.Forms;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormInspectorTests
{
    [Fact]
    public void InspectTextFields_FlatTextField_ReturnsNameAndValue()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sgpdf-form-inspector-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "flat-text-field.pdf");
            CreateFlatTextFieldPdf(path, "customer_name", "Ada Lovelace");

            using (var session = PdfDocumentSession.Open(path))
                Assert.NotEqual(0, session.GetOrganizeFormType());

            PdfAcroFormTextFieldInfo field = Assert.Single(PdfAcroFormInspector.InspectTextFields(path));
            Assert.Equal("customer_name", field.Name);
            Assert.Equal("Ada Lovelace", field.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void CreateFlatTextFieldPdf(string path, string name, string value)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();

        var acroForm = new PdfDictionary(document);
        var fields = new PdfArray(document);
        acroForm.Elements["/Fields"] = fields;
        document.Internals.AddObject(acroForm);
        document.Internals.Catalog.Elements["/AcroForm"] = acroForm.Reference;

        var field = new PdfDictionary(document);
        field.Elements.SetName("/FT", "/Tx");
        field.Elements.SetName("/Subtype", "/Widget");
        field.Elements.SetString("/T", name);
        field.Elements.SetString("/V", value);
        var rect = new PdfArray(document);
        rect.Elements.Add(new PdfInteger(40));
        rect.Elements.Add(new PdfInteger(180));
        rect.Elements.Add(new PdfInteger(360));
        rect.Elements.Add(new PdfInteger(220));
        field.Elements["/Rect"] = rect;
        document.Internals.AddObject(field);
        fields.Elements.Add(field.Reference!);

        var annots = new PdfArray(document);
        annots.Elements.Add(field.Reference!);
        page.Elements["/Annots"] = annots;

        document.Save(path);
    }
}
