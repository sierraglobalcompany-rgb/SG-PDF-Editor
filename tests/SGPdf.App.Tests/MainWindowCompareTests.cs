using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowCompareTests
{
    [Theory]
    [InlineData(false, false, 2, 2, "Sin cambios", "Sin cambios", "Igual cantidad de páginas", "Sin diferencias detectadas")]
    [InlineData(true, false, 2, 2, "Cambios detectados", "Sin cambios", "Igual cantidad de páginas", "Se encontraron diferencias")]
    [InlineData(false, true, 2, 2, "Sin cambios", "Cambios detectados", "Igual cantidad de páginas", "Se encontraron diferencias")]
    [InlineData(false, false, 2, 3, "Sin cambios", "Sin cambios", "Cantidad de páginas diferente", "Se encontraron diferencias")]
    public void BuildComparePresentation_MapsExistingCoreResultWithoutChangingSemantics(
        bool textChanged,
        bool visualChanged,
        int leftPageCount,
        int rightPageCount,
        string expectedTextStatus,
        string expectedVisualStatus,
        string expectedStructureStatus,
        string expectedConclusion)
    {
        object presentation = BuildPresentation(
            "contrato-a.pdf",
            "contrato-b.pdf",
            leftPageCount,
            rightPageCount,
            textChanged,
            visualChanged);

        Assert.Equal("contrato-a.pdf", ReadProperty<string>(presentation, "LeftDisplayName"));
        Assert.Equal("contrato-b.pdf", ReadProperty<string>(presentation, "RightDisplayName"));
        Assert.Equal(leftPageCount, ReadProperty<int>(presentation, "LeftPageCount"));
        Assert.Equal(rightPageCount, ReadProperty<int>(presentation, "RightPageCount"));
        Assert.Equal(expectedTextStatus, ReadProperty<string>(presentation, "TextStatus"));
        Assert.Equal(
            "Cambio visual independiente del texto",
            ReadProperty<string>(presentation, "VisualLabel"));
        Assert.Equal(expectedVisualStatus, ReadProperty<string>(presentation, "VisualStatus"));
        Assert.Equal(expectedStructureStatus, ReadProperty<string>(presentation, "StructureStatus"));
        Assert.Equal(expectedConclusion, ReadProperty<string>(presentation, "Conclusion"));
    }

    private static object BuildPresentation(
        string leftDisplayName,
        string rightDisplayName,
        int leftPageCount,
        int rightPageCount,
        bool textChanged,
        bool visualChanged)
    {
        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;
        Type? resultType = appAssembly.GetType(
            "SGPdf.App.Features.Compare.PdfCompareResult",
            throwOnError: false);
        Assert.NotNull(resultType);

        object? result = Activator.CreateInstance(
            resultType,
            leftPageCount,
            rightPageCount,
            textChanged,
            visualChanged);
        Assert.NotNull(result);

        Type? mainWindowType = appAssembly.GetType("SGPdf.App.MainWindow", throwOnError: false);
        Assert.NotNull(mainWindowType);

        MethodInfo? method = mainWindowType.GetMethod(
            "BuildComparePresentation",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        object? presentation = method.Invoke(null, [leftDisplayName, rightDisplayName, result]);
        Assert.NotNull(presentation);
        return presentation;
    }

    private static T ReadProperty<T>(object instance, string propertyName)
    {
        PropertyInfo? property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        object? value = property.GetValue(instance);
        return Assert.IsType<T>(value);
    }
}
