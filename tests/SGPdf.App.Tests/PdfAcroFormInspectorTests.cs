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

    [Fact]
    public void InspectTextFields_HierarchicalTextAndButton_ReturnsTerminalTextOnly()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sgpdf-form-inspector-hierarchy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "hierarchical-text-field.pdf");
            CreateHierarchicalTextAndUnsupportedButtonPdf(path);

            IReadOnlyList<PdfAcroFormTextFieldInfo> fields = PdfAcroFormInspector.InspectTextFields(path);

            Assert.Equal(2, fields.Count);
            Assert.Contains(fields, field => field.Name == "root_text" && field.Value == "Root value");
            Assert.Contains(fields, field => field.Name == "customer.name" && field.Value == "Grace Hopper");
            Assert.DoesNotContain(fields, field => field.Name == "customer");
            Assert.DoesNotContain(fields, field => field.Name == "accept_terms");
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

        var field = CreateField(document, "/Tx", name, value, 40, 180, 360, 220);
        fields.Elements.Add(field.Reference!);

        var annots = new PdfArray(document);
        annots.Elements.Add(field.Reference!);
        page.Elements["/Annots"] = annots;

        document.Save(path);
    }

    private static void CreateHierarchicalTextAndUnsupportedButtonPdf(string path)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();

        var acroForm = new PdfDictionary(document);
        var fields = new PdfArray(document);
        acroForm.Elements["/Fields"] = fields;
        document.Internals.AddObject(acroForm);
        document.Internals.Catalog.Elements["/AcroForm"] = acroForm.Reference;

        var rootText = CreateField(document, "/Tx", "root_text", "Root value", 40, 210, 360, 240);
        fields.Elements.Add(rootText.Reference!);

        var parent = new PdfDictionary(document);
        parent.Elements.SetName("/FT", "/Tx");
        parent.Elements.SetString("/T", "customer");
        var kids = new PdfArray(document);
        parent.Elements["/Kids"] = kids;
        document.Internals.AddObject(parent);
        fields.Elements.Add(parent.Reference!);

        var child = new PdfDictionary(document);
        child.Elements.SetName("/Subtype", "/Widget");
        child.Elements.SetString("/T", "name");
        child.Elements.SetString("/V", "Grace Hopper");
        child.Elements["/Parent"] = parent.Reference;
        child.Elements["/Rect"] = CreateRect(document, 40, 150, 360, 180);
        document.Internals.AddObject(child);
        kids.Elements.Add(child.Reference!);

        var button = CreateField(document, "/Btn", "accept_terms", string.Empty, 40, 90, 80, 120);
        button.Elements.SetName("/V", "/Yes");
        fields.Elements.Add(button.Reference!);

        var annots = new PdfArray(document);
        annots.Elements.Add(rootText.Reference!);
        annots.Elements.Add(child.Reference!);
        annots.Elements.Add(button.Reference!);
        page.Elements["/Annots"] = annots;

        document.Save(path);
    }

    private static PdfDictionary CreateField(
        PdfDocument document,
        string fieldType,
        string name,
        string value,
        int left,
        int bottom,
        int right,
        int top)
    {
        var field = new PdfDictionary(document);
        field.Elements.SetName("/FT", fieldType);
        field.Elements.SetName("/Subtype", "/Widget");
        field.Elements.SetString("/T", name);
        field.Elements.SetString("/V", value);
        field.Elements["/Rect"] = CreateRect(document, left, bottom, right, top);
        document.Internals.AddObject(field);
        return field;
    }

    private static PdfArray CreateRect(PdfDocument document, int left, int bottom, int right, int top)
    {
        var rect = new PdfArray(document);
        rect.Elements.Add(new PdfInteger(left));
        rect.Elements.Add(new PdfInteger(bottom));
        rect.Elements.Add(new PdfInteger(right));
        rect.Elements.Add(new PdfInteger(top));
        return rect;
    }
}
