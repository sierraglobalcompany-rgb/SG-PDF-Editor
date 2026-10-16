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

    [Fact]
    public void ReadAndSignNavigation_UseDocumentRailOwners_WithoutLegacyModeButtons()
    {
        var shellPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs");
        var shellSource = File.ReadAllText(shellPath);

        Assert.Contains("case \"read\":", shellSource, StringComparison.Ordinal);
        Assert.Contains("await TryEnterReadModeAsync();", shellSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadMode_Click(this, new RoutedEventArgs())", shellSource, StringComparison.Ordinal);
        Assert.Contains("case \"sign\":", shellSource, StringComparison.Ordinal);
        Assert.Contains("await TryEnterSignModeAsync();", shellSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SignMode_Click(this, new RoutedEventArgs())", shellSource, StringComparison.Ordinal);

        var signPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Sign.cs");
        var signSource = File.ReadAllText(signPath);

        Assert.Contains("private async Task<bool> TryEnterReadModeAsync()", signSource, StringComparison.Ordinal);
        Assert.Contains("private async Task<bool> TryEnterSignModeAsync()", signSource, StringComparison.Ordinal);
        Assert.Contains("RestoreReaderAfterSignModeAsync()", signSource, StringComparison.Ordinal);
        Assert.Contains("PrepareReaderForSignModeAsync()", signSource, StringComparison.Ordinal);
        Assert.Contains("CloseReaderFindBar();", signSource, StringComparison.Ordinal);
        Assert.Contains("ClearReaderTextSelection();", signSource, StringComparison.Ordinal);

        Assert.DoesNotContain("_readModeButton", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_signModeButton", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadModeButton", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SignModeButton", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadMode_Click", signSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SignMode_Click", signSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ReaderLifecycleAndTools_DoNotDependOnLegacyModeButtonEvents()
    {
        var readerPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Reader.cs");
        var readerSource = File.ReadAllText(readerPath);

        Assert.Contains("private async Task<bool> PrepareReaderForSignModeAsync()", readerSource, StringComparison.Ordinal);
        Assert.Contains("await EnsureEditSurfaceForCurrentPageAsync();", readerSource, StringComparison.Ordinal);
        Assert.Contains("private async Task RestoreReaderAfterSignModeAsync()", readerSource, StringComparison.Ordinal);
        Assert.Contains("ShowContinuousReaderSurface();", readerSource, StringComparison.Ordinal);
        Assert.Contains("await RefreshReaderRenderWindowAsync();", readerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReaderSignModeAfterClick", readerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReaderReadModeAfterClick", readerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_signModeButton", readerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_readModeButton", readerSource, StringComparison.Ordinal);

        var searchPath = FindRepoFile("src", "SGPdf.App", "MainWindow.ReaderSearch.cs");
        var searchSource = File.ReadAllText(searchPath);
        Assert.DoesNotContain("_signModeButton", searchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReaderSearchSignMode_Click", searchSource, StringComparison.Ordinal);
        Assert.Contains("CloseReaderFindBar()", searchSource, StringComparison.Ordinal);

        var selectionPath = FindRepoFile("src", "SGPdf.App", "MainWindow.ReaderSelection.cs");
        var selectionSource = File.ReadAllText(selectionPath);
        Assert.DoesNotContain("_signModeButton", selectionSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReaderSelectionSignMode_Click", selectionSource, StringComparison.Ordinal);
        Assert.Contains("ClearReaderTextSelection()", selectionSource, StringComparison.Ordinal);
    }

    [Fact]
    public void OrganizeNavigation_UsesOwner_WithoutLegacyButtonOrDuplicateRefresh()
    {
        var shellPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Shell.cs");
        var shellSource = File.ReadAllText(shellPath);

        Assert.Contains("case \"organize\":", shellSource, StringComparison.Ordinal);
        Assert.Contains("TryEnterOrganizeMode();", shellSource, StringComparison.Ordinal);
        Assert.DoesNotContain("await RefreshOrganizeThumbnailRenderWindowAsync();", shellSource, StringComparison.Ordinal);
        Assert.Contains(
            "!string.Equals(actionId, \"organize\", StringComparison.Ordinal)",
            shellSource,
            StringComparison.Ordinal);
        Assert.Contains("TryLeaveOrganizeModeWithGuard()", shellSource, StringComparison.Ordinal);

        var organizePath = FindRepoFile("src", "SGPdf.App", "MainWindow.Organize.cs");
        var organizeSource = File.ReadAllText(organizePath);

        Assert.DoesNotContain("_organizeModeButton", organizeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizeModeButton", organizeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizeMode_Click", organizeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("FindModePanel", organizeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ExistingModeButton_PreviewMouseLeftButtonDown", organizeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_readModeButton", organizeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_signModeButton", organizeSource, StringComparison.Ordinal);
        Assert.Contains("QueueOrganizeThumbnailRefresh();", organizeSource, StringComparison.Ordinal);
        Assert.Contains("OrganizeSaveAsButton", organizeSource, StringComparison.Ordinal);

        var task7WiringPath = FindRepoFile("src", "SGPdf.App", "MainWindow.Organize.Task7Wiring.cs");
        var task7WiringSource = File.ReadAllText(task7WiringPath);
        Assert.Contains("TryInsertOrganizePdf();", task7WiringSource, StringComparison.Ordinal);
        Assert.Contains("TryMergeOrganizePdf();", task7WiringSource, StringComparison.Ordinal);
        Assert.Contains("UpdateOrganizeTask7CommandAvailability();", task7WiringSource, StringComparison.Ordinal);
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
