using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormCapabilityTests
{
    private const string FieldName = "SGPDF_TextGate";
    private const string OriginalValue = "ORIGINAL";
    private const string UpdatedValue = "UPDATED BY SG PDF";

    [Fact]
    public void CapabilityGate_PdfSharpCanUpdateExistingTextFieldValueAndVisibleAppearance()
    {
        string directory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(directory, "acroform-source.pdf");
            string destinationPath = Path.Combine(directory, "acroform-output.pdf");
            CreateTextFieldFixture(sourcePath);
            byte[] sourceBytes = File.ReadAllBytes(sourcePath);

            PdfRenderedPage sourceRender;
            int sourcePageCount;
            using (var sourceSession = PdfDocumentSession.Open(sourcePath))
            {
                sourcePageCount = sourceSession.PageCount;
                Assert.Equal(1, sourcePageCount);
                Assert.NotEqual(0, sourceSession.GetOrganizeFormType());
                sourceRender = sourceSession.RenderPage(0, 72d);
            }

            Assert.Equal(OriginalValue, ReadFieldValue(sourcePath));

            WriteTextFieldValue(sourcePath, destinationPath, FieldName, UpdatedValue);

            Assert.Equal(sourceBytes, File.ReadAllBytes(sourcePath));
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(UpdatedValue, ReadFieldValue(destinationPath));

            using var destinationSession = PdfDocumentSession.Open(destinationPath);
            Assert.Equal(sourcePageCount, destinationSession.PageCount);
            Assert.NotEqual(0, destinationSession.GetOrganizeFormType());
            PdfRenderedPage destinationRender = destinationSession.RenderPage(0, 72d);
            Assert.Equal(sourceRender.PixelWidth, destinationRender.PixelWidth);
            Assert.Equal(sourceRender.PixelHeight, destinationRender.PixelHeight);

            string sourceFieldPixels = HashFieldRegion(sourceRender);
            string destinationFieldPixels = HashFieldRegion(destinationRender);
            Assert.NotEqual(sourceFieldPixels, destinationFieldPixels);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void WriteTextFieldValue(
        string sourcePath,
        string destinationPath,
        string fieldName,
        string value)
    {
        EnablePdfSharpWindowsFontsForCapabilityGate();
        using PdfDocument document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);

#pragma warning disable CS0618
        object acroForm = document.AcroForm;
#pragma warning restore CS0618
        PropertyInfo? fieldsProperty = acroForm.GetType().GetProperty(
            "Fields",
            BindingFlags.Instance | BindingFlags.Public);
        if (fieldsProperty?.GetValue(acroForm) is not object fields)
            throw new InvalidOperationException("PDFsharp no expuso la colección AcroForm.Fields esperada.");

        PropertyInfo? stringIndexer = fields.GetType().GetProperty(
            "Item",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            returnType: null,
            types: new[] { typeof(string) },
            modifiers: null);
        if (stringIndexer?.GetValue(fields, new object[] { fieldName }) is not object field)
            throw new InvalidOperationException($"PDFsharp no encontró el campo AcroForm '{fieldName}'.");

        PropertyInfo? textProperty = field.GetType().GetProperty(
            "Text",
            BindingFlags.Instance | BindingFlags.Public);
        if (textProperty is null || !textProperty.CanWrite)
        {
            throw new InvalidOperationException(
                $"El campo '{fieldName}' fue materializado como {field.GetType().FullName} sin setter Text utilizable.");
        }

        textProperty.SetValue(field, value);
        document.Save(destinationPath);
    }

    private static void EnablePdfSharpWindowsFontsForCapabilityGate()
    {
        Type? settingsType = typeof(XFont).Assembly.GetType("PdfSharp.Fonts.GlobalFontSettings");
        PropertyInfo? useWindowsFonts = settingsType?.GetProperty(
            "UseWindowsFontsUnderWindows",
            BindingFlags.Static | BindingFlags.Public);
        if (useWindowsFonts is null || useWindowsFonts.PropertyType != typeof(bool))
            throw new InvalidOperationException("PDFsharp 6.2.4 no expuso UseWindowsFontsUnderWindows.");

        useWindowsFonts.SetValue(null, true);
    }

    private static void CreateTextFieldFixture(string path)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();
        page.Width = XUnit.FromPoint(400d);
        page.Height = XUnit.FromPoint(300d);

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

    private static string? ReadFieldValue(string path)
    {
        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        PdfDictionary? acroForm = document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
        PdfArray? fields = acroForm?.Elements.GetArray("/Fields");
        if (fields is null)
            return null;

        for (int index = 0; index < fields.Elements.Count; index++)
        {
            PdfDictionary? field = fields.Elements.GetDictionary(index);
            if (field?.Elements.GetString("/T") == FieldName)
                return field.Elements.GetString("/V");
        }

        return null;
    }

    private static string HashFieldRegion(PdfRenderedPage render)
    {
        const int left = 40;
        const int right = 360;
        const int top = 80;
        const int bottom = 120;

        int bytesPerPixel = Math.Max(1, render.Stride / render.PixelWidth);
        using var stream = new MemoryStream();
        for (int y = top; y < bottom; y++)
        {
            int offset = checked((y * render.Stride) + (left * bytesPerPixel));
            int count = checked((right - left) * bytesPerPixel);
            stream.Write(render.Pixels, offset, count);
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sgpdf-acroform-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
