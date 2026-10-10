using System.Collections;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditImagesTests
{
    [Fact]
    public void EditModeButton_IsBetweenFirmarAndOrganizar_AndDisabledWithoutPdf()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var edit = OrganizeWindowTestHost.Element<Button>(window, "EditModeButton");
                Assert.False(edit.IsEnabled);

                var sign = OrganizeWindowTestHost.Element<Button>(window, "SignModeButton");
                var organize = OrganizeWindowTestHost.Element<Button>(window, "OrganizeModeButton");
                var panel = Assert.IsAssignableFrom<Panel>(edit.Parent);
                var buttons = panel.Children.OfType<Button>().ToArray();
                Assert.True(Array.IndexOf(buttons, sign) < Array.IndexOf(buttons, edit));
                Assert.True(Array.IndexOf(buttons, edit) < Array.IndexOf(buttons, organize));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void EnterEditMode_EligiblePdf_CreatesWorkspaceAndUsesSinglePageOverlay()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));

                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterImageEditModeAsync"));
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                Assert.Single(((IEnumerable)OrganizeWindowTestHost.GetField(window, "_activeImageObjects")!).Cast<object>());
                Assert.Equal(Visibility.Collapsed, OrganizeWindowTestHost.Element<Grid>(window, "ReaderContinuousSurface").Visibility);
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<ScrollViewer>(window, "PdfScrollViewer").Visibility);
                Assert.Equal(Visibility.Visible, OrganizeWindowTestHost.Element<Canvas>(window, "ImageEditOverlayCanvas").Visibility);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SignedSource_BlocksBeforeMutableWorkspaceExists()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 1));

                Assert.False(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterImageEditModeAsync"));
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
                Assert.Contains("editar", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void PasswordOpenedSource_BlocksBeforeMutableWorkspaceExists()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizePdfFixtureFactory.CreateProtected();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path, "secret"));
                Assert.False(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterImageEditModeAsync"));
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_imageEditModeActive")!);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtySignature_CancelBlocksEditTransition()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var state = new SignatureEditState(0, new PdfRect(0d, 0d, 300d, 400d));
                state.AddCentered(new SignatureAsset(1, 1, 4, new byte[4], "test"));
                OrganizeWindowTestHost.SetField(window, "_signatureEditState", state);
                OrganizeWindowTestHost.SetField(window, "_signatureModeActive", true);
                OrganizeWindowTestHost.SetField(window, "_resolvePendingSignatureDecision",
                    (Func<SignatureGuardReason, PendingSignatureDecision>)(_ => PendingSignatureDecision.Cancel));

                Assert.False(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterImageEditModeAsync"));
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_signatureModeActive")!);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_imageEditWorkspace"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Selection_SelectsImage_NonImageAndEscapeClearIt()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));
                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterImageEditModeAsync"));

                var info = Assert.IsType<PdfImageObjectInfo>(((IEnumerable)OrganizeWindowTestHost.GetField(window, "_activeImageObjects")!).Cast<object>().Single());
                var centerX = info.Matrix.E + ((info.Matrix.A + info.Matrix.C) / 2d);
                var centerY = info.Matrix.F + ((info.Matrix.B + info.Matrix.D) / 2d);
                OrganizeWindowTestHost.SetField(window, "_currentPdfDeviceTransform",
                    new PdfPageDeviceTransform(0d, 400d, 1d, 0d, 0d, -1d, 300, 400));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SelectImageAtDevicePoint", centerX, 400d - centerY)!);
                Assert.NotNull(OrganizeWindowTestHost.GetField(window, "_selectedImageKey"));
                Assert.NotEmpty(OrganizeWindowTestHost.Element<Canvas>(window, "ImageEditOverlayCanvas").Children.Cast<UIElement>());

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "SelectImageAtDevicePoint", 1d, 1d)!);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_selectedImageKey"));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "SelectImageAtDevicePoint", centerX, 400d - centerY)!);
                OrganizeWindowTestHost.Invoke(window, "HandleImageEditEscape");
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_selectedImageKey"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }
}
