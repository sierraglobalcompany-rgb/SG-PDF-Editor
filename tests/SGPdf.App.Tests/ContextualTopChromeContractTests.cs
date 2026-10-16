using Xunit;

namespace SGPdf.App.Tests;

public sealed class ContextualTopChromeContractTests
{
    [Fact]
    public void EditSaveAs_ReusesExistingOwnerActionInsideContextToolbarHost()
    {
        var appDirectory = FindRepoDirectory("src", "SGPdf.App");
        var shellSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(appDirectory, "MainWindow.Shell*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.Contains("AttachEditToolbarToContextHost();", shellSource, StringComparison.Ordinal);
        Assert.Contains("legacyParent.Children.Remove(_editSaveAsButton);", shellSource, StringComparison.Ordinal);
        Assert.Contains("ContextToolbarHost.Children.Add(_editSaveAsButton);", shellSource, StringComparison.Ordinal);
        Assert.Contains("SgToolbarButtonStyle", shellSource, StringComparison.Ordinal);

        var editSource = File.ReadAllText(Path.Combine(appDirectory, "MainWindow.Edit.cs"));
        Assert.Contains("Name = \"EditSaveAsButton\"", editSource, StringComparison.Ordinal);
        Assert.Contains("saveAsButton.Click += EditSaveAs_Click;", editSource, StringComparison.Ordinal);
        Assert.Contains("_editSaveAsButton.Visibility = _editModeActive", editSource, StringComparison.Ordinal);
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
