using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ContextualShellContractTests
{
    [Fact]
    public void RightContextPanel_IsCollapsedWhenThereIsNoRealContext()
    {
        var path = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var workspaceGrid = document
            .Descendants(presentation + "Grid")
            .Single(grid =>
            {
                var definitions = grid.Element(presentation + "Grid.ColumnDefinitions");
                if (definitions is null)
                    return false;

                var columns = definitions.Elements(presentation + "ColumnDefinition").ToArray();
                return columns.Length == 3 &&
                       string.Equals((string?)columns[0].Attribute("Width"), "240", StringComparison.Ordinal) &&
                       string.Equals((string?)columns[1].Attribute("Width"), "*", StringComparison.Ordinal);
            });

        var workspaceColumns = workspaceGrid
            .Element(presentation + "Grid.ColumnDefinitions")!
            .Elements(presentation + "ColumnDefinition")
            .ToArray();

        Assert.Equal(
            "0",
            (string?)workspaceColumns[2].Attribute("Width"));

        Assert.DoesNotContain(
            document.Descendants(presentation + "TextBlock"),
            element => string.Equals(
                (string?)element.Attribute("Text"),
                "Propiedades",
                StringComparison.Ordinal));
    }

    [Fact]
    public void LabelContext_ReopensRightPanelThroughExistingLabelOwner()
    {
        var xamlPath = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var document = XDocument.Load(xamlPath);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var contextualColumn = document
            .Descendants(presentation + "ColumnDefinition")
            .SingleOrDefault(column =>
                string.Equals(
                    (string?)column.Attribute(x + "Name"),
                    "ContextPropertiesColumn",
                    StringComparison.Ordinal));

        Assert.NotNull(contextualColumn);
        Assert.Equal("0", (string?)contextualColumn!.Attribute("Width"));

        var labelsPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Labels.cs");
        var labelsSource = File.ReadAllText(labelsPath);

        Assert.Contains(
            "ContextPropertiesColumn.Width = new GridLength(0d);",
            labelsSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "ContextPropertiesColumn.Width = new GridLength(320d);",
            labelsSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CommentToolbar_IsHostedInsideWorkspace_AndKeepsRealOwnerActions()
    {
        var xamlPath = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var appDirectory = Path.GetDirectoryName(xamlPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");
        var shellSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(appDirectory, "MainWindow.Shell*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.Contains("Name = \"ContextToolbarHost\"", shellSource, StringComparison.Ordinal);
        Assert.Contains(
            "workspace.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });",
            shellSource,
            StringComparison.Ordinal);
        Assert.Contains("Grid.SetRow(child, Grid.GetRow(child) + 1);", shellSource, StringComparison.Ordinal);
        Assert.Contains("Grid.SetRow(host, 0);", shellSource, StringComparison.Ordinal);
        Assert.Contains("AttachCommentToolbarToContextHost();", shellSource, StringComparison.Ordinal);
        Assert.Contains("ContextToolbarHost.Children.Add(_commentToolbar);", shellSource, StringComparison.Ordinal);
        Assert.Contains("SgToolbarBorderStyle", shellSource, StringComparison.Ordinal);

        var commentsPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Comments.cs");
        var commentsSource = File.ReadAllText(commentsPath);
        Assert.Contains("CommentUndoButton", commentsSource, StringComparison.Ordinal);
        Assert.Contains("CommentRedoButton", commentsSource, StringComparison.Ordinal);
        Assert.Contains("CommentSaveAsButton", commentsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void CommentContext_UsesExistingPanelVisibilityToControlShellWidth()
    {
        var xamlPath = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var appDirectory = Path.GetDirectoryName(xamlPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");
        var shellSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(appDirectory, "MainWindow.Shell*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.Contains("AttachCommentPropertiesToContextColumn();", shellSource, StringComparison.Ordinal);
        Assert.Contains(
            "_commentPropertiesPanel.IsVisibleChanged += CommentPropertiesPanel_IsVisibleChanged;",
            shellSource,
            StringComparison.Ordinal);
        Assert.Contains("SyncContextPropertiesColumn();", shellSource, StringComparison.Ordinal);
        Assert.Contains("ContextPropertiesColumn.Width =", shellSource, StringComparison.Ordinal);
        Assert.Contains("new GridLength(320d)", shellSource, StringComparison.Ordinal);
        Assert.Contains("new GridLength(0d)", shellSource, StringComparison.Ordinal);

        var commentsPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Comments.cs");
        var commentsSource = File.ReadAllText(commentsPath);
        Assert.Contains(
            "_commentPropertiesPanel!.Visibility = Visibility.Visible;",
            commentsSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "_commentPropertiesPanel.Visibility = Visibility.Collapsed;",
            commentsSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SignatureContext_UsesExistingPanelVisibilityToControlShellWidth()
    {
        var xamlPath = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var appDirectory = Path.GetDirectoryName(xamlPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");
        var shellSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(appDirectory, "MainWindow.Shell*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.Contains("AttachSignaturePropertiesToContextColumn();", shellSource, StringComparison.Ordinal);
        Assert.Contains(
            "_signaturePropertiesPanel.IsVisibleChanged += SignaturePropertiesPanel_IsVisibleChanged;",
            shellSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "_signaturePropertiesPanel?.Visibility == Visibility.Visible",
            shellSource,
            StringComparison.Ordinal);

        var signPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Sign.cs");
        var signSource = File.ReadAllText(signPath);
        Assert.Contains(
            "_signaturePropertiesPanel!.Visibility = Visibility.Visible;",
            signSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "_signaturePropertiesPanel.Visibility = Visibility.Collapsed;",
            signSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EditTextContext_UsesRealScrollVisibilityToControlShellWidth()
    {
        var xamlPath = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var appDirectory = Path.GetDirectoryName(xamlPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");
        var shellSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(appDirectory, "MainWindow.Shell*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.Contains("AttachEditTextPropertiesToContextColumn();", shellSource, StringComparison.Ordinal);
        Assert.Contains(
            "_editTextPropertiesScrollViewer.IsVisibleChanged += EditTextPropertiesScrollViewer_IsVisibleChanged;",
            shellSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "_editTextPropertiesScrollViewer?.Visibility == Visibility.Visible",
            shellSource,
            StringComparison.Ordinal);

        var editTextPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Edit.Text.cs");
        var editTextSource = File.ReadAllText(editTextPath);
        Assert.Contains(
            "_editTextPropertiesScrollViewer.Visibility = Visibility.Visible;",
            editTextSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "_editTextPropertiesScrollViewer.Visibility = Visibility.Collapsed;",
            editTextSource,
            StringComparison.Ordinal);
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
