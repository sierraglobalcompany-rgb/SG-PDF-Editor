using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ShellThemeContractTests
{
    private static readonly string[] RequiredKeys =
    {
        "SgHomeCardStyle",
        "SgRailItemStyle",
        "SgRailItemSelectedStyle",
        "SgSecondaryButtonStyle",
        "SgShellIconTileStyle",
        "SgContextPanelStyle",
        "SgContextSectionStyle",
        "SgTitleTextStyle",
        "SgSubtitleTextStyle"
    };

    [Fact]
    public void ModernTheme_DeclaresApprovedShellPrimitives()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Themes", "Modern.xaml");
        var document = XDocument.Load(path);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = document
            .Descendants()
            .Attributes(x + "Key")
            .Select(attribute => attribute.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var key in RequiredKeys)
            Assert.Contains(key, keys);
    }

    [Fact]
    public void ModernTheme_DoesNotIntroduceHardCodedHexColors()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Themes", "Modern.xaml");
        var xaml = File.ReadAllText(path);

        Assert.DoesNotMatch(new Regex(@"#[0-9A-Fa-f]{6,8}\b", RegexOptions.CultureInvariant), xaml);
    }

    [Fact]
    public void IconResources_CoverEveryCatalogIconKey_AndAreMergedOnce()
    {
        var catalogPath = FindRepoFile("src", "SGPdf.App", "Shell", "ToolCatalog.cs");
        var catalogSource = File.ReadAllText(catalogPath);
        var catalogIconKeys = Regex.Matches(
                catalogSource,
                "\"(?<key>ShellIcon[A-Za-z0-9]+)\"",
                RegexOptions.CultureInvariant)
            .Cast<Match>()
            .Select(match => match.Groups["key"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(catalogIconKeys);

        var appPath = FindRepoFile("src", "SGPdf.App", "App.xaml");
        var shellIconsPath = Path.Combine(
            Path.GetDirectoryName(appPath)!,
            "Themes",
            "ShellIcons.xaml");

        Assert.True(
            File.Exists(shellIconsPath),
            "C3 visual contract: Themes/ShellIcons.xaml must materialize the ToolCatalog IconKeys.");

        var iconDocument = XDocument.Load(shellIconsPath);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var iconResourceKeys = iconDocument
            .Descendants()
            .Attributes(x + "Key")
            .Select(attribute => attribute.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var iconKey in catalogIconKeys)
            Assert.Contains(iconKey, iconResourceKeys);

        var appDocument = XDocument.Load(appPath);
        var mergedSources = appDocument
            .Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Select(element => (string?)element.Attribute("Source"))
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .ToArray();

        Assert.Equal(
            1,
            mergedSources.Count(source => string.Equals(
                source,
                "Themes/ShellIcons.xaml",
                StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void HomeDashboard_ConsumesIconKey_WithoutGenericPdfPlaceholder()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Shell", "HomeDashboard.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var genericPdfPlaceholders = document
            .Descendants(presentation + "TextBlock")
            .Where(element => string.Equals(
                (string?)element.Attribute("Text"),
                "PDF",
                StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(genericPdfPlaceholders);

        var iconKeyBindings = document
            .Descendants()
            .Attributes()
            .Where(attribute => attribute.Value.Contains("Binding IconKey", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(iconKeyBindings);
    }

    [Fact]
    public void HomeRail_UsesIconAndVisibleText_AndKeepsFutureSettingsHonest()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Shell", "HomeDashboard.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var labels = new[] { "Inicio", "Herramientas", "Recientes", "Configuración" };

        foreach (var label in labels)
        {
            var button = document
                .Descendants(presentation + "Button")
                .Single(element => element.Attributes().Any(attribute =>
                    attribute.Name.LocalName == "AutomationProperties.Name" &&
                    string.Equals(attribute.Value, label, StringComparison.Ordinal)));

            Assert.True(
                button.Descendants(presentation + "TextBlock").Any(text =>
                    string.Equals((string?)text.Attribute("Text"), label, StringComparison.Ordinal)),
                $"C3 visual contract: Home rail '{label}' must keep visible text next to its icon.");

            Assert.True(
                button.Descendants(presentation + "Path").Any(icon =>
                    icon.Attributes().Any(attribute =>
                        attribute.Name.LocalName == "Data" &&
                        attribute.Value.Contains("ShellIcon", StringComparison.Ordinal))),
                $"C3 visual contract: Home rail '{label}' must consume a ShellIcon resource.");
        }

        var selected = document
            .Descendants(presentation + "Button")
            .Single(element => element.Attributes().Any(attribute =>
                attribute.Name.LocalName == "AutomationProperties.Name" &&
                attribute.Value == "Inicio"));
        Assert.Contains("SgRailItemSelectedStyle", (string?)selected.Attribute("Style") ?? string.Empty);

        var settings = document
            .Descendants(presentation + "Button")
            .Single(element => element.Attributes().Any(attribute =>
                attribute.Name.LocalName == "AutomationProperties.Name" &&
                attribute.Value == "Configuración"));
        Assert.Equal("False", (string?)settings.Attribute("IsEnabled"));
        Assert.False(string.IsNullOrWhiteSpace((string?)settings.Attribute("ToolTip")));
    }

    [Fact]
    public void SelectedRailStyle_HasStructuralCueBeyondColor()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Themes", "Modern.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var selectedStyle = document
            .Descendants(presentation + "Style")
            .Single(style => (string?)style.Attribute(x + "Key") == "SgRailItemSelectedStyle");
        var setters = selectedStyle
            .Elements(presentation + "Setter")
            .Select(setter => new
            {
                Property = (string?)setter.Attribute("Property"),
                Value = (string?)setter.Attribute("Value")
            })
            .ToArray();

        Assert.True(
            setters.Any(setter => setter.Property == "BorderThickness" && setter.Value is not null && setter.Value != "0") ||
            setters.Any(setter => setter.Property == "FontWeight" && setter.Value == "SemiBold"),
            "Selected rail state must expose a structural cue beyond color alone.");
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
