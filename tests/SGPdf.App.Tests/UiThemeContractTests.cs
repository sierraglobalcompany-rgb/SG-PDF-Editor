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
            "SgAccentBrush",
            "SgPrimaryButtonStyle",
            "SgToolbarButtonStyle",
            "SgIconButtonStyle",
            "SgSectionHeaderStyle"
        };

        foreach (var requiredKey in requiredKeys)
            Assert.Contains(requiredKey, keys);
    }

    [Fact]
    public void MainWindow_PreservesReaderShellStructure()
    {
        var document = LoadMainWindow();
        var root = Assert.IsType<XElement>(document.Root);
        var dock = Assert.Single(root.Elements(), element => element.Name.LocalName == "DockPanel");
        var mainGrid = Assert.Single(dock.Elements(), IsThreeColumnGrid);

        var directBorders = mainGrid.Elements().Where(element => element.Name.LocalName == "Border").ToList();
        Assert.Contains(directBorders, border => GridColumn(border) == 0);
        Assert.Contains(directBorders, border => GridColumn(border) == 2);
    }

    [Fact]
    public void MainWindow_UsesModernShellSurfaces()
    {
        var document = LoadMainWindow();
        var root = Assert.IsType<XElement>(document.Root);
        AssertSharedResource(root, "Background", "ApplicationBackgroundBrush");

        AssertSharedResource(FindNamed(document, "NavigationBar"), "Style", "SgToolbarBorderStyle");
        AssertSharedResource(FindNamed(document, "LabelNavigationBar"), "Style", "SgToolbarBorderStyle");

        var dock = Assert.Single(root.Elements(), element => element.Name.LocalName == "DockPanel");
        var mainGrid = Assert.Single(dock.Elements(), IsThreeColumnGrid);
        var center = Assert.Single(mainGrid.Elements(), element =>
            element.Name.LocalName == "Grid" && GridColumn(element) == 1);
        var right = Assert.Single(mainGrid.Elements(), element =>
            element.Name.LocalName == "Border" && GridColumn(element) == 2);

        AssertSharedResource(center, "Background", "LayerFillColorDefaultBrush");
        AssertSharedResource(right, "Background", "CardBackgroundFillColorDefaultBrush");
        AssertSharedResource(right, "BorderBrush", "CardStrokeColorDefaultBrush");
    }

    [Fact]
    public void MainWindow_DefersAppLevelThemeResources()
    {
        var document = LoadMainWindow();
        var root = Assert.IsType<XElement>(document.Root);
        var dock = Assert.Single(root.Elements(), element => element.Name.LocalName == "DockPanel");
        var mainGrid = Assert.Single(dock.Elements(), IsThreeColumnGrid);
        var center = Assert.Single(mainGrid.Elements(), element =>
            element.Name.LocalName == "Grid" && GridColumn(element) == 1);
        var right = Assert.Single(mainGrid.Elements(), element =>
            element.Name.LocalName == "Border" && GridColumn(element) == 2);

        AssertDeferredResource(root, "Background", "ApplicationBackgroundBrush");
        AssertDeferredResource(FindNamed(document, "NavigationBar"), "Style", "SgToolbarBorderStyle");
        AssertDeferredResource(FindNamed(document, "LabelNavigationBar"), "Style", "SgToolbarBorderStyle");
        AssertDeferredResource(center, "Background", "LayerFillColorDefaultBrush");
        AssertDeferredResource(right, "Background", "CardBackgroundFillColorDefaultBrush");
        AssertDeferredResource(right, "BorderBrush", "CardStrokeColorDefaultBrush");
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

    [Theory]
    [InlineData("PdfPasswordDialog.xaml", "PdfPasswordOkButton")]
    [InlineData("SignatureDrawDialog.xaml", "SignatureDrawApplyButton")]
    [InlineData("SignatureLibraryDialog.xaml", "SignatureLibraryUseButton")]
    [InlineData("SignatureNameDialog.xaml", "SignatureNameAcceptButton")]
    [InlineData("SignaturePhotoDialog.xaml", "SignaturePhotoApplyButton")]
    public void Dialog_UsesSharedSurfaceAndButtonHierarchy(string fileName, string primaryButtonName)
    {
        var document = XDocument.Load(ProjectPath("src", "SGPdf.App", fileName));
        var root = Assert.IsType<XElement>(document.Root);
        AssertDeferredResource(root, "Background", "ApplicationBackgroundBrush");

        var buttons = document.Descendants().Where(element => element.Name.LocalName == "Button").ToList();
        Assert.NotEmpty(buttons);

        foreach (var button in buttons)
        {
            var name = (string?)button.Attribute(Xaml + "Name");
            var expectedStyle = string.Equals(name, primaryButtonName, StringComparison.Ordinal)
                ? "SgPrimaryButtonStyle"
                : "SgToolbarButtonStyle";
            AssertDeferredResource(button, "Style", expectedStyle);
        }
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(ProjectPath("src", "SGPdf.App", "MainWindow.xaml"));

    private static XElement FindNamed(XDocument document, string name)
        => Assert.Single(document.Descendants(), element =>
            string.Equals((string?)element.Attribute(Xaml + "Name"), name, StringComparison.Ordinal));

    private static void AssertSharedResource(XElement element, string attributeName, string resourceKey)
    {
        var value = (string?)element.Attribute(attributeName);
        var staticValue = $"{{StaticResource {resourceKey}}}";
        var dynamicValue = $"{{DynamicResource {resourceKey}}}";
        Assert.True(
            string.Equals(value, staticValue, StringComparison.Ordinal) ||
            string.Equals(value, dynamicValue, StringComparison.Ordinal),
            $"Expected {attributeName} to use shared resource {resourceKey}, but was '{value}'.");
    }

    private static void AssertDeferredResource(XElement element, string attributeName, string resourceKey)
        => Assert.Equal($"{{DynamicResource {resourceKey}}}", (string?)element.Attribute(attributeName));

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
            current = current.Parent;

        Assert.NotNull(current);
        return Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
    }
}
