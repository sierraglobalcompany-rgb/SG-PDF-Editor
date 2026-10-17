using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfOpenRoutePendingStateContractTests
{
    [Fact]
    public void CtrlO_OpensAnotherPdfWithoutForcingPendingModeDiscard()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.ReaderShortcuts.cs"));

        Assert.Contains("_openPdfShortcutAction();", source);
        Assert.DoesNotContain("TryLeaveEditModeWithGuard()", source);
        Assert.DoesNotContain("TryLeaveOrganizeModeWithGuard()", source);
        Assert.DoesNotContain("SignatureGuardReason.OpenPdf", source);
    }

    [Fact]
    public void OpenPdfMenu_IsNotADiscardGuard_ButOpenZplRemainsGuarded()
    {
        var editSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Edit.Hardening.cs"));
        Assert.Contains("OpenZplMenuItem", editSource);
        Assert.DoesNotContain("ReferenceEquals(menuItem, window.OpenPdfMenuItem)", editSource);

        var commentSource = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Comments.cs"));
        Assert.Contains("OpenZplMenuItem", commentSource);
        Assert.DoesNotContain("ReferenceEquals(menuItem, window.OpenPdfMenuItem)", commentSource);
    }

    [Fact]
    public void CommentPdfOpenRoutes_DoNotRunLegacySingleDocumentDiscardGuards()
    {
        var integrationPath = TryFindRepoFile("src", "SGPdf.App", "MainWindow.Comments.Integration.cs");
        if (integrationPath is null)
            return;

        var source = File.ReadAllText(integrationPath);
        Assert.DoesNotContain("CommentShortcutGuard", source);
        Assert.DoesNotContain("CommentRecentGuard", source);
        Assert.DoesNotContain("TryLeaveCommentModeWithGuard()", source);
    }

    private static string FindRepoFile(params string[] segments)
        => TryFindRepoFile(segments)
           ?? throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");

    private static string? TryFindRepoFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
