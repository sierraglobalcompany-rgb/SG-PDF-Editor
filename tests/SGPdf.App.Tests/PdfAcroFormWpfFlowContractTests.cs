using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SGPdf.App.Features.Forms;
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

        Assert.Contains("TryLeaveAcroFormWithGuard()", sign, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", edit, StringComparison.Ordinal);
        Assert.Contains("ResetAcroFormUi();", organize, StringComparison.Ordinal);
        Assert.Contains("TryLeaveEditModeWithGuard", forms, StringComparison.Ordinal);
        Assert.Contains("TryLeaveOrganizeModeWithGuard", forms, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_AcroFormRailGuardsDiscardAndPreservesSavedCopy()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = AcroFormWindowFixture.Create();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.SourcePath));
                OrganizeWindowTestHost.Invoke(window, "InitializeFormsUi");
                OrganizeWindowTestHost.Invoke(window, "EditAcroForm_Click", window, new RoutedEventArgs(Button.ClickEvent));

                var editors = Assert.IsAssignableFrom<IDictionary<string, TextBox>>(
                    OrganizeWindowTestHost.GetField(window, "_acroFormEditors"));
                var editor = Assert.Single(editors).Value;
                editor.Text = UpdatedValue;
                var readerSurface = Assert.IsType<Grid>(OrganizeWindowTestHost.GetField(window, "_readerContinuousSurface"));
                var pdfScrollViewer = Assert.IsType<ScrollViewer>(OrganizeWindowTestHost.GetField(window, "PdfScrollViewer"));
                var readerSurfaceVisibility = readerSurface.Visibility;
                var pdfScrollViewerVisibility = pdfScrollViewer.Visibility;

                var confirmationCount = 0;
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardAcroFormChanges", (Func<bool>)(() =>
                {
                    confirmationCount++;
                    return false;
                }));

                OrganizeWindowTestHost.Invoke(window, "EditAcroForm_Click", window, new RoutedEventArgs(Button.ClickEvent));
                Assert.Same(editor, editors[FieldName]);
                Assert.Equal(UpdatedValue, editor.Text);
                Assert.Equal("El formulario ya está abierto.", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text);
                Assert.Equal(0, confirmationCount);

                Assert.False(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterSignModeAsync"));
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.GetField(window, "_acroFormPropertiesScrollViewer") is ScrollViewer scrollViewer
                    ? scrollViewer.Visibility
                    : Visibility.Collapsed);
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_signatureModeActive")!);
                Assert.Same(editor, editors[FieldName]);
                Assert.Equal(UpdatedValue, editor.Text);
                Assert.Equal(readerSurfaceVisibility, readerSurface.Visibility);
                Assert.Equal(pdfScrollViewerVisibility, pdfScrollViewer.Visibility);
                Assert.Equal(1, confirmationCount);

                Assert.False(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterEditModeAsync"));
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_editModeActive")!);
                Assert.Same(editor, editors[FieldName]);
                Assert.Equal(UpdatedValue, editor.Text);
                Assert.Equal(readerSurfaceVisibility, readerSurface.Visibility);
                Assert.Equal(pdfScrollViewerVisibility, pdfScrollViewer.Visibility);
                Assert.Equal(2, confirmationCount);

                string savedCopyPath = Path.Combine(fixture.DirectoryPath, "saved-copy.pdf");
                new PdfAcroFormSaveService(beforeValidation: null).Save(
                    fixture.SourcePath,
                    savedCopyPath,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [FieldName] = UpdatedValue
                    });

                OrganizeWindowTestHost.SetField(window, "_confirmDiscardAcroFormChanges", (Func<bool>)(() =>
                {
                    confirmationCount++;
                    return true;
                }));
                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterSignModeAsync"));
                Assert.Equal(Visibility.Collapsed, ((ScrollViewer)OrganizeWindowTestHost.GetField(window, "_acroFormPropertiesScrollViewer")!).Visibility);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_signatureModeActive")!);
                Assert.True(File.Exists(savedCopyPath));
                Assert.Equal(UpdatedValue, Assert.Single(PdfAcroFormInspector.InspectTextFields(savedCopyPath)).Value);
                Assert.Equal(3, confirmationCount);

                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterReadModeAsync"));
                OrganizeWindowTestHost.Invoke(window, "EditAcroForm_Click", window, new RoutedEventArgs(Button.ClickEvent));
                confirmationCount = 0;
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardAcroFormChanges", (Func<bool>)(() =>
                {
                    confirmationCount++;
                    return false;
                }));
                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterSignModeAsync"));
                Assert.Equal(0, confirmationCount);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    private const string FieldName = "SGPDF_WpfGuard";
    private const string UpdatedValue = "EDITED VALUE";

    private sealed class AcroFormWindowFixture : IDisposable
    {
        private AcroFormWindowFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static AcroFormWindowFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-acroform-wpf-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var sourcePath = Path.Combine(directory, "source.pdf");

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
            field.Elements.SetString("/V", "ORIGINAL VALUE");
            field.Elements.SetString("/DA", "/Helv 14 Tf 0 g");
            field.Elements["/Rect"] = CreateNumberArray(document, 40d, 180d, 360d, 220d);
            field.Elements["/AP"] = CreateAppearanceDictionary(document, "ORIGINAL VALUE");
            document.Internals.AddObject(field);
            fields.Elements.Add(field.Reference!);

            var annots = new PdfArray(document);
            annots.Elements.Add(field.Reference!);
            page.Elements["/Annots"] = annots;
            document.Save(sourcePath);

            return new AcroFormWindowFixture(directory, sourcePath);
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

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
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
