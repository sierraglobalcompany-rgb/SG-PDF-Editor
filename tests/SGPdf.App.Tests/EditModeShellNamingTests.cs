using System.Reflection;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class EditModeShellNamingTests
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void EditModeShell_UsesGeneralNames_WithoutSecondInitializerChain()
    {
        var type = typeof(MainWindow);

        Assert.NotNull(type.GetField("EditLoadedHookRegistered", StaticFlags));
        Assert.NotNull(type.GetField("_editUiInitialized", InstanceFlags));
        Assert.NotNull(type.GetField("_editModeActive", InstanceFlags));
        Assert.NotNull(type.GetField("_editMaterializing", InstanceFlags));
        Assert.NotNull(type.GetField("_editModeButton", InstanceFlags));
        Assert.NotNull(type.GetField("_editSaveAsButton", InstanceFlags));
        Assert.NotNull(type.GetField("_editOverlayCanvas", InstanceFlags));

        Assert.NotNull(type.GetMethod("RegisterEditLoadedHook", StaticFlags));
        Assert.NotNull(type.GetMethod("EditHost_Loaded", StaticFlags));
        Assert.NotNull(type.GetMethod("InitializeEditUi", InstanceFlags));
        Assert.NotNull(type.GetMethod("WireEditGuards", InstanceFlags));
        Assert.NotNull(type.GetMethod("TryLeaveEditModeWithGuard", InstanceFlags));
        Assert.NotNull(type.GetMethod("ResetEditState", InstanceFlags));

        Assert.Null(type.GetField("ImageEditLoadedHookRegistered", StaticFlags));
        Assert.Null(type.GetField("_imageEditUiInitialized", InstanceFlags));
        Assert.Null(type.GetField("_imageEditModeActive", InstanceFlags));
        Assert.Null(type.GetMethod("RegisterImageEditLoadedHook", StaticFlags));
        Assert.Null(type.GetMethod("InitializeImageEditUi", InstanceFlags));
        Assert.Null(type.GetMethod("WireImageEditGuards", InstanceFlags));
        Assert.Null(type.GetMethod("TryLeaveImageEditModeWithGuard", InstanceFlags));
        Assert.Null(type.GetMethod("ResetImageEditState", InstanceFlags));

        Assert.Null(type.GetMethod("InitializeTextEditUi", InstanceFlags));
    }
}
