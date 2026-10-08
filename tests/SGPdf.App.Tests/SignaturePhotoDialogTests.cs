using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignaturePhotoDialogTests
{
    [Fact]
    public void Dialog_DefaultControlsReflectAutomaticSettings()
    {
        RunInSta(() =>
        {
            var dialog = new SignaturePhotoDialog(CreateValidSource());
            Assert.Equal(65d, Assert.IsType<Slider>(dialog.FindName("SignaturePhotoBackgroundSlider")).Value);
            Assert.Equal(0d, Assert.IsType<Slider>(dialog.FindName("SignaturePhotoBrightnessSlider")).Value);
            Assert.Equal(20d, Assert.IsType<Slider>(dialog.FindName("SignaturePhotoContrastSlider")).Value);
            Assert.True(Assert.IsType<CheckBox>(dialog.FindName("SignaturePhotoAutoCropCheckBox")).IsChecked);
            Assert.Equal(0, Assert.IsType<ComboBox>(dialog.FindName("SignaturePhotoInkComboBox")).SelectedIndex);
            dialog.Close();
        });
    }

    [Fact]
    public void AutomaticAndReset_RestoreFrozenAutomaticSettings()
    {
        RunInSta(() =>
        {
            var dialog = new SignaturePhotoDialog(CreateValidSource());
            var background = Assert.IsType<Slider>(dialog.FindName("SignaturePhotoBackgroundSlider"));
            var brightness = Assert.IsType<Slider>(dialog.FindName("SignaturePhotoBrightnessSlider"));
            var contrast = Assert.IsType<Slider>(dialog.FindName("SignaturePhotoContrastSlider"));
            var ink = Assert.IsType<ComboBox>(dialog.FindName("SignaturePhotoInkComboBox"));
            var crop = Assert.IsType<CheckBox>(dialog.FindName("SignaturePhotoAutoCropCheckBox"));

            background.Value = 10;
            brightness.Value = 30;
            contrast.Value = -20;
            ink.SelectedIndex = 2;
            crop.IsChecked = false;

            Assert.IsType<Button>(dialog.FindName("SignaturePhotoResetButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(65d, background.Value);
            Assert.Equal(0d, brightness.Value);
            Assert.Equal(20d, contrast.Value);
            Assert.Equal(0, ink.SelectedIndex);
            Assert.True(crop.IsChecked);
            dialog.Close();
        });
    }

    [Fact]
    public void Apply_ValidSourceSetsPreparedAsset()
    {
        RunInSta(() =>
        {
            var dialog = new SignaturePhotoDialog(CreateValidSource());
            Assert.True(dialog.TryApplyCurrentSettings());
            Assert.NotNull(dialog.PreparedAsset);
            dialog.Close();
        });
    }

    [Fact]
    public void Apply_NoUsableSignatureKeepsPreparedAssetNull()
    {
        RunInSta(() =>
        {
            var dialog = new SignaturePhotoDialog(CreateBlankSource());
            Assert.False(dialog.TryApplyCurrentSettings());
            Assert.Null(dialog.PreparedAsset);
            Assert.Contains("firma", Assert.IsType<TextBlock>(dialog.FindName("SignaturePhotoStatusText")).Text, StringComparison.OrdinalIgnoreCase);
            dialog.Close();
        });
    }

    [Fact]
    public void Cancel_LeavesPreparedAssetNull()
    {
        RunInSta(() =>
        {
            var dialog = new SignaturePhotoDialog(CreateValidSource());
            Assert.Null(dialog.PreparedAsset);
            Assert.IsType<Button>(dialog.FindName("SignaturePhotoCancelButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Null(dialog.PreparedAsset);
            if (dialog.IsLoaded)
                dialog.Close();
        });
    }

    private static SignaturePhotoSource CreateValidSource()
    {
        const int width = 40;
        const int height = 40;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var y = 14; y < 26; y++)
        for (var x = 12; x < 28; x++)
        {
            var offset = (y * width + x) * 4;
            pixels[offset] = 0;
            pixels[offset + 1] = 0;
            pixels[offset + 2] = 0;
            pixels[offset + 3] = 255;
        }
        return new SignaturePhotoSource(width, height, width * 4, pixels, "valid.png");
    }

    private static SignaturePhotoSource CreateBlankSource()
    {
        const int width = 40;
        const int height = 40;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        return new SignaturePhotoSource(width, height, width * 4, pixels, "blank.png");
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
