using System.Reflection;
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

    private static T ReadProperty<T>(object instance, string propertyName)
    {
        PropertyInfo? property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        object? value = property.GetValue(instance);
        return Assert.IsType<T>(value);
    }
}
