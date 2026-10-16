using Xunit;

namespace SGPdf.App.Tests;

public sealed class LegacyReadSignNavigationContractTests
{
    [Fact]
    public void ReadAndSign_UseDocumentRailWithoutLegacySharedModeStrip()
    {
        var appDirectory = FindRepoDirectory("src", "SGPdf.App");

        var shellSource = File.ReadAllText(Path.Combine(appDirectory, "MainWindow.Shell.cs"));
        Assert.Contains("TryPrepareCoreToolTransition(actionId)", shellSource, StringComparison.Ordinal);
        Assert.Contains("ReadMode_Click(this, new RoutedEventArgs());", shellSource, StringComparison.Ordinal);
        Assert.Contains("SignMode_Click(this, new RoutedEventArgs());", shellSource, StringComparison.Ordinal);

        var signSource = File.ReadAllText(Path.Combine(appDirectory, "MainWindow.Sign.cs"));
        Assert.Contains("private void ReadMode_Click(object sender, RoutedEventArgs e)", signSource, StringComparison.Ordinal);
        Assert.Contains("private void SignMode_Click(object sender, RoutedEventArgs e)", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_readModeButton", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_signModeButton", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Name = \"ReadModeButton\"", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Name = \"SignModeButton\"", signSource, StringComparison.Ordinal);

        var commentsSource = File.ReadAllText(Path.Combine(appDirectory, "MainWindow.Comments.cs"));
        Assert.DoesNotContain(
            "_readModeButton.PreviewMouseLeftButtonDown += ExistingModeAfterComment_PreviewMouseLeftButtonDown;",
            commentsSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "_signModeButton.PreviewMouseLeftButtonDown += ExistingModeAfterComment_PreviewMouseLeftButtonDown;",
            commentsSource,
            StringComparison.Ordinal);
    }

    private static string FindRepoDirectory(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException($"Repository directory not found: {Path.Combine(segments)}");
    }
}
