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
