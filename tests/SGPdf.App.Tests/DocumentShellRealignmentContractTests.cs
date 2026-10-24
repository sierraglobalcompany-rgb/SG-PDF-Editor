using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class DocumentShellRealignmentContractTests
{
    [Fact]
    public void DocumentShell_DeclaresRealDocumentTabsAndAddButton()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));
        var xaml = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.xaml"));

        Assert.Contains("x:Name=\"DocumentTabs\"", xaml);
        Assert.Contains("x:Name=\"AddDocumentTabButton\"", xaml);
        Assert.Contains("x:Name=\"DocumentTopShell\"", xaml);
        Assert.Contains("x:Name=\"DocumentTabRow\"", xaml);
    }

    [Fact]
    public void DocumentTabs_ReuseExistingMultiDocumentOwnersInsteadOfCreatingParallelState()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));

        Assert.Contains("DocumentTabs.ItemsSource = _openDocuments", source);
        Assert.Contains("DocumentTabs_SelectionChanged", source);
        Assert.Contains("ActivateDocumentAsync", source);
        Assert.Contains("DocumentTabClose_Click", source);
        Assert.Contains("TryCloseDocumentWithGuard", source);
        Assert.Contains("AddDocumentTabButton_Click", source);
        Assert.DoesNotContain("ObservableCollection<PdfDocumentWorkspace> _tabDocuments", source);
    }

    [Fact]
    public void DocumentTabs_V4UsesApprovedDocumentIconAndHeaderOnlySelectedVisual()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));
        var xaml = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.xaml"));
        var styles = XDocument.Load(FindRepoFile("src", "SGPdf.App", "Themes", "Modern.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        Assert.Contains("SgDocumentTabsStyle", xaml);
        Assert.Contains("SGPdf.Document.png", source);
        Assert.Contains("BuildDocumentTabHeaderTemplate()", source);
        Assert.Contains("TextTrimming.CharacterEllipsis", source);

        var tabControlStyle = styles.Descendants(presentation + "Style")
            .Single(item => (string?)item.Attribute(x + "Key") == "SgDocumentTabsStyle");
        var root = tabControlStyle.Descendants(presentation + "ControlTemplate")
            .Single().Elements().Single();
        // The PDF reader already owns document content. TabControl renders headers ONLY:
        // this prevents WPF from displaying PdfDocumentWorkspace.ToString() under tabs.
        Assert.Equal(presentation + "TabPanel", root.Name);
        Assert.Equal("True", (string?)root.Attribute("IsItemsHost"));
        Assert.Empty(tabControlStyle.Descendants(presentation + "ContentPresenter"));

        var tabItemStyle = styles.Descendants(presentation + "Style")
            .Single(item => (string?)item.Attribute(x + "Key") == "SgDocumentTabItemStyle");
        var chrome = tabItemStyle.Descendants(presentation + "ControlTemplate")
            .Single().Element(presentation + "Border");
        Assert.NotNull(chrome);
        var header = Assert.Single(chrome.Elements(presentation + "ContentPresenter"));
        Assert.Equal("Header", (string?)header.Attribute("ContentSource"));
        Assert.Contains(tabItemStyle.Descendants(presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "IsSelected"
                && (string?)trigger.Attribute("Value") == "True"
                && trigger.Descendants(presentation + "Setter").Any(setter =>
                    (string?)setter.Attribute("TargetName") == "TabChrome"
                    && (string?)setter.Attribute("Property") == "Background"
                    && ((string?)setter.Attribute("Value"))?.Contains("SgSelectionFillBrush") == true));
        var selectedChrome = tabItemStyle.Descendants(presentation + "Border")
            .Single(border => (string?)border.Attribute(x + "Name") == "TabChrome");
        Assert.Equal("8", (string?)selectedChrome.Attribute("CornerRadius"));
    }

    [Fact]
    public void MultiDocumentShell_DeclaresPerDocumentWorkspaceOwner()
    {
        var workspacePath = TryFindRepoFile("src", "SGPdf.App", "Shell", "PdfDocumentWorkspace.cs");
        Assert.True(
            workspacePath is not null,
            "T9.1-R0 contract: each open PDF needs a dedicated PdfDocumentWorkspace owner.");

        var workspaceSource = File.ReadAllText(workspacePath!);
        Assert.Contains("PdfDocumentSession", workspaceSource);
        Assert.Contains("PageNavigationState", workspaceSource);
        Assert.Contains("PdfZoomState", workspaceSource);
        Assert.Contains("IsDirty", workspaceSource);

        var documentOwnerSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));
        Assert.Contains("OpenDocuments", documentOwnerSource);
        Assert.Contains("ActiveDocument", documentOwnerSource);
    }

    [Fact]
    public void ReaderNavigation_DeclaresCollapsibleDrawerAndPerDocumentState()
    {
        var xaml = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.xaml"));
        var readerSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.ReaderThumbnails.cs"));
        var workspaceSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "Shell", "PdfDocumentWorkspace.cs"));
        var navigationOwnerSource = xaml + Environment.NewLine + readerSource;

        Assert.Contains("ReaderNavigationColumn", navigationOwnerSource);
        Assert.Contains("ReaderNavigationDrawer", navigationOwnerSource);
        Assert.Contains("ReaderNavigationToggleButton", navigationOwnerSource);
        Assert.Contains("ToggleReaderNavigation", readerSource);
        Assert.Contains("IsReaderNavigationOpen", workspaceSource);
        Assert.Contains("ReaderNavigationSelectedIndex", workspaceSource);
    }

    [Fact]
    public void ContinuousReader_UsesThemeAwareCanvasBehindWhitePdfPages()
    {
        var readerSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Reader.cs"));

        Assert.Contains("surface.SetResourceReference(Panel.BackgroundProperty, \"LayerFillColorAltBrush\")", readerSource);
        Assert.Contains("Grid.SetRow(surface, Grid.GetRow(PdfScrollViewer))", readerSource);
        Assert.Contains("border.SetValue(Border.BackgroundProperty, Brushes.White)", readerSource);
    }

    [Fact]
    public void PdfWorkspace_DeclaresContextMenuOwner()
    {
        var path = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var contextMenu = document
            .Descendants(presentation + "ContextMenu")
            .SingleOrDefault(element => (string?)element.Attribute(x + "Name") == "PdfWorkspaceContextMenu");

        Assert.NotNull(contextMenu);
    }

    [Fact]
    public void DocumentToolRail_ConsumesOfficialArtworkWithFallbackOnlyForUnmappedTools()
    {
        var shellSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs"));
        var descriptorSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "Shell", "ToolDescriptor.cs"));

        Assert.Contains("DocumentIconPath", descriptorSource);
        Assert.Contains("edit-pdf.png", descriptorSource);
        Assert.Contains("sign.png", descriptorSource);
        Assert.Contains("organize-pages.png", descriptorSource);
        Assert.Contains("ocr.png", descriptorSource);
        Assert.Contains("redact.png", descriptorSource);
        Assert.Contains("compare-pdf.png", descriptorSource);
        Assert.Contains("pdf-to-png.png", descriptorSource);
        Assert.Contains("protect.png", descriptorSource);
        Assert.Contains("\"read\" or \"comment\" or \"forms\" => null", descriptorSource);

        Assert.Contains("System.Windows.Controls.Image", shellSource);
        Assert.Contains("descriptor.DocumentIconPath", shellSource);
        Assert.Contains("System.Windows.Shapes.Path", shellSource);
        Assert.Contains("Width = 24d", shellSource);
        Assert.Contains("TextWrapping = TextWrapping.Wrap", shellSource);
    }

    [Fact]
    public void DocumentToolRail_F14ActionsDelegateToExistingOwnersAndGuardPdfReplacement()
    {
        var shell = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs"));
        var forms = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Forms.cs"));
        var compare = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Compare.cs"));
        var labels = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Labels.cs"));
        var edit = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Edit.cs"));
        var sign = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Sign.cs"));
        var organize = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Organize.cs"));
        var comments = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Comments.cs"));

        var dispatches = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["forms"] = "EditAcroForm_Click(this, new RoutedEventArgs(Button.ClickEvent))",
            ["ocr"] = "Ocr_Click(this, new RoutedEventArgs(Button.ClickEvent))",
            ["redact"] = "await TryEnterEditModeAsync()",
            ["compare"] = "ComparePdf_Click(this, new RoutedEventArgs(Button.ClickEvent))",
            ["convert"] = "ExportPdfToPng_Click(this, new RoutedEventArgs(Button.ClickEvent))",
            ["protect"] = "Protection_Click(this, new RoutedEventArgs(Button.ClickEvent))"
        };
        foreach (var dispatch in dispatches)
        {
            var actionCase = GetToolActionCase(shell, dispatch.Key);
            Assert.Contains(dispatch.Value, actionCase, StringComparison.Ordinal);
            Assert.DoesNotContain("new Pdf", actionCase, StringComparison.Ordinal);
            Assert.DoesNotContain("new Ocr", actionCase, StringComparison.Ordinal);
        }
        Assert.Contains("TryPrepareCoreToolTransition(\"edit\")", GetToolActionCase(shell, "redact"), StringComparison.Ordinal);

        foreach (var owner in new[] { forms, compare, labels, edit, sign, organize, comments })
            Assert.Contains("TryLeaveAcroFormWithGuard()", owner, StringComparison.Ordinal);
        Assert.Contains("_acroFormPropertiesScrollViewer?.Visibility == Visibility.Visible", shell, StringComparison.Ordinal);
        Assert.Contains("? \"forms\"", shell, StringComparison.Ordinal);
        var validate = labels.IndexOf("ValidateRenderedLabels(candidate, rendered)", StringComparison.Ordinal);
        var guard = validate < 0
            ? -1
            : labels.IndexOf("TryLeaveAcroFormWithGuard()", validate, StringComparison.Ordinal);
        var commit = guard < 0
            ? -1
            : labels.IndexOf("CommitLoadedZpl(candidate, rendered)", guard, StringComparison.Ordinal);
        Assert.True(validate >= 0 && guard > validate && commit > guard);

        Assert.DoesNotContain("_ocrModeActive", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("_compareModeActive", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("_convertModeActive", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("_protectModeActive", shell, StringComparison.Ordinal);
    }

    private static string GetToolActionCase(string shell, string actionId)
    {
        var methodStart = shell.IndexOf("private async void HandleToolAction", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = shell.IndexOf("private bool TryPrepareCoreToolTransition", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var method = shell[methodStart..methodEnd];
        var caseStart = method.IndexOf($"case \"{actionId}\":", StringComparison.Ordinal);
        Assert.True(caseStart >= 0, $"No se encontró el dispatch de {actionId}.");
        var nextCase = method.IndexOf("\n            case \"", caseStart + 1, StringComparison.Ordinal);
        var switchEnd = method.IndexOf("\n        }", caseStart, StringComparison.Ordinal);
        var caseEnd = nextCase < 0 ? switchEnd : Math.Min(nextCase, switchEnd);
        Assert.True(caseEnd > caseStart);
        return method[caseStart..caseEnd];
    }

    [Fact]
    public void DocumentToolRail_V4SingleSelectedOwnerAndUniformLayout()
    {
        var shellSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs"));
        var styles = XDocument.Load(FindRepoFile("src", "SGPdf.App", "Themes", "Modern.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        // Only the existing mode-based owner chooses selected styling.
        Assert.Contains("UpdateDocumentToolRailSelection()", shellSource);
        Assert.DoesNotContain("string.Equals(descriptor.Id, \"read\", StringComparison.Ordinal)", shellSource);
        Assert.Contains("descriptor.DocumentIconPath", shellSource);
        Assert.Contains("IsEnabled = descriptor.ActionId is not null", shellSource);
        Assert.Contains("Width = 62d", shellSource);

        var railStyle = styles.Descendants(presentation + "Style")
            .Single(e => (string?)e.Attribute(x + "Key") == "SgRailItemStyle");
        var selectedStyle = styles.Descendants(presentation + "Style")
            .Single(e => (string?)e.Attribute(x + "Key") == "SgRailItemSelectedStyle");
        Assert.Contains(railStyle.Elements(presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "MinHeight"
                   && (string?)setter.Attribute("Value") == "62");
        Assert.Contains(selectedStyle.Elements(presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "Background"
                   && ((string?)setter.Attribute("Value"))?.Contains("SgSelectionFillBrush") == true);
        Assert.Contains(selectedStyle.Elements(presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "BorderBrush"
                   && ((string?)setter.Attribute("Value"))?.Contains("SgAccentBrush") == true);
    }

    [Fact]
    public void ReaderDrawer_V4UsesSegmentedTabsAndAContextRowToggle()
    {
        var readerSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.ReaderThumbnails.cs"));
        var xaml = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.xaml"));
        var styles = XDocument.Load(FindRepoFile("src", "SGPdf.App", "Themes", "Modern.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        Assert.Contains("ReaderContextBar.Children.Insert(0, toggle)", readerSource);
        Assert.Contains("AutomationProperties.SetHelpText(toggle", readerSource);
        Assert.DoesNotContain("Panel.SetZIndex(toggle, 80)", readerSource);
        Assert.Contains("x:Name=\"ReaderContextBar\"", xaml);
        Assert.Contains("x:Name=\"ReaderViewCaption\"", xaml);

        var navigationTabStyle = styles.Descendants(presentation + "Style")
            .Single(style => (string?)style.Attribute(x + "Key") == "SgReaderNavigationTabItemStyle");
        var navigationControlTemplate = styles.Descendants(presentation + "Style")
            .Single(style => (string?)style.Attribute(x + "Key") == "SgReaderNavigationTabsStyle")
            .Descendants(presentation + "ControlTemplate")
            .Single();
        Assert.Contains(navigationControlTemplate.Descendants(presentation + "ContentPresenter"), presenter =>
            (string?)presenter.Attribute("ContentSource") == "SelectedContent");
        Assert.Contains(navigationTabStyle.Descendants(presentation + "Trigger"), trigger =>
            (string?)trigger.Attribute("Property") == "IsSelected" &&
            trigger.Descendants(presentation + "Setter").Any(setter =>
                (string?)setter.Attribute("TargetName") == "NavigationTabChrome" &&
                ((string?)setter.Attribute("Value"))?.Contains("SgSelectionFillBrush") == true));

        var thumbnailStyle = styles.Descendants(presentation + "Style")
            .Single(style => (string?)style.Attribute(x + "Key") == "SgReaderThumbnailItemStyle");
        Assert.Contains(thumbnailStyle.Descendants(presentation + "Trigger"), trigger =>
            (string?)trigger.Attribute("Property") == "IsKeyboardFocusWithin");
    }

    [Fact]
    public void ImageSelectionOverlay_RemainsTransparentInsteadOfTintingSelectedImage()
    {
        var editSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Edit.cs"));

        Assert.Contains("Fill = Brushes.Transparent", editSource);
        Assert.DoesNotContain("Fill = Brushes.DodgerBlue", editSource);
    }

    private static string FindRepoFile(params string[] segments)
        => TryFindRepoFile(segments)
           ?? throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");

    private static string? TryFindRepoFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
