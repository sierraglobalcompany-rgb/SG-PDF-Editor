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
        var document = XDocument.Load(xamlPath);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var host = document
            .Descendants(presentation + "Grid")
            .SingleOrDefault(grid =>
                string.Equals(
                    (string?)grid.Attribute(x + "Name"),
                    "ContextToolbarHost",
                    StringComparison.Ordinal));

        Assert.NotNull(host);
        Assert.Equal("0", (string?)host!.Attribute("Grid.Row"));

        var workspace = host.Parent;
        Assert.NotNull(workspace);
        var rows = workspace!
            .Element(presentation + "Grid.RowDefinitions")?
            .Elements(presentation + "RowDefinition")
            .ToArray();
        Assert.NotNull(rows);
        Assert.Equal(2, rows!.Length);
        Assert.Equal("Auto", (string?)rows[0].Attribute("Height"));
        Assert.Equal("*", (string?)rows[1].Attribute("Height"));

        var shellPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs");
        var shellSource = File.ReadAllText(shellPath);
        Assert.Contains("AttachCommentToolbarToContextHost();", shellSource, StringComparison.Ordinal);
        Assert.Contains("ContextToolbarHost.Children.Add(_commentToolbar);", shellSource, StringComparison.Ordinal);
        Assert.Contains("SgToolbarBorderStyle", shellSource, StringComparison.Ordinal);

        var commentsPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Comments.cs");
        var commentsSource = File.ReadAllText(commentsPath);
        Assert.Contains("CommentUndoButton", commentsSource, StringComparison.Ordinal);
        Assert.Contains("CommentRedoButton", commentsSource, StringComparison.Ordinal);
        Assert.Contains("CommentSaveAsButton", commentsSource, StringComparison.Ordinal);
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
