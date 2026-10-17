using System.IO;
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

    [Fact]
    public void BuildComparePanelText_FormatsExistingPresentationForRightPanel()
    {
        object presentation = BuildPresentation(
            "contrato-a.pdf",
            "contrato-b.pdf",
            leftPageCount: 2,
            rightPageCount: 3,
            textChanged: true,
            visualChanged: false);

        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;
        Type? mainWindowType = appAssembly.GetType("SGPdf.App.MainWindow", throwOnError: false);
        Assert.NotNull(mainWindowType);

        MethodInfo? method = mainWindowType.GetMethod(
            "BuildComparePanelText",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        object? value = method.Invoke(null, [presentation]);
        string text = Assert.IsType<string>(value);

        Assert.Contains("PDF A: contrato-a.pdf (2 páginas)", text, StringComparison.Ordinal);
        Assert.Contains("PDF B: contrato-b.pdf (3 páginas)", text, StringComparison.Ordinal);
        Assert.Contains("Texto: Cambios detectados", text, StringComparison.Ordinal);
        Assert.Contains("Cambio visual independiente del texto: Sin cambios", text, StringComparison.Ordinal);
        Assert.Contains("Estructura: Cantidad de páginas diferente", text, StringComparison.Ordinal);
        Assert.Contains("Resultado: Se encontraron diferencias", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareUi_HasFocusedResetAndCloseWithoutGlobalModeManager()
    {
        string root = FindRepositoryRoot();
        string compare = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Compare.cs"));

        Assert.Contains("ResetCompareUi", compare, StringComparison.Ordinal);
        Assert.Contains("Cerrar comparación", compare, StringComparison.Ordinal);
        Assert.Contains("PropertiesPlaceholderText.Text = \"Propiedades\"", compare, StringComparison.Ordinal);
        Assert.DoesNotContain("ModeManager", compare, StringComparison.Ordinal);
        Assert.DoesNotContain("new Window", compare, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareUi_ReusesExistingWorkspaceResetOwner()
    {
        string root = FindRepositoryRoot();
        string documents = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Documents.cs"));
        string edit = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Edit.cs"));
        string organize = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Organize.cs"));
        string sign = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Sign.cs"));
        string forms = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Forms.cs"));

        Assert.Contains("ResetAcroFormUi();", documents, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", edit, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", organize, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", sign, StringComparison.Ordinal);
        Assert.Contains("ResetCompareUi();", forms, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareStart_HonorsExistingDirtyModeGuardsBeforeComparison()
    {
        string root = FindRepositoryRoot();
        string compare = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Compare.cs"));

        Assert.Contains("TryLeaveEditModeWithGuard", compare, StringComparison.Ordinal);
        Assert.Contains("TryLeaveOrganizeModeWithGuard", compare, StringComparison.Ordinal);
        Assert.Contains("TryResolvePendingSignatureEdits", compare, StringComparison.Ordinal);
        Assert.Contains("PdfCompareService.Compare(leftPath, rightPath)", compare, StringComparison.Ordinal);
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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SGPdf.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
