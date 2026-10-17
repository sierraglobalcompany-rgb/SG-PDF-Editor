using Xunit;

namespace SGPdf.App.Tests;

public sealed class FormsShellContractTests
{
    [Fact]
    public void Forms_FindsEditMenuThroughStableNamedOwnerAfterTopShellReparenting()
    {
        var xaml = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.xaml"));
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Forms.cs"));

        Assert.Contains("x:Name=\"EditMenuItem\"", xaml);
        Assert.Contains("EditMenuItem", source);
        Assert.DoesNotContain("root.Children.OfType<Menu>().FirstOrDefault()", source);
        Assert.DoesNotContain("string.Equals(item.Header?.ToString(), \"_Editar\"", source);
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
