using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool ImageEditHardeningLoadedHookRegistered = RegisterImageEditHardeningLoadedHook();

    private bool _imageEditHardeningUiInitialized;
    private Func<bool> _confirmDiscardImageEditChanges = static () =>
        MessageBox.Show(
            "Hay cambios de imagen sin guardar.\n\n¿Deseas descartarlos y continuar?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private static bool RegisterImageEditHardeningLoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ImageEditHardeningHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ImageEditHardeningHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeImageEditHardeningUi();
    }

    private void InitializeImageEditHardeningUi()
    {
        if (_imageEditHardeningUiInitialized)
            return;

        _imageEditHardeningUiInitialized = true;
        _ = ImageEditHardeningLoadedHookRegistered;
        InitializeImageEditUi();

        if (_readModeButton is not null)
            _readModeButton.PreviewMouseLeftButtonDown += GuardedImageEditModeButton_PreviewMouseLeftButtonDown;
        if (_signModeButton is not null)
            _signModeButton.PreviewMouseLeftButtonDown += GuardedImageEditModeButton_PreviewMouseLeftButtonDown;
        if (_organizeModeButton is not null)
            _organizeModeButton.PreviewMouseLeftButtonDown += GuardedImageEditModeButton_PreviewMouseLeftButtonDown;

        Closing += ImageEditWindow_Closing;
    }

    private void GuardedImageEditModeButton_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_imageEditModeActive && !TryLeaveImageEditModeWithGuard())
            e.Handled = true;
    }

    private bool TryLeaveImageEditModeWithGuard()
    {
        if (!_imageEditModeActive)
            return true;

        if (_imageEditWorkspace?.IsDirty == true && !_confirmDiscardImageEditChanges())
            return false;

        ResetImageEditState();
        return true;
    }

    private void ImageEditWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!TryLeaveImageEditModeWithGuard())
            e.Cancel = true;
    }
}
