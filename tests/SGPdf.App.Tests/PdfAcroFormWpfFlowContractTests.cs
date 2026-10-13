using System.IO;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormWpfFlowContractTests
{
    [Fact]
    public void MainWindow_ExposesMinimalExistingAcroFormFlow()
    {
        string root = FindRepositoryRoot();
        string xamlPath = Path.Combine(root, "src", "SGPdf.App", "MainWindow.xaml");
        string formsPath = Path.Combine(root, "src", "SGPdf.App", "MainWindow.Forms.cs");
        string xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Name=\"EditAcroFormMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AcroFormPropertiesPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AcroFormFieldsPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SaveAcroFormButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Guardar como...\"", xaml, StringComparison.Ordinal);

        Assert.True(File.Exists(formsPath), "Task 5 requires the focused MainWindow.Forms.cs flow.");
        string forms = File.ReadAllText(formsPath);
        Assert.Contains("PdfAcroFormInspector.InspectTextFields", forms, StringComparison.Ordinal);
        Assert.Contains("new PdfAcroFormSaveService(beforeValidation: null).Save", forms, StringComparison.Ordinal);
        Assert.Contains("SaveFileDialog", forms, StringComparison.Ordinal);
        Assert.DoesNotContain("new Window", forms, StringComparison.Ordinal);
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
