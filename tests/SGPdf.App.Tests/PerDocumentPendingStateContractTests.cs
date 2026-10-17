using Xunit;

namespace SGPdf.App.Tests;

public sealed class PerDocumentPendingStateContractTests
{
    [Fact]
    public void DocumentWorkspace_OwnsPendingFeatureStateWithoutOwningWpfControls()
    {
        var pendingPath = TryFindRepoFile("src", "SGPdf.App", "Shell", "PdfDocumentPendingState.cs");
        Assert.True(pendingPath is not null,
            "R1B requires a focused per-document pending-state owner before visible tabs are enabled.");

        var source = File.ReadAllText(pendingPath!);
        Assert.Contains("ImageEditWorkspace", source);
        Assert.Contains("TextEditWorkspace", source);
        Assert.Contains("SignatureEditState", source);
        Assert.Contains("CommentWorkspace", source);
        Assert.Contains("OrganizePlan", source);
        Assert.Contains("AcroForm", source);
        Assert.Contains("IsDirty", source);

        Assert.DoesNotContain("TextBox", source);
        Assert.DoesNotContain("Canvas", source);
        Assert.DoesNotContain("FrameworkElement", source);
        Assert.DoesNotContain("Control", source);
    }

    [Fact]
    public void PdfDocumentWorkspace_ExposesOnePendingStateOwner()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "Shell", "PdfDocumentWorkspace.cs"));

        Assert.Contains("PdfDocumentPendingState PendingState", source);
        Assert.Contains("PendingState = new PdfDocumentPendingState", source);
    }

    [Fact]
    public void DocumentActivation_CapturesPreviousPendingStateAndRestoresTargetState()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));

        Assert.Contains("CaptureActiveDocumentPendingState", source);
        Assert.Contains("RestoreDocumentPendingState", source);
        Assert.Contains("CaptureActiveDocumentPendingState();", source);
        Assert.Contains("RestoreDocumentPendingState(document);", source);

        var activationStart = source.IndexOf("private async Task ActivateDocumentAsync", StringComparison.Ordinal);
        Assert.True(activationStart >= 0);
        var activation = source[activationStart..];
        Assert.DoesNotContain("_signatureEditState = null;", activation);
        Assert.DoesNotContain("ResetAcroFormUi();", activation);
    }

    [Fact]
    public void OpeningAnotherPdf_DoesNotForceDiscardExistingPendingEditOrOrganizeWork()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Reader.cs"));
        var openStart = source.IndexOf("private async Task<bool> TryOpenPdfPathAsync", StringComparison.Ordinal);
        Assert.True(openStart >= 0);
        var openFlow = source[openStart..];

        Assert.DoesNotContain("TryLeaveEditModeWithGuard()", openFlow);
        Assert.DoesNotContain("TryLeaveOrganizeModeWithGuard()", openFlow);
    }

    [Fact]
    public void ClosingOneDocument_HasExplicitDirtyGuardBeforeDisposal()
    {
        var source = File.ReadAllText(FindRepoFile("src", "SGPdf.App", "MainWindow.Documents.cs"));

        Assert.Contains("TryCloseDocumentWithGuard", source);
        Assert.Contains("_confirmCloseDirtyDocument", source);
        Assert.Contains("document.PendingState.IsDirty", source);
        Assert.Contains("UntrackOpenedDocument(document)", source);
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
