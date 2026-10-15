using Xunit;

namespace SGPdf.App.Tests;

public sealed class ShellTransitionOwnershipTests
{
    [Fact]
    public void ShellStateTransitions_AreExplicitAtSuccessfulDocumentCommitOwners()
    {
        var shell = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs"));
        var reader = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Reader.cs"));
        var labels = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Labels.cs"));

        Assert.DoesNotContain("OnPropertyChanged", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("TitleProperty", shell, StringComparison.Ordinal);
        Assert.Contains("SyncShellWorkspaceFromActiveDocument();", reader, StringComparison.Ordinal);
        Assert.Contains("SyncShellWorkspaceFromActiveDocument();", labels, StringComparison.Ordinal);
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
