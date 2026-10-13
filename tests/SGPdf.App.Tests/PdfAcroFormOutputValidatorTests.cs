using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Forms;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfSharpFontGateCollection.Name)]
public sealed class PdfAcroFormOutputValidatorTests
{
    private const string ValidatorTypeName = "SGPdf.App.Features.Forms.PdfAcroFormOutputValidator";
    private const string FieldName = "SGPDF_TextGate";
    private const string OriginalValue = "ORIGINAL";
    private const string UpdatedValue = "UPDATED BY SG PDF";
    private const string WrongValue = "WRONG VALUE";

    [Fact]
    public void Validate_RejectsPersistedValueMismatch()
    {
        using var fixture = CreateFixture();
        string validOutput = Path.Combine(fixture.DirectoryPath, "valid.pdf");
        string tamperedOutput = Path.Combine(fixture.DirectoryPath, "value-mismatch.pdf");

        SaveExpectedUpdate(fixture.SourcePath, validOutput);
        RewriteField(validOutput, tamperedOutput, field => field.Elements.SetString("/V", WrongValue));

        Assert.Equal(WrongValue, ReadFieldValue(tamperedOutput));
        Assert.True(HasNormalAppearance(tamperedOutput));

        Assert.Throws<InvalidDataException>(() =>
            ValidateWithProductionValidator(fixture.SourcePath, tamperedOutput, ExpectedValues()));
    }

    [Fact]
    public void Validate_RejectsChangedFieldWithInvalidAppearance()
    {
        using var fixture = CreateFixture();
        string validOutput = Path.Combine(fixture.DirectoryPath, "valid.pdf");
        string tamperedOutput = Path.Combine(fixture.DirectoryPath, "invalid-appearance.pdf");

        SaveExpectedUpdate(fixture.SourcePath, validOutput);
        RewriteField(validOutput, tamperedOutput, field => field.Elements.Remove("/AP"));

        Assert.Equal(UpdatedValue, ReadFieldValue(tamperedOutput));
        Assert.False(HasNormalAppearance(tamperedOutput));

        Assert.Throws<InvalidDataException>(() =>
            ValidateWithProductionValidator(fixture.SourcePath, tamperedOutput, ExpectedValues()));
    }

    [Fact]
    public void Validate_RejectsVisuallyUnchangedWidgetWhenValueWasExpectedToChange()
    {
        using var fixture = CreateFixture();
        string staleAppearanceOutput = Path.Combine(fixture.DirectoryPath, "stale-appearance.pdf");

        RewriteField(
            fixture.SourcePath,
            staleAppearanceOutput,
            field => field.Elements.SetString("/V", UpdatedValue));

        Assert.Equal(UpdatedValue, ReadFieldValue(staleAppearanceOutput));
        Assert.Equal(
            ReadNormalAppearanceHash(fixture.SourcePath),
            ReadNormalAppearanceHash(staleAppearanceOutput));

        Assert.Throws<InvalidDataException>(() =>
            ValidateWithProductionValidator(fixture.SourcePath, staleAppearanceOutput, ExpectedValues()));
    }

    [Fact]
    public void Validate_RejectsSourceOutputMismatchInTargetWidget()
    {
        using var fixture = CreateFixture();
        string validOutput = Path.Combine(fixture.DirectoryPath, "valid.pdf");
        string movedWidgetOutput = Path.Combine(fixture.DirectoryPath, "moved-widget.pdf");

        SaveExpectedUpdate(fixture.SourcePath, validOutput);
        RewriteField(
            validOutput,
            movedWidgetOutput,
            field => field.Elements["/Rect"] = CreateNumberArray(field.Owner, 55d, 180d, 375d, 220d));

        Assert.Equal(UpdatedValue, ReadFieldValue(movedWidgetOutput));
        Assert.True(HasNormalAppearance(movedWidgetOutput));
        Assert.NotEqual(ReadRectSignature(fixture.SourcePath), ReadRectSignature(movedWidgetOutput));

        Assert.Throws<InvalidDataException>(() =>
            ValidateWithProductionValidator(fixture.SourcePath, movedWidgetOutput, ExpectedValues()));
    }

    private static IReadOnlyDictionary<string, string> ExpectedValues()
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [FieldName] = UpdatedValue
        };

    private static void SaveExpectedUpdate(string sourcePath, string destinationPath)
    {
        new PdfAcroFormSaveService(beforeValidation: null).Save(
            sourcePath,
            destinationPath,
            ExpectedValues());
    }

    private static void ValidateWithProductionValidator(
        string sourcePath,
        string outputPath,
        IReadOnlyDictionary<string, string> expectedValues)
    {
        Type validatorType = typeof(PdfAcroFormSaveService).Assembly.GetType(ValidatorTypeName)
            ?? throw new InvalidOperationException(
                $"RED esperado: todavía no existe el owner productivo '{ValidatorTypeName}'.");

        object validator = Activator.CreateInstance(validatorType, nonPublic: true)
            ?? throw new InvalidOperationException(
                $"El validator '{ValidatorTypeName}' debe exponer un constructor interno sin parámetros.");

        MethodInfo validate = validatorType.GetMethod(
            "Validate",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(string),
                typeof(string),
                typeof(IReadOnlyDictionary<string, string>),
                typeof(CancellationToken)
            ],
            modifiers: null)
            ?? throw new InvalidOperationException(
                $"El validator '{ValidatorTypeName}' debe exponer Validate(sourcePath, outputPath, expectedValues, cancellationToken).");

        try
        {
            validate.Invoke(
                validator,
                [sourcePath, outputPath, expectedValues, CancellationToken.None]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static ValidatorFixture CreateFixture()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-3-task4-2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string sourcePath = Path.Combine(directory, "source.pdf");
        CreateTextFieldFixture(sourcePath);
        return new ValidatorFixture(directory, sourcePath);
    }

    private static void CreateTextFieldFixture(string path)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();
        page.Width = PdfSharp.Drawing.XUnit.FromPoint(400d);
        page.Height = PdfSharp.Drawing.XUnit.FromPoint(300d);

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

    private static void RewriteField(
        string sourcePath,
        string destinationPath,
        Action<PdfDictionary> rewrite)
    {
        using PdfDocument document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
        rewrite(GetFieldDictionary(document));
        document.Save(destinationPath);
    }

    private static string ReadFieldValue(string path)
    {
        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        return GetFieldDictionary(document).Elements.GetString("/V");
    }

    private static bool HasNormalAppearance(string path)
    {
        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        PdfDictionary field = GetFieldDictionary(document);
        PdfDictionary? appearanceDictionary = field.Elements.GetDictionary("/AP");
        return appearanceDictionary?.Elements.GetDictionary("/N")?.Stream is not null;
    }

    private static string ReadNormalAppearanceHash(string path)
    {
        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        PdfDictionary field = GetFieldDictionary(document);
        PdfDictionary? appearanceDictionary = field.Elements.GetDictionary("/AP");
        byte[]? bytes = appearanceDictionary?.Elements.GetDictionary("/N")?.Stream?.UnfilteredValue;
        if (bytes is null)
            throw new InvalidDataException($"El campo '{FieldName}' no contiene /AP/N legible.");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string ReadRectSignature(string path)
    {
        using PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        PdfArray? rect = GetFieldDictionary(document).Elements.GetArray("/Rect");
        if (rect is null)
            throw new InvalidDataException($"El campo '{FieldName}' no contiene /Rect.");
        return string.Join("|", rect.Elements.Select(item => item.ToString()));
    }

    private static PdfDictionary GetFieldDictionary(PdfDocument document)
    {
        PdfDictionary? acroForm = document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
        PdfArray? fields = acroForm?.Elements.GetArray("/Fields");
        if (fields is null)
            throw new InvalidDataException("El PDF no contiene /AcroForm/Fields.");

        for (int index = 0; index < fields.Elements.Count; index++)
        {
            PdfDictionary? field = fields.Elements.GetDictionary(index);
            if (field?.Elements.GetString("/T") == FieldName)
                return field;
        }

        throw new InvalidDataException($"No se encontró el campo '{FieldName}'.");
    }

    private sealed class ValidatorFixture : IDisposable
    {
        internal ValidatorFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
