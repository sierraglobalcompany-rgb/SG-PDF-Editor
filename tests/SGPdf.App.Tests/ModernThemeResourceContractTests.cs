using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ModernThemeResourceContractTests
{
    [Fact]
    public void SgAccentBrush_MaterializesSystemAccentColorWithoutNestedDynamicResource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "src", "SGPdf.App", "Themes", "Modern.xaml")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        var document = XDocument.Load(Path.Combine(directory.FullName, "src", "SGPdf.App", "Themes", "Modern.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var brush = Assert.Single(document.Descendants(presentation + "SolidColorBrush"),
            element => (string?)element.Attribute(xaml + "Key") == "SgAccentBrush");

        Assert.Equal("{StaticResource SystemAccentColor}", (string?)brush.Attribute("Color"));
    }
}
