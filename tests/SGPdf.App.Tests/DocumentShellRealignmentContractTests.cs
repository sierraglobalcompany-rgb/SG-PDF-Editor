using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class DocumentShellRealignmentContractTests
{
    [Fact]
    public void DocumentShell_DeclaresRealDocumentTabsAndAddButton()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));

        Assert.Contains("Name = \"DocumentTabs\"", source);
        Assert.Contains("Name = \"AddDocumentTabButton\"", source);
        Assert.Contains("Name = \"DocumentTopShell\"", source);
        Assert.Contains("Name = \"DocumentTabRow\"", source);
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

    [Fact(Skip = "T9.1-R4: drawer Páginas/Marcadores plegable se implementa después del shell y rail.")]
    public void ReaderNavigation_DeclaresCollapsibleDrawerAndToggleOwner()
    {
        var xaml = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.xaml"));
        var readerSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.ReaderThumbnails.cs"));
        var combined = xaml + Environment.NewLine + readerSource;

        Assert.Contains("ReaderNavigationDrawer", combined);
        Assert.Contains("ReaderNavigationToggleButton", combined);
    }

    [Fact(Skip = "T9.1-R5: menú contextual del workspace PDF se implementa después del drawer.")]
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
        Assert.Contains("Width = 22d", shellSource);
        Assert.Contains("TextWrapping = TextWrapping.NoWrap", shellSource);
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
