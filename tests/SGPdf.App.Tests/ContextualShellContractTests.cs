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
