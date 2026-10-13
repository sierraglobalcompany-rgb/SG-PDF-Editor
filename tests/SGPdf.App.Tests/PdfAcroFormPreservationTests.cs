using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Forms;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormPreservationTests
{
    private const string TargetField = "customer_name";
    private const string TargetOriginal = "Ada Lovelace";
    private const string TargetUpdated = "Grace Hopper";
    private const string UntouchedField = "reference_code";
    private const string UntouchedValue = "KEEP-2048";
    private const string NonTargetText = "SGPDF PRESERVE TEXT 4.4";
    private const string BookmarkTitle = "Preserve destination";
    private const string Uri = "https://example.com/sgpdf-preserve";
    private const string MetadataTitle = "SG PDF F12.3 preservation fixture";
    private const string MetadataAuthor = "Sierra Global";
    private static readonly byte[] ImageBytes = { 0x12, 0x34, 0x56 };

    [Fact]
    public void SaveTextFields_PreservesRepresentativeNonTargetContentAndLeavesSourceByteExact()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-3-task4-4-preservation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string sourcePath = Path.Combine(directory, "source.pdf");
            string destinationPath = Path.Combine(directory, "destination.pdf");
            CreateRepresentativePdf(sourcePath);

            byte[] sourceBefore = File.ReadAllBytes(sourcePath);
            string sourceHash = Convert.ToHexString(SHA256.HashData(sourceBefore));
            RepresentativeSnapshot before = ReadRepresentativeSnapshot(sourcePath);

            object service = CreateService();
            InvokeSave(
                service,
                sourcePath,
                destinationPath,
                new Dictionary<string, string> { [TargetField] = TargetUpdated });

            Assert.True(File.Exists(destinationPath));
            Assert.Equal(sourceBefore, File.ReadAllBytes(sourcePath));
            Assert.Equal(sourceHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))));

            RepresentativeSnapshot after = ReadRepresentativeSnapshot(destinationPath);

            Assert.Equal(before.PageCount, after.PageCount);
            Assert.Equal(2, after.PageCount);
            Assert.Equal(before.NonTargetTextStream, after.NonTargetTextStream);
            Assert.Contains(NonTargetText, after.NonTargetTextStream, StringComparison.Ordinal);
            Assert.Equal(before.ImageHash, after.ImageHash);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(ImageBytes)), after.ImageHash);
            Assert.Equal(before.BookmarkTitle, after.BookmarkTitle);
            Assert.Equal(BookmarkTitle, after.BookmarkTitle);
            Assert.Equal(before.BookmarkDestinationPageObject, after.BookmarkDestinationPageObject);
            Assert.Equal(after.SecondPageObject, after.BookmarkDestinationPageObject);
            Assert.Equal(before.Uri, after.Uri);
            Assert.Equal(Uri, after.Uri);
            Assert.Equal(before.UntouchedFieldValue, after.UntouchedFieldValue);
            Assert.Equal(UntouchedValue, after.UntouchedFieldValue);
            Assert.Equal(before.MetadataTitle, after.MetadataTitle);
            Assert.Equal(MetadataTitle, after.MetadataTitle);
            Assert.Equal(before.MetadataAuthor, after.MetadataAuthor);
            Assert.Equal(MetadataAuthor, after.MetadataAuthor);
            Assert.Equal(TargetOriginal, before.TargetFieldValue);
            Assert.Equal(TargetUpdated, after.TargetFieldValue);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static RepresentativeSnapshot ReadRepresentativeSnapshot(string path)
    {
        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        Assert.Equal(2, document.PageCount);

        PdfPage firstPage = document.Pages[0];
        PdfPage secondPage = document.Pages[1];

        string content = ReadPageContent(firstPage);
        string imageHash = ReadImageHash(firstPage, "/ImKeep");
        string uri = ReadUri(firstPage);

        Assert.Single(document.Outlines);
        PdfOutline outline = document.Outlines[0];
        Assert.NotNull(outline.DestinationPage);

        IReadOnlyDictionary<string, string> fields = PdfAcroFormInspector
            .InspectTextFields(path)
            .ToDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal);

        return new RepresentativeSnapshot(
            document.PageCount,
            content,
            imageHash,
            outline.Title,
            outline.DestinationPage.ReferenceNotNull.ObjectID.ObjectNumber,
            secondPage.ReferenceNotNull.ObjectID.ObjectNumber,
            uri,
            fields[TargetField],
            fields[UntouchedField],
            document.Info.Title,
            document.Info.Author);
    }

    private static string ReadPageContent(PdfPage page)
    {
        PdfDictionary? contents = page.Elements.GetDictionary("/Contents");
        byte[]? bytes = contents?.Stream?.UnfilteredValue;
        if (bytes is null)
            throw new InvalidOperationException("El fixture perdió el stream /Contents de la página 1.");

        return Encoding.ASCII.GetString(bytes);
    }

    private static string ReadImageHash(PdfPage page, string resourceName)
    {
        PdfDictionary? resources = page.Elements.GetDictionary("/Resources");
        PdfDictionary? xObjects = resources?.Elements.GetDictionary("/XObject");
        PdfDictionary? image = xObjects?.Elements.GetDictionary(resourceName);
        if (image?.Elements.GetName("/Subtype") != "/Image")
            throw new InvalidOperationException($"El fixture perdió el XObject de imagen '{resourceName}'.");

        byte[]? bytes = image.Stream?.UnfilteredValue;
        if (bytes is null)
            throw new InvalidOperationException($"El XObject de imagen '{resourceName}' no tiene stream legible.");

        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string ReadUri(PdfPage page)
    {
        PdfArray? annots = page.Elements.GetArray("/Annots");
        if (annots is null)
            throw new InvalidOperationException("El fixture perdió /Annots.");

        for (var index = 0; index < annots.Elements.Count; index++)
        {
            PdfDictionary? annotation = annots.Elements.GetDictionary(index);
            if (annotation?.Elements.GetName("/Subtype") != "/Link")
                continue;

            PdfDictionary? action = annotation.Elements.GetDictionary("/A");
            if (action?.Elements.GetName("/S") == "/URI")
                return action.Elements.GetString("/URI");
        }

        throw new InvalidOperationException("El fixture perdió la anotación URI representativa.");
    }

    private static object CreateService()
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
        return constructor.Invoke(new object?[] { null });
    }

    private static void InvokeSave(
        object service,
        string sourcePath,
        string destinationPath,
        IReadOnlyDictionary<string, string> values)
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
                new object[] { sourcePath, destinationPath, values, CancellationToken.None });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void CreateRepresentativePdf(string path)
    {
        using var document = new PdfDocument();
        document.Info.Title = MetadataTitle;
        document.Info.Author = MetadataAuthor;

        PdfPage firstPage = document.AddPage();
        PdfPage secondPage = document.AddPage();

        AddPageContentAndImage(firstPage, document);

        var acroForm = new PdfDictionary(document);
        var fields = new PdfArray(document);
        acroForm.Elements["/Fields"] = fields;
        acroForm.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
        AddHelveticaResources(acroForm, document);
        document.Internals.AddObject(acroForm);
        document.Internals.Catalog.Elements["/AcroForm"] = acroForm.Reference;

        PdfDictionary target = CreateTextField(
            document,
            TargetField,
            TargetOriginal,
            40d,
            180d,
            360d,
            220d);
        PdfDictionary untouched = CreateTextField(
            document,
            UntouchedField,
            UntouchedValue,
            40d,
            480d,
            360d,
            520d);
        fields.Elements.Add(target.ReferenceNotNull);
        fields.Elements.Add(untouched.ReferenceNotNull);

        PdfDictionary uriAction = new(document);
        uriAction.Elements.SetName("/S", "/URI");
        uriAction.Elements.SetString("/URI", Uri);
        document.Internals.AddObject(uriAction);

        PdfDictionary uriAnnotation = new(document);
        uriAnnotation.Elements.SetName("/Type", "/Annot");
        uriAnnotation.Elements.SetName("/Subtype", "/Link");
        uriAnnotation.Elements["/Rect"] = CreateNumberArray(document, 40d, 560d, 300d, 590d);
        uriAnnotation.Elements["/Border"] = CreateNumberArray(document, 0d, 0d, 0d);
        uriAnnotation.Elements["/A"] = uriAction.ReferenceNotNull;
        document.Internals.AddObject(uriAnnotation);

        var firstAnnots = new PdfArray(document);
        firstAnnots.Elements.Add(target.ReferenceNotNull);
        firstAnnots.Elements.Add(uriAnnotation.ReferenceNotNull);
        firstPage.Elements["/Annots"] = firstAnnots;

        var secondAnnots = new PdfArray(document);
        secondAnnots.Elements.Add(untouched.ReferenceNotNull);
        secondPage.Elements["/Annots"] = secondAnnots;

        document.Outlines.Add(BookmarkTitle, secondPage, opened: true);
        document.Save(path);
    }

    private static void AddPageContentAndImage(PdfPage page, PdfDocument document)
    {
        var font = new PdfDictionary(document);
        font.Elements.SetName("/Type", "/Font");
        font.Elements.SetName("/Subtype", "/Type1");
        font.Elements.SetName("/BaseFont", "/Helvetica");
        document.Internals.AddObject(font);

        var image = new PdfDictionary(document);
        image.Elements.SetName("/Type", "/XObject");
        image.Elements.SetName("/Subtype", "/Image");
        image.Elements.SetInteger("/Width", 1);
        image.Elements.SetInteger("/Height", 1);
        image.Elements.SetName("/ColorSpace", "/DeviceRGB");
        image.Elements.SetInteger("/BitsPerComponent", 8);
        image.CreateStream((byte[])ImageBytes.Clone());
        document.Internals.AddObject(image);

        var fonts = new PdfDictionary(document);
        fonts.Elements["/Helv"] = font.ReferenceNotNull;
        var xObjects = new PdfDictionary(document);
        xObjects.Elements["/ImKeep"] = image.ReferenceNotNull;
        var resources = new PdfDictionary(document);
        resources.Elements["/Font"] = fonts;
        resources.Elements["/XObject"] = xObjects;
        page.Elements["/Resources"] = resources;

        var contents = new PdfDictionary(document);
        contents.CreateStream(Encoding.ASCII.GetBytes(
            $"BT /Helv 12 Tf 50 700 Td ({NonTargetText}) Tj ET\n" +
            "q 24 0 0 24 50 640 cm /ImKeep Do Q\n"));
        document.Internals.AddObject(contents);
        page.Elements["/Contents"] = contents.ReferenceNotNull;
    }

    private static PdfDictionary CreateTextField(
        PdfDocument document,
        string name,
        string value,
        double x1,
        double y1,
        double x2,
        double y2)
    {
        var field = new PdfDictionary(document);
        field.Elements.SetName("/FT", "/Tx");
        field.Elements.SetName("/Subtype", "/Widget");
        field.Elements.SetString("/T", name);
        field.Elements.SetString("/V", value);
        field.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
        field.Elements["/Rect"] = CreateNumberArray(document, x1, y1, x2, y2);
        field.Elements["/AP"] = CreateAppearanceDictionary(document, value);
        document.Internals.AddObject(field);
        return field;
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
        dictionary.Elements["/N"] = appearance.ReferenceNotNull;
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
        fonts.Elements["/Helv"] = font.ReferenceNotNull;
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

    private sealed record RepresentativeSnapshot(
        int PageCount,
        string NonTargetTextStream,
        string ImageHash,
        string BookmarkTitle,
        int BookmarkDestinationPageObject,
        int SecondPageObject,
        string Uri,
        string TargetFieldValue,
        string UntouchedFieldValue,
        string MetadataTitle,
        string MetadataAuthor);
}
