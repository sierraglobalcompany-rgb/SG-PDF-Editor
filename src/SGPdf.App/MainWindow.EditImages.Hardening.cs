using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool ImageEditOpenMenuGuardRegistered = RegisterImageEditOpenMenuGuard();

    private Func<bool> _confirmDiscardImageEditChanges = static () =>
        MessageBox.Show(
            "Hay cambios de imagen sin guardar.\n\n¿Deseas descartarlos y continuar?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private static bool RegisterImageEditOpenMenuGuard()
    {
        EventManager.RegisterClassHandler(
            typeof(MenuItem),
            MenuItem.ClickEvent,
            new RoutedEventHandler(ImageEditOpenMenuItem_Click),
            handledEventsToo: false);
        return true;
    }

    private static void ImageEditOpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || Window.GetWindow(menuItem) is not MainWindow window)
            return;

        if (!ReferenceEquals(menuItem, window.OpenPdfMenuItem) &&
            !ReferenceEquals(menuItem, window.OpenZplMenuItem))
        {
            return;
        }

        if (window._imageEditModeActive && !window.TryLeaveImageEditModeWithGuard())
            e.Handled = true;
    }

    private void WireImageEditGuards()
    {
        _ = ImageEditOpenMenuGuardRegistered;

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
