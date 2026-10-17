using Xunit;

namespace SGPdf.App.Tests;

public sealed class CryptographicSigningShellContractTests
{
    [Fact]
    public void CryptographicSigning_FindsFileMenuThroughOpenPdfOwnerAfterTopShellReparenting()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Signing.cs"));

        Assert.Contains("ItemsControl.ItemsControlFromItemContainer(OpenPdfMenuItem)", source);
        Assert.DoesNotContain("root.Children.OfType<Menu>().FirstOrDefault()", source);
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
