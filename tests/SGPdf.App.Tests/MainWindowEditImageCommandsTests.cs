using System.Collections;
using System.Windows.Controls;
using System.Windows.Input;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditImageCommandsTests
{
    [Fact]
    public void MoveDrag_PreviewCreatesNoHistory_ReleaseCommitsExactlyOne()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var workspace = prepared.Workspace;
                var before = workspace.GetState(prepared.Key);
                var center = ImageEditCommandTestHost.PointAt(before.CurrentMatrix, .5d, .5d);
                var device = ImageHitTester.PdfToDevice(center, prepared.Transform);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "BeginImageMoveAtDevicePoint", device.X, device.Y)!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "UpdateImageMoveAtDevicePoint", device.X + 24d, device.Y - 12d)!);

                Assert.False(workspace.CanUndo);
                Assert.Equal(before.CurrentMatrix, workspace.GetState(prepared.Key).CurrentMatrix);
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_imageEditPreviewMatrix"));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "CompleteImageMove")!);
                Assert.True(workspace.CanUndo);
                Assert.NotEqual(before.CurrentMatrix, workspace.GetState(prepared.Key).CurrentMatrix);

                Assert.True(workspace.Undo());
                Assert.False(workspace.Undo());
                Assert.Equal(before.CurrentMatrix, workspace.GetState(prepared.Key).CurrentMatrix);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void ResizeGesture_ShiftEnablesFreeAspect_DefaultPreservesAspect()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var workspace = prepared.Workspace;
                var original = workspace.GetState(prepared.Key).CurrentMatrix;
                var originalRatio = ImageEditCommandTestHost.EdgeRatio(original);
                var dragged = ImageEditCommandTestHost.PointAt(original, 1d, 1d);
                var target = new PdfPoint(
                    original.E + (original.A * 1.45d) + (original.C * .65d),
                    original.F + (original.B * 1.45d) + (original.D * .65d));
                var draggedDevice = ImageHitTester.PdfToDevice(dragged, prepared.Transform);
                var targetDevice = ImageHitTester.PdfToDevice(target, prepared.Transform);
                var topRight = ImageEditCommandTestHost.ResizeCorner("TopRight");

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "BeginImageResize", topRight, draggedDevice.X, draggedDevice.Y)!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "UpdateImageResizeAtDevicePoint", targetDevice.X, targetDevice.Y, true)!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "CompleteImageResize")!);
                var free = workspace.GetState(prepared.Key).CurrentMatrix;
                Assert.NotEqual(Math.Round(originalRatio, 5), Math.Round(ImageEditCommandTestHost.EdgeRatio(free), 5));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Z, ModifierKeys.Control, null)!);
                Assert.Equal(original, workspace.GetState(prepared.Key).CurrentMatrix);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "BeginImageResize", topRight, draggedDevice.X, draggedDevice.Y)!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "UpdateImageResizeAtDevicePoint", targetDevice.X, targetDevice.Y, false)!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "CompleteImageResize")!);
                var proportional = workspace.GetState(prepared.Key).CurrentMatrix;
                Assert.Equal(originalRatio, ImageEditCommandTestHost.EdgeRatio(proportional), 6);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void RotateDeleteUndoRedo_AreLogicalAndNewEditAfterUndoClearsRedo()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var workspace = prepared.Workspace;
                var original = workspace.GetState(prepared.Key).CurrentMatrix;
                var originalCenter = ImageEditCommandTestHost.PointAt(original, .5d, .5d);
                var session = Assert.IsType<PdfDocumentSession>(OrganizeWindowTestHost.GetField(window, "_session"));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 90d)!);
                var rotated = workspace.GetState(prepared.Key).CurrentMatrix;
                var rotatedCenter = ImageEditCommandTestHost.PointAt(rotated, .5d, .5d);
                Assert.Equal(originalCenter.X, rotatedCenter.X, 6);
                Assert.Equal(originalCenter.Y, rotatedCenter.Y, 6);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "DeleteSelectedImage")!);
                Assert.True(workspace.GetState(prepared.Key).Deleted);
                Assert.Single(session.GetImageObjects(0));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Z, ModifierKeys.Control, null)!);
                Assert.False(workspace.GetState(prepared.Key).Deleted);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Y, ModifierKeys.Control, null)!);
                Assert.True(workspace.GetState(prepared.Key).Deleted);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Z, ModifierKeys.Control, null)!);
                Assert.True(workspace.CanRedo);

                OrganizeWindowTestHost.SetField(window, "_selectedImageKey", prepared.Key);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 15d)!);
                Assert.False(workspace.CanRedo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void ImageEditShortcuts_DoNotStealKeysFromEditableControls()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var textBox = new TextBox();

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Delete, ModifierKeys.None, textBox)!);
                Assert.False(prepared.Workspace.GetState(prepared.Key).Deleted);
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Z, ModifierKeys.Control, textBox)!);
                Assert.False(prepared.Workspace.CanUndo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }
}

internal static class ImageEditCommandTestHost
{
    internal static async Task<(PdfImageObjectInfo Info, ImageObjectKey Key, ImageEditWorkspace Workspace, PdfPageDeviceTransform Transform)> PrepareAsync(
        MainWindow window,
        string path)
    {
        Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, path));
        OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));
        Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterImageEditModeAsync"));

        var info = Assert.IsType<PdfImageObjectInfo>(
            ((IEnumerable)OrganizeWindowTestHost.GetField(window, "_activeImageObjects")!).Cast<object>().Single());
        var key = new ImageObjectKey(info.PageIndex, info.PageObjectIndex);
        var workspace = Assert.IsType<ImageEditWorkspace>(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
        var transform = new PdfPageDeviceTransform(0d, 400d, 1d, 0d, 0d, -1d, 300, 400);
        OrganizeWindowTestHost.SetField(window, "_currentPdfDeviceTransform", transform);

        var center = PointAt(info.Matrix, .5d, .5d);
        var device = ImageHitTester.PdfToDevice(center, transform);
        Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SelectImageAtDevicePoint", device.X, device.Y)!);
        return (info, key, workspace, transform);
    }

    internal static object ResizeCorner(string name)
    {
        var type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Edit.Images.ImageResizeCorner",
            throwOnError: true)!;
        return Enum.Parse(type, name);
    }

    internal static PdfPoint PointAt(PdfObjectMatrix matrix, double u, double v)
        => new(
            (matrix.A * u) + (matrix.C * v) + matrix.E,
            (matrix.B * u) + (matrix.D * v) + matrix.F);

    internal static double EdgeRatio(PdfObjectMatrix matrix)
        => Math.Sqrt((matrix.A * matrix.A) + (matrix.B * matrix.B)) /
           Math.Sqrt((matrix.C * matrix.C) + (matrix.D * matrix.D));

    internal static void CloseClean(MainWindow window)
    {
        try
        {
            OrganizeWindowTestHost.Invoke(window, "ResetImageEditState");
        }
        catch
        {
        }
        OrganizeWindowTestHost.CloseClean(window);
    }
}
