using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using PdfSharp.Pdf;
using SGPdf.App.Features.Forms;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormSaveServiceTests
{
    private const string FieldName = "customer_name";
    private const string OriginalValue = "Ada Lovelace";
    private const string UpdatedValue = "Grace Hopper";

    [Fact]
    public void SaveTextFields_PublishesFromDestinationTempWithoutChangingSource_AndCleansTemp()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-3-task3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string sourcePath = Path.Combine(directory, "source.pdf");
            string destinationPath = Path.Combine(directory, "destination.pdf");
            CreateTextFieldPdf(sourcePath);
            byte[] sourceBefore = File.ReadAllBytes(sourcePath);
            string? observedTemp = null;

            object service = CreateService((observedSource, tempPath) =>
            {
                Assert.Equal(Path.GetFullPath(sourcePath), Path.GetFullPath(observedSource));
                Assert.True(File.Exists(tempPath));
                Assert.False(File.Exists(destinationPath));
                Assert.Equal(
                    Path.GetDirectoryName(Path.GetFullPath(destinationPath)),
                    Path.GetDirectoryName(Path.GetFullPath(tempPath)));
                observedTemp = tempPath;
            });

            InvokeSave(
                service,
                sourcePath,
                destinationPath,
                new Dictionary<string, string> { [FieldName] = UpdatedValue });

            Assert.Equal(sourceBefore, File.ReadAllBytes(sourcePath));
            Assert.True(File.Exists(destinationPath));
            Assert.NotNull(observedTemp);
            Assert.False(File.Exists(observedTemp));
            Assert.Empty(Directory.GetFiles(directory, ".sgpdf-form-*.tmp.pdf"));

            PdfAcroFormTextFieldInfo field = Assert.Single(
                PdfAcroFormInspector.InspectTextFields(destinationPath));
            Assert.Equal(FieldName, field.Name);
            Assert.Equal(UpdatedValue, field.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static object CreateService(Action<string, string>? beforeValidation)
    {
        Type? type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Forms.PdfAcroFormSaveService");
        Assert.NotNull(type);

        ConstructorInfo? constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(Action<string, string>) },
            modifiers: null);
        Assert.NotNull(constructor);
        return constructor.Invoke(new object?[] { beforeValidation });
    }

    private static void InvokeSave(
        object service,
        string sourcePath,
        string destinationPath,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        MethodInfo? method = service.GetType().GetMethod(
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
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            method.Invoke(
                service,
                new object[] { sourcePath, destinationPath, values, cancellationToken });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void CreateTextFieldPdf(string path)
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
