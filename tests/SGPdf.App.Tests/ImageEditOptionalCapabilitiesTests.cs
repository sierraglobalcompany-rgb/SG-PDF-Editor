using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Input;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using SkiaSharp;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditOptionalCapabilitiesTests
{
    [Fact]
    public void OpacityCommand_ZeroToHundred_IsUndoableAndFullOpacityReturnsToOriginalState()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var workspace = prepared.Workspace;
                Assert.Null(workspace.GetState(prepared.Key).Opacity);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SetSelectedImageOpacityPercent", 50)!);
                Assert.Equal((byte)128, workspace.GetState(prepared.Key).Opacity);
                Assert.True(workspace.CanUndo);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Z, ModifierKeys.Control, null)!);
                Assert.Null(workspace.GetState(prepared.Key).Opacity);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Y, ModifierKeys.Control, null)!);
                Assert.Equal((byte)128, workspace.GetState(prepared.Key).Opacity);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SetSelectedImageOpacityPercent", 0)!);
                Assert.Equal((byte)0, workspace.GetState(prepared.Key).Opacity);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SetSelectedImageOpacityPercent", 100)!);
                Assert.Null(workspace.GetState(prepared.Key).Opacity);
                Assert.False(workspace.IsDirty);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void OptionalCapabilityToolbar_ExposesOpacityZeroToHundredAndFourOrderActions()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var canvas = Assert.IsType<Canvas>(OrganizeWindowTestHost.GetField(window, "_editOverlayCanvas"));

                var opacity = canvas.Children
                    .OfType<Button>()
                    .Single(button => Equals(button.Tag, "ImageOpacityButton"));
                Assert.NotNull(opacity.ContextMenu);
                var slider = Assert.IsType<Slider>(Assert.Single(opacity.ContextMenu!.Items.Cast<object>()));
                Assert.Equal("ImageOpacitySlider", slider.Tag);
                Assert.Equal(0d, slider.Minimum);
                Assert.Equal(100d, slider.Maximum);
                Assert.Equal(100d, slider.Value);

                var order = canvas.Children
                    .OfType<Button>()
                    .Single(button => Equals(button.Tag, "ImageOrderButton"));
                Assert.NotNull(order.ContextMenu);
                var tags = order.ContextMenu!.Items
                    .OfType<MenuItem>()
                    .Select(item => item.Tag?.ToString())
                    .ToArray();
                Assert.Equal(new[]
                {
                    "ImageOrderBack",
                    "ImageOrderBackward",
                    "ImageOrderForward",
                    "ImageOrderFront"
                }, tags);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Theory]
    [InlineData(100)]
    [InlineData(50)]
    [InlineData(0)]
    public void OpacityWriter_SaveReopenRender_MatchesPinnedRuntimeEvidence(int percent)
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateImageWithVectorNeighbor();
        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var state = workspace.EnsureObject(image);
        var alpha = percent == 100
            ? (byte?)null
            : checked((byte)Math.Round(percent * 255d / 100d, MidpointRounding.AwayFromZero));
        if (state.Opacity != alpha)
            workspace.Commit(ImageEditOperationKind.SetOpacity, state with { Opacity = alpha });

        var output = Path.Combine(fixture.DirectoryPath, $"opacity-task7-{percent}.pdf");
        Save(CreateWriter(), workspace, output);

        using var saved = PdfDocumentSession.Open(output);
        var pixel = PixelAt(saved.RenderPage(0, 72d), 100, 95);
        Assert.Equal((byte)255, pixel.A);
        if (percent == 100)
        {
            Assert.Equal(new Rgba(100, 149, 237, 255), pixel);
        }
        else if (percent == 0)
        {
            Assert.Equal(new Rgba(255, 255, 255, 255), pixel);
        }
        else
        {
            Assert.InRange(pixel.R, (byte)175, (byte)180);
            Assert.InRange(pixel.G, (byte)198, (byte)204);
            Assert.InRange(pixel.B, (byte)242, (byte)248);
        }
    }

    [Fact]
    public void ZOrderCommands_ForwardBackwardFrontBack_AreLogicalAndUndoable()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateOverlappingImagesAndVector();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareOverlapAsync(window, fixture.Path);
                var workspace = prepared.Workspace;
                var first = prepared.Keys[0];
                var second = prepared.Keys[1];

                OrganizeWindowTestHost.SetField(window, "_selectedImageKey", first);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "MoveSelectedImageForward")!);
                Assert.Equal(1, workspace.GetState(first).TargetObjectIndex);
                Assert.True(workspace.Undo());
                Assert.Null(workspace.GetState(first).TargetObjectIndex);
                Assert.True(workspace.Redo());
                Assert.Equal(1, workspace.GetState(first).TargetObjectIndex);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "BringSelectedImageToFront")!);
                Assert.Equal(2, workspace.GetState(first).TargetObjectIndex);
                Assert.True(workspace.Undo());
                Assert.Equal(1, workspace.GetState(first).TargetObjectIndex);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SendSelectedImageToBack")!);
                Assert.Null(workspace.GetState(first).TargetObjectIndex);

                OrganizeWindowTestHost.SetField(window, "_selectedImageKey", second);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "MoveSelectedImageBackward")!);
                Assert.Equal(1, workspace.GetState(second).TargetObjectIndex);
                Assert.True(workspace.Undo());
                Assert.Null(workspace.GetState(second).TargetObjectIndex);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Theory]
    [InlineData("MoveSelectedImageForward", 0, "red@1,blue@2", "blue")]
    [InlineData("BringSelectedImageToFront", 0, "blue@1,red@2", "red")]
    [InlineData("MoveSelectedImageBackward", 1, "red@0,blue@1", "green")]
    [InlineData("SendSelectedImageToBack", 1, "blue@0,red@1", "green")]
    public void ZOrderCommands_SaveReopenRender_ReorderAcrossImageAndVectorWithoutLoss(
        string command,
        int selectedImageOrdinal,
        string expectedImageOrder,
        string expectedTopColor)
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateOverlappingImagesAndVector();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareOverlapAsync(window, fixture.Path);
                OrganizeWindowTestHost.SetField(window, "_selectedImageKey", prepared.Keys[selectedImageOrdinal]);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, command)!);

                var output = Path.Combine(fixture.DirectoryPath, $"task7-{command}.pdf");
                Save(CreateWriter(), prepared.Workspace, output);

                var types = ImageEditNativeCharacterizationHarness.Inspect(
                    output,
                    context => context.GetObjects().Select(item => item.Type).ToArray());
                Assert.Equal(3, types.Length);
                Assert.Equal(2, types.Count(type => type == 3));
                Assert.Equal(1, types.Count(type => type != 3));

                using var saved = PdfDocumentSession.Open(output);
                var actualOrder = string.Join(",", saved.GetImageObjects(0)
                    .OrderBy(image => image.PageObjectIndex)
                    .Select(image => $"{DominantColor(saved.GetImagePng(0, image.PageObjectIndex))}@{image.PageObjectIndex}"));
                Assert.Equal(expectedImageOrder, actualOrder);

                var top = PixelAt(saved.RenderPage(0, 72d), 160, 180);
                Assert.Equal(expectedTopColor, DominantColor(top));
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    private static async Task<(ImageEditWorkspace Workspace, ImageObjectKey[] Keys)> PrepareOverlapAsync(
        MainWindow window,
        string path)
    {
        Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, path));
        OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));
        Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterEditModeAsync"));

        var infos = ((IEnumerable)OrganizeWindowTestHost.GetField(window, "_activeImageObjects")!)
            .Cast<PdfImageObjectInfo>()
            .OrderBy(info => info.PageObjectIndex)
            .ToArray();
        Assert.Equal(2, infos.Length);
        var workspace = Assert.IsType<ImageEditWorkspace>(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
        return (workspace, infos.Select(info => new ImageObjectKey(info.PageIndex, info.PageObjectIndex)).ToArray());
    }

    private static object CreateWriter()
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Pdf.PdfEditWriter",
            throwOnError: true)!;
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(info => info.GetParameters().Length == 4);
        return constructor.Invoke(new object?[] { null, null, null, null });
    }

    private static void Save(object writer, ImageEditWorkspace workspace, string destinationPath)
    {
        var method = writer.GetType().GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(ImageEditWorkspace),
                typeof(string),
                typeof(bool),
                typeof(CancellationToken)
            },
            modifiers: null);
        Assert.NotNull(method);
        try
        {
            method.Invoke(writer, new object?[] { workspace, destinationPath, false, CancellationToken.None });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static Rgba PixelAt(PdfRenderedPage page, int x, int y)
    {
        var offset = checked((y * page.Stride) + (x * 4));
        return new Rgba(
            page.Pixels[offset + 2],
            page.Pixels[offset + 1],
            page.Pixels[offset],
            page.Pixels[offset + 3]);
    }

    private static string DominantColor(byte[] png)
    {
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);
        var pixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        return DominantColor(new Rgba(pixel.Red, pixel.Green, pixel.Blue, pixel.Alpha));
    }

    private static string DominantColor(Rgba pixel)
    {
        if (pixel.R > pixel.G + 40 && pixel.R > pixel.B + 40)
            return "red";
        if (pixel.B > pixel.R + 40 && pixel.B > pixel.G + 40)
            return "blue";
        if (pixel.G > pixel.R + 40 && pixel.G > pixel.B + 40)
            return "green";
        return "other";
    }

    private readonly record struct Rgba(byte R, byte G, byte B, byte A);
}
