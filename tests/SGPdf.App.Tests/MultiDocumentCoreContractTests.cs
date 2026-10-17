using Xunit;

namespace SGPdf.App.Tests;

public sealed class MultiDocumentCoreContractTests
{
    [Fact]
    public void OpenPdfDialog_AllowsSelectingMoreThanOnePdf()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.xaml.cs"));

        Assert.Contains("Multiselect = true", source);
        Assert.Contains("dialog.FileNames", source);
    }

    [Fact]
    public void OpeningAnotherPdf_AddsWorkspaceInsteadOfDisposingPreviousSession()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Reader.cs"));

        Assert.Contains("TrackOpenedDocument", source);
        Assert.Contains("ActivateDocumentAsync", source);
        Assert.DoesNotContain("previousSession?.Dispose()", source);
    }

    [Fact]
    public void ActiveDocumentState_IsOwnedByDocumentWorkspacePartial()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));

        Assert.Contains("ObservableCollection<PdfDocumentWorkspace> _openDocuments", source);
        Assert.Contains("IReadOnlyList<PdfDocumentWorkspace> OpenDocuments", source);
        Assert.Contains("PdfDocumentWorkspace? ActiveDocument", source);
        Assert.Contains("ActiveDocument.Session", source);
        Assert.Contains("ActiveDocument.Navigation", source);
        Assert.Contains("ActiveDocument.Zoom", source);
    }

    [Fact]
    public void Workspace_PreservesReaderPageZoomAndScrollState()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "Shell", "PdfDocumentWorkspace.cs"));

        Assert.Contains("PageNavigationState Navigation", source);
        Assert.Contains("PdfZoomState Zoom", source);
        Assert.Contains("double VerticalOffset", source);
        Assert.Contains("SetNavigation", source);
        Assert.Contains("SetZoom", source);
    }

    [Fact]
    public void ClosingWindow_DisposesEveryOpenWorkspace()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));

        Assert.Contains("foreach (var document in _openDocuments", source);
        Assert.Contains("document.Dispose()", source);
        Assert.Contains("_openDocuments.Clear()", source);
    }

    private static string FindRepoFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");
    }
}
