using Xunit;

namespace SGPdf.App.Tests;

public sealed class LegacyNavigationParityContractTests
{
    [Fact]
    public void EditNavigation_UsesDocumentRailOwner_WithoutLegacyModeButton()
    {
        var shellPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs");
        var shellSource = File.ReadAllText(shellPath);

        Assert.Contains("case \"edit\":", shellSource, StringComparison.Ordinal);
        Assert.Contains("await TryEnterEditModeAsync();", shellSource, StringComparison.Ordinal);
        Assert.Contains(
            "!string.Equals(actionId, \"edit\", StringComparison.Ordinal)",
            shellSource,
            StringComparison.Ordinal);
        Assert.Contains("TryLeaveEditModeWithGuard()", shellSource, StringComparison.Ordinal);

        var editPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Edit.cs");
        var editSource = File.ReadAllText(editPath);

        Assert.DoesNotContain("_editModeButton", editSource, StringComparison.Ordinal);
        Assert.DoesNotContain("EditModeButton", editSource, StringComparison.Ordinal);
        Assert.DoesNotContain("EditMode_Click", editSource, StringComparison.Ordinal);

        Assert.Contains("EditSaveAsButton", editSource, StringComparison.Ordinal);
        Assert.Contains("EditSaveAs_Click", editSource, StringComparison.Ordinal);
        Assert.Contains("TrySaveEditWorkspace()", editSource, StringComparison.Ordinal);
    }

    [Fact]
    public void CommentNavigation_UsesDocumentRailOwner_WithoutLegacyModeButton()
    {
        var shellPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs");
        var shellSource = File.ReadAllText(shellPath);

        Assert.Contains("case \"comment\":", shellSource, StringComparison.Ordinal);
        Assert.Contains("TryEnterCommentMode();", shellSource, StringComparison.Ordinal);
        Assert.Contains(
            "!string.Equals(actionId, \"comment\", StringComparison.Ordinal)",
            shellSource,
            StringComparison.Ordinal);
        Assert.Contains("TryLeaveCommentModeWithGuard()", shellSource, StringComparison.Ordinal);

        var commentPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Comments.cs");
        var commentSource = File.ReadAllText(commentPath);

        Assert.DoesNotContain("_commentModeButton", commentSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CommentModeButton", commentSource, StringComparison.Ordinal);

        Assert.Contains("CommentToolbar", commentSource, StringComparison.Ordinal);
        Assert.Contains("CommentSaveAsButton", commentSource, StringComparison.Ordinal);
        Assert.Contains("TrySaveCommentWorkspace()", commentSource, StringComparison.Ordinal);
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