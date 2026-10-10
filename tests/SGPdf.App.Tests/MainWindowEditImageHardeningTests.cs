using System.ComponentModel;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditImageHardeningTests
{
    [Fact]
    public void DirtyImageEdit_OpenPdfCancel_PreservesCurrentSessionAndWorkspace()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var first = ImageEditPdfFixtureFactory.CreateSingleImage();
            using var second = ImageEditPdfFixtureFactory.CreateMultipleImages();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, first.Path);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 10d)!);
                Assert.True(prepared.Workspace.IsDirty);
                var originalSession = OrganizeWindowTestHost.GetField(window, "_session");
                var originalWorkspace = OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace");
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardImageEditChanges", (Func<bool>)(() => false));

                Assert.False(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryOpenPdfPathAsync", second.Path, null));

                Assert.Same(originalSession, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Same(originalWorkspace, OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
                Assert.True(prepared.Workspace.IsDirty);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtyImageEdit_ModeSwitchCancel_KeepsEditState()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 5d)!);
                var workspace = OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace");
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardImageEditChanges", (Func<bool>)(() => false));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryLeaveImageEditModeWithGuard")!);

                Assert.Same(workspace, OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
                Assert.True(prepared.Workspace.IsDirty);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtyImageEdit_DiscardProceedsAndDropsOnlyInMemoryWorkspace()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var session = Assert.IsType<PdfDocumentSession>(OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 5d)!);
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardImageEditChanges", (Func<bool>)(() => true));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryLeaveImageEditModeWithGuard")!);

                Assert.Null(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
                Assert.Single(session.GetImageObjects(0));
                Assert.False(prepared.Workspace.SourceOpenedWithPassword);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void CleanImageEdit_LeaveDoesNotPrompt()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var prompts = 0;
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardImageEditChanges", (Func<bool>)(() =>
                {
                    prompts++;
                    return false;
                }));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryLeaveImageEditModeWithGuard")!);
                Assert.Equal(0, prompts);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtyImageEdit_WindowCloseCancel_IsGuarded()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 5d)!);
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardImageEditChanges", (Func<bool>)(() => false));
                var args = new CancelEventArgs();

                OrganizeWindowTestHost.Invoke(window, "ImageEditWindow_Closing", null, args);

                Assert.True(args.Cancel);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
                Assert.True(prepared.Workspace.IsDirty);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }
}
