using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Forms;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormSaveServiceHierarchyTests
{
    private const string FullFieldName = "customer.name";
    private const string OriginalValue = "Ada Lovelace";
    private const string UpdatedValue = "Grace Hopper";

    [Fact]
    public void SaveTextFields_HierarchicalTextField_PersistsValueAndAppearance()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-3-task3-hierarchy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string sourcePath = Path.Combine(directory, "source.pdf");
            string destinationPath = Path.Combine(directory, "destination.pdf");
            CreateHierarchicalTextFieldPdf(sourcePath);
            byte[] sourceBefore = File.ReadAllBytes(sourcePath);
            byte[] sourceAppearance = ReadChildNormalAppearance(sourcePath);
            Assert.Contains(OriginalValue, Encoding.ASCII.GetString(sourceAppearance));

            object service = CreateService();
            InvokeSave(
                service,
                sourcePath,
                destinationPath,
                new Dictionary<string, string> { [FullFieldName] = UpdatedValue });

            Assert.Equal(sourceBefore, File.ReadAllBytes(sourcePath));

            PdfAcroFormTextFieldInfo field = Assert.Single(
                PdfAcroFormInspector.InspectTextFields(destinationPath));
            Assert.Equal(FullFieldName, field.Name);
            Assert.Equal(UpdatedValue, field.Value);

            byte[] destinationAppearance = ReadChildNormalAppearance(destinationPath);
            Assert.NotEqual(
                Encoding.ASCII.GetString(sourceAppearance),
                Encoding.ASCII.GetString(destinationAppearance));
            Assert.Contains(UpdatedValue, Encoding.ASCII.GetString(destinationAppearance));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static object CreateService()
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Forms.PdfAcroFormSaveService",
            throwOnError: true)!;
        ConstructorInfo constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(Action<string, string>) },
            modifiers: null)!;
        return constructor.Invoke(new object?[] { null });
    }

    private static void InvokeSave(
        object service,
        string sourcePath,
        string destinationPath,
        IReadOnlyDictionary<string, string> values)
    {
        MethodInfo method = service.GetType().GetMethod(
            "Save",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[]
            {
                typeof(string),
                typeof(string),
                typeof(IReadOnlyDictionary<string, string>),
                typeof(CancellationToken)
            },
            modifiers: null)!;

        try
        {
            method.Invoke(
                service,
                new object[] { sourcePath, destinationPath, values, CancellationToken.None });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static byte[] ReadChildNormalAppearance(string path)
    {
        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        PdfDictionary? acroForm = document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
        PdfArray? fields = acroForm?.Elements.GetArray("/Fields");
        PdfDictionary? parent = fields?.Elements.GetDictionary(0);
        PdfArray? kids = parent?.Elements.GetArray("/Kids");
        PdfDictionary? child = kids?.Elements.GetDictionary(0);
        PdfDictionary? appearanceDictionary = child?.Elements.GetDictionary("/AP");
        PdfDictionary? normalAppearance = appearanceDictionary?.Elements.GetDictionary("/N");
        byte[]? bytes = normalAppearance?.Stream?.UnfilteredValue;
        return bytes is null
            ? throw new InvalidOperationException("El campo jerárquico no tiene un /AP/N legible.")
            : (byte[])bytes.Clone();
    }

    private static void CreateHierarchicalTextFieldPdf(string path)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();

        var acroForm = new PdfDictionary(document);
        var fields = new PdfArray(document);
        acroForm.Elements["/Fields"] = fields;
        acroForm.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
        AddHelveticaResources(acroForm, document);
        document.Internals.AddObject(acroForm);
        document.Internals.Catalog.Elements["/AcroForm"] = acroForm.Reference;

        var parent = new PdfDictionary(document);
        parent.Elements.SetName("/FT", "/Tx");
        parent.Elements.SetString("/T", "customer");
        parent.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
        var kids = new PdfArray(document);
        parent.Elements["/Kids"] = kids;
        document.Internals.AddObject(parent);
        fields.Elements.Add(parent.Reference!);

        var child = new PdfDictionary(document);
        child.Elements.SetName("/Subtype", "/Widget");
        child.Elements.SetString("/T", "name");
        child.Elements.SetString("/V", OriginalValue);
        child.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
        child.Elements["/Parent"] = parent.Reference;
        child.Elements["/Rect"] = CreateNumberArray(document, 40d, 180d, 360d, 220d);
        child.Elements["/AP"] = CreateAppearanceDictionary(document, OriginalValue);
        document.Internals.AddObject(child);
        kids.Elements.Add(child.Reference!);

        var annots = new PdfArray(document);
        annots.Elements.Add(child.Reference!);
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
            $"q 1 1 1 rg 0 0 320 40 re f BT /Helv 14 Tf 0 g 6 13 Td ({text}) Tj ET Q\n"));
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
}
