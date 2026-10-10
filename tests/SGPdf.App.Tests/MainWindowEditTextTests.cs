using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditTextTests
{
    [Fact]
    public void EnterEditMode_TextPdfEnumeratesActivePageAndBuildsConservativePanel()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);

                Assert.NotNull(prepared.Workspace);
                Assert.Single(ActiveTexts(window));
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_selectedTextKey"));

                var panel = OrganizeWindowTestHost.Element<StackPanel>(window, "EditTextPropertiesPanel");
                Assert.Equal(Visibility.Collapsed, panel.Visibility);
                Assert.NotNull(OrganizeWindowTestHost.Element<TextBox>(window, "EditTextContentTextBox"));
                Assert.NotNull(OrganizeWindowTestHost.Element<TextBox>(window, "EditTextSizeTextBox"));
                Assert.NotNull(OrganizeWindowTestHost.Element<TextBox>(window, "EditTextColorTextBox"));
                Assert.NotNull(OrganizeWindowTestHost.Element<TextBlock>(window, "EditTextFontValueText"));
                Assert.NotNull(OrganizeWindowTestHost.Element<TextBlock>(window, "EditTextStrategyValueText"));
                Assert.NotNull(OrganizeWindowTestHost.Element<Button>(window, "EditTextApplyButton"));
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void MixedSelection_UsesTopmostPageObjectIndexAndKeepsSingleAuthoritativeSelection()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);
                var original = Assert.Single(ActiveTexts(window));
                var imageWorkspace = Assert.IsType<ImageEditWorkspace>(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));

                var topImage = ImageCovering(original, original.Key.PageObjectIndex + 10);
                imageWorkspace.EnsureObject(topImage);
                OrganizeWindowTestHost.SetField(window, "_activeImageObjects", new[] { topImage });

                var point = QuadCenter(original.Quad);
                var device = ImageHitTester.PdfToDevice(point, prepared.Transform);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SelectEditObjectAtDevicePoint", device.X, device.Y)!);
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_selectedImageKey"));
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_selectedTextKey"));
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<StackPanel>(window, "EditTextPropertiesPanel").Visibility);

                var topText = original with { Key = new TextObjectKey(0, topImage.PageObjectIndex + 10), Text = "TOP TEXT" };
                prepared.Workspace.EnsureObject(topText);
                OrganizeWindowTestHost.SetField(window, "_activeTextObjects", new[] { topText });
                var textPoint = QuadCenter(topText.Quad);
                var textDevice = ImageHitTester.PdfToDevice(textPoint, prepared.Transform);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SelectEditObjectAtDevicePoint", textDevice.X, textDevice.Y)!);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_selectedImageKey"));
                Assert.Equal(topText.Key, Assert.IsType<TextObjectKey>(OrganizeWindowTestHost.GetField(window, "_selectedTextKey")));
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<StackPanel>(window, "EditTextPropertiesPanel").Visibility);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void TextPanel_ShowsMetadataAndValidApplyDirtiesOnlyText_InvalidApplyPreservesState()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);
                var text = Assert.Single(ActiveTexts(window));
                SelectText(window, text, prepared.Transform);

                var content = OrganizeWindowTestHost.Element<TextBox>(window, "EditTextContentTextBox");
                var size = OrganizeWindowTestHost.Element<TextBox>(window, "EditTextSizeTextBox");
                var color = OrganizeWindowTestHost.Element<TextBox>(window, "EditTextColorTextBox");
                var font = OrganizeWindowTestHost.Element<TextBlock>(window, "EditTextFontValueText");
                var strategy = OrganizeWindowTestHost.Element<TextBlock>(window, "EditTextStrategyValueText");

                Assert.Equal(text.Text, content.Text);
                Assert.Equal(text.FontName, font.Text);
                Assert.Contains("Fuente original", strategy.Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(content.IsReadOnly);
                Assert.False(size.IsReadOnly);
                Assert.False(color.IsReadOnly);

                content.Text = "CASA 321";
                color.Text = "#112233";
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryApplySelectedTextEdit")!);

                var state = prepared.Workspace.GetState(text.Key);
                Assert.Equal("CASA 321", state.Text);
                Assert.Equal((uint)0x11, state.FillColor.Red);
                Assert.Equal((uint)0x22, state.FillColor.Green);
                Assert.Equal((uint)0x33, state.FillColor.Blue);
                Assert.True(prepared.Workspace.IsDirty);
                Assert.False(Assert.IsType<ImageEditWorkspace>(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace")).IsDirty);

                var committed = state;
                content.Text = "   ";
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryApplySelectedTextEdit")!);
                Assert.Equal(committed, prepared.Workspace.GetState(text.Key));
                Assert.Contains("vacío", OrganizeWindowTestHost.Element<TextBlock>(window, "EditTextValidationText").Text, StringComparison.OrdinalIgnoreCase);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void NonFillText_IsPresentedReadOnlyAndCannotApply()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);
                var original = Assert.Single(ActiveTexts(window));
                var readOnly = original with
                {
                    Key = new TextObjectKey(0, original.Key.PageObjectIndex + 20),
                    Text = "READ ONLY",
                    TextRenderMode = 1
                };
                prepared.Workspace.EnsureObject(readOnly);
                OrganizeWindowTestHost.SetField(window, "_activeTextObjects", new[] { readOnly });
                SelectText(window, readOnly, prepared.Transform);

                Assert.True(OrganizeWindowTestHost.Element<TextBox>(window, "EditTextContentTextBox").IsReadOnly);
                Assert.True(OrganizeWindowTestHost.Element<TextBox>(window, "EditTextSizeTextBox").IsReadOnly);
                Assert.True(OrganizeWindowTestHost.Element<TextBox>(window, "EditTextColorTextBox").IsReadOnly);
                Assert.False(OrganizeWindowTestHost.Element<Button>(window, "EditTextApplyButton").IsEnabled);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void CtrlZInsideTextEditorBelongsToTextBox_NotImageUndo()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = PdfEditWriterTextFixtureFactory.CreateImageThenText();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));
                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterEditModeAsync"));

                var imageWorkspace = Assert.IsType<ImageEditWorkspace>(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                var image = Assert.Single(((IEnumerable)OrganizeWindowTestHost.GetField(window, "_activeImageObjects")!).Cast<PdfImageObjectInfo>());
                OrganizeWindowTestHost.SetField(window, "_selectedImageKey", new ImageObjectKey(image.PageIndex, image.PageObjectIndex));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 5d)!);
                Assert.True(imageWorkspace.CanUndo);

                var editor = OrganizeWindowTestHost.Element<TextBox>(window, "EditTextContentTextBox");
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryHandleImageEditShortcut", Key.Z, ModifierKeys.Control, editor)!);
                Assert.True(imageWorkspace.CanUndo);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtyTextParticipatesInSharedLeaveGuard()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);
                var text = Assert.Single(ActiveTexts(window));
                SelectText(window, text, prepared.Transform);
                OrganizeWindowTestHost.Element<TextBox>(window, "EditTextContentTextBox").Text = "CASA 321";
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryApplySelectedTextEdit")!);
                Assert.True(prepared.Workspace.IsDirty);
                Assert.False(Assert.IsType<ImageEditWorkspace>(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace")).IsDirty);

                var prompts = 0;
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardImageEditChanges", (Func<bool>)(() =>
                {
                    prompts++;
                    return false;
                }));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryLeaveEditModeWithGuard")!);
                Assert.Equal(1, prompts);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_editModeActive")!);
                Assert.True(prepared.Workspace.IsDirty);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("block")]
    [InlineData("decline")]
    public void TextSaveAs_NonPublishingPathsInvokeCombinedWriterZeroTimesAndKeepDirty(string path)
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);
                var text = Assert.Single(ActiveTexts(window));
                SelectText(window, text, prepared.Transform);
                OrganizeWindowTestHost.Element<TextBox>(window, "EditTextContentTextBox").Text = "CASA 321";
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryApplySelectedTextEdit")!);

                var writerCalls = 0;
                OrganizeWindowTestHost.SetField(
                    window,
                    "_saveCombinedEditCopy",
                    (Action<ImageEditWorkspace, TextEditWorkspace, string, bool, CancellationToken>)
                    ((_, _, _, _, _) => writerCalls++));

                if (path == "cancel")
                {
                    OrganizeWindowTestHost.SetField(window, "_selectEditPdfDestination", (Func<string?>)(() => null));
                }
                else
                {
                    OrganizeWindowTestHost.SetField(
                        window,
                        "_selectEditPdfDestination",
                        (Func<string?>)(() => Path.Combine(fixture.DirectoryPath, "saved.pdf")));
                    var findings = path == "block"
                        ? new[]
                        {
                            new PdfEditFinding(
                                PdfEditFindingKind.CryptographicSignature,
                                PdfEditFindingSeverity.Block,
                                "block",
                                PdfEditPreservationStatus.Unknown)
                        }
                        : new[]
                        {
                            new PdfEditFinding(
                                PdfEditFindingKind.Metadata,
                                PdfEditFindingSeverity.Warning,
                                "warning",
                                PdfEditPreservationStatus.Unknown)
                        };
                    OrganizeWindowTestHost.SetField(
                        window,
                        "_inspectEditPreflight",
                        (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) => new PdfEditPreflightResult(findings)));
                    if (path == "decline")
                        OrganizeWindowTestHost.SetField(window, "_confirmEditWarnings", (Func<PdfEditPreflightResult, bool>)(_ => false));
                }

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);
                Assert.Equal(0, writerCalls);
                Assert.True(prepared.Workspace.IsDirty);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    private static async Task<(TextEditWorkspace Workspace, PdfPageDeviceTransform Transform)> PrepareTextAsync(
        MainWindow window,
        string path)
    {
        Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, path));
        OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));
        Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterEditModeAsync"));
        var workspace = Assert.IsType<TextEditWorkspace>(OrganizeWindowTestHost.GetField(window, "_textEditWorkspace"));
        var transform = new PdfPageDeviceTransform(0d, 400d, 1d, 0d, 0d, -1d, 300, 400);
        OrganizeWindowTestHost.SetField(window, "_currentPdfDeviceTransform", transform);
        return (workspace, transform);
    }

    private static IReadOnlyList<PdfTextObjectInfo> ActiveTexts(MainWindow window)
        => ((IEnumerable)OrganizeWindowTestHost.GetField(window, "_activeTextObjects")!)
            .Cast<PdfTextObjectInfo>()
            .ToArray();

    private static void SelectText(MainWindow window, PdfTextObjectInfo text, PdfPageDeviceTransform transform)
    {
        var device = ImageHitTester.PdfToDevice(QuadCenter(text.Quad), transform);
        Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SelectEditObjectAtDevicePoint", device.X, device.Y)!);
        Assert.Equal(text.Key, Assert.IsType<TextObjectKey>(OrganizeWindowTestHost.GetField(window, "_selectedTextKey")));
    }

    private static PdfPoint QuadCenter(PdfTextObjectQuad quad)
        => new(
            (quad.X1 + quad.X2 + quad.X3 + quad.X4) / 4d,
            (quad.Y1 + quad.Y2 + quad.Y3 + quad.Y4) / 4d);

    private static PdfImageObjectInfo ImageCovering(PdfTextObjectInfo text, int objectIndex)
    {
        var xs = new[] { text.Quad.X1, text.Quad.X2, text.Quad.X3, text.Quad.X4 };
        var ys = new[] { text.Quad.Y1, text.Quad.Y2, text.Quad.Y3, text.Quad.Y4 };
        var left = xs.Min();
        var right = xs.Max();
        var bottom = ys.Min();
        var top = ys.Max();
        var matrix = new PdfObjectMatrix(right - left, 0d, 0d, top - bottom, left, bottom);
        return new PdfImageObjectInfo(
            text.Key.PageIndex,
            objectIndex,
            matrix,
            new PdfObjectBounds(left, bottom, right, top),
            new PdfImageObjectMetadata(20, 10, 32, 0));
    }
}
