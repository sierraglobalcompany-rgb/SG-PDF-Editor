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
