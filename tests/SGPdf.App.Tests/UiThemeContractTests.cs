using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class UiThemeContractTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void AppXaml_UsesSystemFluentTheme()
    {
        var document = XDocument.Load(ProjectPath("src", "SGPdf.App", "App.xaml"));
        var root = Assert.IsType<XElement>(document.Root);

        Assert.Equal("System", (string?)root.Attribute("ThemeMode"));
    }

    [Fact]
    public void AppXaml_MergesModernThemeDictionary()
    {
        var document = XDocument.Load(ProjectPath("src", "SGPdf.App", "App.xaml"));

        var mergedSources = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Select(element => (string?)element.Attribute("Source"))
            .Where(source => !string.IsNullOrWhiteSpace(source));

        Assert.Contains("Themes/Modern.xaml", mergedSources);
    }

    [Fact]
    public void ModernTheme_DefinesRequiredVisualResources()
    {
        var themePath = ProjectPath("src", "SGPdf.App", "Themes", "Modern.xaml");
        Assert.True(File.Exists(themePath), $"Missing theme dictionary: {themePath}");

        var document = XDocument.Load(themePath);
        var keys = document
            .Descendants()
            .Attributes(Xaml + "Key")
            .Select(attribute => attribute.Value)
            .ToHashSet(StringComparer.Ordinal);

        var requiredKeys = new[]
        {
            "SgSurfaceBrush",
            "SgPanelBrush",
            "SgCanvasBrush",
            "SgBorderBrush",
            "SgAccentBrush",
            "SgPrimaryButtonStyle",
            "SgToolbarButtonStyle",
            "SgIconButtonStyle",
            "SgSectionHeaderStyle"
        };

        foreach (var requiredKey in requiredKeys)
        {
            Assert.Contains(requiredKey, keys);
        }
    }

    [Fact]
    public void MainWindow_PreservesReaderShellStructure()
    {
        var document = LoadMainWindow();
        var root = Assert.IsType<XElement>(document.Root);
        var dock = Assert.Single(root.Elements().Where(element => element.Name.LocalName == "DockPanel"));
        var mainGrid = Assert.Single(dock.Elements().Where(IsThreeColumnGrid));

        var directBorders = mainGrid.Elements().Where(element => element.Name.LocalName == "Border").ToList();
        Assert.Contains(directBorders, border => GridColumn(border) == 0);
        Assert.Contains(directBorders, border => GridColumn(border) == 2);
    }

    [Fact]
    public void MainWindow_UsesModernShellSurfaces()
    {
        var document = LoadMainWindow();
        var root = Assert.IsType<XElement>(document.Root);
        Assert.Equal("{StaticResource SgSurfaceBrush}", (string?)root.Attribute("Background"));

        Assert.Equal(
            "{StaticResource SgToolbarBorderStyle}",
            (string?)FindNamed(document, "NavigationBar").Attribute("Style"));
        Assert.Equal(
            "{StaticResource SgToolbarBorderStyle}",
            (string?)FindNamed(document, "LabelNavigationBar").Attribute("Style"));

        var dock = Assert.Single(root.Elements().Where(element => element.Name.LocalName == "DockPanel"));
        var mainGrid = Assert.Single(dock.Elements().Where(IsThreeColumnGrid));
        var center = Assert.Single(mainGrid.Elements().Where(element =>
            element.Name.LocalName == "Grid" && GridColumn(element) == 1));
        var right = Assert.Single(mainGrid.Elements().Where(element =>
            element.Name.LocalName == "Border" && GridColumn(element) == 2));

        Assert.Equal("{StaticResource SgCanvasBrush}", (string?)center.Attribute("Background"));
        Assert.Equal("{StaticResource SgPanelBrush}", (string?)right.Attribute("Background"));
        Assert.Equal("{StaticResource SgBorderBrush}", (string?)right.Attribute("BorderBrush"));
    }

    [Fact]
    public void MainWindow_PreservesBehaviorOwnedControlNames()
    {
        var document = LoadMainWindow();
        var requiredNames = new[]
        {
            "OpenPdfMenuItem",
            "OpenZplMenuItem",
            "PrintPdfMenuItem",
            "NavigationBar",
            "PreviousPageButton",
            "PageNumberTextBox",
            "PageCountText",
            "NextPageButton",
            "ZoomOutButton",
            "ZoomPercentButton",
            "ZoomInButton",
            "FitPageButton",
            "FitWidthButton",
            "LabelNavigationBar",
            "PdfScrollViewer",
            "PdfImage",
            "LabelSheetCanvas",
            "EmptyStateText",
            "PropertiesPlaceholderText",
            "LabelPropertiesPanel",
            "StatusText"
        };

        var names = document
            .Descendants()
            .Attributes(Xaml + "Name")
            .Select(attribute => attribute.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var requiredName in requiredNames)
            Assert.Contains(requiredName, names);
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(ProjectPath("src", "SGPdf.App", "MainWindow.xaml"));

    private static XElement FindNamed(XDocument document, string name)
        => Assert.Single(document.Descendants().Where(element =>
            string.Equals((string?)element.Attribute(Xaml + "Name"), name, StringComparison.Ordinal)));

    private static bool IsThreeColumnGrid(XElement element)
        => element.Name.LocalName == "Grid" &&
           element.Elements().Any(child =>
               child.Name.LocalName == "Grid.ColumnDefinitions" &&
               child.Elements().Count(column => column.Name.LocalName == "ColumnDefinition") == 3);

    private static int GridColumn(XElement element)
    {
        var value = element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName == "Grid.Column")?.Value;
        return int.TryParse(value, out var column) ? column : 0;
    }

    private static string ProjectPath(params string[] segments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "SGPdf.slnx")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
    }
}
