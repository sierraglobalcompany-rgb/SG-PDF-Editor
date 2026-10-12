using System.Collections;
using System.Windows.Controls;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowRedactionPresentationTests
{
    [Fact]
    public void TextSelection_ExposesRedactionEligibilityAndConservativeUnsafeReason()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectEditPreflight",
                    (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)
                    ((_, _) => new PdfEditPreflightResult(Array.Empty<PdfEditFinding>())));

                var safe = Assert.Single(ActiveTexts(window));
                SelectText(window, safe, prepared.Transform);

                var state = OrganizeWindowTestHost.Element<TextBlock>(window, "EditTextRedactionStateText");
                Assert.Contains("Redacción segura disponible", state.Text, StringComparison.OrdinalIgnoreCase);

                var unsafeText = safe with
                {
                    Key = new TextObjectKey(0, safe.Key.PageObjectIndex + 20),
                    Text = "UNSAFE",
                    TextRenderMode = 1
                };
                prepared.Workspace.EnsureObject(unsafeText);
                OrganizeWindowTestHost.SetField(window, "_activeTextObjects", new[] { unsafeText });
                SelectText(window, unsafeText, prepared.Transform);

                Assert.Contains("Redacción segura no disponible", state.Text, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("texto visible seguro", state.Text, StringComparison.OrdinalIgnoreCase);
            }
            finally { ImageEditCommandTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void RedactCommand_StagesDraftWithoutMutatingSource()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
            var sourceBefore = File.ReadAllBytes(fixture.Path);
            var window = new MainWindow();
            try
            {
                var prepared = await PrepareTextAsync(window, fixture.Path);
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectEditPreflight",
                    (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)
                    ((_, _) => new PdfEditPreflightResult(Array.Empty<PdfEditFinding>())));

                var text = Assert.Single(ActiveTexts(window));
                SelectText(window, text, prepared.Transform);

                var redact = OrganizeWindowTestHost.Element<Button>(window, "EditTextRedactButton");
                Assert.True(redact.IsEnabled);

                var workspace = Assert.IsType<RedactionWorkspace>(
                    OrganizeWindowTestHost.GetField(window, "_redactionWorkspace"));
                Assert.False(workspace.IsDirty);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryStageSelectedTextRedaction")!);

                Assert.True(workspace.IsDirty);
                var target = Assert.Single(workspace.Targets);
                Assert.Equal(text.Key, target.Original.Key);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "HasUnsavedEditChanges")!);
                Assert.Equal(sourceBefore, File.ReadAllBytes(fixture.Path));
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
}
