using System.IO;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormWpfFlowContractTests
{
    [Fact]
    public void MainWindow_ExposesMinimalExistingAcroFormFlow()
    {
        string root = FindRepositoryRoot();
        string formsPath = Path.Combine(root, "src", "SGPdf.App", "MainWindow.Forms.cs");

        Assert.True(File.Exists(formsPath), "Task 5 requires the focused MainWindow.Forms.cs flow.");
        string forms = File.ReadAllText(formsPath);

        Assert.Contains("Name = \"EditAcroFormMenuItem\"", forms, StringComparison.Ordinal);
        Assert.Contains("Name = \"AcroFormPropertiesPanel\"", forms, StringComparison.Ordinal);
        Assert.Contains("Name = \"AcroFormFieldsPanel\"", forms, StringComparison.Ordinal);
        Assert.Contains("Name = \"SaveAcroFormButton\"", forms, StringComparison.Ordinal);
        Assert.Contains("Content = \"Guardar como...\"", forms, StringComparison.Ordinal);
        Assert.Contains("PdfAcroFormInspector.InspectTextFields", forms, StringComparison.Ordinal);
        Assert.Contains("new PdfAcroFormSaveService(beforeValidation: null).Save", forms, StringComparison.Ordinal);
        Assert.Contains("SaveFileDialog", forms, StringComparison.Ordinal);
        Assert.Contains("PropertiesPlaceholderText.Parent as Grid", forms, StringComparison.Ordinal);
        Assert.DoesNotContain("new Window", forms, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_FormSaveTargetsOnlyChangedFieldsAndSkipsNoOpSave()
    {
        string root = FindRepositoryRoot();
        string forms = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Forms.cs"));

        Assert.Contains("_acroFormOriginalValues", forms, StringComparison.Ordinal);
        Assert.Contains("GetChangedAcroFormValues", forms, StringComparison.Ordinal);
        Assert.Contains("if (values.Count == 0)", forms, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_FormStateMovesThroughDocumentPendingOwnerWhenWorkspaceChanges()
    {
        string root = FindRepositoryRoot();
        string forms = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Forms.cs"));
        string pendingForms = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Forms.PendingState.cs"));
        string documents = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Documents.cs"));
        string labels = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Labels.cs"));

        Assert.Contains("ResetAcroFormUi", forms, StringComparison.Ordinal);
        Assert.Contains("CaptureAcroFormCurrentValues", pendingForms, StringComparison.Ordinal);
        Assert.Contains("RestoreAcroFormPendingState", pendingForms, StringComparison.Ordinal);
        Assert.Contains("CaptureActiveDocumentPendingState();", documents, StringComparison.Ordinal);
        Assert.Contains("RestoreDocumentPendingState(document);", documents, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", labels, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_AcroFormFlowIsMutuallyExclusiveWithExistingModes()
    {
        string root = FindRepositoryRoot();
        string forms = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Forms.cs"));
        string sign = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Sign.cs"));
        string edit = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Edit.cs"));
        string organize = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Organize.cs"));

        Assert.Contains("ResetAcroFormUi();", sign, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", edit, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", organize, StringComparison.Ordinal);
        Assert.Contains("TryLeaveEditModeWithGuard", forms, StringComparison.Ordinal);
        Assert.Contains("TryLeaveOrganizeModeWithGuard", forms, StringComparison.Ordinal);
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
