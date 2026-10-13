using System.Reflection;
using System.Text;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCompareServiceTests
{
    [Fact]
    public void Compare_IdenticalDocuments_ReturnsNoTextOrVisualChanges()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-4-compare-core-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string leftPath = Path.Combine(directory, "left.pdf");
            string rightPath = Path.Combine(directory, "right.pdf");

            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.Save(leftPath);
            }

            File.Copy(leftPath, rightPath);

            object result = Compare(leftPath, rightPath);
            Assert.Equal(1, ReadProperty<int>(result, "LeftPageCount"));
            Assert.Equal(1, ReadProperty<int>(result, "RightPageCount"));
            Assert.False(ReadProperty<bool>(result, "TextChanged"));
            Assert.False(ReadProperty<bool>(result, "VisualChanged"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Compare_TextOnlyChange_ClassifiesTextWithoutVisualOnlyChange()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-4-compare-text-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string leftPath = Path.Combine(directory, "left.pdf");
            string rightPath = Path.Combine(directory, "right.pdf");
            CreateTextPdf(leftPath, "BASELINE");
            CreateTextPdf(rightPath, "CHANGED");

            object result = Compare(leftPath, rightPath);
            Assert.True(ReadProperty<bool>(result, "TextChanged"));
            Assert.False(ReadProperty<bool>(result, "VisualChanged"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static object Compare(string leftPath, string rightPath)
    {
        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;
        Type? serviceType = appAssembly.GetType(
            "SGPdf.App.Features.Compare.PdfCompareService",
            throwOnError: false);
        Assert.NotNull(serviceType);

        MethodInfo? compareMethod = serviceType.GetMethod(
            "Compare",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(string), typeof(string)],
            modifiers: null);
        Assert.NotNull(compareMethod);

        object? result = compareMethod.Invoke(null, [leftPath, rightPath]);
        Assert.NotNull(result);

        Type? resultType = serviceType.Assembly.GetType(
            "SGPdf.App.Features.Compare.PdfCompareResult",
            throwOnError: false);
        Assert.NotNull(resultType);
        Assert.Equal(resultType, result.GetType());
        return result;
    }

    private static void CreateTextPdf(string path, string text)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();
        page.Width = PdfSharp.Drawing.XUnit.FromPoint(400d);
        page.Height = PdfSharp.Drawing.XUnit.FromPoint(300d);

        var resources = new PdfDictionary(document);
        var fonts = new PdfDictionary(document);
        var font = new PdfDictionary(document);
        font.Elements.SetName("/Type", "/Font");
        font.Elements.SetName("/Subtype", "/Type1");
        font.Elements.SetName("/BaseFont", "/Helvetica");
        font.Elements.SetName("/Encoding", "/WinAnsiEncoding");
        document.Internals.AddObject(font);
        fonts.Elements["/F1"] = font.Reference;
        resources.Elements["/Font"] = fonts;
        page.Elements["/Resources"] = resources;

        var content = new PdfDictionary(document);
        content.CreateStream(Encoding.ASCII.GetBytes(
            $"BT /F1 20 Tf 40 220 Td ({text}) Tj ET\n"));
        document.Internals.AddObject(content);
        page.Elements["/Contents"] = content.Reference;

        document.Save(path);
    }

    private static T ReadProperty<T>(object instance, string propertyName)
    {
        PropertyInfo? property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        object? value = property.GetValue(instance);
        return Assert.IsType<T>(value);
    }
}
