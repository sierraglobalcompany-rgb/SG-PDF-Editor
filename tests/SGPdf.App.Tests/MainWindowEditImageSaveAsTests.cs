using SGPdf.App.Features.Edit;
using System.Windows;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditImageSaveAsTests
{
    [Fact]
    public void EditMode_ExposesSaveAsCommandIndependentOfSelection()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                OrganizeWindowTestHost.Invoke(window, "HandleImageEditEscape");

                var button = Assert.IsType<System.Windows.Controls.Button>(window.FindName("EditSaveAsButton"));
                Assert.Equal("Guardar como...", button.Content);
                Assert.Equal(Visibility.Visible, button.Visibility);
                Assert.True(button.IsEnabled);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void CleanPreflight_SaveSucceedsWithoutWarningAndMarksBaselineOnlyAfterWriter()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await DirtyWorkspaceAsync(window, fixture.Path);
                var confirmCalls = 0;
                var writerCalls = 0;
                var dirtyDuringWriter = false;
                var destination = Path.Combine(fixture.DirectoryPath, "saved.pdf");

                OrganizeWindowTestHost.SetField(window, "_selectEditPdfDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(window, "_inspectEditPreflight", (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) => EmptyResult()));
                OrganizeWindowTestHost.SetField(window, "_confirmEditWarnings", (Func<PdfEditPreflightResult, bool>)(_ => { confirmCalls++; return true; }));
                OrganizeWindowTestHost.SetField(window, "_saveImageEditCopy", (Action<ImageEditWorkspace, string, bool, CancellationToken>)((workspace, path, confirmed, _) =>
                {
                    writerCalls++;
                    dirtyDuringWriter = workspace.IsDirty;
                    Assert.Equal(destination, path);
                    Assert.False(confirmed);
                }));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);

                Assert.Equal(0, confirmCalls);
                Assert.Equal(1, writerCalls);
                Assert.True(dirtyDuringWriter);
                Assert.False(prepared.Workspace.IsDirty);
                Assert.False(prepared.Workspace.CanUndo);
                Assert.False(prepared.Workspace.CanRedo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Warning_SaveRequiresExactlyOneConfirmationAndPassesConfirmationToWriter()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await DirtyWorkspaceAsync(window, fixture.Path);
                var confirmCalls = 0;
                var writerCalls = 0;
                var confirmedAtWriter = false;

                SetDestination(window, fixture.DirectoryPath);
                SetPreflight(window, WarningResult());
                OrganizeWindowTestHost.SetField(window, "_confirmEditWarnings", (Func<PdfEditPreflightResult, bool>)(_ => { confirmCalls++; return true; }));
                OrganizeWindowTestHost.SetField(window, "_saveImageEditCopy", (Action<ImageEditWorkspace, string, bool, CancellationToken>)((_, _, confirmed, _) =>
                {
                    writerCalls++;
                    confirmedAtWriter = confirmed;
                }));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);

                Assert.Equal(1, confirmCalls);
                Assert.Equal(1, writerCalls);
                Assert.True(confirmedAtWriter);
                Assert.False(prepared.Workspace.IsDirty);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void WarningDeclined_InvokesWriterZeroTimesAndPreservesDirtyHistory()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await DirtyWorkspaceAsync(window, fixture.Path);
                var confirmCalls = 0;
                var writerCalls = 0;

                SetDestination(window, fixture.DirectoryPath);
                SetPreflight(window, WarningResult());
                OrganizeWindowTestHost.SetField(window, "_confirmEditWarnings", (Func<PdfEditPreflightResult, bool>)(_ => { confirmCalls++; return false; }));
                OrganizeWindowTestHost.SetField(window, "_saveImageEditCopy", (Action<ImageEditWorkspace, string, bool, CancellationToken>)((_, _, _, _) => writerCalls++));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);

                Assert.Equal(1, confirmCalls);
                Assert.Equal(0, writerCalls);
                Assert.True(prepared.Workspace.IsDirty);
                Assert.True(prepared.Workspace.CanUndo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Block_InvokesNeitherWarningNorWriterAndPreservesDirtyHistory()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await DirtyWorkspaceAsync(window, fixture.Path);
                var confirmCalls = 0;
                var writerCalls = 0;

                SetDestination(window, fixture.DirectoryPath);
                SetPreflight(window, BlockResult());
                OrganizeWindowTestHost.SetField(window, "_confirmEditWarnings", (Func<PdfEditPreflightResult, bool>)(_ => { confirmCalls++; return true; }));
                OrganizeWindowTestHost.SetField(window, "_saveImageEditCopy", (Action<ImageEditWorkspace, string, bool, CancellationToken>)((_, _, _, _) => writerCalls++));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);

                Assert.Equal(0, confirmCalls);
                Assert.Equal(0, writerCalls);
                Assert.True(prepared.Workspace.IsDirty);
                Assert.True(prepared.Workspace.CanUndo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DestinationCancel_InvokesNoPreflightOrWriterAndPreservesDirtyHistory()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await DirtyWorkspaceAsync(window, fixture.Path);
                var preflightCalls = 0;
                var writerCalls = 0;

                OrganizeWindowTestHost.SetField(window, "_selectEditPdfDestination", (Func<string?>)(() => null));
                OrganizeWindowTestHost.SetField(window, "_inspectEditPreflight", (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) => { preflightCalls++; return EmptyResult(); }));
                OrganizeWindowTestHost.SetField(window, "_saveImageEditCopy", (Action<ImageEditWorkspace, string, bool, CancellationToken>)((_, _, _, _) => writerCalls++));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);

                Assert.Equal(0, preflightCalls);
                Assert.Equal(0, writerCalls);
                Assert.True(prepared.Workspace.IsDirty);
                Assert.True(prepared.Workspace.CanUndo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void WriterFailure_PreservesDirtyHistoryAndReleasesMaterializingGuard()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await DirtyWorkspaceAsync(window, fixture.Path);
                SetDestination(window, fixture.DirectoryPath);
                SetPreflight(window, EmptyResult());
                OrganizeWindowTestHost.SetField(window, "_saveImageEditCopy", (Action<ImageEditWorkspace, string, bool, CancellationToken>)((_, _, _, _) => throw new IOException("forced save failure")));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);

                Assert.True(prepared.Workspace.IsDirty);
                Assert.True(prepared.Workspace.CanUndo);
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_editMaterializing")!);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void MaterializingGuard_RejectsSecondSaveBeforeDestinationDialog()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                await DirtyWorkspaceAsync(window, fixture.Path);
                var destinationCalls = 0;
                OrganizeWindowTestHost.SetField(window, "_selectEditPdfDestination", (Func<string?>)(() => { destinationCalls++; return "unused.pdf"; }));
                OrganizeWindowTestHost.SetField(window, "_editMaterializing", true);

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);
                Assert.Equal(0, destinationCalls);
            }
            finally
            {
                OrganizeWindowTestHost.SetField(window, "_editMaterializing", false);
                ImageEditCommandTestHost.CloseClean(window);
            }
        });
    }

    private static async Task<(PdfImageObjectInfo Info, ImageObjectKey Key, ImageEditWorkspace Workspace, PdfPageDeviceTransform Transform)> DirtyWorkspaceAsync(
        MainWindow window,
        string path)
    {
        var prepared = await ImageEditCommandTestHost.PrepareAsync(window, path);
        Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 5d)!);
        Assert.True(prepared.Workspace.IsDirty);
        Assert.True(prepared.Workspace.CanUndo);
        return prepared;
    }

    private static void SetDestination(MainWindow window, string directory)
        => OrganizeWindowTestHost.SetField(
            window,
            "_selectEditPdfDestination",
            (Func<string?>)(() => Path.Combine(directory, "saved.pdf")));

    private static void SetPreflight(MainWindow window, PdfEditPreflightResult result)
        => OrganizeWindowTestHost.SetField(
            window,
            "_inspectEditPreflight",
            (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) => result));

    private static PdfEditPreflightResult EmptyResult()
        => new(Array.Empty<PdfEditFinding>());

    private static PdfEditPreflightResult WarningResult()
        => new(new[]
        {
            new PdfEditFinding(
                PdfEditFindingKind.Metadata,
                PdfEditFindingSeverity.Warning,
                "synthetic warning",
                PdfEditPreservationStatus.Unknown)
        });

    private static PdfEditPreflightResult BlockResult()
        => new(new[]
        {
            new PdfEditFinding(
                PdfEditFindingKind.CryptographicSignature,
                PdfEditFindingSeverity.Block,
                "synthetic block",
                PdfEditPreservationStatus.Unknown)
        });
}
