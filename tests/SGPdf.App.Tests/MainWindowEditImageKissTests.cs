using System.Reflection;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditImageKissTests
{
    [Fact]
    public void EditImageUi_UsesSingleInitializationPathWithoutSecondaryHardeningLoadedHook()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
        var type = typeof(MainWindow);

        Assert.Null(type.GetField("_imageEditHardeningUiInitialized", flags));
        Assert.Null(type.GetField("ImageEditHardeningLoadedHookRegistered", flags));
        Assert.Null(type.GetMethod("RegisterImageEditHardeningLoadedHook", flags));
        Assert.Null(type.GetMethod("ImageEditHardeningHost_Loaded", flags));
        Assert.Null(type.GetMethod("InitializeImageEditHardeningUi", flags));
    }
}
