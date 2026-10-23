using System.Reflection;
using System.Windows.Controls;
using SGPdf.App.Features.Utilities;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowCropUnlockUtilityTests
{
    [Fact]
    public void Herramientas_CropAndUnlockCommands_ArePresentAndFollowActiveIdleState()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                OrganizeWindowTestHost.EnsureLoaded(window);
                var unlock = OrganizeWindowTestHost.Element<MenuItem>(window, "UnlockPdfMenuItem");
                var crop = OrganizeWindowTestHost.Element<MenuItem>(window, "CropPdfMenuItem");
                var imageWatermark = OrganizeWindowTestHost.Element<MenuItem>(window, "ImageWatermarkMenuItem");
                Assert.Equal("Desbloquear PDF con contraseña...", unlock.Header);
                Assert.Equal("Recortar área visible...", crop.Header);
                Assert.False(unlock.IsEnabled);
                Assert.False(crop.IsEnabled);
                Assert.False(imageWatermark.IsEnabled);

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True(unlock.IsEnabled);
                Assert.True(crop.IsEnabled);
                Assert.True(imageWatermark.IsEnabled);

                OrganizeWindowTestHost.SetField(window, "_utilityExecuting", true);
                InvokeAvailabilityUpdate(window);
                Assert.False(unlock.IsEnabled);
                Assert.False(crop.IsEnabled);
                Assert.False(imageWatermark.IsEnabled);

                OrganizeWindowTestHost.SetField(window, "_utilityExecuting", false);
                InvokeAvailabilityUpdate(window);
                Assert.True(unlock.IsEnabled);
                Assert.True(crop.IsEnabled);
                Assert.True(imageWatermark.IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void UtilityDialogs_ShowRequiredInputsAndReversibleCropWarning()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var unlock = new PdfUnlockDialog();
            var crop = new PdfCropDialog(612d, 792d);
            try
            {
                Assert.IsType<PasswordBox>(unlock.FindName("CurrentPasswordBox"));
                Assert.Equal(
                    "Confirmo que estoy autorizado para crear una copia sin contraseña.",
                    Assert.IsType<CheckBox>(unlock.FindName("AuthorizationCheckBox")).Content);
                Assert.Contains(
                    "el contenido fuera del área puede recuperarse. No sirve para censurar información.",
                    Assert.IsType<TextBlock>(crop.FindName("CropWarningText")).Text,
                    StringComparison.OrdinalIgnoreCase);
                Assert.IsType<TextBox>(crop.FindName("LeftTextBox"));
                Assert.IsType<TextBox>(crop.FindName("RightTextBox"));
                Assert.IsType<TextBox>(crop.FindName("TopTextBox"));
                Assert.IsType<TextBox>(crop.FindName("BottomTextBox"));
                Assert.Contains("mm", Assert.IsType<TextBlock>(crop.FindName("OutputSizeText")).Text);
            }
            finally
            {
                crop.Close();
                unlock.Close();
            }
        });
    }

    private static void InvokeAvailabilityUpdate(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod(
            "UpdateUtilityCommandAvailability",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, null);
    }
}
