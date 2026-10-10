using System.Collections;
using System.Windows.Controls;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using SkiaSharp;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditImageExtractReplaceTests
{
    [Fact]
    public void SelectedImage_ShowsReplaceAndExtractContextActions()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var canvas = OrganizeWindowTestHost.Element<Canvas>(window, "EditOverlayCanvas");
                var tags = canvas.Children
                    .OfType<Button>()
                    .Select(button => button.Tag?.ToString())
                    .Where(tag => tag is not null)
                    .ToArray();

                Assert.Contains("ImageReplaceButton", tags);
                Assert.Contains("ImageExtractButton", tags);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Replace_CancelAndInvalid_LeaveWorkspaceUnchanged()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var before = prepared.Workspace.GetState(prepared.Key);

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryReplaceSelectedImageFromPath", (object?)null)!);
                Assert.Equal(before, prepared.Workspace.GetState(prepared.Key));
                Assert.False(prepared.Workspace.CanUndo);

                var invalid = Path.Combine(fixture.DirectoryPath, "invalid.png");
                File.WriteAllBytes(invalid, new byte[] { 1, 2, 3, 4 });
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryReplaceSelectedImageFromPath", invalid)!);
                Assert.Equal(before, prepared.Workspace.GetState(prepared.Key));
                Assert.False(prepared.Workspace.CanUndo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Replace_SuccessPreservesCurrentMatrixAndCreatesUndoableLogicalReplace()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var replacement = Path.Combine(fixture.DirectoryPath, "replacement.png");
            Task5ImageTestFixture.WritePng(replacement, transparent: true);
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var before = prepared.Workspace.GetState(prepared.Key);
                var session = Assert.IsType<PdfDocumentSession>(OrganizeWindowTestHost.GetField(window, "_session"));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryReplaceSelectedImageFromPath", replacement)!);
                var after = prepared.Workspace.GetState(prepared.Key);
                Assert.Equal(before.CurrentMatrix, after.CurrentMatrix);
                Assert.NotNull(after.ReplacementAsset);
                Assert.True(prepared.Workspace.CanUndo);
                Assert.Single(session.GetImageObjects(0));

                File.Delete(replacement);
                Assert.False(File.Exists(replacement));
                Assert.NotNull(prepared.Workspace.GetState(prepared.Key).ReplacementAsset);

                Assert.True(prepared.Workspace.Undo());
                var undone = prepared.Workspace.GetState(prepared.Key);
                Assert.Equal(before.CurrentMatrix, undone.CurrentMatrix);
                Assert.Null(undone.ReplacementAsset);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Extract_Cancel_WritesNothingAndDoesNotInvokeExtraction()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var extractCalls = 0;
                var writeCalls = 0;
                OrganizeWindowTestHost.SetField(window, "_extractSelectedImagePng",
                    (Func<PdfDocumentSession, int, int, CancellationToken, ReadOnlyMemory<byte>>)((_, _, _, _) =>
                    {
                        extractCalls++;
                        return new byte[] { 1, 2, 3 };
                    }));
                OrganizeWindowTestHost.SetField(window, "_writeExtractedImage",
                    (Action<string, ReadOnlyMemory<byte>>)((_, _) => writeCalls++));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryExtractSelectedImageToPath", (object?)null)!);
                Assert.Equal(0, extractCalls);
                Assert.Equal(0, writeCalls);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Extract_Success_WritesSelectedPngThroughSeamWithoutDirtyingWorkspace()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var png = CreateTinyPng();
                string? writtenPath = null;
                byte[]? writtenBytes = null;
                var destination = Path.Combine(fixture.DirectoryPath, "extracted.png");
                OrganizeWindowTestHost.SetField(window, "_extractSelectedImagePng",
                    (Func<PdfDocumentSession, int, int, CancellationToken, ReadOnlyMemory<byte>>)((_, page, index, _) =>
                    {
                        Assert.Equal(prepared.Key.PageIndex, page);
                        Assert.Equal(prepared.Key.PageObjectIndex, index);
                        return png;
                    }));
                OrganizeWindowTestHost.SetField(window, "_writeExtractedImage",
                    (Action<string, ReadOnlyMemory<byte>>)((path, bytes) =>
                    {
                        writtenPath = path;
                        writtenBytes = bytes.ToArray();
                    }));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryExtractSelectedImageToPath", destination)!);
                Assert.Equal(destination, writtenPath);
                Assert.Equal(png, writtenBytes);
                Assert.False(prepared.Workspace.IsDirty);
                Assert.False(prepared.Workspace.CanUndo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    private static byte[] CreateTinyPng()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Bgra8888, SKAlphaType.Opaque);
        bitmap.Erase(SKColors.Orange);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
